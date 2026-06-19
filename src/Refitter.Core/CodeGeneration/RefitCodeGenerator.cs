using System.Text;
using NSwag;

namespace Refitter.Core;

/// <summary>
/// Generates Refit client and interface code from an OpenAPI document.
/// Handles both single-file and multi-file output modes.
/// </summary>
internal sealed class RefitCodeGenerator
{
    /// <summary>
    /// Generates all Refit code as a single string.
    /// </summary>
    public string Generate(OpenApiDocument document, RefitGeneratorSettings settings)
    {
        var result = RunPipeline(document, settings);
        return FormatSingleFile(result, settings, settings, settings);
    }

    /// <summary>
    /// Generates Refit code as multiple files (interfaces, contracts, DI, serializer context).
    /// </summary>
    public GeneratorOutput GenerateMultipleFiles(OpenApiDocument document, RefitGeneratorSettings settings)
    {
        var result = RunPipeline(document, settings);
        return new(FormatMultipleFiles(result, settings, settings, settings, document));
    }

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
        RefitGeneratorSettings settings,
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
