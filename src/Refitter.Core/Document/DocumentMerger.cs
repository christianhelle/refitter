#nullable enable

namespace Refitter.Core;

/// <summary>
/// Merges documents into (a copy of) the first one. Paths, schemas, security schemes and tags of the other
/// documents are added when missing; an existing one must be equivalent, or merging fails.
/// </summary>
internal sealed class DocumentMerger : IDocumentMerger
{
    private readonly DocumentEquivalenceComparer comparer;

    public DocumentMerger(DocumentEquivalenceComparer comparer)
    {
        this.comparer = comparer;
    }

    /// <summary>
    /// Merges multiple OpenAPI documents into a single document.
    /// </summary>
    /// <param name="documents">The documents to merge.</param>
    /// <returns>A merged document containing all paths, schemas, and other elements from the input documents.</returns>
    public ApiDocument Merge(ApiDocument[] documents)
    {
        if (documents == null || documents.Length == 0)
            throw new ArgumentException("The documents parameter cannot be null or empty.", nameof(documents));

        var baseDocument = ApiDocumentWriter.Clone(documents[0]);
        var tagNames = new HashSet<string>(baseDocument.Tags.Select(t => t.Name!), StringComparer.Ordinal);

        for (var i = 1; i < documents.Length; i++)
        {
            var document = documents[i];
            foreach (var path in document.Paths)
            {
                MergeIfMissingOrThrowOnConflict(
                    baseDocument.Paths,
                    path.Key,
                    path.Value,
                    "path",
                    (key, value) => baseDocument.AddPath(key, value));
            }

            // The schemas and the definitions are the same, so this checks every schema twice, like before
            foreach (var schema in document.Components.Schemas)
            {
                MergeIfMissingOrThrowOnConflict(
                    baseDocument.Components.Schemas,
                    schema.Key,
                    schema.Value,
                    "schema",
                    (key, value) => baseDocument.Components.Schemas[key] = value);
            }

            foreach (var definition in document.Definitions)
            {
                MergeIfMissingOrThrowOnConflict(
                    baseDocument.Definitions,
                    definition.Key,
                    definition.Value,
                    "definition",
                    (key, value) => baseDocument.Definitions[key] = value);
            }

            foreach (var securityDefinition in document.SecurityDefinitions)
            {
                MergeIfMissingOrThrowOnConflict(
                    baseDocument.SecurityDefinitions,
                    securityDefinition.Key,
                    securityDefinition.Value,
                    "security scheme",
                    (key, value) => baseDocument.SecurityDefinitions[key] = value);
            }

            foreach (var tag in document.Tags)
            {
                if (tagNames.Add(tag.Name!))
                    baseDocument.Tags.Add(tag);
            }
        }

        return baseDocument;
    }

    private void MergeIfMissingOrThrowOnConflict<TValue>(
        IDictionary<string, TValue> target,
        string key,
        TValue value,
        string itemType,
        Action<string, TValue> add)
    {
        if (!target.TryGetValue(key, out var existingValue))
        {
            add(key, value);
            return;
        }

        if (!comparer.AreEquivalent(existingValue, value))
            throw CreateMergeConflictException(itemType, key);
    }

    private static InvalidOperationException CreateMergeConflictException(string itemType, string key) =>
        new($"Cannot merge OpenAPI documents because a duplicate {itemType} '{key}' was found. Refitter fails fast on merge collisions to avoid silent data loss.");
}
