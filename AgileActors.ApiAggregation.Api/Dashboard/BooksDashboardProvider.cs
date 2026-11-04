using System.Text.Json;
using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public sealed class BooksDashboardProvider : IDashboardProvider
{
    private readonly IBookService _bookService;

    public BooksDashboardProvider(IBookService bookService)
    {
        _bookService = bookService;
    }

    public async Task<DashboardSection> GetSectionAsync(DashboardRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var books = await _bookService.SearchBooksAsync(request.BookQuery, cancellationToken);
            var greekTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");

            return new DashboardSection
            {
                Category = "books",
                Provider = "open-library",
                Status = DashboardSectionStatus.Fresh,
                FetchedAt = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, greekTimeZone),
                Data = books
            };
        }
        catch (ProviderQueryRejectedException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return new DashboardSection
            {
                Category = "books",
                Provider = "open-library",
                Status = DashboardSectionStatus.Unavailable,
                Error = new DashboardSectionError
                {
                    Code = "query_rejected",
                    Message = exception.Message
                }
            };
        }
        catch (Exception exception) when (
            !cancellationToken.IsCancellationRequested &&
            exception is HttpRequestException or InvalidDataException or JsonException or OperationCanceledException)
        {
            var isTimeout = exception is OperationCanceledException;

            return new DashboardSection
            {
                Category = "books",
                Provider = "open-library",
                Status = DashboardSectionStatus.Unavailable,
                Error = new DashboardSectionError
                {
                    Code = isTimeout ? "provider_timeout" : "provider_unavailable",
                    Message = isTimeout ? "The book provider did not respond in time." : "Book data could not be retrieved."
                }
            };
        }
    }
}