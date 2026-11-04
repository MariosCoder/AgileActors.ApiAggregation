using System.Net;
using AgileActors.ApiAggregation.Api.Http;

namespace AgileActors.ApiAggregation.Tests;

public sealed class ProviderRetryHandlerTests
{
    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task SendAsync_WhenRetryableStatusThenSuccess_RetriesOnce(HttpStatusCode retryableStatus)
    {
        var responseNumber = 0;

        var stub = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            responseNumber++;

            var statusCode = responseNumber == 1 ? retryableStatus : HttpStatusCode.OK;
            var response = new HttpResponseMessage(statusCode);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);

            return Task.FromResult(response);
        });

        var retryHandler = new ProviderRetryHandler
        {
            InnerHandler = stub
        };

        using var client = new HttpClient(retryHandler);
        using var result = await client.GetAsync("https://example.com/test");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, stub.CallCount);
    }

    [Fact]
    public async Task SendAsync_WhenServiceRemainsUnavailable_StopsAfterThreeAttempts()
    {
        var stub = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);

            return Task.FromResult(response);
        });

        var retryHandler = new ProviderRetryHandler
        {
            InnerHandler = stub
        };

        using var client = new HttpClient(retryHandler);
        using var result = await client.GetAsync("https://example.com/test");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        Assert.Equal(3, stub.CallCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task SendAsync_WhenStatusIsNotRetryable_ReturnsWithoutRetrying(HttpStatusCode statusCode)
    {
        var stub = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            var response = new HttpResponseMessage(statusCode);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);

            return Task.FromResult(response);
        });

        var retryHandler = new ProviderRetryHandler
        {
            InnerHandler = stub
        };

        using var client = new HttpClient(retryHandler);
        using var result = await client.GetAsync("https://example.com/test");

        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(1, stub.CallCount);
    }

    [Fact]
    public async Task SendAsync_WhenConnectionFailsThenSucceeds_RetriesOnce()
    {
        var attemptCount = 0;

        var stub = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            attemptCount++;

            if (attemptCount == 1)
            {
                throw new HttpRequestException(HttpRequestError.ConnectionError, "Simulated connection failure.");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var retryHandler = new ProviderRetryHandler
        {
            InnerHandler = stub
        };

        using var client = new HttpClient(retryHandler);
        using var result = await client.GetAsync("https://example.com/test");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, stub.CallCount);
    }

    [Fact]
    public async Task SendAsync_WhenCancelledDuringRetryDelay_DoesNotRetry()
    {
        using var cancellation = new CancellationTokenSource();

        var stub = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMinutes(1));

            return Task.FromResult(response);
        });

        var retryHandler = new ProviderRetryHandler
        {
            InnerHandler = stub
        };

        using var client = new HttpClient(retryHandler);

        var pendingRequest = client.GetAsync("https://example.com/test", cancellation.Token);

        Assert.False(pendingRequest.IsCompleted);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pendingRequest.WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.Equal(1, stub.CallCount);
    }

    [Fact]
    public async Task SendAsync_WhenTimeoutExpiresDuringRetryDelay_DoesNotRetry()
    {
        var stub = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMinutes(1));

            return Task.FromResult(response);
        });

        var retryHandler = new ProviderRetryHandler
        {
            InnerHandler = stub
        };

        using var client = new HttpClient(retryHandler)
        {
            Timeout = TimeSpan.FromMilliseconds(200)
        };

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync("https://example.com/test").WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.IsType<TimeoutException>(exception.InnerException);
        Assert.Equal(1, stub.CallCount);
    }

    [Fact]
    public async Task SendAsync_WhenConnectionKeepsFailing_StopsAfterThreeAttempts()
    {
        var stub = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            throw new HttpRequestException(HttpRequestError.ConnectionError, "Simulated connection failure.");
        });

        var retryHandler = new ProviderRetryHandler
        {
            InnerHandler = stub
        };

        using var client = new HttpClient(retryHandler);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("https://example.com/test").WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.Equal(HttpRequestError.ConnectionError, exception.HttpRequestError);
        Assert.Equal(3, stub.CallCount);
    }

    [Theory]
    [InlineData(HttpRequestError.SecureConnectionError)]
    [InlineData(HttpRequestError.UserAuthenticationError)]
    [InlineData(HttpRequestError.InvalidResponse)]
    [InlineData(HttpRequestError.Unknown)]
    public async Task SendAsync_WhenNetworkErrorIsNotRetryable_ThrowsWithoutRetrying(HttpRequestError error)
    {
        var stub = new StubHttpMessageHandler((request, cancellationToken) =>
        {
            throw new HttpRequestException(error, "Simulated non-retryable failure.");
        });

        var retryHandler = new ProviderRetryHandler
        {
            InnerHandler = stub
        };

        using var client = new HttpClient(retryHandler);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("https://example.com/test").WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.Equal(error, exception.HttpRequestError);
        Assert.Equal(1, stub.CallCount);
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