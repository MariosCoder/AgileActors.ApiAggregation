namespace AgileActors.ApiAggregation.Api.Dashboard;

public sealed class DashboardCachePolicy
{
    public TimeSpan FreshFor { get; }

    public TimeSpan MaximumAge { get; }

    public DashboardCachePolicy(TimeSpan freshFor, TimeSpan maximumAge)
    {
        if (freshFor <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(freshFor), "The freshness period must be positive.");
        }

        if (maximumAge < freshFor)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAge), "The maximum age must be at least the freshness period.");
        }

        FreshFor = freshFor;
        MaximumAge = maximumAge;
    }
}