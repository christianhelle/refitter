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

    public override RefitGeneratedCode GenerateCode()
    {
        var code = new StringBuilder();
        var interfaceNames = new List<string>();
        var dynamicQuerystringParametersCodeBuilder = new StringBuilder();

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
                var methodName = settings.OperationNameTemplate ?? "Execute";

                this.docGenerator.AppendInterfaceDocumentation(operation, code);
                
                var interfaceName = GetInterfaceName(kv, verb, operation);
                interfaceNames.Add(interfaceName);
                code.AppendLine($$"""
                                  {{GenerateInterfaceDeclaration(interfaceName)}}
                                  {{Separator}}{
                                  """);

                var operationModel = generator.CreateOperationModel(operation);
                var dynamicQuerystringParameterType = interfaceName.Replace("I", string.Empty).Replace("Endpoint", "QueryParams");
                var parameters = ParameterExtractor.GetParameters(operationModel, operation, settings, dynamicQuerystringParameterType, out var methodDynamicQuerystringParameters).ToList();

                var hasDynamicQuerystringParameter = !string.IsNullOrWhiteSpace(methodDynamicQuerystringParameters);
                if (hasDynamicQuerystringParameter)
                    dynamicQuerystringParametersCodeBuilder.AppendLine(methodDynamicQuerystringParameters);

                var parametersString = string.Join(", ", parameters);
                var hasApizrRequestOptionsParameter = settings.ApizrSettings?.WithRequestOptions == true;

                this.docGenerator.AppendMethodDocumentation(operationModel, IsApiResponseType(returnType), hasDynamicQuerystringParameter, hasApizrRequestOptionsParameter, code);
                GenerateObsoleteAttribute(operation, code);
                GenerateForMultipartFormData(operationModel, code);
                GenerateAcceptHeaders(operations, operation, code);

                code.AppendLine($"{Separator}{Separator}[{verb}(\"{kv.Key}\")]")
                    .AppendLine($"{Separator}{Separator}{returnType} {methodName}({parametersString});")
                    .AppendLine($"{Separator}}}")
                    .AppendLine();

                if (parametersString.Contains("?") && settings is { OptionalParameters: true, ApizrSettings: not null })
                {
                    this.docGenerator.AppendMethodDocumentation(operationModel, IsApiResponseType(returnType), false, hasApizrRequestOptionsParameter, code);
                    GenerateObsoleteAttribute(operation, code);
                    GenerateForMultipartFormData(operationModel, code);
                    GenerateAcceptHeaders(operations, operation, code);

                    parametersString = string.Join(", ", parameters.Where(parameter => !parameter.Contains("?")));

                    code.AppendLine($"{Separator}{Separator}[{verb}(\"{kv.Key}\")]")
                        .AppendLine($"{Separator}{Separator}{returnType} {methodName}({parametersString});")
                        .AppendLine();
                }
            }
        }

        if (dynamicQuerystringParametersCodeBuilder.Length > 0) 
            code.AppendLine(dynamicQuerystringParametersCodeBuilder.ToString());

        return new RefitGeneratedCode(code.ToString(), interfaceNames.ToArray());
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