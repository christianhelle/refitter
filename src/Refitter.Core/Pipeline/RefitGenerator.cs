using NSwag;

namespace Refitter.Core;

/// <summary>
/// Generates Refit clients and interfaces based on an OpenAPI specification.
/// </summary>
public class RefitGenerator
{
    private static readonly RefitCodeGenerator CodeGenerator = new();

    private readonly RefitGeneratorSettings settings;
    private readonly OpenApiDocument document;
    private ApiDocumentInfo? documentInfo;

    private RefitGenerator(OpenApiDocument document, RefitGeneratorSettings settings)
    {
        this.settings = settings;
        this.document = document;
    }

    /// <summary>
    /// Describes the OpenAPI document used to generate Refit clients and interfaces,
    /// after filtering and schema trimming.
    /// </summary>
    public ApiDocumentInfo DocumentInfo => documentInfo ??= new ApiDocumentInfo(document);

    internal OpenApiDocument NSwagDocument => document;

    /// <summary>
    /// Creates a new instance of the <see cref="RefitGenerator"/> class asynchronously
    /// by loading the document from settings, then filtering, and cleaning it.
    /// </summary>
    public static async Task<RefitGenerator> CreateAsync(
        RefitGeneratorSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        var openApiDocument = await GetOpenApiDocument(settings, cancellationToken).ConfigureAwait(false);
        var processed = NSwagDocumentFilter.FilterByTags(openApiDocument, settings.IncludeTags);
        processed = NSwagDocumentFilter.FilterByPath(processed, settings.IncludePathMatches);
        processed = await CleanSchemaAsync(
                processed,
                settings.TrimUnusedSchema,
                settings.KeepSchemaPatterns,
                settings.IncludeInheritanceHierarchy)
            .ConfigureAwait(false);

        return new(processed, settings);
    }

    private static async Task<OpenApiDocument> GetOpenApiDocument(
        RefitGeneratorSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (settings.OpenApiPaths is { Length: > 0 })
            return await NSwagDocumentFactory
                .CreateAsync(settings.OpenApiPaths, settings.AllowRemoteReferences, cancellationToken)
                .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(settings.OpenApiPath))
        {
            throw new ArgumentException(
                "Either OpenApiPath or OpenApiPaths must be provided with at least one valid OpenAPI specification path.",
                nameof(settings));
        }

        return await NSwagDocumentFactory
            .CreateAsync(settings.OpenApiPath!, settings.AllowRemoteReferences, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<OpenApiDocument> CleanSchemaAsync(
        OpenApiDocument document,
        bool removeUnusedSchema,
        string[] keepSchemaPatterns,
        bool includeInheritanceHierarchy)
    {
        if (keepSchemaPatterns == null) throw new ArgumentNullException(nameof(keepSchemaPatterns));

        if (!removeUnusedSchema)
            return document;

        var result = await CloneDocumentAsync(document).ConfigureAwait(false);
        var cleaner = new NSwagSchemaCleaner(result, keepSchemaPatterns)
        {
            IncludeInheritanceHierarchy = includeInheritanceHierarchy
        };

        cleaner.RemoveUnreferencedSchema();
        return result;
    }

    private static async Task<OpenApiDocument> CloneDocumentAsync(OpenApiDocument document)
        => await OpenApiDocument.FromJsonAsync(document.ToJson()).ConfigureAwait(false);

    /// <summary>
    /// Generates Refit clients and interfaces based on an OpenAPI specification
    /// and returns the generated code as a string.
    /// </summary>
    /// <returns>The generated code as a string.</returns>
    public string Generate() => CodeGenerator.Generate(document, settings);

    /// <summary>
    /// Generates multiple files containing Refit interfaces and contracts.
    /// </summary>
    /// <returns>A GeneratorOutput containing all generated code files.</returns>
    public GeneratorOutput GenerateMultipleFiles() => CodeGenerator.GenerateMultipleFiles(document, settings);
}
