namespace AgileActors.ApiAggregation.Api.Statistics;

public sealed class ProviderAnomalyMonitor : BackgroundService
{
    private readonly ProviderStatisticsStore _statistics;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProviderAnomalyMonitor> _logger;

    public ProviderAnomalyMonitor(ProviderStatisticsStore statistics, TimeProvider timeProvider, ILogger<ProviderAnomalyMonitor> logger)
    {
        _statistics = statistics;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), _timeProvider);

        _logger.LogInformation("Provider anomaly monitoring started. Checking every minute against the recent five-minute average.");

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                foreach (var provider in _statistics.GetSnapshot())
                {
                    if (!ProviderAnomalyDetector.IsAnomaly(provider))
                    {
                        continue;
                    }

                    _logger.LogWarning(
                        "Provider {Provider} is slower than usual. Recent average: {RecentAverageMs:F2} ms across {RecentRequestCount} attempts. Overall average: {OverallAverageMs:F2} ms across {TotalRequests} attempts.",
                        provider.Provider,
                        provider.RecentAverageResponseTimeMs,
                        provider.RecentRequestCount,
                        provider.AverageResponseTimeMs,
                        provider.TotalRequests);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Application shutdown cancels the timer wait.
        }
    }
}