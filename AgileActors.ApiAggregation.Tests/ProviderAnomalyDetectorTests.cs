using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Statistics;

namespace AgileActors.ApiAggregation.Tests;

public sealed class ProviderAnomalyDetectorTests
{
    [Theory]
    [InlineData(100d, 140d, 1L, false)]
    [InlineData(100d, 150d, 1L, false)]
    [InlineData(100d, 151d, 1L, true)]
    [InlineData(100d, 100d, 5L, false)]
    [InlineData(100d, 300d, 0L, false)]
    [InlineData(0d, 300d, 1L, false)]
    [InlineData(null, 300d, 1L, false)]
    [InlineData(100d, null, 1L, false)]
    public void IsAnomaly_WhenComparingAverages_UsesThresholdAndAvailableData(double? overallAverage, double? recentAverage, long recentCount, bool expected)
    {
        var statistics = new ProviderStatisticsResponse
        {
            Provider = "tmdb",
            TotalRequests = 100,
            AverageResponseTimeMs = overallAverage,
            RecentRequestCount = recentCount,
            RecentAverageResponseTimeMs = recentAverage,
            Buckets = Array.Empty<PerformanceBucketResponse>()
        };

        var result = ProviderAnomalyDetector.IsAnomaly(statistics);

        Assert.Equal(expected, result);
    }
}