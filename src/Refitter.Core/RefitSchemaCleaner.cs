using NSwag;

namespace Refitter.Core;

/// <summary>
/// Cleans an OpenAPI document by removing unreferenced schemas.
/// Wraps <see cref="SchemaCleaner"/> and returns a new document without mutating the input.
/// </summary>
public sealed class RefitSchemaCleaner : IRefitSchemaCleaner
{
    /// <summary>
    /// Removes unreferenced schemas from the document when <paramref name="removeUnusedSchema"/> is true.
    /// </summary>
    /// <param name="document">The OpenAPI document to clean.</param>
    /// <param name="removeUnusedSchema">Whether to remove unused schemas.</param>
    /// <param name="keepSchemaPatterns">Regex patterns for schemas to always keep.</param>
    /// <param name="includeInheritanceHierarchy">Whether to keep inheritance hierarchy types.</param>
    /// <returns>The cleaned OpenAPI document.</returns>
    public OpenApiDocument Clean(
        OpenApiDocument document,
        bool removeUnusedSchema,
        string[] keepSchemaPatterns,
        bool includeInheritanceHierarchy)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (keepSchemaPatterns == null) throw new ArgumentNullException(nameof(keepSchemaPatterns));

        if (!removeUnusedSchema)
            return document;

        var result = CloneDocument(document);
        var cleaner = new SchemaCleaner(result, keepSchemaPatterns)
        {
            IncludeInheritanceHierarchy = includeInheritanceHierarchy
        };

        cleaner.RemoveUnreferencedSchema();
        return result;
    }

    private static OpenApiDocument CloneDocument(OpenApiDocument document)
        => OpenApiDocument.FromJsonAsync(document.ToJson()).GetAwaiter().GetResult();
}
