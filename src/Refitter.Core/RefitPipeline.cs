using NSwag;

namespace Refitter.Core;

public static class RefitPipeline
{
    /// <summary>
    /// Creates a new instance of the <see cref="RefitGenerator"/> class synchronously
    /// from a pre-loaded <see cref="OpenApiDocument"/>.
    /// Pipeline stages: filter by tags → filter by path → clean schemas
    /// </summary>
    private static readonly IRefitSchemaCleaner SchemaCleaner = new RefitSchemaCleaner();

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
    /// Creates a new instance of the <see cref="RefitGenerator"/> class asynchronously.
    /// Pipeline stages: load → filter by tags → filter by path → clean schemas
    /// </summary>
    public static async Task<RefitGenerator> CreateAsync(RefitGeneratorSettings settings)
    {
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
