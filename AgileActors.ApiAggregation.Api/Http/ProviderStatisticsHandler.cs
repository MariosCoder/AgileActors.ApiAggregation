using System.Diagnostics;
using AgileActors.ApiAggregation.Api.Statistics;

namespace AgileActors.ApiAggregation.Api.Http;

public sealed class ProviderStatisticsHandler : DelegatingHandler
{
    private readonly string _provider;
    private readonly ProviderStatisticsStore _statistics;

    public ProviderStatisticsHandler(string provider, ProviderStatisticsStore statistics)
    {
        _provider = provider;
        _statistics = statistics;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            var duration = Stopwatch.GetElapsedTime(startedAt);
            _statistics.Record(_provider, duration);
        }
    }
}