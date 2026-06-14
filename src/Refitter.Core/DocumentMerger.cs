using Newtonsoft.Json.Linq;
using NJsonSchema;
using NSwag;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal sealed class DocumentMerger : IDocumentMerger
{
    private readonly DocumentEquivalenceComparer _comparer;

    public DocumentMerger(DocumentEquivalenceComparer comparer)
    {
        _comparer = comparer;
    }

    public OpenApiDocument Merge(OpenApiDocument[] documents)
    {
        var baseDocument = CloneDocument(documents[0]);
        var tagNames = new HashSet<string>(baseDocument.Tags.Select(t => t.Name), StringComparer.Ordinal);

        for (var i = 1; i < documents.Length; i++)
        {
            var document = documents[i];
            foreach (var path in document.Paths)
            {
                MergeIfMissingOrThrowOnConflict(baseDocument.Paths, path.Key, path.Value, "path");
            }

            if (document.Components.Schemas.Count > 0)
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

            if (document.Tags.Count > 0)
            {
                foreach (var tag in document.Tags)
                {
                    if (tagNames.Add(tag.Name))
                        baseDocument.Tags.Add(tag);
                }
            }
        }

        return baseDocument;
    }

    private static OpenApiDocument CloneDocument(OpenApiDocument document)
        => OpenApiDocument.FromJsonAsync(document.ToJson()).GetAwaiter().GetResult();

    private void MergeIfMissingOrThrowOnConflict<TValue>(
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

        if (!_comparer.AreEquivalent(existingValue, value))
            throw CreateMergeConflictException(itemType, key);
    }

    private static InvalidOperationException CreateMergeConflictException(string itemType, string key) =>
        new($"Cannot merge OpenAPI documents because a duplicate {itemType} '{key}' was found. Refitter fails fast on merge collisions to avoid silent data loss.");
}
