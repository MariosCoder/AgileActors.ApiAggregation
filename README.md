# API Aggregation

My take on the Agile Actors .NET assignment.

It brings together current weather from Open-Meteo, popular movies from TMDB,
and book searches from Open Library. The dashboard endpoint fetches the
selected providers concurrently and returns their results in separate sections.

There's no database. Cached data and request statistics live in memory and
reset when the app restarts.

## Running it

You'll need the .NET 10 SDK and a TMDB API Read Access Token.

1. Open `AgileActors.ApiAggregation.sln` in Visual Studio.
2. Right-click the API project and select **Manage User Secrets**.
3. Add your TMDB token:

```json
{
  "Tmdb": {
    "AccessToken": "YOUR_TMDB_READ_ACCESS_TOKEN"
  }
}
```

4. Set `AgileActors.ApiAggregation.Api` as the startup project.
5. Run it using the HTTPS profile.

Swagger is available at:

https://localhost:7254/swagger

The token belongs in User Secrets, not in `appsettings.json` or Git.
Open-Meteo and Open Library don't need credentials for these requests.

## Authentication

All API endpoints require a JWT bearer token, including statistics.
Swagger and the OpenAPI document remain accessible in Development.

The token for calling this API is separate from the TMDB token used by
the application to fetch movies.

For local testing, run this from the solution directory:

```bash
dotnet user-jwts create --project AgileActors.ApiAggregation.Api/AgileActors.ApiAggregation.Api.csproj --name interview-reviewer --audience https://localhost:7254 --valid-for 1h
```

This creates a development token valid for one hour. The tool configures
the development issuer and audience and stores the signing key through
User Secrets. Keep generated tokens and signing keys out of Git.

Restart the API after creating the initial token.

### Using Swagger

1. Open https://localhost:7254/swagger.
2. Click **Authorize**.
3. Paste the generated token without the `Bearer` prefix.
4. Click **Authorize**, then close the dialog.
5. Use **Try it out** on an endpoint.

### Using Postman

Set the request's Authorization type to **Bearer Token**, then paste the
generated token into the Token field.

Postman adds this header:

```http
Authorization: Bearer YOUR_TOKEN
```

The API checks the signature, issuer, audience, and validity period.
Missing or invalid tokens return HTTP 401. A 30-second clock allowance is
configured for token validity checks.

To check authentication without calling an external provider, use
`GET /api/statistics`. It should return 401 without a token and 200 with
a valid token.

When the token expires, generate another with the same command.

This setup is for running and reviewing the assignment locally. It doesn't
include a login endpoint or a user database. A deployed version would need
a production token issuer and corresponding validation configuration.

## Dashboard

```http
GET /api/dashboard?latitude=37.9838&longitude=23.7275&bookQuery=The%20Hobbit
```

By default, this requests all three providers.

| Parameter | What it does |
| --- | --- |
| `categories` | Optional category selection: `weather`, `movies`, or `books`. Repeat it to select more than one. |
| `latitude` | Required when weather is selected. Between -90 and 90. |
| `longitude` | Required when weather is selected. Between -180 and 180. |
| `bookQuery` | Required when books are selected. Up to 200 characters; cannot be blank. |
| `sortBy` | `providerOrder` by default, or `title`. |
| `sortDirection` | `asc` by default, or `desc`. Applies to title sorting. |

Category matching ignores case and surrounding spaces. Repeated categories
don't cause extra provider calls. Unknown categories and invalid inputs
return HTTP 400 with validation details.

A movies-only request doesn't need coordinates or a book query:

```http
GET /api/dashboard?categories=movies
```

Movies and books, with titles sorted from Z to A:

```http
GET /api/dashboard?categories=movies&categories=books&bookQuery=The%20Hobbit&sortBy=title&sortDirection=desc
```

Sorting happens separately within each movie or book list. It doesn't change
the weather section or the order of the sections themselves.

Each list returns at most three items. Sorting happens before that limit,
using the fetched batch: the first TMDB popular-movies page or up to ten
Open Library search results. It isn't a search across the providers' entire
catalogues.

Without title sorting, each list keeps the provider's original order.

### Response format

Here's an illustrative movies-only response, shortened to one item:

```json
{
  "status": "complete",
  "sections": [
    {
      "category": "movies",
      "provider": "tmdb",
      "status": "fresh",
      "fetchedAt": "2026-11-04T14:30:00+02:00",
      "data": [
        {
          "id": 123,
          "title": "Example Movie",
          "overview": "An example description.",
          "releaseDate": "2026-10-01",
          "rating": 7.5
        }
      ],
      "error": null
    }
  ]
}
```

The contents of `data` depend on the section:

| Category | Data |
| --- | --- |
| `weather` | An object with `temperatureCelsius`, `windSpeedKmh`, and `weatherCode`. |
| `movies` | A list containing `id`, `title`, `overview`, `releaseDate`, and `rating`. |
| `books` | A list containing `id`, `title`, `authors`, and `firstPublishYear`. |

Some provider fields, such as a movie's release date or a book's publication
year, can be `null`.

`fetchedAt` uses the Europe/Athens time zone, including daylight saving time.
Cached and stale responses keep the original fetch time.

### Statuses and failures

Each section has one of these statuses:

- `fresh`: fetched successfully during this request.
- `cached`: served from a fresh cache entry.
- `stale`: a refresh failed, so older cached data was used.
- `unavailable`: no usable data could be returned.

An unavailable section has `data: null`. A successful search with no matches
has `data: []`; that's a valid result.

A failed or stale section includes an error with a `code` and `message`.
The codes are `provider_timeout`, `provider_unavailable`, and `query_rejected`.

The overall dashboard status describes only the selected sections:

- `complete`: every section is fresh or cached.
- `partial`: some data is usable, but at least one section is stale or unavailable.
- `unavailable`: none of the selected sections has usable data.

A valid dashboard request returns HTTP 200 even when providers fail.
The section statuses describe those failures, allowing successful sections
to remain available.

## Individual endpoints

These are also available for trying each integration separately:

```http
GET /api/weather?latitude=37.9838&longitude=23.7275
GET /api/movies/popular
GET /api/books/search?query=The%20Hobbit
```

They return the weather object or movie/book arrays directly, without the
dashboard section wrapper. Movies and books are limited to three items.

These endpoints use the same retry and statistics handlers. Caching and
stale fallback belong to the dashboard flow.

If Open Library rejects a query, the individual books endpoint returns
HTTP 400. In the dashboard, this becomes an unavailable book section with
the `query_rejected` error code.

## Try it out

Start the API with the HTTPS profile and authorize in Swagger or Postman first.

The links below show complete request URLs using `https://localhost:7254`.
Copy a URL into an authorized Postman request, or enter its parameters in
Swagger. Clicking it directly in a browser won't attach the bearer token
and will normally return HTTP 401.

The expected results below assume a valid token. Successful provider
requests also depend on the external APIs being available.

Successful requests depend on the external providers being available.
The examples below describe the expected result when they respond normally.

### Basic requests

| Try it | What to expect |
| --- | --- |
| [Full dashboard for Athens](https://localhost:7254/api/dashboard?latitude=37.9838&longitude=23.7275&bookQuery=The%20Hobbit) | Weather, movies, and books. |
| [Weather only](https://localhost:7254/api/dashboard?categories=weather&latitude=37.9838&longitude=23.7275) | One weather section. No book query needed. |
| [Movies only](https://localhost:7254/api/dashboard?categories=movies) | One movie section with up to three items. No other inputs needed. |
| [Books only](https://localhost:7254/api/dashboard?categories=books&bookQuery=The%20Hobbit) | One book section with up to three items. No coordinates needed. |
| [Movies and books](https://localhost:7254/api/dashboard?categories=movies&categories=books&bookQuery=Dune) | Two sections. Repeating `categories` selects more than one. |
| [Weather and movies](https://localhost:7254/api/dashboard?categories=weather&categories=movies&latitude=37.9838&longitude=23.7275) | Two sections, without requesting books. |

### Sorting

Sorting applies separately to the movie and book lists. Each list is sorted
before taking its first three items.

| Try it | What to expect |
| --- | --- |
| [Movies A–Z](https://localhost:7254/api/dashboard?categories=movies&sortBy=title&sortDirection=asc) | Movies ordered by title, ascending. |
| [Movies Z–A](https://localhost:7254/api/dashboard?categories=movies&sortBy=title&sortDirection=desc) | Movies ordered by title, descending. |
| [Books A–Z](https://localhost:7254/api/dashboard?categories=books&bookQuery=The%20Hobbit&sortBy=title&sortDirection=asc) | Books ordered by title, ascending. |
| [Books Z–A](https://localhost:7254/api/dashboard?categories=books&bookQuery=The%20Hobbit&sortBy=title&sortDirection=desc) | Books ordered by title, descending. |
| [Movies and books A–Z](https://localhost:7254/api/dashboard?categories=movies&categories=books&bookQuery=Dune&sortBy=title) | Both lists sorted independently. Omitting direction defaults to ascending. |
| [Keep provider order](https://localhost:7254/api/dashboard?categories=movies&sortBy=providerOrder&sortDirection=desc) | Original provider order. Direction is ignored unless sorting by title. |

Ascending and descending requests can return different items. They sort the
whole fetched batch, rather than reversing an already limited three-item list.

### Category matching

| Try it | What to expect |
| --- | --- |
| [Uppercase category](https://localhost:7254/api/dashboard?categories=MOVIES) | Same selection as `movies`. |
| [Category with surrounding spaces](https://localhost:7254/api/dashboard?categories=%20movies%20) | Spaces are trimmed; movies are selected. |
| [Repeated category](https://localhost:7254/api/dashboard?categories=movies&categories=MOVIES) | One movie section, not two. |

`%20` is how a space is encoded in these URLs.

### Invalid inputs

These should return HTTP 400 with validation details, rather than a
dashboard response.

| Try it | What should be rejected |
| --- | --- |
| [Unknown category](https://localhost:7254/api/dashboard?categories=music) | `music` isn't a registered category. |
| [Weather without coordinates](https://localhost:7254/api/dashboard?categories=weather) | Both latitude and longitude are required. |
| [Weather without longitude](https://localhost:7254/api/dashboard?categories=weather&latitude=37.9838) | Longitude is required. |
| [Latitude outside its range](https://localhost:7254/api/dashboard?categories=weather&latitude=91&longitude=23.7275) | Latitude must be between -90 and 90. |
| [Longitude outside its range](https://localhost:7254/api/dashboard?categories=weather&latitude=37.9838&longitude=181) | Longitude must be between -180 and 180. |
| [Books without a query](https://localhost:7254/api/dashboard?categories=books) | A book query is required. |
| [Books with a blank query](https://localhost:7254/api/dashboard?categories=books&bookQuery=%20%20%20) | Spaces alone aren't a valid query. |
| [Unknown sorting option](https://localhost:7254/api/dashboard?categories=movies&sortBy=rating) | `rating` isn't a supported sorting option. |
| [Unknown sorting direction](https://localhost:7254/api/dashboard?categories=movies&sortBy=title&sortDirection=sideways) | Direction must be `asc` or `desc`. |
| [Undefined numeric sorting value](https://localhost:7254/api/dashboard?categories=movies&sortBy=99) | Numeric values outside the enum are rejected. |
| [Default dashboard without inputs](https://localhost:7254/api/dashboard) | All categories are selected by default, so coordinates and a book query are required. |

### Individual endpoints

These return provider data directly, without the dashboard wrapper.

- [Current weather in Athens](https://localhost:7254/api/weather?latitude=37.9838&longitude=23.7275)
- [Popular movies](https://localhost:7254/api/movies/popular)
- [Search books for The Hobbit](https://localhost:7254/api/books/search?query=The%20Hobbit)
- [Search books for Dune](https://localhost:7254/api/books/search?query=Dune)
- [Missing book query — expect 400](https://localhost:7254/api/books/search)
- [Invalid weather latitude — expect 400](https://localhost:7254/api/weather?latitude=91&longitude=23.7275)

### Check caching and statistics together

Restart the application first to clear its cache and counters. Send every
request below through authorized Swagger or Postman, including the requests
to the statistics endpoint.

1. Open [statistics](https://localhost:7254/api/statistics).
   All three providers should have zero requests and `null` averages.
2. Open the [full dashboard](https://localhost:7254/api/dashboard?latitude=37.9838&longitude=23.7275&bookQuery=The%20Hobbit).
   Successful sections should say `fresh`.
3. Refresh [statistics](https://localhost:7254/api/statistics).
   Each provider should have one recorded attempt if no retries were needed.
4. Repeat the same dashboard request immediately.
   Successfully cached sections should now say `cached`.
5. Refresh statistics again. Those cache hits shouldn't increase the counts.
6. Open [the same dashboard with title sorting](https://localhost:7254/api/dashboard?latitude=37.9838&longitude=23.7275&bookQuery=The%20Hobbit&sortBy=title&sortDirection=desc).
   Sorting should reuse the cached data, without adding provider requests.

The individual endpoints bypass the dashboard cache, so calling those can
increase the statistics even when dashboard data is already cached.

### Provider failures

There isn't a public URL parameter that forces a provider timeout or outage.
The unit tests simulate these conditions so they can be checked reliably.

Open Library has previously rejected the standalone query `The`. You can
compare its handling through the
[individual books endpoint](https://localhost:7254/api/books/search?query=The)
and the
[books-only dashboard](https://localhost:7254/api/dashboard?categories=books&bookQuery=The).

If the provider still rejects that query, the individual endpoint returns
HTTP 400. The dashboard returns HTTP 200 with an `unavailable` book section
and a `query_rejected` error. This example depends on the provider's current
behavior.

## Caching and retries

| Data | Fresh for | Maximum total age for fallback |
| --- | --- | --- |
| Weather | 5 minutes | 30 minutes |
| Movies | 15 minutes | 2 hours |
| Books | 15 minutes | 2 hours |

Only successful results are cached. The cache holds up to 500 entries and
may evict entries before their maximum age, so fallback isn't guaranteed.

Weather keys include the coordinates. Book keys include the trimmed,
case-preserved query. Movies use a fixed key for the selected popular page.

Sorting is applied after reading the cache. Different sorting options can
reuse the same cached batch without changing it.

Dashboard fetches use one semaphore per provider to prevent overlapping
cache refreshes for that provider. Fresh cache hits bypass it. Different
providers still run concurrently, but cache misses for different inputs
to the same provider are serialized. This is a deliberate simplification.

A failed refresh can use stale data for `provider_timeout` or
`provider_unavailable`, provided the entry hasn't exceeded its maximum age.
Rejected queries don't use stale fallback.

Outgoing bodyless GET requests allow up to two retries: three attempts total.

Retryable HTTP statuses are 408, 429, 500, 502, 503, and 504. Selected network
failures, including connection and name-resolution failures, are also retried.

The handler respects `Retry-After` when supplied. Otherwise, it waits
200–300 milliseconds before the first retry and 400–500 before the second.

The HTTP client's 10-second timeout covers attempts and retry delays.
Caller cancellation stops the operation.

Invalid JSON, missing required provider data, authentication failures,
and rejected queries aren't retried. Stale fallback is deliberately broader:
some failures that aren't worth retrying can still use older cached data.

## Request statistics

```http
GET /api/statistics
```

This returns an entry for each provider: `open-meteo`, `tmdb`, and `open-library`.

Each entry includes:

- `totalRequests`: completed attempts since startup.
- `averageResponseTimeMs`: their overall average duration.
- `recentRequestCount`: completed attempts recorded within the last five minutes.
- `recentAverageResponseTimeMs`: the average for those recent attempts.
- `buckets`: lifetime request counts and averages for each performance group.

The buckets are:

| Name | Duration |
| --- | --- |
| `fast` | Less than 100 ms |
| `average` | 100–200 ms, including both boundaries |
| `slow` | More than 200 ms |

An average is `null` when there are no observations.

Each completed outgoing attempt counts, including failures and attempts
cancelled after starting. Retries count separately. Dashboard cache hits
and requests to the statistics endpoint don't add provider requests.

Timing runs from sending an attempt until response headers arrive or the
attempt fails. It excludes retry delays, subsequent response-body reading,
and JSON parsing.

The handler order is:

```text
HTTP client → retry handler → statistics handler → network
```

The store uses a lock to protect updates and snapshot reads. Lifetime
statistics use counts and summed durations. Overall averages use total
duration divided by total requests, not an average of bucket averages.

For monitoring, the store also keeps a queue of recent timestamps and
durations. Samples at least five minutes old are removed when recording
or reading statistics. Expiring a recent sample doesn't change lifetime
totals.

Recent averages become `null` when no samples remain in the window.

Statistics are local to this application instance and reset on restart.

## Tests

Tests cover caching, stale fallback, overlapping cache requests, retry
limits, cancellation, category selection, input validation, sorting,
statistics calculations, and concurrent statistics updates.

Authentication integration tests run the actual API pipeline in an
in-memory test server. They use a separate test signing key and check
valid, missing, expired, unsigned, and otherwise invalid tokens.

Monitoring tests advance controlled clocks instead of waiting several
minutes. They check recent-window calculations, slowdown detection, and
the background worker's warning log and shutdown.

## Project structure

- `Controllers`: HTTP endpoints.
- `Providers`: external API calls and provider response models.
- `Services`: interfaces used by the provider implementations.
- `Dashboard`: aggregation, provider adapters, caching, and list sorting.
- `Http`: outgoing retry and statistics handlers.
- `Statistics`: the in-memory statistics store and background anomaly monitoring.
- `OpenApi`: bearer-token documentation used by Swagger.
- `Models`: request and response contracts.

To add another integration, implement its service and an `IDashboardProvider`
adapter, then register them in `Program.cs`. Give its HTTP client a statistics
handler and add its identifier to the statistics store registration.

Category selection uses the registered adapters' `Category` properties.
Provider-specific inputs, validation, and any list sorting need to be added
where appropriate.

## Background anomaly monitoring

A background service checks provider statistics once a minute. The first
check runs one minute after startup.

It logs a warning when a provider's average duration over the last five
minutes is more than 50% higher than its overall average:

recent average > overall average × 1.5

For example, an overall average of 100 ms and a recent average of 160 ms
triggers a warning. A recent average of exactly 150 ms doesn't.

The overall average includes recent attempts too. Comparisons are skipped
when there are no recent attempts, an average is missing, or the overall
average is zero.

Warnings include the provider, both averages, and their request counts.
The warning repeats once per check while the condition remains true.
Logs appear through the application's configured logging providers, such
as the console and Visual Studio's Debug output.

The monitor only reads existing statistics. It doesn't poll external APIs
or add provider requests. Its snapshot reads also clear expired recent
samples when no new requests are coming in.

A startup information message confirms that the monitor's timer is ready.
An anomaly warning isn't expected immediately: when all observations are
recent, the recent and overall averages are the same.

Tests use controlled clocks and simulated observations to check window
expiry, the threshold, timer execution, warning logging, and clean shutdown.

## Data sources and credits

- Weather: [Open-Meteo](https://open-meteo.com/).
  Weather data is provided under
  [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).
  This API selects and renames fields from the provider response.
- Movies: [The Movie Database (TMDB)](https://www.themoviedb.org/).
- Books: [Open Library](https://openlibrary.org/).

<a href="https://www.themoviedb.org/">
  <img src="docs/tmdb-logo.svg" alt="TMDB" width="120">
</a>

This product uses the TMDB API but is not endorsed or certified by TMDB.

Provider documentation:
[Open-Meteo](https://open-meteo.com/en/docs),
[TMDB](https://developer.themoviedb.org/docs/getting-started),
and [Open Library](https://openlibrary.org/developers/api).