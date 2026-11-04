using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public static class DashboardSectionFactory
{
    private static readonly TimeZoneInfo GreekTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Athens");

    public static DashboardSection CreateFresh(string category, string provider, object data)
    {
        return new DashboardSection
        {
            Category = category,
            Provider = provider,
            Status = DashboardSectionStatus.Fresh,
            FetchedAt = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, GreekTimeZone),
            Data = data
        };
    }

    public static DashboardSection CreateUnavailable(string category, string provider, string code, string message)
    {
        return new DashboardSection
        {
            Category = category,
            Provider = provider,
            Status = DashboardSectionStatus.Unavailable,
            Error = new DashboardSectionError
            {
                Code = code,
                Message = message
            }
        };
    }

    public static DashboardSection CreateCached(string category, string provider, DashboardCacheEntry entry)
    {
        return new DashboardSection
        {
            Category = category,
            Provider = provider,
            Status = DashboardSectionStatus.Cached,
            FetchedAt = TimeZoneInfo.ConvertTime(entry.FetchedAt, GreekTimeZone),
            Data = entry.Data
        };
    }

    public static DashboardSection CreateStale(string category, string provider, DashboardCacheEntry entry, DashboardSectionError error)
    {
        return new DashboardSection
        {
            Category = category,
            Provider = provider,
            Status = DashboardSectionStatus.Stale,
            FetchedAt = TimeZoneInfo.ConvertTime(entry.FetchedAt, GreekTimeZone),
            Data = entry.Data,
            Error = error
        };
    }

    public static DashboardSection WithData(DashboardSection section, object data)
    {
        return new DashboardSection
        {
            Category = section.Category,
            Provider = section.Provider,
            Status = section.Status,
            FetchedAt = section.FetchedAt,
            Data = data,
            Error = section.Error
        };
    }
}