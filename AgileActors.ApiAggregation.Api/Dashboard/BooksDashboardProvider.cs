using System.Text.Json;
using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public sealed class BooksDashboardProvider : IDashboardProvider
{
    public string Category => "books";
    private static readonly DashboardCachePolicy CachePolicy = new(TimeSpan.FromMinutes(15), TimeSpan.FromHours(2));

    private readonly IBookService _bookService;
    private readonly DashboardCacheService _cacheService;

    public BooksDashboardProvider(IBookService bookService, DashboardCacheService cacheService)
    {
        _bookService = bookService;
        _cacheService = cacheService;
    }

    public async Task<DashboardSection> GetSectionAsync(DashboardRequest request, CancellationToken cancellationToken = default)
    {
        var key = DashboardCacheKeys.Books(request.BookQuery!);

        var section = await _cacheService.GetSectionAsync(Category, "open-library", key, CachePolicy, token => FetchSectionAsync(request, token), cancellationToken);

        if (section.Data is not IReadOnlyList<BookResponse> books)
        {
            return section;
        }

        var sortedBooks = DashboardListSorter.SortAndLimit(books, book => book.Title, request.SortBy, request.SortDirection);

        return DashboardSectionFactory.WithData(section, sortedBooks);
    }

    private async Task<DashboardSection> FetchSectionAsync(DashboardRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var books = await _bookService.SearchBooksAsync(request.BookQuery!, cancellationToken);

            return DashboardSectionFactory.CreateFresh("books", "open-library", books);
        }
        catch (ProviderQueryRejectedException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return DashboardSectionFactory.CreateUnavailable("books", "open-library", "query_rejected", exception.Message);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested && exception is HttpRequestException or HttpIOException or InvalidDataException or JsonException or OperationCanceledException)
        {
            var isTimeout = exception is OperationCanceledException;
            var code = isTimeout ? "provider_timeout" : "provider_unavailable";
            var message = isTimeout ? "The book provider did not respond in time." : "Book data could not be retrieved.";

            return DashboardSectionFactory.CreateUnavailable("books", "open-library", code, message);
        }
    }
}