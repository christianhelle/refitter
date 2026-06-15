using NSwag;

namespace Refitter.Core;

/// <summary>
/// Generates Refit clients and interfaces based on an OpenAPI specification.
/// </summary>
public class RefitGenerator(RefitGeneratorSettings settings, OpenApiDocument document)
{
    private static readonly RefitCodeGenerator CodeGenerator = new();

    /// <summary>
    /// OpenAPI specifications used to generate Refit clients and interfaces.
    /// This is the filtered/cleaned document after pipeline processing.
    /// </summary>
    public OpenApiDocument OpenApiDocument => document;

    /// <summary>
    /// Creates a new instance of the <see cref="RefitGenerator"/> class asynchronously
    /// by loading the document from settings, then filtering and cleaning it.
    /// </summary>
    public static async Task<RefitGenerator> CreateAsync(RefitGeneratorSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        var openApiDocument = await GetOpenApiDocument(settings).ConfigureAwait(false);
        var processed = RefitDocumentFilter.FilterByTags(openApiDocument, settings.IncludeTags);
        processed = RefitDocumentFilter.FilterByPath(processed, settings.IncludePathMatches);
        processed = CleanSchema(
            processed,
            settings.TrimUnusedSchema,
            settings.KeepSchemaPatterns,
            settings.IncludeInheritanceHierarchy);

        return new RefitGenerator(settings, processed);
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

    private static OpenApiDocument CleanSchema(
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

    /// <summary>
    /// Generates Refit clients and interfaces based on an OpenAPI specification and returns the generated code as a string.
    /// </summary>
    /// <returns>The generated code as a string.</returns>
    public string Generate() => CodeGenerator.Generate(document, settings);

    /// <summary>
    /// Generates multiple files containing Refit interfaces and contracts.
    /// </summary>
    /// <returns>A GeneratorOutput containing all generated code files.</returns>
    public GeneratorOutput GenerateMultipleFiles() => CodeGenerator.GenerateMultipleFiles(document, settings);
}
