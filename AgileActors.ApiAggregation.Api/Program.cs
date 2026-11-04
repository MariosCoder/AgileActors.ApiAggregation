using AgileActors.ApiAggregation.Api.Providers.OpenMeteo;
using AgileActors.ApiAggregation.Api.Services;
using System.Net.Http.Headers;
using AgileActors.ApiAggregation.Api.Providers.Tmdb;
using AgileActors.ApiAggregation.Api.Providers.OpenLibrary;
using AgileActors.ApiAggregation.Api.Dashboard;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgileActors.ApiAggregation.Api.Http;
using AgileActors.ApiAggregation.Api.Statistics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using AgileActors.ApiAggregation.Api.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters.ValidateIssuer = true;
    options.TokenValidationParameters.ValidateAudience = true;
    options.TokenValidationParameters.ValidateLifetime = true;
    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
    options.TokenValidationParameters.RequireSignedTokens = true;
    options.TokenValidationParameters.RequireExpirationTime = true;
    options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
});

builder.Services.AddAuthorization();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
    options.AddDocumentTransformer<BearerSecurityDocumentTransformer>();
});

builder.Services.AddSingleton<ProviderStatisticsStore>(services => new ProviderStatisticsStore(new[] { "open-meteo", "tmdb", "open-library" }, services.GetRequiredService<TimeProvider>()));
builder.Services.AddTransient<ProviderRetryHandler>();

builder.Services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>(client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
})
.AddHttpMessageHandler<ProviderRetryHandler>()
.AddHttpMessageHandler(services => new ProviderStatisticsHandler("open-meteo", services.GetRequiredService<ProviderStatisticsStore>()));

var tmdbAccessToken = builder.Configuration["Tmdb:AccessToken"];

if (string.IsNullOrWhiteSpace(tmdbAccessToken))
{
    throw new InvalidOperationException("TMDB access token is missing. Set Tmdb:AccessToken in User Secrets.");
}

builder.Services.AddHttpClient<IMovieService, TmdbMovieService>(client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tmdbAccessToken);
})
.AddHttpMessageHandler<ProviderRetryHandler>()
.AddHttpMessageHandler(services => new ProviderStatisticsHandler("tmdb", services.GetRequiredService<ProviderStatisticsStore>()));

builder.Services.AddHttpClient<IBookService, OpenLibraryBookService>(client =>
{
    client.BaseAddress = new Uri("https://openlibrary.org/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AgileActors.ApiAggregation/1.0");
}).AddHttpMessageHandler<ProviderRetryHandler>()
.AddHttpMessageHandler(services => new ProviderStatisticsHandler("open-library", services.GetRequiredService<ProviderStatisticsStore>()));

builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddScoped<IDashboardProvider, WeatherDashboardProvider>();
builder.Services.AddScoped<IDashboardProvider, MoviesDashboardProvider>();
builder.Services.AddScoped<IDashboardProvider, BooksDashboardProvider>();
builder.Services.AddSingleton<DashboardCache>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<DashboardFetchCoordinator>();
builder.Services.AddScoped<DashboardCacheService>();
builder.Services.AddHostedService<ProviderAnomalyMonitor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "API Aggregation v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization();

app.Run();

public partial class Program
{
}