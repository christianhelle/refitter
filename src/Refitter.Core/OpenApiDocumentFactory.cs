using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Readers;

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
    /// <param name="openApiPath">The path or URL to the OpenAPI specification.</param>
    /// <returns>A new instance of the <see cref="OpenApiDocument"/> class.</returns>
    public static async Task<OpenApiDocument> CreateAsync(string openApiPath)
    {
        var readResult = await OpenApiMultiFileReader.Read(openApiPath);
        var specificationVersion = readResult.OpenApiDiagnostic.SpecificationVersion;

        if (IsYaml(openApiPath))
        {
            var yaml = readResult.OpenApiDocument.SerializeAsYaml(specificationVersion);
            return await OpenApiYamlDocument.FromYamlAsync(yaml);
        }

        var json = readResult.OpenApiDocument.SerializeAsJson(specificationVersion);
        return await OpenApiDocument.FromJsonAsync(json);
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