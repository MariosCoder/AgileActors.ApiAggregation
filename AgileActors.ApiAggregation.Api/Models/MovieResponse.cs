namespace AgileActors.ApiAggregation.Api.Models;

public sealed class MovieResponse
{
    public int Id { get; init; }

    public required string Title { get; init; }

    public string? Overview { get; init; }

    public DateOnly? ReleaseDate { get; init; }

    public double? Rating { get; init; }
}