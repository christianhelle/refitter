using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Refitter.Core;

/// <summary>
/// Decides whether two parts of different documents (paths, schemas, security schemes) describe the same thing,
/// by comparing their JSON with the properties of every object sorted.
/// </summary>
internal sealed class DocumentEquivalenceComparer
{
    /// <summary>
    /// Determines whether two values are equivalent by comparing their canonical representations.
    /// </summary>
    public bool AreEquivalent<TValue>(TValue existingValue, TValue incomingValue)
    {
        if (ReferenceEquals(existingValue, incomingValue) ||
            EqualityComparer<TValue>.Default.Equals(existingValue, incomingValue))
        {
            return true;
        }

        try
        {
            return string.Equals(
                CreateCanonicalJson(existingValue!),
                CreateCanonicalJson(incomingValue!),
                StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates the canonical JSON of a value: the JSON of a document containing it, with sorted properties.
    /// Schemas that cannot be written in a document of their own are described by their main keywords.
    /// </summary>
    public string CreateCanonicalJson(object value)
    {
        try
        {
            using var json = JsonDocument.Parse(CreateOpenApiJson(value));
            var builder = new StringBuilder();
            WriteCanonical(builder, json.RootElement);
            return builder.ToString();
        }
        catch when (value is ApiSchema schema)
        {
            return CreateCanonicalSchemaJson(schema, new HashSet<ApiSchema>());
        }
    }

    internal string CreateOpenApiJson(object value)
    {
        var document = new ApiDocument
        {
            Info = new ApiInfo { Title = "Refitter equivalence comparison", Version = "1.0" },
        };

        switch (value)
        {
            case ApiDocument apiDocument:
                return ApiDocumentWriter.Write(apiDocument);
            case ApiSchema schema:
                document.Definitions["Schema"] = schema;
                AddReferencedSchemas(document.Definitions, schema);
                break;
            case ApiPathItem pathItem:
                document.AddPath("/_", pathItem);
                break;
            case ApiSecurityScheme securityScheme:
                document.SecurityDefinitions["SecurityScheme"] = securityScheme;
                break;
            default:
                return JsonSerializer.Serialize(value);
        }

        return ApiDocumentWriter.Write(document);
    }

    /// <summary>
    /// Adds a schema and the schemas it (transitively) references to the definitions, named after their reference.
    /// </summary>
    internal void AddReferencedSchemas(IDictionary<string, ApiSchema> definitions, ApiSchema schema)
    {
        var visited = new HashSet<ApiSchema>();
        var schemasToProcess = new Stack<ApiSchema>();
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

    internal static string? GetDefinitionName(ApiSchema schema)
    {
        var referencePath = schema.ReferencePath;
        if (string.IsNullOrWhiteSpace(referencePath))
            return null;

        var separatorIndex = referencePath!.LastIndexOf('/');
        return separatorIndex >= 0 && separatorIndex < referencePath.Length - 1
            ? Uri.UnescapeDataString(referencePath.Substring(separatorIndex + 1))
            : null;
    }

    private static IEnumerable<ApiSchema?> EnumerateTraversableSchemas(ApiSchema schema)
    {
        yield return schema.AdditionalItemsSchema;
        yield return schema.AdditionalPropertiesSchema;
        yield return schema.DictionaryKey;
        yield return schema.Item;

        foreach (var item in schema.Items)
            yield return item;

        yield return schema.Not;

        foreach (var property in schema.Properties.Values)
            yield return property;

        foreach (var subSchema in schema.AllOf)
            yield return subSchema;

        foreach (var subSchema in schema.OneOf)
            yield return subSchema;

        foreach (var subSchema in schema.AnyOf)
            yield return subSchema;

        foreach (var definition in schema.Definitions.Values)
            yield return definition;
    }

    internal string CreateCanonicalSchemaJson(ApiSchema schema, ISet<ApiSchema> visited)
    {
        if (schema.Reference != null)
        {
            return visited.Contains(schema.Reference)
                ? "{\"$ref\":\"#\"}"
                : CreateCanonicalSchemaJson(schema.Reference, new HashSet<ApiSchema>(visited));
        }

        var actualSchema = schema.ActualSchema;
        if (!visited.Add(actualSchema))
            return "{\"$ref\":\"#\"}";

        var properties = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["type"] = Quote(actualSchema.Type.ToString()),
            ["allowAdditionalProperties"] = actualSchema.AllowAdditionalProperties ? "true" : "false",
        };

        AddString(properties, "format", actualSchema.Format);
        AddString(properties, "title", actualSchema.Title);
        AddString(properties, "description", actualSchema.Description);
        if (actualSchema.IsNullableRaw.HasValue)
            properties["nullable"] = actualSchema.IsNullableRaw.Value ? "true" : "false";

        AddSchema(properties, "additionalProperties", actualSchema.AdditionalPropertiesSchema, visited);
        AddSchema(properties, "items", actualSchema.Item, visited);
        AddSchemaArray(properties, "allOf", actualSchema.AllOf, visited);
        AddSchemaArray(properties, "oneOf", actualSchema.OneOf, visited);
        AddSchemaArray(properties, "anyOf", actualSchema.AnyOf, visited);

        if (actualSchema.RequiredProperties.Count > 0)
        {
            properties["required"] = "[" + string.Join(
                ",",
                actualSchema.RequiredProperties.OrderBy(name => name, StringComparer.Ordinal).Select(Quote)) + "]";
        }

        if (actualSchema.Properties.Count > 0)
        {
            properties["properties"] = "{" + string.Join(
                ",",
                actualSchema.Properties
                    .OrderBy(property => property.Key, StringComparer.Ordinal)
                    .Select(property => Quote(property.Key) + ":" +
                                        CreateCanonicalSchemaJson(property.Value, new HashSet<ApiSchema>(visited)))) + "}";
        }

        if (actualSchema.Enumeration.Count > 0)
        {
            properties["enum"] = "[" + string.Join(
                ",",
                actualSchema.Enumeration
                    .OrderBy(value => value?.ToString() ?? string.Empty, StringComparer.Ordinal)
                    .Select(CanonicalRawJson)) + "]";
        }

        if (actualSchema.ExtensionData is { Count: > 0 })
        {
            properties["extensions"] = "{" + string.Join(
                ",",
                actualSchema.ExtensionData
                    .OrderBy(extension => extension.Key, StringComparer.Ordinal)
                    .Select(extension => Quote(extension.Key) + ":" + CanonicalRawJson(extension.Value))) + "}";
        }

        return "{" + string.Join(",", properties.Select(p => Quote(p.Key) + ":" + p.Value)) + "}";
    }

    private void AddSchema(SortedDictionary<string, string> properties, string name, ApiSchema? schema, ISet<ApiSchema> visited)
    {
        if (schema != null)
            properties[name] = CreateCanonicalSchemaJson(schema, new HashSet<ApiSchema>(visited));
    }

    private void AddSchemaArray(SortedDictionary<string, string> properties, string name, IEnumerable<ApiSchema> schemas, ISet<ApiSchema> visited)
    {
        var items = schemas
            .Select(schema => CreateCanonicalSchemaJson(schema, new HashSet<ApiSchema>(visited)))
            .OrderBy(json => json, StringComparer.Ordinal)
            .ToList();

        if (items.Count > 0)
            properties[name] = "[" + string.Join(",", items) + "]";
    }

    private static void AddString(SortedDictionary<string, string> properties, string name, string? value)
    {
        if (value != null)
            properties[name] = Quote(value);
    }

    private static string CanonicalRawJson(object? value)
    {
        var text = value is null or RawJsonObject or RawJsonArray or string or bool or DateTime or IFormattable
            ? RawJson.ToIndentedString(value)
            : JsonSerializer.Serialize(value);
        using var json = JsonDocument.Parse(text);
        var builder = new StringBuilder();
        WriteCanonical(builder, json.RootElement);
        return builder.ToString();
    }

    private static string Quote(string value) => JsonSerializer.Serialize(value);

    private static void WriteCanonical(StringBuilder builder, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                builder.Append('{');
                var first = true;
                foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    if (!first)
                        builder.Append(',');
                    first = false;
                    builder.Append(Quote(property.Name)).Append(':');
                    WriteCanonical(builder, property.Value);
                }

                builder.Append('}');
                break;
            case JsonValueKind.Array:
                builder.Append('[');
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    if (index++ > 0)
                        builder.Append(',');
                    WriteCanonical(builder, item);
                }

                builder.Append(']');
                break;
            case JsonValueKind.Number:
                builder.Append(
                    element.TryGetDecimal(out var number)
                        ? number.ToString("G29", CultureInfo.InvariantCulture)
                        : element.GetRawText());
                break;
            default:
                builder.Append(element.GetRawText());
                break;
        }
    }
}
