using System.Text.Json.Serialization;

namespace AgileActors.ApiAggregation.Api.Providers.OpenLibrary;

public sealed class OpenLibrarySearchResponse
{
    [JsonPropertyName("docs")]
    public List<OpenLibraryBook>? Documents { get; init; }
}

public sealed class OpenLibraryBook
{
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("author_name")]
    public List<string>? Authors { get; init; }

    [JsonPropertyName("first_publish_year")]
    public int? FirstPublishYear { get; init; }
}