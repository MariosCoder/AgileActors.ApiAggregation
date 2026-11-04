using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Api.Statistics;

public static class ProviderAnomalyDetector
{
    public static bool IsAnomaly(ProviderStatisticsResponse statistics)
    {
        if (statistics.RecentRequestCount == 0 || statistics.AverageResponseTimeMs is not double overall || overall <= 0 || statistics.RecentAverageResponseTimeMs is not double recent)
        {
            return false;
        }

        return recent > overall * 1.5;
    }
}