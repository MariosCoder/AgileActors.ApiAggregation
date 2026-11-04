using AgileActors.ApiAggregation.Api.Dashboard;
using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Tests;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetDashboardAsync_WhenMoviesSelected_CallsOnlyMovieProvider()
    {
        var weather = new StubDashboardProvider("weather");
        var movies = new StubDashboardProvider("movies");
        var books = new StubDashboardProvider("books");

        var service = new DashboardService(new IDashboardProvider[] { weather, movies, books });
        var request = new DashboardRequest
        {
            Categories = new[] { "movies" }
        };

        var result = await service.GetDashboardAsync(request);

        var section = Assert.Single(result.Sections);

        Assert.Equal("movies", section.Category);
        Assert.Equal(DashboardStatus.Complete, result.Status);
        Assert.Equal(0, weather.CallCount);
        Assert.Equal(1, movies.CallCount);
        Assert.Equal(0, books.CallCount);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenCategoriesOmitted_CallsAllProviders()
    {
        var weather = new StubDashboardProvider("weather");
        var movies = new StubDashboardProvider("movies");
        var books = new StubDashboardProvider("books");

        var service = new DashboardService(new IDashboardProvider[] { weather, movies, books });

        var result = await service.GetDashboardAsync(new DashboardRequest());

        Assert.Equal(new[] { "weather", "movies", "books" }, result.Sections.Select(section => section.Category));
        Assert.Equal(DashboardStatus.Complete, result.Status);
        Assert.Equal(1, weather.CallCount);
        Assert.Equal(1, movies.CallCount);
        Assert.Equal(1, books.CallCount);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenCategoriesContainSpacesCaseAndDuplicates_CallsEachSelectedProviderOnce()
    {
        var weather = new StubDashboardProvider("weather");
        var movies = new StubDashboardProvider("movies");
        var books = new StubDashboardProvider("books");

        var service = new DashboardService(new IDashboardProvider[] { weather, movies, books });
        var request = new DashboardRequest
        {
            Categories = new[] { " Movies ", "MOVIES", "books" }
        };

        var result = await service.GetDashboardAsync(request);

        Assert.Equal(new[] { "movies", "books" }, result.Sections.Select(section => section.Category));
        Assert.Equal(DashboardStatus.Complete, result.Status);
        Assert.Equal(0, weather.CallCount);
        Assert.Equal(1, movies.CallCount);
        Assert.Equal(1, books.CallCount);
    }

    [Theory]
    [InlineData("music")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetDashboardAsync_WhenCategoryIsUnknown_RejectsBeforeCallingAnyProvider(string category)
    {
        var weather = new StubDashboardProvider("weather");
        var movies = new StubDashboardProvider("movies");
        var books = new StubDashboardProvider("books");

        var service = new DashboardService(new IDashboardProvider[] { weather, movies, books });
        var request = new DashboardRequest
        {
            Categories = new[] { "movies", category }
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GetDashboardAsync(request));

        Assert.Equal(nameof(DashboardRequest.Categories), exception.ParamName);
        Assert.Equal(0, weather.CallCount);
        Assert.Equal(0, movies.CallCount);
        Assert.Equal(0, books.CallCount);
    }

    private sealed class StubDashboardProvider : IDashboardProvider
    {
        public string Category { get; }
        public int CallCount { get; private set; }

        public StubDashboardProvider(string category)
        {
            Category = category;
        }

        public Task<DashboardSection> GetSectionAsync(DashboardRequest request, CancellationToken cancellationToken = default)
        {
            CallCount++;

            var section = DashboardSectionFactory.CreateFresh(Category, "test-provider", Array.Empty<object>());

            return Task.FromResult(section);
        }
    }
}