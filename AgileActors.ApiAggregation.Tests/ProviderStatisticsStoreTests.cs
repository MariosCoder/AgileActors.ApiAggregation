using AgileActors.ApiAggregation.Api.Statistics;

namespace AgileActors.ApiAggregation.Tests;

public sealed class ProviderStatisticsStoreTests
{
    [Fact]
    public void GetSnapshot_BeforeRequests_ReturnsAllProvidersWithNoAverages()
    {
        var store = new ProviderStatisticsStore(new[] { "open-meteo", "tmdb", "open-library" });

        var snapshot = store.GetSnapshot();

        Assert.Equal(new[] { "open-library", "open-meteo", "tmdb" }, snapshot.Select(provider => provider.Provider));

        Assert.All(snapshot, provider =>
        {
            Assert.Equal(0L, provider.TotalRequests);
            Assert.Null(provider.AverageResponseTimeMs);
            Assert.Equal(new[] { "fast", "average", "slow" }, provider.Buckets.Select(bucket => bucket.Name));

            Assert.All(provider.Buckets, bucket =>
            {
                Assert.Equal(0L, bucket.RequestCount);
                Assert.Null(bucket.AverageResponseTimeMs);
            });
        });
    }

    [Theory]
    [InlineData(0, "fast")]
    [InlineData(99, "fast")]
    [InlineData(100, "average")]
    [InlineData(150, "average")]
    [InlineData(200, "average")]
    [InlineData(201, "slow")]
    public void Record_WhenDurationFallsInBucket_UpdatesOnlyThatBucket(double milliseconds, string expectedBucket)
    {
        var store = new ProviderStatisticsStore(new[] { "tmdb" });

        store.Record("tmdb", TimeSpan.FromMilliseconds(milliseconds));

        var provider = Assert.Single(store.GetSnapshot());
        var bucket = Assert.Single(provider.Buckets.Where(item => item.Name == expectedBucket));

        Assert.Equal(1L, provider.TotalRequests);
        Assert.Equal(milliseconds, provider.AverageResponseTimeMs!.Value);
        Assert.Equal(1L, bucket.RequestCount);
        Assert.Equal(milliseconds, bucket.AverageResponseTimeMs!.Value);

        Assert.All(provider.Buckets.Where(item => item.Name != expectedBucket), other =>
        {
            Assert.Equal(0L, other.RequestCount);
            Assert.Null(other.AverageResponseTimeMs);
        });
    }

    [Fact]
    public void Record_WhenBucketsHaveDifferentCounts_CalculatesWeightedOverallAverage()
    {
        var store = new ProviderStatisticsStore(new[] { "tmdb" });

        store.Record("tmdb", TimeSpan.FromMilliseconds(50));
        store.Record("tmdb", TimeSpan.FromMilliseconds(50));
        store.Record("tmdb", TimeSpan.FromMilliseconds(200));
        store.Record("tmdb", TimeSpan.FromMilliseconds(500));

        var provider = Assert.Single(store.GetSnapshot());
        var fast = Assert.Single(provider.Buckets.Where(bucket => bucket.Name == "fast"));
        var average = Assert.Single(provider.Buckets.Where(bucket => bucket.Name == "average"));
        var slow = Assert.Single(provider.Buckets.Where(bucket => bucket.Name == "slow"));

        Assert.Equal(4L, provider.TotalRequests);
        Assert.Equal(200d, provider.AverageResponseTimeMs!.Value);

        Assert.Equal(2L, fast.RequestCount);
        Assert.Equal(50d, fast.AverageResponseTimeMs!.Value);
        Assert.Equal(1L, average.RequestCount);
        Assert.Equal(200d, average.AverageResponseTimeMs!.Value);
        Assert.Equal(1L, slow.RequestCount);
        Assert.Equal(500d, slow.AverageResponseTimeMs!.Value);
    }

    [Fact]
    public void Record_WhenProvidersDiffer_KeepsTheirStatisticsSeparate()
    {
        var store = new ProviderStatisticsStore(new[] { "tmdb", "open-library" });

        store.Record("tmdb", TimeSpan.FromMilliseconds(50));
        store.Record("open-library", TimeSpan.FromMilliseconds(300));

        var snapshot = store.GetSnapshot();
        var movies = Assert.Single(snapshot.Where(provider => provider.Provider == "tmdb"));
        var books = Assert.Single(snapshot.Where(provider => provider.Provider == "open-library"));

        Assert.Equal(1L, movies.TotalRequests);
        Assert.Equal(50d, movies.AverageResponseTimeMs!.Value);
        Assert.Equal(1L, books.TotalRequests);
        Assert.Equal(300d, books.AverageResponseTimeMs!.Value);
    }

    [Fact]
    public void GetSnapshot_WhenStoreChanges_DoesNotChangeEarlierSnapshot()
    {
        var store = new ProviderStatisticsStore(new[] { "tmdb" });

        store.Record("tmdb", TimeSpan.FromMilliseconds(50));

        var earlier = Assert.Single(store.GetSnapshot());

        store.Record("tmdb", TimeSpan.FromMilliseconds(150));

        var later = Assert.Single(store.GetSnapshot());

        Assert.Equal(1L, earlier.TotalRequests);
        Assert.Equal(50d, earlier.AverageResponseTimeMs!.Value);
        Assert.Equal(0L, earlier.Buckets.Single(bucket => bucket.Name == "average").RequestCount);

        Assert.Equal(2L, later.TotalRequests);
        Assert.Equal(100d, later.AverageResponseTimeMs!.Value);
        Assert.Equal(1L, later.Buckets.Single(bucket => bucket.Name == "average").RequestCount);
    }

    [Fact]
    public void Record_WhenCalledConcurrently_PreservesCountsAndConsistentSnapshots()
    {
        var store = new ProviderStatisticsStore(new[] { "tmdb" });

        Parallel.For(0, 10000, index =>
        {
            store.Record("tmdb", TimeSpan.FromMilliseconds(150));

            if (index % 100 == 0)
            {
                var snapshot = Assert.Single(store.GetSnapshot());

                Assert.Equal(snapshot.TotalRequests, snapshot.Buckets.Sum(bucket => bucket.RequestCount));
                Assert.Equal(150d, snapshot.AverageResponseTimeMs!.Value);
            }
        });

        var result = Assert.Single(store.GetSnapshot());

        Assert.Equal(10000L, result.TotalRequests);
        Assert.Equal(150d, result.AverageResponseTimeMs!.Value);
        Assert.Equal(10000L, result.Buckets.Single(bucket => bucket.Name == "average").RequestCount);
    }

    [Fact]
    public void Record_WhenDurationIsNegative_RejectsWithoutChangingStatistics()
    {
        var store = new ProviderStatisticsStore(new[] { "tmdb" });

        Assert.Throws<ArgumentOutOfRangeException>(() => store.Record("tmdb", TimeSpan.FromMilliseconds(-1)));

        Assert.Equal(0L, Assert.Single(store.GetSnapshot()).TotalRequests);
    }

    [Fact]
    public void Record_WhenProviderIsUnknown_RejectsWithoutChangingStatistics()
    {
        var store = new ProviderStatisticsStore(new[] { "tmdb" });

        Assert.Throws<ArgumentException>(() => store.Record("unknown", TimeSpan.FromMilliseconds(50)));

        Assert.Equal(0L, Assert.Single(store.GetSnapshot()).TotalRequests);
    }
}