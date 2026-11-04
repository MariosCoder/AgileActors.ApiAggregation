using AgileActors.ApiAggregation.Api.Dashboard;
using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Tests;

public sealed class DashboardListSorterTests
{
    [Fact]
    public void SortAndLimit_WhenAscending_SortsBeforeLimitingWithoutChangingSource()
    {
        var titles = new[] { "Zulu", "bravo", "Delta", "Alpha" };

        var result = DashboardListSorter.SortAndLimit(titles, title => title, DashboardSortBy.Title, DashboardSortDirection.Asc);

        Assert.Equal(new[] { "Alpha", "bravo", "Delta" }, result);
        Assert.Equal(new[] { "Zulu", "bravo", "Delta", "Alpha" }, titles);
    }

    [Fact]
    public void SortAndLimit_WhenDescending_ReturnsHighestTitlesFirst()
    {
        var titles = new[] { "Alpha", "Delta", "bravo", "Zulu" };

        var result = DashboardListSorter.SortAndLimit(titles, title => title, DashboardSortBy.Title, DashboardSortDirection.Desc);

        Assert.Equal(new[] { "Zulu", "Delta", "bravo" }, result);
    }

    [Theory]
    [InlineData(DashboardSortDirection.Asc)]
    [InlineData(DashboardSortDirection.Desc)]
    public void SortAndLimit_WhenUsingProviderOrder_PreservesOriginalOrder(DashboardSortDirection direction)
    {
        var titles = new[] { "Zulu", "Alpha", "Delta", "bravo" };

        var result = DashboardListSorter.SortAndLimit(titles, title => title, DashboardSortBy.ProviderOrder, direction);

        Assert.Equal(new[] { "Zulu", "Alpha", "Delta" }, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SortAndLimit_WhenFewerThanThreeItems_ReturnsAllAvailableItems(int count)
    {
        var titles = new[] { "Alpha", "Bravo" }.Take(count).ToArray();

        var result = DashboardListSorter.SortAndLimit(titles, title => title, DashboardSortBy.Title, DashboardSortDirection.Asc);

        Assert.Equal(titles, result);
    }
}