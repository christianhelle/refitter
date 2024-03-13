using System.Text.RegularExpressions;

using NJsonSchema;

using NSwag;

namespace Refitter.Core;

public class SchemaCleaner
{
    private readonly OpenApiDocument document;
    private readonly string[] keepSchemaPatterns;

    public SchemaCleaner(OpenApiDocument document, string[] keepSchemaPatterns)
    {
        this.document = document;
        this.keepSchemaPatterns = keepSchemaPatterns;
    }

    public void RemoveUnreferencedSchema()
    {
        var usage = FindUsedSchema(document);

        var unused = document.Components.Schemas.Where(s => !usage.Contains(s.Key))
            .ToArray();

        foreach (var unusedSchema in unused)
        {
            document.Components.Schemas.Remove(unusedSchema);
        }
    }

    private HashSet<string> FindUsedSchema(OpenApiDocument doc)
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
            if (schemaIdLookup.TryGetValue(schema.ActualSchema, out var refId))
            {
                if (!seenIds.Add(refId))
                {
                    // prevent recursion
                    continue;
                }
            }

            foreach (var subSchema in EnumerateSchema(schema.ActualSchema))
            {
                TryPush(subSchema, toProcess);
            }
        }

        return seenIds;
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
                foreach (var kvpBody in body.Content)
                {
                    var content = kvpBody.Value;
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

        static IEnumerable<JsonSchema?> EnumerateInternal(JsonSchema schema)
        {
            // schema = schema.ActualSchema;
            yield return schema.AdditionalItemsSchema;
            yield return schema.AdditionalPropertiesSchema;
            if (schema.AllInheritedSchemas != null)
            {
                foreach (JsonSchema s in schema.AllInheritedSchemas)
                {
                    yield return s;
                }
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

            foreach (var subSchema in schema.AnyOf)
            {
                yield return subSchema;
            }

            foreach (var subSchema in schema.OneOf)
            {
                yield return subSchema;
            }

            foreach (var kvp in schema.Properties)
            {
                var subSchema = kvp.Value;
                yield return subSchema;
            }

            foreach (var kvp in schema.Definitions)
            {
                var subSchema = kvp.Value;
                yield return subSchema;
            }
        }
    }
}