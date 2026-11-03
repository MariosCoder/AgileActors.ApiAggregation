using System.ComponentModel.DataAnnotations;
using AgileActors.ApiAggregation.Api.Models;
using AgileActors.ApiAggregation.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgileActors.ApiAggregation.Api.Controllers;

[ApiController]
[Route("api/weather")]
public sealed class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet]
    public async Task<ActionResult<WeatherResponse>> Get([FromQuery, Required, Range(-90, 90)] double? latitude, [FromQuery, Required, Range(-180, 180)] double? longitude, CancellationToken cancellationToken)
    {
        var weather = await _weatherService.GetWeatherAsync(latitude!.Value, longitude!.Value, cancellationToken);

        return Ok(weather);
    }
}