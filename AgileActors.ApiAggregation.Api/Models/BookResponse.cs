namespace AgileActors.ApiAggregation.Api.Models;

public sealed class BookResponse
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public IReadOnlyList<string> Authors { get; init; } = Array.Empty<string>();

    public int? FirstPublishYear { get; init; }
}