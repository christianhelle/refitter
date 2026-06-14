using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NSwag;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal sealed class DocumentEquivalenceComparer
{
    public bool AreEquivalent<TValue>(TValue existingValue, TValue incomingValue)
    {
        if (ReferenceEquals(existingValue, incomingValue) ||
            EqualityComparer<TValue>.Default.Equals(existingValue, incomingValue))
        {
            return true;
        }

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

    public JToken CreateCanonicalJsonToken(object value)
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

    public JToken NormalizeJsonToken(JToken token)
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

    public JToken CreateCanonicalSchemaToken(JsonSchema schema, ISet<JsonSchema> visited)
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

    public JObject RemoveNullProperties(JObject json)
    {
        foreach (var property in json.Properties().Where(property => property.Value.Type == JTokenType.Null).ToArray())
        {
            property.Remove();
        }

        return json;
    }

    public string CreateOpenApiJson(object value)
        => value switch
        {
            OpenApiDocument document => document.ToJson(),
            JsonSchema schema => CreateDocumentWithSchema(schema).ToJson(),
            NSwag.OpenApiPathItem pathItem => CreateDocumentWithPath(pathItem).ToJson(),
            NSwag.OpenApiSecurityScheme securityScheme => CreateDocumentWithSecurityScheme(securityScheme).ToJson(),
            _ => JsonConvert.SerializeObject(value, Formatting.None)
        };

    public void AddReferencedSchemas(IDictionary<string, JsonSchema> definitions, JsonSchema schema)
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

    public string? GetDefinitionName(JsonSchema schema)
    {
        var referencePath = ((NJsonSchema.References.IJsonReferenceBase)schema).ReferencePath;
        if (string.IsNullOrWhiteSpace(referencePath))
            return null;

        var separatorIndex = referencePath!.LastIndexOf('/');
        return separatorIndex >= 0 && separatorIndex < referencePath.Length - 1
            ? Uri.UnescapeDataString(referencePath.Substring(separatorIndex + 1))
            : null;
    }

    private JToken CreateCanonicalSchemaReferenceToken(JsonSchema reference, ISet<JsonSchema> visited) =>
        visited.Contains(reference)
            ? new JObject { ["$ref"] = "#" }
            : CreateCanonicalSchemaToken(reference, new HashSet<JsonSchema>(visited, JsonSchemaReferenceComparer.Instance));

    private void AddSchemaToken(JObject json, string propertyName, JsonSchema? schema, ISet<JsonSchema> visited)
    {
        if (schema != null)
            json[propertyName] = CreateCanonicalSchemaToken(schema, new HashSet<JsonSchema>(visited, JsonSchemaReferenceComparer.Instance));
    }

    private void AddSchemaArray(JObject json, string propertyName, IEnumerable<JsonSchema> schemas, ISet<JsonSchema> visited)
    {
        var items = schemas
            .Select(schema => CreateCanonicalSchemaToken(schema, new HashSet<JsonSchema>(visited, JsonSchemaReferenceComparer.Instance)))
            .ToArray();

        if (items.Length > 0)
            json[propertyName] = new JArray(items);
    }

    private OpenApiDocument CreateDocumentWithSchema(JsonSchema schema)
    {
        var document = CreateSerializationDocument();
        document.Definitions["Schema"] = schema;
        AddReferencedSchemas(document.Definitions, schema);
        return document;
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
}
