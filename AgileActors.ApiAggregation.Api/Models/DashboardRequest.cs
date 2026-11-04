using System.ComponentModel.DataAnnotations;

namespace AgileActors.ApiAggregation.Api.Models;

public sealed class DashboardRequest
{
    [Required]
    [Range(-90, 90)]
    public double? Latitude { get; init; }

    [Required]
    [Range(-180, 180)]
    public double? Longitude { get; init; }

    [Required]
    [StringLength(200)]
    public string BookQuery { get; init; } = string.Empty;
}