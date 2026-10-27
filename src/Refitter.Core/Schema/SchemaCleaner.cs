using System.Text.RegularExpressions;
using NJsonSchema;
using NSwag;

namespace Refitter.Core;

/// <summary>
/// Cleans up the OpenAPI schema by removing unreferenced schemas and handling inheritance hierarchies.
/// </summary>

public class SchemaCleaner
{
    private readonly OpenApiDocument document;
    private readonly Regex[] keepSchemaRegexes;

    /// <summary>
    /// Gets or sets a value indicating whether to include inheritance hierarchy in the schema cleaning process.
    /// </summary>
    public bool IncludeInheritanceHierarchy { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaCleaner"/> class.
    /// </summary>
    /// <param name="document">The OpenAPI document to clean.</param>
    /// <param name="keepSchemaPatterns">Regular expression patterns for schemas to keep.</param>
    public SchemaCleaner(OpenApiDocument document, string[] keepSchemaPatterns)
    {
        this.document = document;
        keepSchemaRegexes = keepSchemaPatterns
            .Select(x => new Regex(x, RegexOptions.Compiled, TimeSpan.FromSeconds(1)))
            .ToArray();
    }

    /// <summary>
    /// Removes unreferenced schemas and discriminator mappings from the OpenAPI document.
    /// Mappings remain when their targets are otherwise reachable or inheritance inclusion is
    /// enabled.
    /// </summary>
    public void RemoveUnreferencedSchema()
    {
        var (usedJsonSchema, usage) = FindUsedJsonSchema(document);
        var unused = document.Components.Schemas.Where(s => !usage.Contains(s.Key))
            .ToArray();

        foreach (var unusedSchema in unused)
        {
            document.Components.Schemas.Remove(unusedSchema);
        }

        if (!IncludeInheritanceHierarchy)
        {
            // Fix any "abstract/sum" types so that the unused-types get removed
            foreach (var mapping in usedJsonSchema
                         .Select(schema => schema.DiscriminatorObject)
                         .OfType<OpenApiDiscriminator>()
                         .Select(discriminator => discriminator.Mapping))
            {
                var keepMappings = mapping
                    .Where(x => usedJsonSchema.Contains(x.Value.ActualSchema))
                    .ToArray();

                mapping.Clear();
                foreach (var kvp in keepMappings)
                {
                    mapping[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    private (IReadOnlyCollection<JsonSchema>, HashSet<string>) FindUsedJsonSchema(OpenApiDocument doc)
    {
        var schemaIdLookup = BuildSchemaIdLookup();
        var toProcess = CollectRootSchemas(doc);

        var seenIds = new HashSet<string>();
        var seen = new HashSet<JsonSchema>();
        while (toProcess.Count > 0)
        {
            var schema = toProcess.Pop();
            var actualSchema = schema.ActualSchema;
            if (!seen.Add(actualSchema))
            {
                continue;
            }

            if (schemaIdLookup.TryGetValue(actualSchema, out var refIds))
            {
                seenIds.UnionWith(refIds);
            }

            foreach (var subSchema in EnumerateSchema(actualSchema))
            {
                TryPush(subSchema, toProcess);
            }
        }

        return (seen, seenIds);
    }

    private Dictionary<JsonSchema, List<string>> BuildSchemaIdLookup()
    {
        var schemaIdLookup = new Dictionary<JsonSchema, List<string>>();
        foreach (var kvp in document.Components.Schemas)
        {
            var actualSchema = kvp.Value.ActualSchema;
            if (!schemaIdLookup.TryGetValue(actualSchema, out var aliases))
            {
                aliases = [];
                schemaIdLookup[actualSchema] = aliases;
            }

            aliases.Add(kvp.Key);
        }

        return schemaIdLookup;
    }

    private Stack<JsonSchema> CollectRootSchemas(OpenApiDocument doc)
    {
        var toProcess = new Stack<JsonSchema>();
        foreach (var kvp in doc.Components.Schemas.Where(kvp => keepSchemaRegexes.Any(x => x.IsMatch(kvp.Key))))
        {
            TryPush(kvp.Value, toProcess);
        }

        foreach (var schema in doc.Paths.Select(kvp => kvp.Value).SelectMany(GetSchemaForPath))
        {
            TryPush(schema, toProcess);
        }

        return toProcess;
    }

    private static IEnumerable<JsonSchema?> GetSchemaForPath(OpenApiPathItem pathItem)
    {
        foreach (var p in pathItem.Parameters)
        {
            yield return p;
        }

        foreach (var schema in pathItem.Values.SelectMany(GetSchemaForOperation))
        {
            yield return schema;
        }
    }

    private static IEnumerable<JsonSchema?> GetSchemaForOperation(OpenApiOperation op)
    {
        if (op.RequestBody != null)
        {
            var body = op.RequestBody;
            foreach (var content in body.Content.Select(kvpBody => kvpBody.Value))
            {
                yield return content.Schema;
            }
        }

        foreach (var p in op.ActualParameters)
        {
            yield return p;
        }

        foreach (var resp in op.ActualResponses.Select(x => x.Value))
        {
            foreach (var header in resp.Headers.Select(x => x.Value))
            {
                yield return header;
            }

            foreach (var mediaType in resp.Content.Select(x => x.Value))
            {
                yield return mediaType.Schema;
            }
        }
    }

    private static void TryPush(JsonSchema? schema, Stack<JsonSchema> stack)
    {
        if (schema == null)
        {
            return;
        }

        stack.Push(schema);
    }

    private IEnumerable<JsonSchema> EnumerateSchema(JsonSchema schema) =>
        EnumerateChildSchemas(schema)
            .Where(x => x != null)
            .Select(x => x!);

    private IEnumerable<JsonSchema?> EnumerateChildSchemas(JsonSchema schema)
    {
        var schemaElement = schema.ActualSchema;

        yield return schemaElement.AdditionalItemsSchema;
        yield return schemaElement.AdditionalPropertiesSchema;
        foreach (JsonSchema s in schemaElement.AllInheritedSchemas)
        {
            yield return s;
        }

        if (schemaElement.DictionaryKey != null)
        {
            yield return schemaElement.DictionaryKey;
        }

        if (schemaElement.Item != null)
        {
            yield return schemaElement.Item;
        }

        foreach (JsonSchema s in schemaElement.Items)
        {
            yield return s;
        }

        yield return schemaElement.Not;

        foreach (var subSchema in schemaElement.AllOf)
        {
            yield return subSchema;
        }

        if (schemaElement.DiscriminatorObject != null && IncludeInheritanceHierarchy)
        {
            // abstract type
            // if we let these out, we get a bunch of "AnonymousN"-classes
            foreach (var subSchema in schemaElement.DiscriminatorObject.Mapping)
            {
                yield return subSchema.Value;
            }
        }

        foreach (var subSchema in schemaElement.AnyOf)
        {
            yield return subSchema;
        }

        foreach (var subSchema in schemaElement.OneOf)
        {
            yield return subSchema;
        }

        foreach (var subSchema in schemaElement.ActualProperties.Select(kvp => kvp.Value))
        {
            yield return subSchema;
        }

        foreach (var subSchema in schemaElement.Definitions.Select(kvp => kvp.Value))
        {
            yield return subSchema;
        }
    }
}
