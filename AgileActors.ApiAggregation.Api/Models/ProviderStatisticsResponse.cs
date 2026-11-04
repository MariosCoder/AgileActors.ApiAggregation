namespace AgileActors.ApiAggregation.Api.Models;

public sealed class ProviderStatisticsResponse
{
    public required string Provider { get; init; }

    public long TotalRequests { get; init; }

    public double? AverageResponseTimeMs { get; init; }

    public long RecentRequestCount { get; init; }

    public double? RecentAverageResponseTimeMs { get; init; }

    public required IReadOnlyList<PerformanceBucketResponse> Buckets { get; init; }
}