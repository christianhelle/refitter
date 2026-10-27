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
        this.comparer = comparer;
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
            MergeAll(baseDocument.Paths, document.Paths, "path");
            MergeAll(baseDocument.Components?.Schemas, document.Components?.Schemas, "schema");
            MergeAll(baseDocument.Definitions, document.Definitions, "definition");
            MergeAll(baseDocument.SecurityDefinitions, document.SecurityDefinitions, "security scheme");

            foreach (var tag in document.Tags.Where(tag => tagNames.Add(tag.Name)))
            {
                baseDocument.Tags.Add(tag);
            }
        }

        return baseDocument;
    }

    private static OpenApiDocument CloneDocument(OpenApiDocument document)
        => OpenApiDocument.FromJsonAsync(document.ToJson()).GetAwaiter().GetResult();

    private void MergeAll<TValue>(
        IDictionary<string, TValue>? target,
        IDictionary<string, TValue>? source,
        string itemType)
    {
        if (target == null || source == null)
            return;

        foreach (var item in source)
        {
            MergeIfMissingOrThrowOnConflict(target, item.Key, item.Value, itemType);
        }
    }

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
