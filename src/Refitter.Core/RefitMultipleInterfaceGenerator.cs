using System.Text;
using NSwag;

namespace Refitter.Core;

internal class RefitMultipleInterfaceGenerator : RefitInterfaceGenerator
{
    private readonly HashSet<string> knownIdentifiers = new();

    internal RefitMultipleInterfaceGenerator(
        RefitGeneratorSettings settings,
        OpenApiDocument document,
        CustomCSharpClientGenerator generator,
        XmlDocumentationGenerator docGenerator)
        : base(settings, document, generator, docGenerator)
    {
    }

    public override IEnumerable<GeneratedCode> GenerateCode()
    {
        foreach (var kv in document.Paths)
        {
            foreach (var operations in kv.Value)
            {
                var operation = operations.Value;

                if (!settings.GenerateDeprecatedOperations && operation.IsDeprecated)
                {
                    continue;
                }

                var returnType = GetTypeName(operation);
                var verb = operations.Key.CapitalizeFirstCharacter();
                var methodName = !string.IsNullOrWhiteSpace(settings.OperationNameTemplate)
                    ? settings.OperationNameTemplate!.Replace("{operationName}", "Execute")
                    : "Execute";
                var code = new StringBuilder();

                this.docGenerator.AppendInterfaceDocumentation(operation, code);

                var interfaceName = GetInterfaceName(kv, verb, operation);
                code.AppendLine($$"""
                                  {{GenerateInterfaceDeclaration(interfaceName)}}
                                  {{Separator}}{
                                  """);

                var operationModel = generator.CreateOperationModel(operation);
                var dynamicQuerystringParameterType = interfaceName.Replace("I", string.Empty).Replace("Endpoint", "QueryParams");
                var parameters = ParameterExtractor.GetParameters(operationModel, operation, settings, dynamicQuerystringParameterType, out var methodDynamicQuerystringParameters).ToList();

                var hasDynamicQuerystringParameter = !string.IsNullOrWhiteSpace(methodDynamicQuerystringParameters);
                if (hasDynamicQuerystringParameter)
                    yield return new GeneratedCode(
                        dynamicQuerystringParameterType,
                        methodDynamicQuerystringParameters!);

                var parametersString = string.Join(", ", parameters);
                var hasApizrRequestOptionsParameter = settings.ApizrSettings?.WithRequestOptions == true;
                var hasCancellationToken = settings.UseCancellationTokens && !hasApizrRequestOptionsParameter;

                this.docGenerator.AppendMethodDocumentation(operationModel, IsApiResponseType(returnType), hasDynamicQuerystringParameter, hasApizrRequestOptionsParameter, hasCancellationToken, code);
                GenerateObsoleteAttribute(operation, code);
                GenerateForMultipartFormData(operationModel, code);
                GenerateHeaders(operations, operation, operationModel, code);

                code.AppendLine($"{Separator}{Separator}[{verb}(\"{kv.Key}\")]")
                    .AppendLine($"{Separator}{Separator}{returnType} {methodName}({parametersString});")
                    .AppendLine($"{Separator}}}");

                if (parametersString.Contains("?") && settings is { OptionalParameters: true, ApizrSettings: not null })
                {
                    this.docGenerator.AppendMethodDocumentation(operationModel, IsApiResponseType(returnType), false, hasApizrRequestOptionsParameter, hasCancellationToken, code);
                    GenerateObsoleteAttribute(operation, code);
                    GenerateForMultipartFormData(operationModel, code);
                    GenerateHeaders(operations, operation, operationModel, code);

                    parametersString = string.Join(", ", parameters.Where(parameter => !parameter.Contains("?")));

                    code.AppendLine($"{Separator}{Separator}[{verb}(\"{kv.Key}\")]")
                        .AppendLine($"{Separator}{Separator}{returnType} {methodName}({parametersString});")
                        .AppendLine();
                }

                yield return new GeneratedCode(interfaceName, code.ToString());
            }
        }
    }

    private string GetInterfaceName(
        KeyValuePair<string, OpenApiPathItem> kv,
        string verb,
        OpenApiOperation operation)
    {
        var name = IdentifierUtils.Counted(
            knownIdentifiers,
            "I" + generator
                .BaseSettings
                .OperationNameGenerator
                .GetOperationName(document, kv.Key, verb, operation).CapitalizeFirstCharacter(),
            suffix: "Endpoint");
        knownIdentifiers.Add(name);
        return name;
    }

    private string GenerateInterfaceDeclaration(string name)
    {
        var modifier = settings.TypeAccessibility.ToString().ToLowerInvariant();
        return $"""
                {Separator}{GetGeneratedCodeAttribute()}
                {Separator}{modifier} partial interface {name}
                """;
    }
}
