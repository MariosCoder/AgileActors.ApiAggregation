using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Api.Services;

public interface IBookService
{
    Task<IReadOnlyList<BookResponse>> SearchBooksAsync(string query, CancellationToken cancellationToken = default);
}