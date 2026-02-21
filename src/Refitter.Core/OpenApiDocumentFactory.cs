using System.Net;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Readers;
using NSwag;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

/// <summary>
/// Creates an <see cref="NSwag.OpenApiDocument"/> from a specified path or URL.
/// </summary>
public static class OpenApiDocumentFactory
{
    /// <summary>
    /// Creates a merged <see cref="NSwag.OpenApiDocument"/> from multiple paths or URLs.
    /// The first document serves as the base; paths and schemas from subsequent documents are merged in.
    /// </summary>
    /// <param name="openApiPaths">The paths or URLs to the OpenAPI specifications.</param>
    /// <returns>A merged <see cref="NSwag.OpenApiDocument"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="openApiPaths"/> is null or empty.</exception>
    public static async Task<OpenApiDocument> CreateAsync(IEnumerable<string> openApiPaths)
    {
        var paths = openApiPaths?.ToArray() ?? throw new ArgumentException("At least one OpenAPI path must be specified.", nameof(openApiPaths));
        if (paths.Length == 0)
            throw new ArgumentException("At least one OpenAPI path must be specified.", nameof(openApiPaths));

        if (paths.Length == 1)
            return await CreateAsync(paths[0]);

        var documents = new OpenApiDocument[paths.Length];
        for (var i = 0; i < paths.Length; i++)
            documents[i] = await CreateAsync(paths[i]);

        return Merge(documents);
    }

    private static OpenApiDocument Merge(OpenApiDocument[] documents)
    {
        var baseDocument = documents[0];
        foreach (var document in documents.Skip(1))
        {
            foreach (var path in document.Paths)
            {
                if (!baseDocument.Paths.ContainsKey(path.Key))
                    baseDocument.Paths[path.Key] = path.Value;
            }

            if (document.Components?.Schemas != null)
            {
                foreach (var schema in document.Components.Schemas)
                {
                    if (baseDocument.Components?.Schemas != null &&
                        !baseDocument.Components.Schemas.ContainsKey(schema.Key))
                        baseDocument.Components.Schemas[schema.Key] = schema.Value;
                }
            }

            if (document.Definitions != null)
            {
                foreach (var definition in document.Definitions)
                {
                    if (!baseDocument.Definitions.ContainsKey(definition.Key))
                        baseDocument.Definitions[definition.Key] = definition.Value;
                }
            }

            if (document.Tags != null)
            {
                baseDocument.Tags ??= [];
                foreach (var tag in document.Tags)
                {
                    if (baseDocument.Tags.All(t => t.Name != tag.Name))
                        baseDocument.Tags.Add(tag);
                }
            }
        }

        return baseDocument;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="NSwag.OpenApiDocument"/> class asynchronously.
    /// </summary>
    /// <param name="openApiPath">The path or URL to the OpenAPI specification.</param>
    /// <returns>A new instance of the <see cref="NSwag.OpenApiDocument"/> class.</returns>
    public static async Task<OpenApiDocument> CreateAsync(string openApiPath)
    {
        try
        {
            var readResult = await OpenApiMultiFileReader.Read(openApiPath);
            if (!readResult.ContainedExternalReferences)
                return await CreateUsingNSwagAsync(openApiPath);

            var specificationVersion = readResult.OpenApiDiagnostic.SpecificationVersion;
            PopulateMissingRequiredFields(openApiPath, readResult);

            if (IsYaml(openApiPath))
            {
                var yaml = readResult.OpenApiDocument.SerializeAsYaml(specificationVersion);
                return await OpenApiYamlDocument.FromYamlAsync(yaml);
            }

            var json = readResult.OpenApiDocument.SerializeAsJson(specificationVersion);
            return await OpenApiDocument.FromJsonAsync(json);
        }
        catch (Exception)
        {
            // Fallback to NSwag if OpenApiMultiFileReader fails (e.g., for files without external references)
            return await CreateUsingNSwagAsync(openApiPath);
        }
    }

    private static async Task<OpenApiDocument> CreateUsingNSwagAsync(string openApiPath)
    {
        if (IsHttp(openApiPath))
        {
            var content = await GetHttpContent(openApiPath);
            return IsYaml(openApiPath)
                ? await OpenApiYamlDocument.FromYamlAsync(content)
                : await OpenApiDocument.FromJsonAsync(content);
        }

        return IsYaml(openApiPath)
            ? await OpenApiYamlDocument.FromFileAsync(openApiPath)
            : await OpenApiDocument.FromFileAsync(openApiPath);
    }

    private static void PopulateMissingRequiredFields(
        string openApiPath,
        Result readResult)
    {
        var document = readResult.OpenApiDocument;
        if (document.Info is null)
        {
            document.Info = new Microsoft.OpenApi.Models.OpenApiInfo
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
    /// Gets the content of the URI as a string and decompresses it if necessary. 
    /// </summary>
    /// <param name="openApiPath">The path to the OpenAPI document.</param>
    /// <returns>The content of the HTTP request.</returns>
    private static async Task<string> GetHttpContent(string openApiPath)
    {
        var httpMessageHandler = new HttpClientHandler();
        httpMessageHandler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
        using var http = new HttpClient(httpMessageHandler);
        var content = await http.GetStringAsync(openApiPath);
        return content;
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
