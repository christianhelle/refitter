using System.Net;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal sealed class HttpDocumentStrategy : IFailureReportingStrategy
{
    private readonly HttpClient httpClient;
    private readonly ReferencePolicy policy;

    public HttpDocumentStrategy()
        : this(CreateDefaultHttpClient())
    {
    }

    public HttpDocumentStrategy(ReferencePolicy policy)
        : this(CreateDefaultHttpClient(), policy)
    {
    }

    public HttpDocumentStrategy(HttpClient httpClient)
        : this(httpClient, ReferencePolicy.Unrestricted)
    {
    }

    public HttpDocumentStrategy(HttpClient httpClient, ReferencePolicy policy)
    {
        this.httpClient = httpClient;
        this.policy = policy;
    }

    public async Task<OpenApiDocument?> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (!PathUtilities.IsHttp(path))
            return null;

        cancellationToken.ThrowIfCancellationRequested();

        using var response = await httpClient
            .GetAsync(path, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var content = await response.Content
            .ReadAsStringWithCancellationAsync(cancellationToken)
            .ConfigureAwait(false);

        return await OpenApiDocumentParser
            .ParseAsync(content, null, PathUtilities.IsYaml(path), policy, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<OpenApiDocument?> TryLoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await LoadAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException or TaskCanceledException)
                throw;

            return null;
        }
    }

    private static HttpClient CreateDefaultHttpClient()
    {
        var client = new HttpClient(
            new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        client.DefaultRequestHeaders.Add(
            "User-Agent",
            $"refitter/{typeof(HttpDocumentStrategy).Assembly.GetName().Version}");

        return client;
    }
}
