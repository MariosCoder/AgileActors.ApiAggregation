using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public sealed class DashboardCacheService
{
    private readonly DashboardCache _cache;
    private readonly DashboardFetchCoordinator _coordinator;
    private readonly TimeProvider _timeProvider;

    public DashboardCacheService(DashboardCache cache, DashboardFetchCoordinator coordinator, TimeProvider timeProvider)
    {
        _cache = cache;
        _coordinator = coordinator;
        _timeProvider = timeProvider;
    }

    public async Task<DashboardSection> GetSectionAsync(string category, string provider, string key, DashboardCachePolicy policy, Func<CancellationToken, Task<DashboardSection>> fetchSection, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entry = _cache.Get(key);

        if (IsFresh(entry, policy))
        {
            return DashboardSectionFactory.CreateCached(category, provider, entry!);
        }

        var providerLock = _coordinator.GetLock(provider);

        await providerLock.WaitAsync(cancellationToken);

        try
        {
            entry = _cache.Get(key);

            if (IsFresh(entry, policy))
            {
                return DashboardSectionFactory.CreateCached(category, provider, entry!);
            }

            var section = await fetchSection(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (section.Status == DashboardSectionStatus.Fresh)
            {
                if (section.Data is null || section.FetchedAt is null)
                {
                    throw new InvalidOperationException("A fresh section must contain data and a fetch timestamp.");
                }

                var newEntry = new DashboardCacheEntry
                {
                    Data = section.Data,
                    FetchedAt = section.FetchedAt.Value
                };

                _cache.Set(key, newEntry, policy);
            }

            var error = section.Error;
            var canUseFallback = section.Status == DashboardSectionStatus.Unavailable && error?.Code is "provider_timeout" or "provider_unavailable";

            if (canUseFallback)
            {
                var fallbackEntry = _cache.Get(key);

                if (fallbackEntry is not null && _timeProvider.GetUtcNow() - fallbackEntry.FetchedAt < policy.MaximumAge)
                {
                    return DashboardSectionFactory.CreateStale(category, provider, fallbackEntry, error!);
                }
            }

            return section;
        }
        finally
        {
            providerLock.Release();
        }
    }

    private bool IsFresh(DashboardCacheEntry? entry, DashboardCachePolicy policy)
    {
        return entry is not null && _timeProvider.GetUtcNow() - entry.FetchedAt < policy.FreshFor;
    }
}