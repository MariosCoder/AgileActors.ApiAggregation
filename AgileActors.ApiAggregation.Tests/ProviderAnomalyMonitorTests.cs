using AgileActors.ApiAggregation.Api.Statistics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace AgileActors.ApiAggregation.Tests;

public sealed class ProviderAnomalyMonitorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTimerTicks_LogsSlowdownAndStopsCleanly()
    {
        var clock = new FakeTimeProvider();
        var store = new ProviderStatisticsStore(new[] { "tmdb" }, clock);
        var logger = new RecordingLogger();

        for (var index = 0; index < 20; index++)
        {
            store.Record("tmdb", TimeSpan.FromMilliseconds(100));
        }

        clock.Advance(TimeSpan.FromMinutes(6));

        for (var index = 0; index < 5; index++)
        {
            store.Record("tmdb", TimeSpan.FromMilliseconds(300));
        }

        using var monitor = new ProviderAnomalyMonitor(store, clock, logger);

        await monitor.StartAsync(CancellationToken.None);

        try
        {
            await logger.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.False(logger.Warning.Task.IsCompleted);

            clock.Advance(TimeSpan.FromSeconds(59));

            Assert.False(logger.Warning.Task.IsCompleted);

            clock.Advance(TimeSpan.FromSeconds(1));

            var message = await logger.Warning.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Contains("tmdb", message);
            Assert.Contains("slower than usual", message);
            Assert.Equal(25L, Assert.Single(store.GetSnapshot()).TotalRequests);
        }
        finally
        {
            using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            await monitor.StopAsync(shutdown.Token);
        }

        Assert.NotNull(monitor.ExecuteTask);
        Assert.True(monitor.ExecuteTask.IsCompletedSuccessfully);
    }

    private sealed class RecordingLogger : ILogger<ProviderAnomalyMonitor>
    {
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<string> Warning { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);

            if (logLevel == LogLevel.Information && message.StartsWith("Provider anomaly monitoring started.", StringComparison.Ordinal))
            {
                Started.TrySetResult(true);
            }

            if (logLevel == LogLevel.Warning)
            {
                Warning.TrySetResult(message);
            }
        }
    }
}