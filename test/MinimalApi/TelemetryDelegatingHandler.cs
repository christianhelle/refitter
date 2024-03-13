namespace Petstore;

class TelemetryDelegatingHandler : DelegatingHandler
{
    private readonly ILogger logger;

    public TelemetryDelegatingHandler(ILogger<TelemetryDelegatingHandler> logger)
    {
        this.logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Outbound HTTP Request - {Method} {Uri}", request.Method, request.RequestUri);
        return base.SendAsync(request, cancellationToken);
    }
}
