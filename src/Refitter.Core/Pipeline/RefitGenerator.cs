using System.Text;
using NSwag;

namespace Refitter.Core;

/// <summary>
/// Generates Refit clients and interfaces based on an OpenAPI specification.
/// </summary>
public class RefitGenerator(
    RefitGeneratorSettings settings,
    OpenApiDocument document)
{
    /// <summary>
    /// OpenAPI specifications used to generate Refit clients and interfaces.
    /// This is the filtered/cleaned document after pipeline processing.
    /// </summary>
    public OpenApiDocument OpenApiDocument => document;

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
        var processed = RefitDocumentFilter.FilterByTags(openApiDocument, settings.IncludeTags);
        processed = RefitDocumentFilter.FilterByPath(processed, settings.IncludePathMatches);
        processed = await CleanSchemaAsync(
                processed,
                settings.TrimUnusedSchema,
                settings.KeepSchemaPatterns,
                settings.IncludeInheritanceHierarchy)
            .ConfigureAwait(false);

        return new(settings, processed);
    }

    private static async Task<OpenApiDocument> GetOpenApiDocument(
        RefitGeneratorSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (settings.OpenApiPaths is { Length: > 0 })
            return await OpenApiDocumentFactory
                .CreateAsync(settings.OpenApiPaths, settings.AllowRemoteReferences, cancellationToken)
                .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(settings.OpenApiPath))
        {
            throw new ArgumentException(
                "Either OpenApiPath or OpenApiPaths must be provided with at least one valid OpenAPI specification path.",
                nameof(settings));
        }

        return await OpenApiDocumentFactory
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
        var cleaner = new SchemaCleaner(result, keepSchemaPatterns)
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
    public string Generate() => FormatSingleFile(RunPipeline(document, settings), settings, settings, settings);

    /// <summary>
    /// Generates multiple files containing Refit interfaces and contracts.
    /// </summary>
    /// <returns>A GeneratorOutput containing all generated code files.</returns>
    public GeneratorOutput GenerateMultipleFiles() =>
        new(FormatMultipleFiles(RunPipeline(document, settings), settings, settings, document));

    private static GenerationResult RunPipeline(
        OpenApiDocument document,
        RefitGeneratorSettings settings)
    {
        var factory = new CSharpClientGeneratorFactory(settings, document);
        var generator = factory.Create();
        var docGenerator = new XmlDocumentationGenerator(settings);
        var interfaceGenerator = new InterfaceGenerator(settings, document, generator, docGenerator);

        var pipeline = new GeneratorPipeline(
            interfaceGenerator,
            [
                new Swagger2OptionalReferenceNullabilityNormalizer(),
                new EnumStringConverterInjector(),
            ]);

        return pipeline.Run(document, settings, generator);
    }

    private static string FormatSingleFile(
        GenerationResult result,
        RefitGeneratorSettings settings,
        ICodeGenerationConfiguration codeGeneration,
        INamingConfiguration naming)
    {
        var contracts = codeGeneration.GenerateClients
            ? RefitInterfaceImports
                .GetImportedNamespaces(settings)
                .Aggregate(
                    result.Contracts,
                    (current, import) => current.Replace($"{import}.", string.Empty))
            : result.Contracts;

        var output = new StringBuilder()
            .AppendLine(
                codeGeneration.GenerateClients
                    ? string.Join("", result.Interfaces.Select(c => c.Content))
                    : string.Empty)
            .AppendLine()
            .AppendLine(codeGeneration.GenerateContracts ? contracts : string.Empty)
            .AppendLine(result.SerializerContext)
            .AppendLine(result.DependencyInjectionCode)
            .ToString()
            .TrimEnd();

        return ApplyContractTypeSuffix(output, naming);
    }

    private static IReadOnlyList<GeneratedCode> FormatMultipleFiles(
        GenerationResult result,
        ICodeGenerationConfiguration codeGeneration,
        INamingConfiguration naming,
        OpenApiDocument document)
    {
        var generatedFiles = new List<GeneratedCode>(result.Interfaces);

        if (codeGeneration.GenerateContracts)
        {
            generatedFiles.Add(new(TypenameConstants.Contracts, result.Contracts));
        }

        if (!string.IsNullOrWhiteSpace(result.SerializerContext))
        {
            generatedFiles.Add(
                new(
                    JsonSerializerContextGenerator.GetContextTypeName(naming, document.Info?.Title),
                    result.SerializerContext));
        }

        if (!string.IsNullOrWhiteSpace(result.DependencyInjectionCode))
        {
            generatedFiles.Add(
                new(
                    TypenameConstants.DependencyInjection,
                    result.DependencyInjectionCode));
        }

        return ApplyContractTypeSuffix(generatedFiles, naming);
    }

    private static string ApplyContractTypeSuffix(string content, INamingConfiguration naming)
    {
        var contractTypeSuffix = naming.ContractTypeSuffix;
        return contractTypeSuffix is not null && !string.IsNullOrWhiteSpace(contractTypeSuffix)
            ? ContractTypeSuffixApplier.ApplySuffix(content, contractTypeSuffix)
            : content;
    }

    private static IReadOnlyList<GeneratedCode> ApplyContractTypeSuffix(
        IReadOnlyList<GeneratedCode> generatedFiles,
        INamingConfiguration naming)
    {
        var contractTypeSuffix = naming.ContractTypeSuffix;
        return contractTypeSuffix is not null && !string.IsNullOrWhiteSpace(contractTypeSuffix)
            ? generatedFiles
                .Select(
                    f => f with
                    {
                        Content = ContractTypeSuffixApplier.ApplySuffix(f.Content, contractTypeSuffix)
                    })
                .ToList()
            : generatedFiles;
    }
}
