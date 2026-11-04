namespace AgileActors.ApiAggregation.Api.Models;

public sealed class DashboardSection
{
    public required string Category { get; init; }

    public required string Provider { get; init; }

    public required DashboardSectionStatus Status { get; init; }

    public DateTimeOffset? FetchedAt { get; init; }

    public object? Data { get; init; }

    public DashboardSectionError? Error { get; init; }
}