using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using NSwag;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal sealed class DocumentLoader : IDocumentLoader
{
    private static readonly HttpClient HttpClient = new(
        new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    static DocumentLoader()
    {
        HttpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            $"refitter/{typeof(DocumentLoader).Assembly.GetName().Version}");
    }

    public async Task<OpenApiDocument> LoadAsync(
        string openApiPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(openApiPath))
            throw new ArgumentException("The openApiPath parameter cannot be null, empty, or contain only whitespace.", nameof(openApiPath));

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var readResult = await OpenApiMultiFileReader
                .Read(openApiPath, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!readResult.ContainedExternalReferences)
                return await CreateUsingNSwagAsync(openApiPath, cancellationToken).ConfigureAwait(false);

            var specificationVersion = readResult.OpenApiDiagnostic.SpecificationVersion;
            PopulateMissingRequiredFields(openApiPath, readResult);

            if (IsYaml(openApiPath))
            {
                var yaml = await readResult.OpenApiDocument.SerializeAsYamlAsync(specificationVersion).ConfigureAwait(false);
                return await OpenApiYamlDocument.FromYamlAsync(yaml).ConfigureAwait(false);
            }

            var json = await readResult.OpenApiDocument.SerializeAsJsonAsync(specificationVersion).ConfigureAwait(false);
            return await OpenApiDocument.FromJsonAsync(json).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ex is not TaskCanceledException)
        {
            return await CreateUsingNSwagAsync(openApiPath, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<OpenApiDocument> CreateUsingNSwagAsync(
        string openApiPath,
        CancellationToken cancellationToken = default)
    {
        if (IsHttp(openApiPath))
        {
            var content = await GetHttpContent(openApiPath, cancellationToken).ConfigureAwait(false);
            return IsYaml(openApiPath)
                ? await OpenApiYamlDocument.FromYamlAsync(content).ConfigureAwait(false)
                : await OpenApiDocument.FromJsonAsync(content).ConfigureAwait(false);
        }

        return IsYaml(openApiPath)
            ? await OpenApiYamlDocument.FromFileAsync(openApiPath).ConfigureAwait(false)
            : await OpenApiDocument.FromFileAsync(openApiPath).ConfigureAwait(false);
    }

    [ExcludeFromCodeCoverage]
    private static void PopulateMissingRequiredFields(
        string openApiPath,
        Result readResult)
    {
        var document = readResult.OpenApiDocument;
        if (document.Info is null)
        {
            document.Info = new Microsoft.OpenApi.OpenApiInfo
            {
                Title = Path.GetFileNameWithoutExtension(openApiPath),
                Version = readResult.OpenApiDiagnostic.SpecificationVersion.GetDisplayName()
            };
        }
        else
        {
            document.Info.Title ??= Path.GetFileNameWithoutExtension(openApiPath);
            document.Info.Version ??= readResult.OpenApiDiagnostic.SpecificationVersion.GetDisplayName();
        }
    }

    private static bool IsHttp(string path)
    {
        return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> GetHttpContent(
        string openApiPath,
        CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.GetAsync(openApiPath, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    private static bool IsYaml(string path)
    {
        var queryIndex = path.IndexOf('?');
        var fragmentIndex = path.IndexOf('#');
        var endIndex = path.Length;

        if (queryIndex >= 0)
            endIndex = Math.Min(endIndex, queryIndex);
        if (fragmentIndex >= 0)
            endIndex = Math.Min(endIndex, fragmentIndex);

        var basePath = endIndex < path.Length ? path.Substring(0, endIndex) : path;

        return basePath.EndsWith("yaml", StringComparison.OrdinalIgnoreCase) ||
               basePath.EndsWith("yml", StringComparison.OrdinalIgnoreCase);
    }
}
