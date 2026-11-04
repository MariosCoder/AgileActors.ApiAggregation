using AgileActors.ApiAggregation.Api.Statistics;

namespace AgileActors.ApiAggregation.Tests;

public sealed class ProviderRecentStatisticsTests
{
    [Fact]
    public void GetSnapshot_WhenRequestsAreRecent_IncludesThemInRecentAverage()
    {
        var clock = new ManualTimeProvider();
        var store = new ProviderStatisticsStore(new[] { "tmdb" }, clock);

        store.Record("tmdb", TimeSpan.FromMilliseconds(100));
        store.Record("tmdb", TimeSpan.FromMilliseconds(300));

        var result = Assert.Single(store.GetSnapshot());

        Assert.Equal(2L, result.TotalRequests);
        Assert.Equal(200d, result.AverageResponseTimeMs!.Value);
        Assert.Equal(2L, result.RecentRequestCount);
        Assert.Equal(200d, result.RecentAverageResponseTimeMs!.Value);
    }

    [Theory]
    [InlineData(299, true)]
    [InlineData(300, false)]
    [InlineData(301, false)]
    public void GetSnapshot_WhenSampleReachesWindowBoundary_ExpiresAtFiveMinutes(int elapsedSeconds, bool shouldRemain)
    {
        var clock = new ManualTimeProvider();
        var store = new ProviderStatisticsStore(new[] { "tmdb" }, clock);

        store.Record("tmdb", TimeSpan.FromMilliseconds(100));
        clock.Advance(TimeSpan.FromSeconds(elapsedSeconds));

        var result = Assert.Single(store.GetSnapshot());

        Assert.Equal(1L, result.TotalRequests);
        Assert.Equal(100d, result.AverageResponseTimeMs!.Value);

        if (shouldRemain)
        {
            Assert.Equal(1L, result.RecentRequestCount);
            Assert.Equal(100d, result.RecentAverageResponseTimeMs!.Value);
        }
        else
        {
            Assert.Equal(0L, result.RecentRequestCount);
            Assert.Null(result.RecentAverageResponseTimeMs);
        }
    }

    [Fact]
    public void GetSnapshot_WhenOnlyOlderSampleExpires_KeepsNewerSampleAndLifetimeTotals()
    {
        var clock = new ManualTimeProvider();
        var store = new ProviderStatisticsStore(new[] { "tmdb" }, clock);

        store.Record("tmdb", TimeSpan.FromMilliseconds(100));

        clock.Advance(TimeSpan.FromMinutes(4));
        store.Record("tmdb", TimeSpan.FromMilliseconds(300));

        clock.Advance(TimeSpan.FromMinutes(2));

        var result = Assert.Single(store.GetSnapshot());

        Assert.Equal(2L, result.TotalRequests);
        Assert.Equal(200d, result.AverageResponseTimeMs!.Value);
        Assert.Equal(1L, result.RecentRequestCount);
        Assert.Equal(300d, result.RecentAverageResponseTimeMs!.Value);

        Assert.Equal(1L, result.Buckets.Single(bucket => bucket.Name == "average").RequestCount);
        Assert.Equal(1L, result.Buckets.Single(bucket => bucket.Name == "slow").RequestCount);
    }

    [Fact]
    public void Record_WhenEarlierSamplesHaveExpired_RecentAverageUsesNewSampleOnly()
    {
        var clock = new ManualTimeProvider();
        var store = new ProviderStatisticsStore(new[] { "tmdb" }, clock);

        store.Record("tmdb", TimeSpan.FromMilliseconds(100));

        clock.Advance(TimeSpan.FromMinutes(6));
        store.Record("tmdb", TimeSpan.FromMilliseconds(500));

        var result = Assert.Single(store.GetSnapshot());

        Assert.Equal(2L, result.TotalRequests);
        Assert.Equal(300d, result.AverageResponseTimeMs!.Value);
        Assert.Equal(1L, result.RecentRequestCount);
        Assert.Equal(500d, result.RecentAverageResponseTimeMs!.Value);
    }

    [Fact]
    public void AnomalyDetection_WhenRecentRequestsSlowDown_DetectsUntilRecentSamplesExpire()
    {
        var clock = new ManualTimeProvider();
        var store = new ProviderStatisticsStore(new[] { "tmdb" }, clock);

        for (var index = 0; index < 20; index++)
        {
            store.Record("tmdb", TimeSpan.FromMilliseconds(100));
        }

        clock.Advance(TimeSpan.FromMinutes(6));

        for (var index = 0; index < 5; index++)
        {
            store.Record("tmdb", TimeSpan.FromMilliseconds(300));
        }

        var slowdown = Assert.Single(store.GetSnapshot());

        Assert.Equal(25L, slowdown.TotalRequests);
        Assert.Equal(140d, slowdown.AverageResponseTimeMs!.Value);
        Assert.Equal(5L, slowdown.RecentRequestCount);
        Assert.Equal(300d, slowdown.RecentAverageResponseTimeMs!.Value);
        Assert.True(ProviderAnomalyDetector.IsAnomaly(slowdown));

        clock.Advance(TimeSpan.FromMinutes(5));

        var idle = Assert.Single(store.GetSnapshot());

        Assert.Equal(25L, idle.TotalRequests);
        Assert.Equal(140d, idle.AverageResponseTimeMs!.Value);
        Assert.Equal(0L, idle.RecentRequestCount);
        Assert.Null(idle.RecentAverageResponseTimeMs);
        Assert.False(ProviderAnomalyDetector.IsAnomaly(idle));
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp()
        {
            return _timestamp;
        }

        public void Advance(TimeSpan duration)
        {
            _timestamp += duration.Ticks;
        }
    }
}