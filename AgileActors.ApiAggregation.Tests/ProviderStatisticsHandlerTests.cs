using System.Net;
using System.Net.Http.Headers;
using AgileActors.ApiAggregation.Api.Http;
using AgileActors.ApiAggregation.Api.Statistics;

namespace AgileActors.ApiAggregation.Tests;

public sealed class ProviderStatisticsHandlerTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task SendAsync_WhenResponseArrives_RecordsOneAttemptAndPreservesResponse(HttpStatusCode statusCode)
    {
        var store = new ProviderStatisticsStore(new[] { "tmdb" });
        var expectedResponse = new HttpResponseMessage(statusCode);

        var stub = new StubHttpMessageHandler((request, cancellationToken) => Task.FromResult(expectedResponse));

        var handler = new ProviderStatisticsHandler("tmdb", store)
        {
            InnerHandler = stub
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/test");
        using var response = await invoker.SendAsync(request, CancellationToken.None);

        var statistics = Assert.Single(store.GetSnapshot());

        Assert.Same(expectedResponse, response);
        Assert.Equal(1, stub.CallCount);
        Assert.Equal(1L, statistics.TotalRequests);
        Assert.Equal(1L, statistics.Buckets.Sum(bucket => bucket.RequestCount));
        Assert.NotNull(statistics.AverageResponseTimeMs);
        Assert.True(statistics.AverageResponseTimeMs!.Value >= 0);
    }

    [Fact]
    public async Task SendAsync_WhenConnectionFails_RecordsAttemptAndPreservesException()
    {
        var store = new ProviderStatisticsStore(new[] { "tmdb" });
        var expectedException = new HttpRequestException(HttpRequestError.ConnectionError, "Simulated connection failure.");

        var stub = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            throw expectedException;
        });

        var handler = new ProviderStatisticsHandler("tmdb", store)
        {
            InnerHandler = stub
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/test");

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => invoker.SendAsync(request, CancellationToken.None));

        Assert.Same(expectedException, exception);
        Assert.Equal(1, stub.CallCount);
        Assert.Equal(1L, Assert.Single(store.GetSnapshot()).TotalRequests);
    }

    [Fact]
    public async Task SendAsync_WhenAlreadyCancelled_DoesNotSendOrRecordAttempt()
    {
        var store = new ProviderStatisticsStore(new[] { "tmdb" });

        var stub = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var handler = new ProviderStatisticsHandler("tmdb", store)
        {
            InnerHandler = stub
        };

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/test");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invoker.SendAsync(request, cancellation.Token));

        Assert.Equal(0, stub.CallCount);
        Assert.Equal(0L, Assert.Single(store.GetSnapshot()).TotalRequests);
    }

    [Fact]
    public async Task SendAsync_WhenCancelledAfterAttemptStarts_RecordsAttempt()
    {
        var store = new ProviderStatisticsStore(new[] { "tmdb" });
        using var cancellation = new CancellationTokenSource();

        var stub = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            cancellation.Cancel();

            return Task.FromCanceled<HttpResponseMessage>(cancellationToken);
        });

        var handler = new ProviderStatisticsHandler("tmdb", store)
        {
            InnerHandler = stub
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/test");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invoker.SendAsync(request, cancellation.Token));

        Assert.Equal(1, stub.CallCount);
        Assert.Equal(1L, Assert.Single(store.GetSnapshot()).TotalRequests);
    }

    [Theory]
    [InlineData(true, HttpStatusCode.OK)]
    [InlineData(false, HttpStatusCode.ServiceUnavailable)]
    public async Task SendAsync_WhenRetriesOccur_RecordsEveryAttempt(bool recovers, HttpStatusCode expectedStatus)
    {
        var store = new ProviderStatisticsStore(new[] { "tmdb" });
        var attemptCount = 0;

        var stub = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            attemptCount++;

            var statusCode = recovers && attemptCount == 3 ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;
            var response = new HttpResponseMessage(statusCode);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);

            return Task.FromResult(response);
        });

        var statisticsHandler = new ProviderStatisticsHandler("tmdb", store)
        {
            InnerHandler = stub
        };

        var retryHandler = new ProviderRetryHandler
        {
            InnerHandler = statisticsHandler
        };

        using var client = new HttpClient(retryHandler);
        using var response = await client.GetAsync("https://example.com/test").WaitAsync(TimeSpan.FromSeconds(5));

        var statistics = Assert.Single(store.GetSnapshot());

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(3, stub.CallCount);
        Assert.Equal(3L, statistics.TotalRequests);
        Assert.Equal(3L, statistics.Buckets.Sum(bucket => bucket.RequestCount));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;

        public int CallCount { get; private set; }

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        {
            _send = send;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;

            return _send(request, cancellationToken);
        }
    }
}