using System.Globalization;
using System.Net.Http.Json;
using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;

namespace AgileActors.ApiAggregation.Api.Providers.Tmdb;

public sealed class TmdbMovieService : IMovieService
{
    private readonly HttpClient _httpClient;

    public TmdbMovieService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<MovieResponse>> GetPopularMoviesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<TmdbMoviesResponse>("3/movie/popular?language=en-US&page=1", cancellationToken);

        if (response?.Results is null)
        {
            throw new InvalidDataException("TMDB returned a response without movie results.");
        }

        var movies = new List<MovieResponse>();

        foreach (var movie in response.Results)
        {
            if (movie.Id is not int id || id <= 0 || string.IsNullOrWhiteSpace(movie.Title))
            {
                throw new InvalidDataException("TMDB returned a movie without a valid ID or title.");
            }

            DateOnly? releaseDate = DateOnly.TryParseExact(movie.ReleaseDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) ? parsedDate : null;

            movies.Add(new MovieResponse
            {
                Id = id,
                Title = movie.Title,
                Overview = movie.Overview,
                ReleaseDate = releaseDate,
                Rating = movie.VoteCount > 0 ? movie.VoteAverage : null
            });
        }

        return movies;
    }
}