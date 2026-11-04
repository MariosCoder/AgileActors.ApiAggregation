using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Statistics;
using Microsoft.AspNetCore.Mvc;

namespace AgileActors.ApiAggregation.Api.Controllers;

[ApiController]
[Route("api/statistics")]
public sealed class StatisticsController : ControllerBase
{
    private readonly ProviderStatisticsStore _statistics;

    public StatisticsController(ProviderStatisticsStore statistics)
    {
        _statistics = statistics;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<ProviderStatisticsResponse>> Get()
    {
        return Ok(_statistics.GetSnapshot());
    }
}