# Agile Actors API Aggregation

ASP.NET Core API aggregation interview assignment.

## Current status

Initial controller-based .NET 10 project scaffold. Aggregation, caching, statistics,
error handling, and unit tests have not been implemented yet.

## Development setup

Requires the .NET 10 SDK and Visual Studio with ASP.NET and web development support.

Open `AgileActors.ApiAggregation.sln` in Visual Studio, or run:

```powershell
dotnet build
dotnet run --project AgileActors.ApiAggregation.Api --launch-profile http
```

Use the listening URL printed in the console. The template endpoint is
`GET /weatherforecast`; the OpenAPI document is at `/openapi/v1.json` in Development.
The weather endpoint returns generated sample data, not an external provider.

## Storage

No database connection is required. The planned statistics store and cache will
use application memory, so their contents will reset when the process restarts.
