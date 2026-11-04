using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public interface IDashboardProvider
{
    Task<DashboardSection> GetSectionAsync(DashboardRequest request, CancellationToken cancellationToken = default);
}