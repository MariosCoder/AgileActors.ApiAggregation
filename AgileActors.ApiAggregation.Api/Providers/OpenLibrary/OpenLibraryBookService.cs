using System.Net.Http.Json;
using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;
using System.Net;

namespace AgileActors.ApiAggregation.Api.Providers.OpenLibrary;

public sealed class OpenLibraryBookService : IBookService
{
    private readonly HttpClient _httpClient;

    public OpenLibraryBookService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<BookResponse>> SearchBooksAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var encodedQuery = Uri.EscapeDataString(query.Trim());
        var url = $"search.json?q={encodedQuery}&fields=key,title,author_name,first_publish_year&limit=10";

        OpenLibrarySearchResponse? response;

        try
        {
            response = await _httpClient.GetFromJsonAsync<OpenLibrarySearchResponse>(url, cancellationToken);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            throw new ProviderQueryRejectedException("The book provider could not process this query. Try a more specific title or author.", exception);
        }

        if (response?.Documents is null)
        {
            throw new InvalidDataException("Open Library returned a response without book results.");
        }

        var books = new List<BookResponse>();

        foreach (var book in response.Documents.Take(3))
        {
            if (string.IsNullOrWhiteSpace(book.Key) || string.IsNullOrWhiteSpace(book.Title))
            {
                throw new InvalidDataException("Open Library returned a book without a valid ID or title.");
            }

            books.Add(new BookResponse
            {
                Id = book.Key,
                Title = book.Title,
                Authors = book.Authors is null ? Array.Empty<string>() : book.Authors.ToArray(),
                FirstPublishYear = book.FirstPublishYear
            });
        }

        return books;
    }
}