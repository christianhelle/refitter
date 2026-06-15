using Newtonsoft.Json.Linq;
using NJsonSchema;
using NSwag;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal sealed class DocumentMerger : IDocumentMerger
{
    private readonly DocumentEquivalenceComparer comparer;

    /// <summary>
    /// Initializes a new instance of the DocumentMerger class.
    /// </summary>
    /// <param name="comparer">The comparer used to detect equivalent document elements during merging.</param>
    public DocumentMerger(DocumentEquivalenceComparer comparer)
    {
        comparer = comparer;
    }

    /// <summary>
    /// Merges multiple OpenAPI documents into a single document.
    /// </summary>
    /// <param name="documents">The array of OpenAPI documents to merge.</param>
    /// <returns>A merged OpenAPI document containing all paths, schemas, and other elements from the input documents.</returns>
    public OpenApiDocument Merge(OpenApiDocument[] documents)
    {
        if (documents == null || documents.Length == 0)
            throw new ArgumentException("The documents parameter cannot be null or empty.", nameof(documents));

        var baseDocument = CloneDocument(documents[0]);
        var tagNames = new HashSet<string>(baseDocument.Tags.Select(t => t.Name), StringComparer.Ordinal);

        for (var i = 1; i < documents.Length; i++)
        {
            var document = documents[i];
            foreach (var path in document.Paths)
            {
                MergeIfMissingOrThrowOnConflict(baseDocument.Paths, path.Key, path.Value, "path");
            }

            if (document.Components?.Schemas?.Count > 0)
            {
                if (baseDocument.Components?.Schemas != null)
                {
                    foreach (var schema in document.Components.Schemas)
                    {
                        MergeIfMissingOrThrowOnConflict(baseDocument.Components.Schemas, schema.Key, schema.Value, "schema");
                    }
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

        if (!comparer.AreEquivalent(existingValue, value))
            throw CreateMergeConflictException(itemType, key);
    }

    private static InvalidOperationException CreateMergeConflictException(string itemType, string key) =>
        new($"Cannot merge OpenAPI documents because a duplicate {itemType} '{key}' was found. Refitter fails fast on merge collisions to avoid silent data loss.");
}
