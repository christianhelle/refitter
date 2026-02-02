using System.Text;
using NSwag;

namespace Refitter.Core;

internal class RefitMultipleInterfaceByTagGenerator : RefitInterfaceGenerator
{
    private readonly HashSet<string> knownIdentifiers = new();

    internal RefitMultipleInterfaceByTagGenerator(
        RefitGeneratorSettings settings,
        OpenApiDocument document,
        CustomCSharpClientGenerator generator,
        XmlDocumentationGenerator docGenerator)
        : base(settings, document, generator, docGenerator)
    {
    }

    public override IEnumerable<GeneratedCode> GenerateCode()
    {
        var ungroupedTitle = settings.Naming.UseOpenApiTitle
            ? IdentifierUtils.Sanitize(document.Info?.Title ?? "ApiClient")
            : settings.Naming.InterfaceName;
        ungroupedTitle = ungroupedTitle.CapitalizeFirstCharacter();

        var byGroup = document.Paths
            .SelectMany(x => x.Value, (k, v) => (PathItem: k, Operation: v))
            .GroupBy(x => GetGroupName(x.Operation.Value, ungroupedTitle), (k, v) => new { Key = k, Combined = v });

        Dictionary<string, StringBuilder> interfacesByGroup = new();
        Dictionary<string, string> interfacesNamesByGroup = new();

        foreach (var kv in byGroup)
        {
            foreach (var op in kv.Combined)
            {
                var operations = op.Operation;
                var operation = operations.Value;

                if (!settings.GenerateDeprecatedOperations && operation.IsDeprecated)
                {
                    continue;
                }

                var returnType = GetTypeName(operation);
                var verb = operations.Key.CapitalizeFirstCharacter();

                string interfaceName = null!;
                if (!interfacesByGroup.TryGetValue(kv.Key, out var sb))
                {
                    interfacesByGroup[kv.Key] = sb = new StringBuilder();
                    this.docGenerator.AppendInterfaceDocumentationByTag(document, kv.Key, sb);

                    interfaceName = GetInterfaceName(kv.Key);
                    sb.AppendLine($$"""
                                    {{GenerateInterfaceDeclaration(interfaceName)}}
                                    {{Separator}}{
                                    """);

                    interfacesNamesByGroup[kv.Key] = interfaceName;
                }

                var operationName = GetOperationName(interfaceName, op.PathItem.Key, operations.Key, operation);
                var dynamicQuerystringParameterType = operationName + "QueryParams";
                var operationModel = generator.CreateOperationModel(operation);
                var parameters = ParameterExtractor
                    .GetParameters(
                        operationModel,
                        operation,
                        settings,
                        dynamicQuerystringParameterType,
                        out var operationDynamicQuerystringParameters)
                    .ToList();

                var hasDynamicQuerystringParameter = !string.IsNullOrWhiteSpace(operationDynamicQuerystringParameters);
                if (hasDynamicQuerystringParameter)
                    yield return new GeneratedCode(
                        dynamicQuerystringParameterType,
                        operationDynamicQuerystringParameters!);

                var parametersString = string.Join(", ", parameters);
                var hasApizrRequestOptionsParameter = settings.ApizrSettings?.WithRequestOptions == true;
                var hasCancellationToken = settings.UseCancellationTokens && !hasApizrRequestOptionsParameter;

                this.docGenerator.AppendMethodDocumentation(operationModel, IsApiResponseType(returnType), hasDynamicQuerystringParameter, hasApizrRequestOptionsParameter, hasCancellationToken, sb);
                GenerateObsoleteAttribute(operation, sb);
                GenerateForMultipartFormData(operationModel, sb);
                GenerateHeaders(operations, operation, operationModel, sb);

                sb.AppendLine($"{Separator}{Separator}[{verb}(\"{op.PathItem.Key}\")]")
                    .AppendLine($"{Separator}{Separator}{returnType} {operationName}({parametersString});")
                    .AppendLine();

                if (parametersString.Contains("?") && settings is { OptionalParameters: true, ApizrSettings: not null })
                {
                    this.docGenerator.AppendMethodDocumentation(operationModel, IsApiResponseType(returnType), false, hasApizrRequestOptionsParameter, hasCancellationToken, sb);
                    GenerateObsoleteAttribute(operation, sb);
                    GenerateForMultipartFormData(operationModel, sb);
                    GenerateHeaders(operations, operation, operationModel, sb);

                    parametersString = string.Join(", ", parameters.Where(parameter => !parameter.Contains("?")));

                    sb.AppendLine($"{Separator}{Separator}[{verb}(\"{op.PathItem.Key}\")]")
                        .AppendLine($"{Separator}{Separator}{returnType} {operationName}({parametersString});")
                        .AppendLine();
                }
            }
        }

        foreach (var keyValuePair in interfacesByGroup)
        {
            var code = new StringBuilder();
            var key = keyValuePair.Key;
            var value = keyValuePair.Value;

            while (char.IsWhiteSpace(value[value.Length - 1]))
            {
                value.Length--;
            }

            code.AppendLine(value.ToString());
            code.AppendLine($"{Separator}}}");

            yield return new GeneratedCode(
                interfacesNamesByGroup[key],
                code.ToString());
        }
    }

    private string GetGroupName(OpenApiOperation operation, string ungroupedTitle)
    {
        if (operation.Tags.FirstOrDefault() is string group && !string.IsNullOrWhiteSpace(group))
        {
            return group.SanitizeControllerTag();
        }

        return ungroupedTitle;
    }

    private string GetInterfaceName(string name)
    {
        var generatedName = IdentifierUtils.Counted(
            knownIdentifiers,
            $"I{name.CapitalizeFirstCharacter()}",
            suffix: "Api"
            );

        knownIdentifiers.Add(generatedName);
        return generatedName;
    }

    private string GetOperationName(
        string interfaceName,
        string name,
        string verb,
        OpenApiOperation operation)
    {
        var generatedName = IdentifierUtils.Counted(knownIdentifiers, GenerateOperationName(name, verb, operation, capitalizeFirstCharacter: true), parent: interfaceName);
        knownIdentifiers.Add($"{interfaceName}.{generatedName}");
        return generatedName;
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
