using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace AgileActors.ApiAggregation.Tests;

public sealed class AuthenticationApiFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "test-issuer";
    private const string Audience = "test-api";

    private readonly SymmetricSecurityKey _signingKey = new(RandomNumberGenerator.GetBytes(32));

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Tmdb:AccessToken"] = "test-only-placeholder"
            });
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidIssuer = Issuer;
                options.TokenValidationParameters.ValidAudience = Audience;
                options.TokenValidationParameters.IssuerSigningKey = _signingKey;
            });
        });
    }

    public string CreateToken(string scenario)
    {
        var now = DateTime.UtcNow;
        var issuer = scenario == "wrong-issuer" ? "another-issuer" : Issuer;
        var audience = scenario == "wrong-audience" ? "another-api" : Audience;
        var notBefore = scenario == "not-yet-valid" ? now.AddMinutes(5) : now.AddMinutes(-10);
        DateTime? expires = scenario == "expired" ? now.AddMinutes(-5) : now.AddMinutes(10);

        if (scenario == "no-expiry")
        {
            expires = null;
        }

        var signingKey = scenario == "wrong-signature" ? new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) : _signingKey;
        SigningCredentials? credentials = scenario == "unsigned" ? null : new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: new[] { new Claim("sub", "test-user") },
            notBefore: notBefore,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}