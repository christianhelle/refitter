using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using NSwag;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

/// <summary>
/// Creates an <see cref="NSwag.OpenApiDocument"/> from a specified path or URL.
/// </summary>
public static class OpenApiDocumentFactory
{
    private static readonly HttpClient HttpClient = new(
        new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    static OpenApiDocumentFactory()
    {
        HttpClient.DefaultRequestHeaders.Add("User-Agent", $"refitter/{typeof(OpenApiDocumentFactory).Assembly.GetName().Version}");
    }

    /// <summary>
    /// Creates a merged <see cref="NSwag.OpenApiDocument"/> from multiple paths or URLs.
    /// The first document serves as the base; paths and schemas from subsequent documents are merged in.
    /// </summary>
    /// <param name="openApiPaths">The paths or URLs to the OpenAPI specifications.</param>
    /// <returns>A merged <see cref="NSwag.OpenApiDocument"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="openApiPaths"/> is null or empty.</exception>
    public static async Task<OpenApiDocument> CreateAsync(IEnumerable<string> openApiPaths)
    {
        if (openApiPaths == null)
            throw new ArgumentNullException(nameof(openApiPaths));

        var paths = openApiPaths.ToArray();
        if (paths.Length == 0)
            throw new ArgumentException("At least one OpenAPI path must be specified.", nameof(openApiPaths));

        if (paths.Length == 1)
            return await CreateAsync(paths[0]).ConfigureAwait(false);

        var documents = new OpenApiDocument[paths.Length];
        for (var i = 0; i < paths.Length; i++)
            documents[i] = await CreateAsync(paths[i]).ConfigureAwait(false);

        return Merge(documents);
    }

    private static OpenApiDocument Merge(OpenApiDocument[] documents)
    {
        var baseDocument = documents[0];
        var tags = baseDocument.Tags;
        HashSet<string>? tagNames = null;

        if (tags != null)
        {
            tagNames = new HashSet<string>(tags.Select(t => t.Name), StringComparer.Ordinal);
        }

        for (var i = 1; i < documents.Length; i++)
        {
            var document = documents[i];
            foreach (var path in document.Paths)
            {
                if (!baseDocument.Paths.ContainsKey(path.Key))
                    baseDocument.Paths[path.Key] = path.Value;
            }

            if (document.Components?.Schemas != null)
            {
                // Ensure base document has schemas dictionary initialized (#1016)
                // Components property is read-only but auto-initialized by NSwag
                foreach (var schema in document.Components.Schemas)
                {
                    if (!baseDocument.Components.Schemas.ContainsKey(schema.Key))
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
                tagNames ??= new HashSet<string>(baseDocument.Tags.Select(t => t.Name), StringComparer.Ordinal);
                foreach (var tag in document.Tags)
                {
                    if (tagNames.Add(tag.Name))
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
            // Fallback to NSwag if OpenApiMultiFileReader fails (e.g., for files without external references)
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

    /// <summary>
    /// Determines whether the specified path is an HTTP URL.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is an HTTP URL, otherwise false.</returns>
    private static bool IsHttp(string path)
    {
        return path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the content of the URI as a string and decompresses it if necessary. 
    /// </summary>
    /// <param name="openApiPath">The path to the OpenAPI document.</param>
    /// <returns>The content of the HTTP request.</returns>
    private static Task<string> GetHttpContent(string openApiPath)
        => HttpClient.GetStringAsync(openApiPath);


    /// <summary>
    /// Determines whether the specified path is a YAML file.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is a YAML file, otherwise false.</returns>
    private static bool IsYaml(string path)
    {
        return path.EndsWith("yaml", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith("yml", StringComparison.OrdinalIgnoreCase);
    }
}
