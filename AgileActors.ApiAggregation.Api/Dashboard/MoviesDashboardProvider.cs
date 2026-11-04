using System.Text.Json;
using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public sealed class MoviesDashboardProvider : IDashboardProvider
{
    private readonly IMovieService _movieService;

    public MoviesDashboardProvider(IMovieService movieService)
    {
        _movieService = movieService;
    }

    public async Task<DashboardSection> GetSectionAsync(DashboardRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var movies = await _movieService.GetPopularMoviesAsync(cancellationToken);
            var greekTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");

            return new DashboardSection
            {
                Category = "movies",
                Provider = "tmdb",
                Status = DashboardSectionStatus.Fresh,
                FetchedAt = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, greekTimeZone),
                Data = movies
            };
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested && exception is HttpRequestException or InvalidDataException or JsonException or OperationCanceledException)
        {
            var isTimeout = exception is OperationCanceledException;

            return new DashboardSection
            {
                Category = "movies",
                Provider = "tmdb",
                Status = DashboardSectionStatus.Unavailable,
                Error = new DashboardSectionError
                {
                    Code = isTimeout ? "provider_timeout" : "provider_unavailable",
                    Message = isTimeout ? "The movie provider did not respond in time." : "Movie data could not be retrieved."
                }
            };
        }
    }
}