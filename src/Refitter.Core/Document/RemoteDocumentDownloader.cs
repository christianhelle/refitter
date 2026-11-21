using System.Net;
using System.Text;

namespace Refitter.Core;

/// <summary>
/// Downloads the content of a remote reference for the reference resolvers. The resolvers download the content
/// themselves, instead of letting NJsonSchema do it, so the content can be checked before any parser reads
/// it, and so a response of unbounded size is never buffered.
/// </summary>
internal static class RemoteDocumentDownloader
{
    /// <summary>
    /// The largest response accepted. Real OpenAPI documents are a few megabytes at most.
    /// </summary>
    internal const long MaxResponseBytes = 50L * 1024 * 1024;

    private static readonly Lazy<HttpClient> SharedClient = new(CreateHttpClient);

    internal static async Task<string> DownloadAsync(
        string url,
        long maxBytes = MaxResponseBytes,
        CancellationToken cancellationToken = default)
    {
        using var response = await SharedClient.Value
            .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.ContentLength > maxBytes)
            throw TooLarge(url, maxBytes);

        using var buffer = new MemoryStream();
        using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
        {
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, 0, chunk.Length, cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (buffer.Length + read > maxBytes)
                    throw TooLarge(url, maxBytes);

                buffer.Write(chunk, 0, read);
            }
        }

        buffer.Position = 0;
        using var reader = new StreamReader(buffer, GetEncoding(response), detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static ReferenceResolutionException TooLarge(string url, long maxBytes) =>
        new($"Remote reference \"{url}\" is larger than the supported limit of {maxBytes} bytes and is rejected.");

    private static Encoding GetEncoding(HttpResponseMessage response)
    {
        var charSet = response.Content.Headers.ContentType?.CharSet?.Trim('"', ' ');
        if (string.IsNullOrEmpty(charSet))
            return Encoding.UTF8;

        try
        {
            return Encoding.GetEncoding(charSet);
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }

    private static HttpClient CreateHttpClient()
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
            $"refitter/{typeof(RemoteDocumentDownloader).Assembly.GetName().Version}");

        return client;
    }
}
