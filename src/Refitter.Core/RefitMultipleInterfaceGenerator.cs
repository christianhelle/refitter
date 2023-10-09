using System.Text;

using NSwag;

namespace Refitter.Core;

internal class RefitMultipleInterfaceGenerator : RefitInterfaceGenerator
{
    private readonly HashSet<string> knownIdentifiers = new();

    internal RefitMultipleInterfaceGenerator(
        RefitGeneratorSettings settings,
        OpenApiDocument document,
        CustomCSharpClientGenerator generator)
        : base(settings, document, generator)
    {
    }

    public override RefitGeneratedCode GenerateCode()
    {
        var code = new StringBuilder();
        var interfaceNames = new List<string>();
        
        foreach (var kv in document.Paths)
        {
            foreach (var operations in kv.Value)
            {
                var operation = operations.Value;

                if (!settings.GenerateDeprecatedOperations && operation.IsDeprecated)
                {
                    continue;
                }

                var returnTypeParameter = new[] { "200", "201", "203", "206" }
                    .Where(code => operation.Responses.ContainsKey(code))
                    .Select(code => generator.GetTypeName(operation.Responses[code].ActualResponse.Schema, true, null))
                    .FirstOrDefault();

                var returnType = GetReturnType(returnTypeParameter);

                var verb = operations.Key.CapitalizeFirstCharacter();

                GenerateInterfaceXmlDocComments(operation, code);
                
                var interfaceName = GetInterfaceName(kv, verb, operation);
                interfaceNames.Add(interfaceName);
                code.AppendLine($$"""
                                  {{GenerateInterfaceDeclaration(interfaceName)}}
                                  {{Separator}}{
                                  """);

                var operationModel = generator.CreateOperationModel(operation);
                var parameters = ParameterExtractor.GetParameters(operationModel, operation, settings);
                var parametersString = string.Join(", ", parameters);

                GenerateMethodXmlDocComments(operation, code);
                GenerateObsoleteAttribute(operation, code);
                GenerateForMultipartFormData(operationModel, code);
                GenerateAcceptHeaders(operations, operation, code);

                var methodName = settings.OperationNameTemplate ?? "Execute";
                code.AppendLine($"{Separator}{Separator}[{verb}(\"{kv.Key}\")]")
                    .AppendLine($"{Separator}{Separator}{returnType} {methodName}({parametersString});")
                    .AppendLine($"{Separator}}}")
                    .AppendLine();
            }
        }

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