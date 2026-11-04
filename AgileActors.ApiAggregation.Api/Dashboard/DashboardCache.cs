using Microsoft.Extensions.Caching.Memory;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public sealed class DashboardCache : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = 500
    });

    public DashboardCacheEntry? Get(string key)
    {
        return _cache.Get<DashboardCacheEntry>(key);
    }

    public void Set(string key, DashboardCacheEntry entry, DashboardCachePolicy policy)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = entry.FetchedAt.Add(policy.MaximumAge),
            Size = 1
        };

        _cache.Set(key, entry, options);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }
}