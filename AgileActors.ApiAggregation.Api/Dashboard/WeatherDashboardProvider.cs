using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;
using System.Text.Json;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public sealed class WeatherDashboardProvider : IDashboardProvider
{
    private readonly IWeatherService _weatherService;

    public WeatherDashboardProvider(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    public async Task<DashboardSection> GetSectionAsync(DashboardRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var weather = await _weatherService.GetWeatherAsync(request.Latitude!.Value,request.Longitude!.Value,cancellationToken);

            var greekTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");

            return new DashboardSection
            {
                Category = "weather",
                Provider = "open-meteo",
                Status = DashboardSectionStatus.Fresh,
                FetchedAt = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, greekTimeZone),
                Data = weather
            };
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested && exception is HttpRequestException or InvalidDataException or JsonException or OperationCanceledException)
        {
            var isTimeout = exception is OperationCanceledException;

            return new DashboardSection
            {
                Category = "weather",
                Provider = "open-meteo",
                Status = DashboardSectionStatus.Unavailable,
                Error = new DashboardSectionError
                {
                    Code = isTimeout ? "provider_timeout" : "provider_unavailable",
                    Message = isTimeout
                        ? "The weather provider did not respond in time."
                        : "Weather data could not be retrieved."
                }
            };
        }
    }
}