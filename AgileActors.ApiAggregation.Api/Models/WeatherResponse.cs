namespace AgileActors.ApiAggregation.Api.Models
{
    public sealed class WeatherResponse
    {
        public double TemperatureCelsius { get; init; }

        public double WindSpeedKmh { get; init; }

        public int WeatherCode { get; init; }
    }
}
