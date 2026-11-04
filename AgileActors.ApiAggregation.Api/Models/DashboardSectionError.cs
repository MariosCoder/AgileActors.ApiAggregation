namespace AgileActors.ApiAggregation.Api.Models;

public sealed class DashboardSectionError
{
    public required string Code { get; init; }

    public required string Message { get; init; }
}