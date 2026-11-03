using AgileActors.ApiAggregation.Api.Providers.OpenMeteo;
using AgileActors.ApiAggregation.Api.Services;
using System.Net.Http.Headers;
using AgileActors.ApiAggregation.Api.Providers.Tmdb;
using AgileActors.ApiAggregation.Api.Providers.OpenLibrary;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
});

builder.Services.AddHttpClient<IWeatherService, OpenMeteoWeatherService>(client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

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
});

builder.Services.AddHttpClient<IBookService, OpenLibraryBookService>(client =>
{
    client.BaseAddress = new Uri("https://openlibrary.org/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AgileActors.ApiAggregation/1.0");
});

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

app.UseAuthorization();

app.MapControllers();

app.Run();
