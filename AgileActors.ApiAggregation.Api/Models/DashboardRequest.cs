using System.ComponentModel.DataAnnotations;

namespace AgileActors.ApiAggregation.Api.Models;

public sealed class DashboardRequest : IValidatableObject
{
    [Range(-90, 90)]
    public double? Latitude { get; init; }

    [Range(-180, 180)]
    public double? Longitude { get; init; }

    [StringLength(200)]
    public string? BookQuery { get; init; }

    public string[] Categories { get; init; } = Array.Empty<string>();

    [EnumDataType(typeof(DashboardSortBy))]
    public DashboardSortBy SortBy { get; init; } = DashboardSortBy.ProviderOrder;

    [EnumDataType(typeof(DashboardSortDirection))]
    public DashboardSortDirection SortDirection { get; init; } = DashboardSortDirection.Asc;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var includesWeather = Categories.Length == 0 || Categories.Any(category => string.Equals(category?.Trim(), "weather", StringComparison.OrdinalIgnoreCase));
        if (includesWeather && Latitude is null)
        {
            yield return new ValidationResult("Latitude is required when weather is selected.", new[] { nameof(Latitude) });
        }

        if (includesWeather && Longitude is null)
        {
            yield return new ValidationResult("Longitude is required when weather is selected.", new[] { nameof(Longitude) });
        }

        var includesBooks = Categories.Length == 0 || Categories.Any(category => string.Equals(category?.Trim(), "books", StringComparison.OrdinalIgnoreCase));
        if (includesBooks && string.IsNullOrWhiteSpace(BookQuery))
        {
            yield return new ValidationResult("BookQuery is required when books are selected.", new[] { nameof(BookQuery) });
        }
    }
}