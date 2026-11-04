using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgileActors.ApiAggregation.Api.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;

    public MoviesController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet("popular")]
    public async Task<ActionResult<IReadOnlyList<MovieResponse>>> GetPopular(CancellationToken cancellationToken)
    {
        var movies = await _movieService.GetPopularMoviesAsync(cancellationToken);

        return Ok(movies.Take(3).ToArray());
    }
}