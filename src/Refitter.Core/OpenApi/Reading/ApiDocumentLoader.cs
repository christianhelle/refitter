using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>Loads OpenAPI and Swagger documents (JSON or YAML) into an <see cref="ApiDocument"/>.</summary>
internal static class ApiDocumentLoader
{
    private const string ComponentsPathItemsPrefix = "#/components/pathItems/";

    internal static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
        MaxDepth = 256,
    };

    private static readonly Regex SchemaTypeRegex = new(
        "(?:\\\"(?<schemaType>openapi|swagger)\\\")(?:\\s*:\\s*)(?:\\\"(?<schemaVersion>[^\"]*)\\\")",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static ApiDocument LoadFile(string path) =>
        Load(File.ReadAllText(path), path, PathUtilities.IsYaml(path));

    public static ApiDocument Load(string content, string? documentPath, bool isYaml)
    {
        var json = isYaml ? OpenApiDocumentParser.ConvertYamlToJson(content) : content;
        var schemaType = DetectSchemaType(json);

        using var jsonDocument = JsonDocument.Parse(InlinePathItemReferences(json), JsonOptions);
        var document = new ApiJsonReader(schemaType).ReadDocument(jsonDocument.RootElement);
        document.DocumentPath = documentPath;

        new ApiReferenceResolver(document, DownloadText).Resolve();
        return document;
    }

    /// <summary>Detects the specification from the version field, and falls back to Swagger 2.0.</summary>
    internal static ApiSchemaType DetectSchemaType(string json)
    {
        var match = SchemaTypeRegex.Match(json);
        if (match.Success)
        {
            var schemaType = match.Groups["schemaType"].Value.ToLowerInvariant();
            var version = match.Groups["schemaVersion"].Value.ToLowerInvariant();
            if (schemaType == "swagger" && version.StartsWith("2", StringComparison.Ordinal))
                return ApiSchemaType.Swagger2;

            if (schemaType == "openapi" && version.StartsWith("3", StringComparison.Ordinal))
                return ApiSchemaType.OpenApi3;
        }

        return ApiSchemaType.Swagger2;
    }

    /// <summary>
    /// Replaces path items that reference OpenAPI 3.1 <c>components/pathItems</c> with the referenced path item.
    /// Fields next to the <c>$ref</c> (e.g. summary) override the referenced ones (#1274).
    /// </summary>
    private static string InlinePathItemReferences(string json)
    {
        if (json.IndexOf("pathItems", StringComparison.Ordinal) < 0)
            return json;

        var nodeOptions = new JsonNodeOptions { PropertyNameCaseInsensitive = false };
        if (JsonNode.Parse(json, nodeOptions, JsonOptions) is not JsonObject root ||
            root["paths"] is not JsonObject paths ||
            root["components"]?["pathItems"] is not JsonObject pathItems)
        {
            return json;
        }

        var inlined = false;
        foreach (var path in paths.ToList())
        {
            if (path.Value is JsonObject pathItem &&
                TryResolvePathItem(pathItem, pathItems, new HashSet<string>(StringComparer.Ordinal), out var resolved))
            {
                paths[path.Key] = resolved;
                inlined = true;
            }
        }

        return inlined ? root.ToJsonString() : json;
    }

    private static bool TryResolvePathItem(
        JsonObject pathItem,
        JsonObject pathItems,
        HashSet<string> visited,
        out JsonObject resolved)
    {
        resolved = pathItem;
        var reference = pathItem["$ref"] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
        if (reference is null || !reference.StartsWith(ComponentsPathItemsPrefix, StringComparison.Ordinal))
            return false;

        var name = reference
            .Substring(ComponentsPathItemsPrefix.Length)
            .Replace("~1", "/")
            .Replace("~0", "~");

        if (!visited.Add(name) || pathItems[name] is not JsonObject target)
            return false;

        var targetResolved = target;
        if (target["$ref"] is not null && !TryResolvePathItem(target, pathItems, visited, out targetResolved))
            return false;

        resolved = (JsonObject)targetResolved.DeepClone();
        foreach (var sibling in pathItem.Where(p => p.Key != "$ref").ToList())
        {
            resolved[sibling.Key] = sibling.Value?.DeepClone();
        }

        return true;
    }

    private static string DownloadText(string url)
    {
        using var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        return client.GetStringAsync(url).GetAwaiter().GetResult();
    }
}
