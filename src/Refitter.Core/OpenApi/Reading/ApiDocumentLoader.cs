using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

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
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    public static ApiDocument LoadFile(string path) =>
        Load(File.ReadAllText(path), path, PathUtilities.IsYaml(path));

    public static ApiDocument Load(string content, string? documentPath, bool isYaml)
    {
        var json = PrepareJson(content, isYaml);
        var schemaType = DetectSchemaType(json);

        using var jsonDocument = JsonDocument.Parse(InlinePathItemReferences(json), JsonOptions);
        var document = new ApiJsonReader(schemaType).ReadDocument(jsonDocument.RootElement);
        document.DocumentPath = documentPath;

        new ApiReferenceResolver(document, DownloadText).Resolve();
        return document;
    }

    /// <summary>Converts YAML to JSON, and escapes the control characters that JSON strings cannot contain.</summary>
    internal static string PrepareJson(string content, bool isYaml) =>
        EscapeControlCharactersInStrings(isYaml ? ConvertYamlToJson(content) : content);

    /// <summary>Converts YAML to JSON. Every scalar becomes a string (null stays null).</summary>
    internal static string ConvertYamlToJson(string yaml)
    {
        var yamlObject = new DeserializerBuilder().Build().Deserialize(new StringReader(yaml));
        return new SerializerBuilder().JsonCompatible().Build().Serialize(yamlObject!);
    }

    /// <summary>
    /// Escapes control characters (e.g. tabs) that appear unescaped in JSON strings, which documents have
    /// always been allowed to contain.
    /// </summary>
    internal static string EscapeControlCharactersInStrings(string json)
    {
        StringBuilder? builder = null;
        var inString = false;
        var escaped = false;
        for (var i = 0; i < json.Length; i++)
        {
            var c = json[i];
            if (inString && !escaped && c < ' ')
            {
                builder ??= new StringBuilder(json.Length + 16).Append(json, 0, i);
                builder.Append("\\u").Append(((int)c).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                continue;
            }

            if (inString)
            {
                if (escaped)
                    escaped = false;
                else if (c == '\\')
                    escaped = true;
                else if (c == '"')
                    inString = false;
            }
            else if (c == '"')
            {
                inString = true;
            }

            builder?.Append(c);
        }

        return builder?.ToString() ?? json;
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
