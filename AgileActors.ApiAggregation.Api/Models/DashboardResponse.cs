namespace AgileActors.ApiAggregation.Api.Models;

public sealed class DashboardResponse
{
    public required DashboardStatus Status { get; init; }

    public IReadOnlyList<DashboardSection> Sections { get; init; } = Array.Empty<DashboardSection>();
}