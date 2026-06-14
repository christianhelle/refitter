using System.Text;
using NSwag;

namespace Refitter.Core;

public sealed class RefitCodeGenerator : IRefitCodeGenerator
{
    public string Generate(OpenApiDocument document, RefitGeneratorSettings settings)
    {
        var result = RunPipeline(document, settings);
        return FormatSingleFile(result, settings);
    }

    public GeneratorOutput GenerateMultipleFiles(OpenApiDocument document, RefitGeneratorSettings settings)
    {
        var result = RunPipeline(document, settings);
        return new GeneratorOutput(FormatMultipleFiles(result, settings, document));
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
            docGenerator,
            interfaceGenerator,
            new IContractsPostProcessor[]
            {
                new Swagger2OptionalReferenceNullabilityNormalizer(),
                new EnumStringConverterInjector(),
            });

        return pipeline.Run(document, settings, generator);
    }

    private static string FormatSingleFile(GenerationResult result, RefitGeneratorSettings settings)
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

        return ApplyContractTypeSuffix(output, settings);
    }

    private static IReadOnlyList<GeneratedCode> FormatMultipleFiles(
        GenerationResult result,
        RefitGeneratorSettings settings,
        OpenApiDocument document)
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

        return ApplyContractTypeSuffix(generatedFiles, settings);
    }

    private static string ApplyContractTypeSuffix(string content, RefitGeneratorSettings settings)
    {
        var contractTypeSuffix = settings.ContractTypeSuffix;
        return contractTypeSuffix is not null && !string.IsNullOrWhiteSpace(contractTypeSuffix)
            ? ContractTypeSuffixApplier.ApplySuffix(content, contractTypeSuffix)
            : content;
    }

    private static IReadOnlyList<GeneratedCode> ApplyContractTypeSuffix(
        IReadOnlyList<GeneratedCode> generatedFiles,
        RefitGeneratorSettings settings)
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
