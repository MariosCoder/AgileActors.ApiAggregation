using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(DashboardRequest request, CancellationToken cancellationToken = default);
}