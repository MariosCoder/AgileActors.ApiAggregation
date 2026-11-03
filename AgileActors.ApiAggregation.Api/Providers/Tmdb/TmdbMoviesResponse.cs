using System.Text.Json.Serialization;

namespace AgileActors.ApiAggregation.Api.Providers.Tmdb;

public sealed class TmdbMoviesResponse
{
    [JsonPropertyName("results")]
    public List<TmdbMovie>? Results { get; init; }
}

public sealed class TmdbMovie
{
    [JsonPropertyName("id")]
    public int? Id { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("overview")]
    public string? Overview { get; init; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; init; }

    [JsonPropertyName("vote_average")]
    public double? VoteAverage { get; init; }

    [JsonPropertyName("vote_count")]
    public int? VoteCount { get; init; }
}