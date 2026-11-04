using AgileActors.ApiAggregation.Api.Dashboard;
using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;

namespace AgileActors.ApiAggregation.Tests;

public sealed class DashboardProviderFailureTests
{
    [Theory]
    [InlineData("weather")]
    [InlineData("movies")]
    [InlineData("books")]
    public async Task GetSectionAsync_WhenResponseReadFails_ReturnsUnavailableSection(string category)
    {
        using var cache = new DashboardCache();
        using var coordinator = new DashboardFetchCoordinator();

        var cacheService = new DashboardCacheService(cache, coordinator, TimeProvider.System);
        var source = new FailingProviderService();

        IDashboardProvider provider = category switch
        {
            "weather" => new WeatherDashboardProvider(source, cacheService),
            "movies" => new MoviesDashboardProvider(source, cacheService),
            "books" => new BooksDashboardProvider(source, cacheService),
            _ => throw new ArgumentException("Unexpected test category.")
        };

        var request = new DashboardRequest
        {
            Latitude = 37.9838,
            Longitude = 23.7275,
            BookQuery = "The Hobbit",
            Categories = new[] { category }
        };

        var section = await provider.GetSectionAsync(request);

        Assert.Equal(category, section.Category);
        Assert.Equal(DashboardSectionStatus.Unavailable, section.Status);
        Assert.Null(section.Data);
        Assert.NotNull(section.Error);
        Assert.Equal("provider_unavailable", section.Error.Code);
    }

    private sealed class FailingProviderService : IWeatherService, IMovieService, IBookService
    {
        public Task<WeatherResponse> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return Task.FromException<WeatherResponse>(CreateFailure());
        }

        public Task<IReadOnlyList<MovieResponse>> GetPopularMoviesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromException<IReadOnlyList<MovieResponse>>(CreateFailure());
        }

        public Task<IReadOnlyList<BookResponse>> SearchBooksAsync(string query, CancellationToken cancellationToken = default)
        {
            return Task.FromException<IReadOnlyList<BookResponse>>(CreateFailure());
        }

        private static HttpIOException CreateFailure()
        {
            return new HttpIOException(HttpRequestError.ResponseEnded, "Simulated interrupted response.");
        }
    }
}