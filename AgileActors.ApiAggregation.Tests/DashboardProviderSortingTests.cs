using AgileActors.ApiAggregation.Api.Dashboard;
using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;

namespace AgileActors.ApiAggregation.Tests;

public sealed class DashboardProviderSortingTests
{
    [Theory]
    [InlineData("movies")]
    [InlineData("books")]
    public async Task GetSectionAsync_WhenSortChanges_ReusesFullCachedListWithoutChangingEarlierResponse(string category)
    {
        using var cache = new DashboardCache();
        using var coordinator = new DashboardFetchCoordinator();

        var cacheService = new DashboardCacheService(cache, coordinator, TimeProvider.System);
        var source = new StubContentService();

        IDashboardProvider provider = category == "movies"
            ? new MoviesDashboardProvider(source, cacheService)
            : new BooksDashboardProvider(source, cacheService);

        var ascendingRequest = new DashboardRequest
        {
            Categories = new[] { category },
            BookQuery = "test",
            SortBy = DashboardSortBy.Title,
            SortDirection = DashboardSortDirection.Asc
        };

        var descendingRequest = new DashboardRequest
        {
            Categories = new[] { category },
            BookQuery = "test",
            SortBy = DashboardSortBy.Title,
            SortDirection = DashboardSortDirection.Desc
        };

        var providerOrderRequest = new DashboardRequest
        {
            Categories = new[] { category },
            BookQuery = "test"
        };

        var ascending = await provider.GetSectionAsync(ascendingRequest);
        var descending = await provider.GetSectionAsync(descendingRequest);
        var providerOrder = await provider.GetSectionAsync(providerOrderRequest);

        Assert.Equal(new[] { "Alpha", "bravo", "Delta" }, GetTitles(ascending));
        Assert.Equal(new[] { "Zulu", "Delta", "bravo" }, GetTitles(descending));
        Assert.Equal(new[] { "Zulu", "bravo", "Delta" }, GetTitles(providerOrder));

        Assert.Equal(1, source.CallCount);
        Assert.Equal(DashboardSectionStatus.Fresh, ascending.Status);
        Assert.Equal(DashboardSectionStatus.Cached, descending.Status);
        Assert.Equal(DashboardSectionStatus.Cached, providerOrder.Status);

        Assert.NotNull(ascending.FetchedAt);
        Assert.Equal(ascending.FetchedAt, descending.FetchedAt);
        Assert.Equal(ascending.FetchedAt, providerOrder.FetchedAt);

        Assert.Equal(category, ascending.Category);
        Assert.Equal(ascending.Provider, descending.Provider);
        Assert.Null(ascending.Error);
        Assert.Null(descending.Error);
    }

    private static string[] GetTitles(DashboardSection section)
    {
        return section.Data switch
        {
            IReadOnlyList<MovieResponse> movies => movies.Select(movie => movie.Title).ToArray(),
            IReadOnlyList<BookResponse> books => books.Select(book => book.Title).ToArray(),
            _ => throw new InvalidOperationException("Expected movie or book data.")
        };
    }

    private sealed class StubContentService : IMovieService, IBookService
    {
        private readonly string[] _titles = { "Zulu", "bravo", "Delta", "Alpha" };

        public int CallCount { get; private set; }

        public Task<IReadOnlyList<MovieResponse>> GetPopularMoviesAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;

            IReadOnlyList<MovieResponse> movies = _titles.Select((title, index) => new MovieResponse
            {
                Id = index + 1,
                Title = title
            }).ToArray();

            return Task.FromResult(movies);
        }

        public Task<IReadOnlyList<BookResponse>> SearchBooksAsync(string query, CancellationToken cancellationToken = default)
        {
            CallCount++;

            IReadOnlyList<BookResponse> books = _titles.Select((title, index) => new BookResponse
            {
                Id = $"/works/test-{index + 1}",
                Title = title
            }).ToArray();

            return Task.FromResult(books);
        }
    }
}