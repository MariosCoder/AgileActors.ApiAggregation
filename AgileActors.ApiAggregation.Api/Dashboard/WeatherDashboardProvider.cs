using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;
using System.Text.Json;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public sealed class WeatherDashboardProvider : IDashboardProvider
{
    private static readonly DashboardCachePolicy CachePolicy = new(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30));

    private readonly IWeatherService _weatherService;
    private readonly DashboardCacheService _cacheService;

    public WeatherDashboardProvider(IWeatherService weatherService, DashboardCacheService cacheService)
    {
        _weatherService = weatherService;
        _cacheService = cacheService;
    }

    public Task<DashboardSection> GetSectionAsync(DashboardRequest request, CancellationToken cancellationToken = default)
    {
        var key = DashboardCacheKeys.Weather(request.Latitude!.Value, request.Longitude!.Value);

        return _cacheService.GetSectionAsync("weather", "open-meteo", key, CachePolicy, token => FetchSectionAsync(request, token), cancellationToken);
    }

    private async Task<DashboardSection> FetchSectionAsync(DashboardRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var weather = await _weatherService.GetWeatherAsync(request.Latitude!.Value, request.Longitude!.Value, cancellationToken);

            return DashboardSectionFactory.CreateFresh("weather", "open-meteo", weather);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            exception is HttpRequestException or InvalidDataException or JsonException or OperationCanceledException)
        {
            var isTimeout = exception is OperationCanceledException;
            var code = isTimeout ? "provider_timeout" : "provider_unavailable";
            var message = isTimeout ? "The weather provider did not respond in time." : "Weather data could not be retrieved.";

            return DashboardSectionFactory.CreateUnavailable("weather", "open-meteo", code, message);
        }
    }
}