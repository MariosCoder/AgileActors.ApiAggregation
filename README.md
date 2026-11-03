# API Aggregation

My take on the Agile Actors .NET assignment.

The idea is to fetch data from three external APIs and return it through
one endpoint, with filtering, sorting, caching, and request statistics.

## Running it

You'll need the .NET 10 SDK.

Open `AgileActors.ApiAggregation.sln` in Visual Studio and run the API project.

No database setup is needed. The cache and statistics will be kept in
memory and reset when the app restarts.

TMDB requires an API Read Access Token. Right-click the API project in
Visual Studio, select Manage User Secrets, and add:

```json
{
  "Tmdb": {
    "AccessToken": "YOUR_TMDB_READ_ACCESS_TOKEN"
  }
}

## Progress

The three provider integrations are working:

- Open-Meteo for current weather.
- TMDB for popular movies.
- Open Library for book searches.

You can try them separately through Swagger:

- GET /api/weather?latitude=37.9838&longitude=23.7275
- GET /api/movies/popular
- GET /api/books/search?query=The%20Hobbit

The combined dashboard endpoint is next. Caching, filtering, sorting,
error handling, request statistics, and automated tests are still pending.
JWT authentication and background performance monitoring are also planned.

If Open Library rejects a search query, the books endpoint returns
400 Bad Request with a message suggesting a more specific title or author.