using System.Net;
using NSwag;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal sealed class HttpDocumentStrategy : IDocumentLoadingStrategy
{
    private readonly HttpClient httpClient;

    public HttpDocumentStrategy()
        : this(CreateDefaultHttpClient())
    {
    }

    public HttpDocumentStrategy(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<OpenApiDocument?> TryLoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (!PathUtilities.IsHttp(path))
            return null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var response = await httpClient
                .GetAsync(path, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var content = await response.Content
                .ReadAsStringAsync()
                .ConfigureAwait(false);

            return PathUtilities.IsYaml(path)
                ? await OpenApiYamlDocument.FromYamlAsync(content, cancellationToken)
                    .ConfigureAwait(false)
                : await OpenApiDocument.FromJsonAsync(content, cancellationToken)
                    .ConfigureAwait(false);
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
