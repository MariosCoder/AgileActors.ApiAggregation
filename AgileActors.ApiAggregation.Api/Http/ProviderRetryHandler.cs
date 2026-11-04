using System.Net;

namespace AgileActors.ApiAggregation.Api.Http;

public sealed class ProviderRetryHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get || request.Content is not null)
        {
            return await base.SendAsync(request, cancellationToken);
        }

        const int maximumRetries = 2;

        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TimeSpan delay;

            using (var attemptRequest = CreateGetRequest(request))
            {
                try
                {
                    var response = await base.SendAsync(attemptRequest, cancellationToken);

                    if (!IsRetryableStatusCode(response.StatusCode) || attempt >= maximumRetries)
                    {
                        response.RequestMessage = request;
                        return response;
                    }

                    delay = GetRetryDelay(response, attempt + 1);
                    response.Dispose();
                }
                catch (HttpRequestException exception) when (attempt < maximumRetries && !cancellationToken.IsCancellationRequested && IsRetryableNetworkError(exception))
                {
                    delay = GetRetryDelay(null, attempt + 1);
                }
            }

            while (delay > TimeSpan.Zero)
            {
                var wait = delay > TimeSpan.FromDays(1) ? TimeSpan.FromDays(1) : delay;
                await Task.Delay(wait, cancellationToken);
                delay -= wait;
            }
        }
    }

    private static bool IsRetryableStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage? response, int retryNumber)
    {
        var retryAfter = response?.Headers.RetryAfter;

        if (retryAfter?.Delta is TimeSpan delta)
        {
            return delta > TimeSpan.Zero ? delta : TimeSpan.Zero;
        }

        if (retryAfter?.Date is DateTimeOffset retryDate)
        {
            var remaining = retryDate - DateTimeOffset.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        return TimeSpan.FromMilliseconds(200 * retryNumber + Random.Shared.Next(0, 101));
    }

    private static HttpRequestMessage CreateGetRequest(HttpRequestMessage original)
    {
        var copy = new HttpRequestMessage(HttpMethod.Get, original.RequestUri)
        {
            Version = original.Version,
            VersionPolicy = original.VersionPolicy
        };

        foreach (var header in original.Headers)
        {
            copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in original.Options)
        {
            copy.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        return copy;
    }

    private static bool IsRetryableNetworkError(HttpRequestException exception)
    {
        return exception.HttpRequestError is HttpRequestError.NameResolutionError
            or HttpRequestError.ConnectionError
            or HttpRequestError.ResponseEnded;
    }
}