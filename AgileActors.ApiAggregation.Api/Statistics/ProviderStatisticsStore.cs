using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Api.Statistics;

public sealed class ProviderStatisticsStore
{
    private static readonly string[] BucketNames = { "fast", "average", "slow" };
    private static readonly TimeSpan RecentWindow = TimeSpan.FromMinutes(5);

    private readonly object _sync = new();
    private readonly Dictionary<string, ProviderCounters> _providers;
    private readonly TimeProvider _timeProvider;

    public ProviderStatisticsStore(IEnumerable<string> providerNames, TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _providers = providerNames.ToDictionary(name => name, _ => new ProviderCounters(), StringComparer.OrdinalIgnoreCase);
    }

    public void Record(string provider, TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration cannot be negative.");
        }

        var milliseconds = duration.TotalMilliseconds;
        var bucketIndex = milliseconds < 100 ? 0 : milliseconds <= 200 ? 1 : 2;

        lock (_sync)
        {
            if (!_providers.TryGetValue(provider, out var counters))
            {
                throw new ArgumentException($"Unknown statistics provider: {provider}.", nameof(provider));
            }

            var timestamp = _timeProvider.GetTimestamp();

            RemoveExpiredSamples(counters, timestamp);

            counters.Buckets[bucketIndex].RequestCount++;
            counters.Buckets[bucketIndex].TotalDurationMs += milliseconds;
            counters.RecentSamples.Enqueue((timestamp, milliseconds));
        }
    }

    public IReadOnlyList<ProviderStatisticsResponse> GetSnapshot()
    {
        lock (_sync)
        {
            var timestamp = _timeProvider.GetTimestamp();

            return _providers.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry =>
            {
                var counters = entry.Value;

                RemoveExpiredSamples(counters, timestamp);

                var totalRequests = counters.Buckets.Sum(bucket => bucket.RequestCount);
                var totalDurationMs = counters.Buckets.Sum(bucket => bucket.TotalDurationMs);
                var recentDurationMs = counters.RecentSamples.Sum(sample => sample.DurationMs);

                var buckets = counters.Buckets.Select((bucket, index) => new PerformanceBucketResponse
                {
                    Name = BucketNames[index],
                    RequestCount = bucket.RequestCount,
                    AverageResponseTimeMs = GetAverage(bucket.TotalDurationMs, bucket.RequestCount)
                }).ToArray();

                return new ProviderStatisticsResponse
                {
                    Provider = entry.Key,
                    TotalRequests = totalRequests,
                    AverageResponseTimeMs = GetAverage(totalDurationMs, totalRequests),
                    RecentRequestCount = counters.RecentSamples.Count,
                    RecentAverageResponseTimeMs = GetAverage(recentDurationMs, counters.RecentSamples.Count),
                    Buckets = buckets
                };
            }).ToArray();
        }
    }

    private void RemoveExpiredSamples(ProviderCounters counters, long timestamp)
    {
        while (counters.RecentSamples.TryPeek(out var sample) && _timeProvider.GetElapsedTime(sample.Timestamp, timestamp) >= RecentWindow)
        {
            counters.RecentSamples.Dequeue();
        }
    }

    private static double? GetAverage(double totalDurationMs, long requestCount)
    {
        return requestCount == 0 ? null : totalDurationMs / requestCount;
    }

    private sealed class ProviderCounters
    {
        public BucketCounters[] Buckets { get; } = new BucketCounters[] { new(), new(), new() };

        public Queue<(long Timestamp, double DurationMs)> RecentSamples { get; } = new();
    }

    private sealed class BucketCounters
    {
        public long RequestCount { get; set; }

        public double TotalDurationMs { get; set; }
    }
}