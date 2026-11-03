using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Api.Services;

public interface IMovieService
{
    Task<IReadOnlyList<MovieResponse>> GetPopularMoviesAsync(CancellationToken cancellationToken = default);
}