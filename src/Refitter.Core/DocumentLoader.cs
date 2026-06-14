using System.Diagnostics.CodeAnalysis;
using System.Net;
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

    public async Task<OpenApiDocument> LoadAsync(string openApiPath)
    {
        try
        {
            var readResult = await OpenApiMultiFileReader.Read(openApiPath).ConfigureAwait(false);
            if (!readResult.ContainedExternalReferences)
                return await CreateUsingNSwagAsync(openApiPath).ConfigureAwait(false);

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
        catch (Exception)
        {
            return await CreateUsingNSwagAsync(openApiPath).ConfigureAwait(false);
        }
    }

    private static async Task<OpenApiDocument> CreateUsingNSwagAsync(string openApiPath)
    {
        if (IsHttp(openApiPath))
        {
            var content = await GetHttpContent(openApiPath).ConfigureAwait(false);
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

    private static Task<string> GetHttpContent(string openApiPath)
        => HttpClient.GetStringAsync(openApiPath);

    private static bool IsYaml(string path)
    {
        return path.EndsWith("yaml", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("yml", StringComparison.OrdinalIgnoreCase);
    }
}
