using System.Text.RegularExpressions;
using NJsonSchema;
using NSwag;

namespace Refitter.Core;

/// <summary>
/// Cleans up OpenAPI schema by removing unreferenced schemas and handling inheritance hierarchies.
/// </summary>
public class SchemaCleaner
{
    private readonly OpenApiDocument document;
    private readonly string[] keepSchemaPatterns;

    /// <summary>
    /// Gets or sets a value indicating whether to include inheritance hierarchy in the schema cleaning process.
    /// </summary>
    public bool IncludeInheritanceHierarchy { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaCleaner"/> class.
    /// </summary>
    /// <param name="document">The OpenAPI document to clean.</param>
    /// <param name="keepSchemaPatterns">Regular expression patterns for schemas to keep.</param>
    public SchemaCleaner(OpenApiDocument document, string[] keepSchemaPatterns)
    {
        this.document = document;
        this.keepSchemaPatterns = keepSchemaPatterns;
    }

    /// <summary>
    /// Removes unreferenced schemas from the OpenAPI document.
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
            foreach (var schema in usedJsonSchema)
            {
                // Fix any "abstract/sum" types so that the unused-types get removed
                if (schema.DiscriminatorObject != null)
                {
                    var mappings = schema.DiscriminatorObject.Mapping;
                    var keepMappings = mappings.Where(x =>
                            (x.Value.ActualSchema.Id ?? x.Value.Id) is { } id && usage.Contains(id))
                        .ToArray();

                    schema.DiscriminatorObject.Mapping.Clear();
                    foreach (var kvp in keepMappings)
                    {
                        schema.DiscriminatorObject.Mapping[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
    }

    private (IReadOnlyCollection<JsonSchema>, HashSet<string>) FindUsedJsonSchema(OpenApiDocument doc)
    {
        var toProcess = new Stack<JsonSchema>();
        var schemaIdLookup = document.Components.Schemas
            .ToDictionary(x => x.Value,
                x => x.Key);

        var keepSchemaRegexes = keepSchemaPatterns
            .Select(x => new Regex(x, RegexOptions.Compiled))
            .ToList();

        if (doc.Components?.Schemas != null)
        {
            foreach (var kvp in doc.Components.Schemas)
            {
                var schema = kvp.Key;
                if (keepSchemaRegexes.Exists(x => x.IsMatch(schema)))
                {
                    TryPush(kvp.Value, toProcess);
                }
            }
        }

        foreach (var pathItem in doc.Paths.Select(kvp => kvp.Value))
        {
            foreach (JsonSchema? schema in GetSchemaForPath(pathItem))
            {
                TryPush(schema, toProcess);
            }
        }

        var seenIds = new HashSet<string>();
        var seen = new HashSet<JsonSchema>();
        while (toProcess.Count > 0)
        {
            var schema = toProcess.Pop();
            if (!seen.Add(schema.ActualSchema))
            {
                continue;
            }

            // NOTE: NSwag schema stuff seems weird, with all their "Actual..."
            if (schemaIdLookup.TryGetValue(schema.ActualSchema, out var refId) && !seenIds.Add(refId))
            {
                // prevent recursion
                continue;
            }

            foreach (var subSchema in EnumerateSchema(schema.ActualSchema))
            {
                TryPush(subSchema, toProcess);
            }
        }

        return (seen, seenIds);
    }

    private IEnumerable<JsonSchema?> GetSchemaForPath(OpenApiPathItem pathItem)
    {
        foreach (var p in pathItem.Parameters)
        {
            yield return p;
        }

        foreach (var op in pathItem.Values)
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
    }

    private void TryPush(JsonSchema? schema, Stack<JsonSchema> stack)
    {
        if (schema == null)
        {
            return;
        }

        stack.Push(schema);
    }

    private IEnumerable<JsonSchema> EnumerateSchema(JsonSchema? schema)
    {
        if (schema is null)
        {
            return Enumerable.Empty<JsonSchema>();
        }

        return EnumerateInternal(schema)
            .Where(x => x != null)
            .Select(x => x!);

        IEnumerable<JsonSchema?> EnumerateInternal(JsonSchema schema)
        {
            schema = schema.ActualSchema;

            yield return schema.AdditionalItemsSchema;
            yield return schema.AdditionalPropertiesSchema;
            if (schema.AllInheritedSchemas != null)
            {
                foreach (JsonSchema s in schema.AllInheritedSchemas)
                {
                    yield return s;
                }
            }

            if (schema.Item != null)
            {
                yield return schema.Item;
            }

            if (schema.Items != null)
            {
                foreach (JsonSchema s in schema.Items)
                {
                    yield return s;
                }
            }

            yield return schema.Not;


            foreach (var subSchema in schema.AllOf)
            {
                yield return subSchema;
            }

            if (schema.DiscriminatorObject != null && IncludeInheritanceHierarchy)
            {
                // abstract type
                // if we let these out, we get a bunch of "AnonymousN"-classes
                foreach (var subSchema in schema.DiscriminatorObject.Mapping)
                {
                    yield return subSchema.Value;
                }
            }

            foreach (var subSchema in schema.AnyOf)
            {
                yield return subSchema;
            }

            foreach (var subSchema in schema.OneOf)
            {
                yield return subSchema;
            }

            foreach (var subSchema in schema.ActualProperties.Select(kvp => kvp.Value))
            {
                yield return subSchema;
            }

            foreach (var subSchema in schema.Definitions.Select(kvp => kvp.Value))
            {
                yield return subSchema;
            }
        }
    }
}
