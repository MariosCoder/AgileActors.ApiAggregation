using System.Globalization;
using System.Net.Http.Json;
using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;

namespace AgileActors.ApiAggregation.Api.Providers.OpenMeteo;

public sealed class OpenMeteoWeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;

    public OpenMeteoWeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WeatherResponse> GetWeatherAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var url = string.Create(CultureInfo.InvariantCulture, $"v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,weather_code,wind_speed_10m&temperature_unit=celsius&wind_speed_unit=kmh");

        var response = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(url, cancellationToken);
        var current = response?.Current;

        if (current?.TemperatureCelsius is not double temperature || current.WindSpeedKmh is not double windSpeed || current.WeatherCode is not int weatherCode)
        {
            throw new InvalidDataException("Open-Meteo returned incomplete weather data.");
        }

        return new WeatherResponse
        {
            TemperatureCelsius = temperature,
            WindSpeedKmh = windSpeed,
            WeatherCode = weatherCode
        };
    }
}