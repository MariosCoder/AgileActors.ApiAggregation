using System.ComponentModel.DataAnnotations;
using AgileActors.ApiAggregation.Api.Models;

namespace AgileActors.ApiAggregation.Tests;

public sealed class DashboardRequestTests
{
    [Theory]
    [InlineData("movies", null, null, null)]
    [InlineData("weather", 37.9838, 23.7275, null)]
    [InlineData("books", null, null, "The Hobbit")]
    [InlineData(null, 37.9838, 23.7275, "The Hobbit")]
    public void Validation_WhenSelectedProvidersHaveRequiredInputs_Succeeds(string? category, double? latitude, double? longitude, string? bookQuery)
    {
        var request = new DashboardRequest
        {
            Categories = category is null ? Array.Empty<string>() : new[] { category },
            Latitude = latitude,
            Longitude = longitude,
            BookQuery = bookQuery
        };

        var errors = GetValidationErrors(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validation_WhenAllCategoriesSelectedWithoutInputs_RequiresCoordinatesAndBookQuery()
    {
        var errors = GetValidationErrors(new DashboardRequest());
        var fields = errors.SelectMany(error => error.MemberNames).OrderBy(name => name).ToArray();

        Assert.Equal(new[] { "BookQuery", "Latitude", "Longitude" }, fields);
    }

    [Theory]
    [InlineData("weather")]
    [InlineData(" WEATHER ")]
    public void Validation_WhenWeatherSelectedWithoutCoordinates_RequiresOnlyCoordinates(string category)
    {
        var request = new DashboardRequest
        {
            Categories = new[] { category }
        };

        var errors = GetValidationErrors(request);
        var fields = errors.SelectMany(error => error.MemberNames).OrderBy(name => name).ToArray();

        Assert.Equal(new[] { "Latitude", "Longitude" }, fields);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validation_WhenBooksSelectedWithBlankQuery_RequiresBookQuery(string? query)
    {
        var request = new DashboardRequest
        {
            Categories = new[] { " BOOKS " },
            BookQuery = query
        };

        var errors = GetValidationErrors(request);
        var error = Assert.Single(errors);

        Assert.Contains(nameof(DashboardRequest.BookQuery), error.MemberNames);
    }

    [Theory]
    [InlineData(-91, 0, "Latitude")]
    [InlineData(91, 0, "Latitude")]
    [InlineData(0, -181, "Longitude")]
    [InlineData(0, 181, "Longitude")]
    public void Validation_WhenCoordinateIsOutOfRange_RejectsIt(double latitude, double longitude, string expectedField)
    {
        var request = new DashboardRequest
        {
            Categories = new[] { "weather" },
            Latitude = latitude,
            Longitude = longitude
        };

        var errors = GetValidationErrors(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(expectedField));
    }

    [Fact]
    public void Validation_WhenBookQueryExceedsMaximumLength_RejectsIt()
    {
        var request = new DashboardRequest
        {
            Categories = new[] { "books" },
            BookQuery = new string('a', 201)
        };

        var errors = GetValidationErrors(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(DashboardRequest.BookQuery)));
    }

    [Theory]
    [InlineData(99, 0, "SortBy")]
    [InlineData(0, 99, "SortDirection")]
    public void Validation_WhenSortingValueIsUndefined_RejectsIt(int sortBy, int sortDirection, string expectedField)
    {
        var request = new DashboardRequest
        {
            Categories = new[] { "movies" },
            SortBy = (DashboardSortBy)sortBy,
            SortDirection = (DashboardSortDirection)sortDirection
        };

        var errors = GetValidationErrors(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(expectedField));
    }

    private static List<ValidationResult> GetValidationErrors(DashboardRequest request)
    {
        var errors = new List<ValidationResult>();

        Validator.TryValidateObject(request, new ValidationContext(request), errors, validateAllProperties: true);

        return errors;
    }
}