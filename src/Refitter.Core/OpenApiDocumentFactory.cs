using System.Net;

using NSwag;

namespace Refitter.Core;

/// <summary>
/// Creates an <see cref="OpenApiDocument"/> from a specified path or URL.
/// </summary>
public static class OpenApiDocumentFactory
{
    /// <summary>
    /// Creates a new instance of the <see cref="OpenApiDocument"/> class asynchronously.
    /// </summary>
    /// <param name="settings">The settings used to configure the generator.</param>
    /// <returns>A new instance of the <see cref="OpenApiDocument"/> class.</returns>
    public static async Task<OpenApiDocument> CreateAsync(RefitGeneratorSettings settings)
    {
        OpenApiDocument document;
        if (IsHttp(settings.OpenApiPath))
        {
            var content = await GetHttpContent(settings);

            if (IsYaml(settings.OpenApiPath))
            {
                document = await OpenApiYamlDocument.FromYamlAsync(content);
            }
            else
            {
                document = await OpenApiDocument.FromJsonAsync(content);
            }
        }
        else 
        {
            if (IsYaml(settings.OpenApiPath))
            {
                document = await OpenApiYamlDocument.FromFileAsync(settings.OpenApiPath);
            }
            else
            {
                document = await OpenApiDocument.FromFileAsync(settings.OpenApiPath);
            }
        }

        return document;
    }

    /// <summary>
    /// Gets the content of the URI as a string and decompresses it if necessary. 
    /// </summary>
    /// <param name="settings">The settings used to configure the generator.</param>
    /// <returns>The content of the HTTP request.</returns>
    private static async Task<string> GetHttpContent(RefitGeneratorSettings settings)
    {
        var httpMessageHandler = new HttpClientHandler();
        httpMessageHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        using var http = new HttpClient(httpMessageHandler);
        var content = await http.GetStringAsync(settings.OpenApiPath);
        return content;
    }

    /// <summary>
    /// Determines whether the specified path is an HTTP URL.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is an HTTP URL, otherwise false.</returns>
    private static bool IsHttp(string path)
    {
        return path.StartsWith("http://") || path.StartsWith("https://");
    }

    /// <summary>
    /// Determines whether the specified path is a YAML file.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is a YAML file, otherwise false.</returns>
    private static bool IsYaml(string path)
    {
        return path.EndsWith("yaml") || path.EndsWith("yml");
    }
}