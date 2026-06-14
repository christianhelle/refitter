using NSwag;

namespace Refitter.Core;

/// <summary>
/// Orchestrates the code generation pipeline: load -> filter -> clean -> generate.
/// Replaces the document mutation logic that was previously inside <see cref="RefitGenerator.Create"/>.
/// </summary>
public static class RefitPipeline
{
    private static readonly IRefitSchemaCleaner SchemaCleaner = new RefitSchemaCleaner();

    /// <summary>
    /// Creates a new <see cref="RefitGenerator"/> synchronously from a pre-loaded document.
    /// Pipeline: filter by tags -> filter by path -> clean schemas
    /// </summary>
    /// <param name="document">The pre-loaded OpenAPI document.</param>
    /// <param name="settings">The generator settings.</param>
    /// <returns>A new RefitGenerator with the processed document.</returns>
    public static RefitGenerator Create(OpenApiDocument document, RefitGeneratorSettings settings)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        var processed = RefitDocumentFilter.FilterByTags(document, settings.IncludeTags);
        processed = RefitDocumentFilter.FilterByPath(processed, settings.IncludePathMatches);
        processed = SchemaCleaner.Clean(
            processed,
            settings.TrimUnusedSchema,
            settings.KeepSchemaPatterns,
            settings.IncludeInheritanceHierarchy);

        return new RefitGenerator(settings, processed);
    }

    /// <summary>
    /// Creates a new <see cref="RefitGenerator"/> asynchronously by loading the document from settings.
    /// Pipeline: load -> filter by tags -> filter by path -> clean schemas
    /// </summary>
    /// <param name="settings">The generator settings containing the OpenAPI path(s).</param>
    /// <returns>A new RefitGenerator with the processed document.</returns>
    public static async Task<RefitGenerator> CreateAsync(RefitGeneratorSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        var openApiDocument = await GetOpenApiDocument(settings).ConfigureAwait(false);
        return Create(openApiDocument, settings);
    }

    private static async Task<OpenApiDocument> GetOpenApiDocument(RefitGeneratorSettings settings)
    {
        if (settings.OpenApiPaths is { Length: > 0 })
            return await OpenApiDocumentFactory.CreateAsync(settings.OpenApiPaths).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(settings.OpenApiPath))
        {
            throw new ArgumentException(
                "Either OpenApiPath or OpenApiPaths must be provided with at least one valid OpenAPI specification path.",
                nameof(settings));
        }

        return await OpenApiDocumentFactory.CreateAsync(settings.OpenApiPath!).ConfigureAwait(false);
    }
}
