using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public static class DashboardListSorter
{
    public static IReadOnlyList<T> SortAndLimit<T>(IEnumerable<T> items, Func<T, string> getTitle, DashboardSortBy sortBy, DashboardSortDirection sortDirection)
    {
        if (sortBy == DashboardSortBy.Title)
        {
            items = sortDirection == DashboardSortDirection.Desc
                ? items.OrderByDescending(getTitle, StringComparer.OrdinalIgnoreCase)
                : items.OrderBy(getTitle, StringComparer.OrdinalIgnoreCase);
        }

        return items.Take(3).ToArray();
    }
}