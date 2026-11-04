namespace AgileActors.ApiAggregation.Api.Models;

public sealed class PerformanceBucketResponse
{
    public required string Name { get; init; }

    public long RequestCount { get; init; }

    public double? AverageResponseTimeMs { get; init; }
}