using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
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
        var baseDocument = OpenApiDocument.FromJsonAsync(documents[0].ToJson(documents[0].SchemaType)).GetAwaiter().GetResult();
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
                MergeIfMissingOrThrowOnConflict(baseDocument.Paths, path.Key, path.Value, "path");
            }

            if (document.Components?.Schemas != null)
            {
                foreach (var schema in document.Components.Schemas)
                {
                    MergeIfMissingOrThrowOnConflict(baseDocument.Components.Schemas, schema.Key, schema.Value, "schema");
                }
            }

            if (document.Definitions != null)
            {
                foreach (var definition in document.Definitions)
                {
                    MergeIfMissingOrThrowOnConflict(baseDocument.Definitions, definition.Key, definition.Value, "definition");
                }
            }

            if (document.SecurityDefinitions != null)
            {
                foreach (var securityDefinition in document.SecurityDefinitions)
                {
                    MergeIfMissingOrThrowOnConflict(baseDocument.SecurityDefinitions, securityDefinition.Key, securityDefinition.Value, "security scheme");
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

    private static void MergeIfMissingOrThrowOnConflict<TValue>(
        IDictionary<string, TValue> target,
        string key,
        TValue value,
        string itemType)
    {
        if (!target.TryGetValue(key, out var existingValue))
        {
            target[key] = value;
            return;
        }

        if (!AreEquivalent(existingValue, value))
            throw CreateMergeConflictException(itemType, key);
    }

    private static bool AreEquivalent<TValue>(TValue existingValue, TValue incomingValue)
    {
        if (ReferenceEquals(existingValue, incomingValue) || EqualityComparer<TValue>.Default.Equals(existingValue, incomingValue))
            return true;

        try
        {
            return JToken.DeepEquals(
                CreateCanonicalJsonToken(existingValue!),
                CreateCanonicalJsonToken(incomingValue!));
        }
        catch
        {
            return false;
        }
    }

    private static JToken CreateCanonicalJsonToken(object value)
    {
        try
        {
            return NormalizeJsonToken(JToken.Parse(CreateOpenApiJson(value)));
        }
        catch when (value is JsonSchema schema)
        {
            return CreateCanonicalSchemaToken(schema, new HashSet<JsonSchema>(JsonSchemaReferenceComparer.Instance));
        }
    }

    private static string CreateOpenApiJson(object value)
        => value switch
        {
            OpenApiDocument document => document.ToJson(),
            JsonSchema schema => CreateDocumentWithSchema(schema).ToJson(),
            NSwag.OpenApiPathItem pathItem => CreateDocumentWithPath(pathItem).ToJson(),
            NSwag.OpenApiSecurityScheme securityScheme => CreateDocumentWithSecurityScheme(securityScheme).ToJson(),
            _ => JsonConvert.SerializeObject(value, Formatting.None)
        };

    private static OpenApiDocument CreateDocumentWithSchema(JsonSchema schema)
    {
        var document = CreateSerializationDocument();
        document.Definitions["Schema"] = schema;
        AddReferencedSchemas(document.Definitions, schema);
        return document;
    }

    private static void AddReferencedSchemas(IDictionary<string, JsonSchema> definitions, JsonSchema schema)
    {
        var visited = new HashSet<JsonSchema>(JsonSchemaReferenceComparer.Instance);
        var schemasToProcess = new Stack<JsonSchema>();
        schemasToProcess.Push(schema);

        while (schemasToProcess.Count > 0)
        {
            var schemaToProcess = schemasToProcess.Pop();
            var actualSchema = schemaToProcess.ActualSchema;
            if (!visited.Add(actualSchema))
                continue;

            var definitionName = GetDefinitionName(schemaToProcess) ?? GetDefinitionName(actualSchema);
            if (definitionName != null && !definitions.ContainsKey(definitionName))
                definitions.Add(definitionName, actualSchema);

            foreach (var childSchema in EnumerateTraversableSchemas(actualSchema))
            {
                if (childSchema != null)
                    schemasToProcess.Push(childSchema);
            }
        }
    }

    private static string? GetDefinitionName(JsonSchema schema)
    {
        var referencePath = ((NJsonSchema.References.IJsonReferenceBase)schema).ReferencePath;
        if (string.IsNullOrWhiteSpace(referencePath))
            return null;

        var separatorIndex = referencePath!.LastIndexOf('/');
        return separatorIndex >= 0 && separatorIndex < referencePath.Length - 1
            ? Uri.UnescapeDataString(referencePath.Substring(separatorIndex + 1))
            : null;
    }

    private static IEnumerable<JsonSchema?> EnumerateTraversableSchemas(JsonSchema schema)
    {
        yield return schema.AdditionalItemsSchema;
        yield return schema.AdditionalPropertiesSchema;
        yield return schema.DictionaryKey;
        yield return schema.Item;

        if (schema.Items != null)
        {
            foreach (var item in schema.Items)
            {
                yield return item;
            }
        }

        yield return schema.Not;

        foreach (var property in schema.Properties.Values)
        {
            yield return property;
        }

        foreach (var subSchema in schema.AllOf)
        {
            yield return subSchema;
        }

        foreach (var subSchema in schema.OneOf)
        {
            yield return subSchema;
        }

        foreach (var subSchema in schema.AnyOf)
        {
            yield return subSchema;
        }

        foreach (var definition in schema.Definitions.Values)
        {
            yield return definition;
        }
    }

    private static OpenApiDocument CreateDocumentWithPath(NSwag.OpenApiPathItem pathItem)
    {
        var document = CreateSerializationDocument();
        document.Paths["/_"] = pathItem;
        return document;
    }

    private static OpenApiDocument CreateDocumentWithSecurityScheme(NSwag.OpenApiSecurityScheme securityScheme)
    {
        var document = CreateSerializationDocument();
        document.SecurityDefinitions["SecurityScheme"] = securityScheme;
        return document;
    }

    private static OpenApiDocument CreateSerializationDocument()
        => new()
        {
            Info =
            {
                Title = "Refitter equivalence comparison",
                Version = "1.0"
            }
        };

    private static JToken NormalizeJsonToken(JToken token)
        => token switch
        {
            JObject jsonObject => new JObject(
                jsonObject
                    .Properties()
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
                    .Select(property => new JProperty(property.Name, NormalizeJsonToken(property.Value)))),
            JArray jsonArray => new JArray(jsonArray.Select(NormalizeJsonToken)),
            _ => token.DeepClone()
        };

    private static JToken CreateCanonicalSchemaToken(JsonSchema schema, ISet<JsonSchema> visited)
    {
        if (schema.Reference != null)
            return CreateCanonicalSchemaReferenceToken(schema.Reference, visited);

        var actualSchema = schema.ActualSchema;
        if (!visited.Add(actualSchema))
            return new JObject { ["$ref"] = "#" };

        var json = new JObject
        {
            ["type"] = actualSchema.Type.ToString(),
            ["format"] = actualSchema.Format,
            ["title"] = actualSchema.Title,
            ["description"] = actualSchema.Description,
            ["nullable"] = actualSchema.IsNullableRaw,
            ["allowAdditionalProperties"] = actualSchema.AllowAdditionalProperties
        };

        AddSchemaToken(json, "additionalProperties", actualSchema.AdditionalPropertiesSchema, visited);
        AddSchemaToken(json, "items", actualSchema.Item, visited);
        AddSchemaArray(json, "allOf", actualSchema.AllOf, visited);
        AddSchemaArray(json, "oneOf", actualSchema.OneOf, visited);
        AddSchemaArray(json, "anyOf", actualSchema.AnyOf, visited);

        if (actualSchema.RequiredProperties.Count > 0)
            json["required"] = new JArray(actualSchema.RequiredProperties.OrderBy(name => name, StringComparer.Ordinal));

        if (actualSchema.Properties.Count > 0)
        {
            json["properties"] = new JObject(
                actualSchema.Properties
                    .OrderBy(property => property.Key, StringComparer.Ordinal)
                    .Select(property => new JProperty(
                        property.Key,
                        CreateCanonicalSchemaToken(property.Value, new HashSet<JsonSchema>(visited, JsonSchemaReferenceComparer.Instance)))));
        }

        if (actualSchema.Enumeration.Count > 0)
            json["enum"] = new JArray(actualSchema.Enumeration.Select(value => value != null ? JToken.FromObject(value) : JValue.CreateNull()));

        if (actualSchema.ExtensionData is { Count: > 0 })
        {
            json["extensions"] = new JObject(
                actualSchema.ExtensionData
                    .OrderBy(extension => extension.Key, StringComparer.Ordinal)
                    .Select(extension => new JProperty(
                        extension.Key,
                        extension.Value != null ? NormalizeJsonToken(JToken.FromObject(extension.Value)) : JValue.CreateNull())));
        }

        return RemoveNullProperties(json);
    }

    private static JToken CreateCanonicalSchemaReferenceToken(JsonSchema reference, ISet<JsonSchema> visited) =>
        visited.Contains(reference)
            ? new JObject { ["$ref"] = "#" }
            : CreateCanonicalSchemaToken(reference, new HashSet<JsonSchema>(visited, JsonSchemaReferenceComparer.Instance));

    private static void AddSchemaToken(JObject json, string propertyName, JsonSchema? schema, ISet<JsonSchema> visited)
    {
        if (schema != null)
            json[propertyName] = CreateCanonicalSchemaToken(schema, new HashSet<JsonSchema>(visited, JsonSchemaReferenceComparer.Instance));
    }

    private static void AddSchemaArray(JObject json, string propertyName, IEnumerable<JsonSchema> schemas, ISet<JsonSchema> visited)
    {
        var items = schemas
            .Select(schema => CreateCanonicalSchemaToken(schema, new HashSet<JsonSchema>(visited, JsonSchemaReferenceComparer.Instance)))
            .ToArray();

        if (items.Length > 0)
            json[propertyName] = new JArray(items);
    }

    private static JObject RemoveNullProperties(JObject json)
    {
        foreach (var property in json.Properties().Where(property => property.Value.Type == JTokenType.Null).ToArray())
        {
            property.Remove();
        }

        return json;
    }

    private sealed class JsonSchemaReferenceComparer : IEqualityComparer<JsonSchema>
    {
        public static JsonSchemaReferenceComparer Instance { get; } = new();

        public bool Equals(JsonSchema? x, JsonSchema? y) =>
            ReferenceEquals(x, y);

        public int GetHashCode(JsonSchema obj) =>
            RuntimeHelpers.GetHashCode(obj);
    }

    private static InvalidOperationException CreateMergeConflictException(string itemType, string key) =>
        new($"Cannot merge OpenAPI documents because a duplicate {itemType} '{key}' was found. Refitter fails fast on merge collisions to avoid silent data loss.");

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
