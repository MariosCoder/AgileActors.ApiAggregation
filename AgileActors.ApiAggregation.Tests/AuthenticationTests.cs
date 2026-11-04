using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AgileActors.ApiAggregation.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AgileActors.ApiAggregation.Tests;

public sealed class AuthenticationTests : IClassFixture<AuthenticationApiFactory>
{
    private readonly AuthenticationApiFactory _factory;

    public AuthenticationTests(AuthenticationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Statistics_WhenTokenIsMissing_ReturnsUnauthorized()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/api/statistics");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
    }

    [Fact]
    public async Task Statistics_WhenTokenIsValid_ReturnsStatistics()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken("valid"));

        using var response = await client.GetAsync("/api/statistics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var statistics = await response.Content.ReadFromJsonAsync<ProviderStatisticsResponse[]>();

        Assert.NotNull(statistics);
        Assert.Equal(new[] { "open-library", "open-meteo", "tmdb" }, statistics.Select(provider => provider.Provider));
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("expired")]
    [InlineData("not-yet-valid")]
    [InlineData("wrong-issuer")]
    [InlineData("wrong-audience")]
    [InlineData("wrong-signature")]
    [InlineData("no-expiry")]
    [InlineData("unsigned")]
    public async Task Statistics_WhenTokenIsInvalid_ReturnsUnauthorized(string scenario)
    {
        using var client = CreateClient();

        var token = scenario == "malformed" ? "not-a-valid-token" : _factory.CreateToken(scenario);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/api/statistics");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/dashboard?categories=")]
    [InlineData("/api/dashboard?categories=%20%20")]
    [InlineData("/api/dashboard?categories=movies&categories=")]
    public async Task Dashboard_WhenCategoryEntryIsBlank_ReturnsBadRequest(string url)
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.CreateToken("valid"));

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }
}