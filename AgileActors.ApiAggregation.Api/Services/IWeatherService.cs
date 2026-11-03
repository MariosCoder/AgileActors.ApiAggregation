using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Api.Services
{
    public interface IWeatherService
    {
        Task<WeatherResponse> GetWeatherAsync(double latitude,double longitude,CancellationToken cancellationToken = default);
    }
}
