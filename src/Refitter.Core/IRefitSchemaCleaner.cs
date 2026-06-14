using NSwag;

namespace Refitter.Core;

/// <summary>
/// Cleans an OpenAPI document by removing unreferenced schemas.
/// Returns a new document without mutating the input.
/// </summary>
public interface IRefitSchemaCleaner
{
    /// <summary>
    /// Removes unreferenced schemas from the document when <paramref name="removeUnusedSchema"/> is true.
    /// </summary>
    /// <param name="document">The OpenAPI document to clean.</param>
    /// <param name="removeUnusedSchema">Whether to remove unused schemas.</param>
    /// <param name="keepSchemaPatterns">Regex patterns for schemas to always keep.</param>
    /// <param name="includeInheritanceHierarchy">Whether to keep inheritance hierarchy types.</param>
    /// <returns>The cleaned OpenAPI document.</returns>
    OpenApiDocument Clean(
        OpenApiDocument document,
        bool removeUnusedSchema,
        string[] keepSchemaPatterns,
        bool includeInheritanceHierarchy);
}
