using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly IReadOnlyList<IDashboardProvider> _providers;

    public DashboardService(IEnumerable<IDashboardProvider> providers)
    {
        _providers = providers.ToArray();
    }

    public async Task<DashboardResponse> GetDashboardAsync(DashboardRequest request, CancellationToken cancellationToken = default)
    {
        var tasks = _providers.Select(provider => provider.GetSectionAsync(request, cancellationToken)).ToArray();

        var sections = await Task.WhenAll(tasks);

        var hasUsableData = sections.Any(section => section.Status != DashboardSectionStatus.Unavailable);
        var allSectionsHealthy = sections.All(section => section.Status is DashboardSectionStatus.Fresh or DashboardSectionStatus.Cached);

        var status = DashboardStatus.Unavailable;

        if (hasUsableData)
        {
            status = allSectionsHealthy ? DashboardStatus.Complete : DashboardStatus.Partial;
        }

        return new DashboardResponse
        {
            Status = status,
            Sections = sections
        };
    }
}