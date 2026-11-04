using System.Globalization;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public static class DashboardCacheKeys
{
    public static string Weather(double latitude, double longitude)
    {
        return string.Create(CultureInfo.InvariantCulture, $"open-meteo:weather:{latitude:R}:{longitude:R}");
    }

    public static string PopularMovies()
    {
        return "tmdb:movies:popular:en-US:page-1";
    }

    public static string Books(string query)
    {
        return $"open-library:books:{query.Trim()}";
    }
}