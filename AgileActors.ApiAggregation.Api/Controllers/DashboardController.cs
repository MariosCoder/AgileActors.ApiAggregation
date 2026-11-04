using AgileActors.ApiAggregation.Api.Dashboard;
using AgileActors.ApiAggregation.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace AgileActors.ApiAggregation.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get([FromQuery] DashboardRequest request, CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardService.GetDashboardAsync(request, cancellationToken);

        return Ok(dashboard);
    }
}