using System.Text.Json.Serialization;

namespace AgileActors.ApiAggregation.Api.Providers.OpenMeteo;

public sealed class OpenMeteoResponse
{
    [JsonPropertyName("current")]
    public OpenMeteoCurrentWeather? Current { get; init; }
}

public sealed class OpenMeteoCurrentWeather
{
    [JsonPropertyName("temperature_2m")]
    public double? TemperatureCelsius { get; init; }

    [JsonPropertyName("wind_speed_10m")]
    public double? WindSpeedKmh { get; init; }

    [JsonPropertyName("weather_code")]
    public int? WeatherCode { get; init; }
}