using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Api.Dashboard;

public interface IDashboardProvider
{
    string Category { get; }
    Task<DashboardSection> GetSectionAsync(DashboardRequest request, CancellationToken cancellationToken = default);
}