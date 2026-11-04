using AgileActors.ApiAggregation.Api.Dashboard;
using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Tests;

public sealed class DashboardCacheServiceTests
{
    [Fact]
    public async Task GetSectionAsync_WhenResultIsFresh_ReusesCachedData()
    {
        using var cache = new DashboardCache();
        using var coordinator = new DashboardFetchCoordinator();

        var service = new DashboardCacheService(cache, coordinator, TimeProvider.System);
        var policy = new DashboardCachePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30));
        var weather = new WeatherResponse { TemperatureCelsius = 21.5, WindSpeedKmh = 12.3, WeatherCode = 0 };
        var fetchCount = 0;

        Task<DashboardSection> FetchSection(CancellationToken cancellationToken)
        {
            fetchCount++;

            return Task.FromResult(DashboardSectionFactory.CreateFresh("weather", "open-meteo", weather));
        }

        var first = await service.GetSectionAsync("weather", "open-meteo", "weather-test", policy, FetchSection, CancellationToken.None);
        var second = await service.GetSectionAsync("weather", "open-meteo", "weather-test", policy, FetchSection, CancellationToken.None);

        Assert.Equal(DashboardSectionStatus.Fresh, first.Status);
        Assert.Equal(DashboardSectionStatus.Cached, second.Status);
        Assert.Equal(1, fetchCount);
        Assert.Same(weather, second.Data);
        Assert.Equal(first.FetchedAt, second.FetchedAt);
        Assert.Null(second.Error);
    }

    [Fact]
    public async Task GetSectionAsync_WhenRefreshTimesOut_ReturnsStaleData()
    {
        using var cache = new DashboardCache();
        using var coordinator = new DashboardFetchCoordinator();

        var service = new DashboardCacheService(cache, coordinator, TimeProvider.System);
        var policy = new DashboardCachePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30));
        var weather = new WeatherResponse { TemperatureCelsius = 21.5, WindSpeedKmh = 12.3, WeatherCode = 0 };
        var fetchedAt = DateTimeOffset.UtcNow.AddMinutes(-6);
        var fetchCount = 0;

        cache.Set("weather-test", new DashboardCacheEntry
        {
            Data = weather,
            FetchedAt = fetchedAt
        }, policy);

        Task<DashboardSection> FetchSection(CancellationToken cancellationToken)
        {
            fetchCount++;

            return Task.FromResult(DashboardSectionFactory.CreateUnavailable("weather", "open-meteo", "provider_timeout", "The weather provider did not respond in time."));
        }

        var result = await service.GetSectionAsync("weather", "open-meteo", "weather-test", policy, FetchSection, CancellationToken.None);

        Assert.Equal(1, fetchCount);
        Assert.Equal(DashboardSectionStatus.Stale, result.Status);
        Assert.Same(weather, result.Data);
        Assert.Equal(fetchedAt, result.FetchedAt);
        Assert.NotNull(result.Error);
        Assert.Equal("provider_timeout", result.Error.Code);
    }

    [Fact]
    public async Task GetSectionAsync_WhenQueryIsRejected_DoesNotReturnStaleData()
    {
        using var cache = new DashboardCache();
        using var coordinator = new DashboardFetchCoordinator();

        var service = new DashboardCacheService(cache, coordinator, TimeProvider.System);
        var policy = new DashboardCachePolicy(TimeSpan.FromMinutes(15), TimeSpan.FromHours(2));
        var books = Array.Empty<BookResponse>();
        var fetchCount = 0;

        cache.Set("books-test", new DashboardCacheEntry
        {
            Data = books,
            FetchedAt = DateTimeOffset.UtcNow.AddMinutes(-16)
        }, policy);

        Task<DashboardSection> FetchSection(CancellationToken cancellationToken)
        {
            fetchCount++;

            return Task.FromResult(DashboardSectionFactory.CreateUnavailable("books", "open-library", "query_rejected", "The book provider could not process this query."));
        }

        var result = await service.GetSectionAsync("books", "open-library", "books-test", policy, FetchSection, CancellationToken.None);

        Assert.Equal(1, fetchCount);
        Assert.Equal(DashboardSectionStatus.Unavailable, result.Status);
        Assert.Null(result.Data);
        Assert.Null(result.FetchedAt);
        Assert.NotNull(result.Error);
        Assert.Equal("query_rejected", result.Error.Code);
    }

    [Fact]
    public async Task GetSectionAsync_WhenAlreadyCancelled_DoesNotFetchData()
    {
        using var cache = new DashboardCache();
        using var coordinator = new DashboardFetchCoordinator();
        using var cancellationSource = new CancellationTokenSource();

        var service = new DashboardCacheService(cache, coordinator, TimeProvider.System);
        var policy = new DashboardCachePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30));
        var fetchCount = 0;

        Task<DashboardSection> FetchSection(CancellationToken cancellationToken)
        {
            fetchCount++;

            return Task.FromResult(DashboardSectionFactory.CreateUnavailable("weather", "open-meteo", "provider_timeout", "The weather provider did not respond in time."));
        }

        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetSectionAsync("weather", "open-meteo", "weather-test", policy, FetchSection, cancellationSource.Token));

        Assert.Equal(0, fetchCount);
    }

    [Fact]
    public async Task GetSectionAsync_WhenRequestsOverlap_FetchesOnlyOnce()
    {
        using var cache = new DashboardCache();
        using var coordinator = new DashboardFetchCoordinator();

        var service = new DashboardCacheService(cache, coordinator, TimeProvider.System);
        var policy = new DashboardCachePolicy(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30));
        var weather = new WeatherResponse { TemperatureCelsius = 21.5, WindSpeedKmh = 12.3, WeatherCode = 0 };
        var releaseFetch = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fetchCount = 0;

        async Task<DashboardSection> FetchSection(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref fetchCount);

            await releaseFetch.Task.WaitAsync(cancellationToken);

            return DashboardSectionFactory.CreateFresh("weather", "open-meteo", weather);
        }

        var firstTask = service.GetSectionAsync("weather", "open-meteo", "weather-test", policy, FetchSection, CancellationToken.None);
        var secondTask = service.GetSectionAsync("weather", "open-meteo", "weather-test", policy, FetchSection, CancellationToken.None);

        releaseFetch.SetResult(true);

        var results = await Task.WhenAll(firstTask, secondTask).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(1, fetchCount);
        Assert.Equal(DashboardSectionStatus.Fresh, results[0].Status);
        Assert.Equal(DashboardSectionStatus.Cached, results[1].Status);
        Assert.Same(weather, results[1].Data);
        Assert.Equal(results[0].FetchedAt, results[1].FetchedAt);
    }
}