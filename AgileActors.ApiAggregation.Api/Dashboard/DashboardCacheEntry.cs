namespace AgileActors.ApiAggregation.Api.Dashboard;

public sealed class DashboardCacheEntry
{
    public required object Data { get; init; }

    public required DateTimeOffset FetchedAt { get; init; }
}