using System.Collections.Concurrent;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public sealed class DashboardFetchCoordinator : IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _providerLocks = new();

    public SemaphoreSlim GetLock(string provider)
    {
        return _providerLocks.GetOrAdd(provider, _ => new SemaphoreSlim(1, 1));
    }

    public void Dispose()
    {
        foreach (var providerLock in _providerLocks.Values)
        {
            providerLock.Dispose();
        }
    }
}