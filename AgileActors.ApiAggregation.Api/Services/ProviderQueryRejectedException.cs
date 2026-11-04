namespace AgileActors.ApiAggregation.Api.Services;

public sealed class ProviderQueryRejectedException : Exception
{
    public ProviderQueryRejectedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}