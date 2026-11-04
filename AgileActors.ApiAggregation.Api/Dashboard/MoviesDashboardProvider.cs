using System.Text.Json;
using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public sealed class MoviesDashboardProvider : IDashboardProvider
{
    private static readonly DashboardCachePolicy CachePolicy = new(TimeSpan.FromMinutes(15), TimeSpan.FromHours(2));

    private readonly IMovieService _movieService;
    private readonly DashboardCacheService _cacheService;

    public MoviesDashboardProvider(IMovieService movieService, DashboardCacheService cacheService)
    {
        _movieService = movieService;
        _cacheService = cacheService;
    }

    public Task<DashboardSection> GetSectionAsync(DashboardRequest request, CancellationToken cancellationToken = default)
    {
        var key = DashboardCacheKeys.PopularMovies();

        return _cacheService.GetSectionAsync("movies", "tmdb", key, CachePolicy, token => FetchSectionAsync(request, token), cancellationToken);
    }

    private async Task<DashboardSection> FetchSectionAsync(DashboardRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var movies = await _movieService.GetPopularMoviesAsync(cancellationToken);

            return DashboardSectionFactory.CreateFresh("movies", "tmdb", movies);
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            exception is HttpRequestException or InvalidDataException or JsonException or OperationCanceledException)
        {
            var isTimeout = exception is OperationCanceledException;
            var code = isTimeout ? "provider_timeout" : "provider_unavailable";
            var message = isTimeout ? "The movie provider did not respond in time." : "Movie data could not be retrieved.";

            return DashboardSectionFactory.CreateUnavailable("movies", "tmdb", code, message);
        }
    }
}