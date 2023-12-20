using System.Text;

using NSwag;

namespace Refitter.Core;

internal class RefitMultipleInterfaceByTagGenerator : RefitInterfaceGenerator
{
    private readonly HashSet<string> knownIdentifiers = new();

    internal RefitMultipleInterfaceByTagGenerator(
        RefitGeneratorSettings settings,
        OpenApiDocument document,
        CustomCSharpClientGenerator generator)
        : base(settings, document, generator)
    {
    }

    public override RefitGeneratedCode GenerateCode()
    {
        var ungroupedTitle = settings.Naming.UseOpenApiTitle
            ? IdentifierUtils.Sanitize(document.Info?.Title ?? "ApiClient")
            : settings.Naming.InterfaceName;
        ungroupedTitle = ungroupedTitle.CapitalizeFirstCharacter();

        var byGroup = document.Paths
            .SelectMany(x => x.Value, (k, v) => (PathItem: k, Operation: v))
            .GroupBy(x => GetGroupName(x.Operation.Value, ungroupedTitle), (k, v) => new { Key = k, Combined = v });

        Dictionary<string, StringBuilder> interfacesByGroup = new();
        var interfaceNames = new List<string>();
        
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
                    GenerateInterfaceXmlDocComments(operation, sb);

                    interfaceName = GetInterfaceName(kv.Key);
                    interfaceNames.Add(interfaceName);
                    sb.AppendLine($$"""
                                    {{GenerateInterfaceDeclaration(interfaceName)}}
                                    {{Separator}}{
                                    """);
                }

                var operationModel = generator.CreateOperationModel(operation);
                var parameters = ParameterExtractor.GetParameters(operationModel, operation, settings);
                var parametersString = string.Join(", ", parameters);

                GenerateMethodXmlDocComments(operation, sb);
                GenerateObsoleteAttribute(operation, sb);
                GenerateForMultipartFormData(operationModel, sb);
                GenerateAcceptHeaders(operations, operation, sb);

                var opName = GetOperationName(interfaceName, op.PathItem.Key, operations.Key, operation);
                sb.AppendLine($"{Separator}{Separator}[{verb}(\"{op.PathItem.Key}\")]")
                    .AppendLine($"{Separator}{Separator}{returnType} {opName}({parametersString});")
                    .AppendLine();
            }
        }

        var code = new StringBuilder();
        foreach (var value in interfacesByGroup.Select(kv => kv.Value))
        {
            while (char.IsWhiteSpace(value[value.Length - 1]))
            {
                value.Length--;
            }

            code.AppendLine(value.ToString());
            code.AppendLine($"{Separator}}}");
            code.AppendLine();
        }

        return new RefitGeneratedCode(code.ToString(), interfaceNames.ToArray());
    }

    private string GetGroupName(OpenApiOperation operation, string ungroupedTitle)
    {
        if (operation.Tags.FirstOrDefault() is string group && !string.IsNullOrWhiteSpace(group))
        {
            return IdentifierUtils.Sanitize(group)
                .CapitalizeFirstCharacter();
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