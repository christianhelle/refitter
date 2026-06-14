using System.Text;
using System.Text.RegularExpressions;
using NSwag;

namespace Refitter.Core;

/// <summary>
/// Generates Refit clients and interfaces based on an OpenAPI specification.
/// </summary>
public class RefitGenerator(RefitGeneratorSettings settings, OpenApiDocument document)
{

    /// <summary>
    /// OpenAPI specifications used to generate Refit clients and interfaces.
    /// </summary>
    public OpenApiDocument OpenApiDocument => document;

    /// <summary>
    /// Creates a new instance of the <see cref="RefitGenerator"/> class synchronously
    /// from a pre-loaded <see cref="OpenApiDocument"/>.
    /// </summary>
    /// <param name="document">The pre-loaded OpenAPI document.</param>
    /// <param name="settings">The settings used to configure the generator.</param>
    /// <returns>A new instance of the <see cref="RefitGenerator"/> class.</returns>
    public static RefitGenerator Create(OpenApiDocument document, RefitGeneratorSettings settings)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        ProcessTagFilters(document, settings.IncludeTags);
        ProcessPathFilters(document, settings.IncludePathMatches);
        ProcessContractFilter(document, settings.TrimUnusedSchema, settings.KeepSchemaPatterns, settings.IncludeInheritanceHierarchy);

        return new RefitGenerator(settings, document);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="RefitGenerator"/> class asynchronously.
    /// </summary>
    /// <param name="settings">The settings used to configure the generator.</param>
    /// <returns>A new instance of the <see cref="RefitGenerator"/> class.</returns>
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

    private static void ProcessContractFilter(OpenApiDocument openApiDocument, bool removeUnusedSchema, string[] includeSchemaMatches,
        bool includeInheritanceHierarchy)
    {
        if (!removeUnusedSchema)
        {
            return;
        }
        var cleaner = new SchemaCleaner(openApiDocument, includeSchemaMatches)
        {
            IncludeInheritanceHierarchy = includeInheritanceHierarchy
        };
        cleaner.RemoveUnreferencedSchema();
    }

    private static void ProcessTagFilters(OpenApiDocument document, IReadOnlyCollection<string> includeTags)
    {
        if (includeTags.Count == 0)
        {
            return;
        }
        var clonedPaths = document.Paths.Where(pair => pair.Value != null)
            // as we modify the document.Paths
            // we have to enumerate on a snapshot of the items
            .ToArray();
        foreach (var path in clonedPaths)
        {
            if (path.Value == null) continue;

            var methods = path.Value.Where(pair => pair.Value != null)
                // same reason as with document.Paths
                .ToArray();
            foreach (var method in methods)
            {
                if (method.Value == null) continue;

                var exclude = method.Value.Tags?.Exists(includeTags.Contains) != true;
                if (exclude)
                {
                    path.Value.Remove(method.Key);
                }
                if (path.Value.Count == 0)
                {
                    document.Paths.Remove(path.Key);
                }
            }
        }
    }

    private static void ProcessPathFilters(OpenApiDocument document,
        string[] pathMatchExpressions)
    {
        if (pathMatchExpressions.Length == 0)
        {
            return;
        }

        // compile all expressions here once, as we will use them more than once
        var regexes = pathMatchExpressions
            .Select(x => new Regex(x, RegexOptions.Compiled, TimeSpan.FromSeconds(1)))
            .ToArray();
        var paths = document.Paths.Keys
            .Where(
                pathKey =>
                {
                    for (var i = 0; i < regexes.Length; i++)
                    {
                        if (regexes[i].IsMatch(pathKey))
                            return false;
                    }

                    return true;
                })
            .ToArray();

        foreach (string pathKey in paths)
        {
            document.Paths.Remove(pathKey);
        }
    }

    /// <summary>
    /// Generates Refit clients and interfaces based on an OpenAPI specification and returns the generated code as a string.
    /// </summary>
    /// <returns>The generated code as a string.</returns>
    public string Generate() => FormatSingleFile(RunPipeline());

    /// <summary>
    /// Generates multiple files containing Refit interfaces and contracts.
    /// </summary>
    /// <returns>A GeneratorOutput containing all generated code files.</returns>
    public GeneratorOutput GenerateMultipleFiles() => new(FormatMultipleFiles(RunPipeline()));

    private GenerationResult RunPipeline()
    {
        var factory = new CSharpClientGeneratorFactory(settings, document);
        var generator = factory.Create();
        var docGenerator = new XmlDocumentationGenerator(settings);

        // Create the interface generator before calling GenerateFile() so that
        // OperationNameGenerator.CheckForDuplicateOperationIds() sees the original
        // (pre-generation) operation IDs. GenerateFile() auto-populates operation IDs
        // with globally unique names which would prevent the switch to the path segments
        // generator, causing unnecessary numeric suffixes in ByTag mode.
        var interfaceGenerator = new InterfaceGenerator(settings, document, generator, docGenerator);

        var pipeline = new GeneratorPipeline(
            docGenerator,
            interfaceGenerator,
            new IContractsPostProcessor[]
            {
                new Swagger2OptionalReferenceNullabilityNormalizer(),
                new EnumStringConverterInjector(),
            });

        return pipeline.Run(document, settings, generator);
    }

    private string FormatSingleFile(GenerationResult result)
    {
        var contracts = settings.GenerateClients
            ? RefitInterfaceImports
                .GetImportedNamespaces(settings)
                .Aggregate(
                    result.Contracts,
                    (current, import) => current.Replace($"{import}.", string.Empty))
            : result.Contracts;

        var output = new StringBuilder()
            .AppendLine(settings.GenerateClients ? string.Join("", result.Interfaces.Select(c => c.Content)) : string.Empty)
            .AppendLine()
            .AppendLine(settings.GenerateContracts ? contracts : string.Empty)
            .AppendLine(result.SerializerContext)
            .AppendLine(result.DependencyInjectionCode)
            .ToString()
            .TrimEnd();

        return ApplyContractTypeSuffix(output);
    }

    private IReadOnlyList<GeneratedCode> FormatMultipleFiles(GenerationResult result)
    {
        var generatedFiles = new List<GeneratedCode>(result.Interfaces);

        if (settings.GenerateContracts)
        {
            generatedFiles.Add(new GeneratedCode(TypenameConstants.Contracts, result.Contracts));
        }

        if (!string.IsNullOrWhiteSpace(result.SerializerContext))
        {
            generatedFiles.Add(
                new GeneratedCode(
                    JsonSerializerContextGenerator.GetContextTypeName(settings, document.Info?.Title),
                    result.SerializerContext));
        }

        if (!string.IsNullOrWhiteSpace(result.DependencyInjectionCode))
        {
            generatedFiles.Add(
                new GeneratedCode(
                    TypenameConstants.DependencyInjection,
                    result.DependencyInjectionCode));
        }

        return ApplyContractTypeSuffix(generatedFiles);
    }

    private string ApplyContractTypeSuffix(string content)
    {
        var contractTypeSuffix = settings.ContractTypeSuffix;
        return contractTypeSuffix is not null && !string.IsNullOrWhiteSpace(contractTypeSuffix)
            ? ContractTypeSuffixApplier.ApplySuffix(content, contractTypeSuffix)
            : content;
    }

    private IReadOnlyList<GeneratedCode> ApplyContractTypeSuffix(IReadOnlyList<GeneratedCode> generatedFiles)
    {
        var contractTypeSuffix = settings.ContractTypeSuffix;
        return contractTypeSuffix is not null && !string.IsNullOrWhiteSpace(contractTypeSuffix)
            ? generatedFiles
                .Select(f => f with
                {
                    Content = ContractTypeSuffixApplier.ApplySuffix(f.Content, contractTypeSuffix)
                })
                .ToList()
            : generatedFiles;
    }

}
