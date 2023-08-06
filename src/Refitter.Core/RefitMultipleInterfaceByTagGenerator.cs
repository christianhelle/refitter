using System.Text;

using NSwag;

namespace Refitter.Core;

internal class RefitMultipleInterfaceByTagGenerator : IRefitInterfaceGenerator
{
    private const string Separator = "    ";

    private readonly RefitGeneratorSettings settings;
    private readonly OpenApiDocument document;
    private readonly CustomCSharpClientGenerator generator;

    internal RefitMultipleInterfaceByTagGenerator(
        RefitGeneratorSettings settings,
        OpenApiDocument document,
        CustomCSharpClientGenerator generator)
    {
        this.settings = settings;
        this.document = document;
        this.generator = generator;
        generator.BaseSettings.OperationNameGenerator = new OperationNameGenerator(document);
    }

    public string GenerateCode()
    {
        var ungroupedTitle = settings.Naming.UseOpenApiTitle
            ? document.Info?.Title?
                  .Replace(" ", string.Empty)
                  .Replace("-", string.Empty)
                  .Replace(".", string.Empty) ??
              "ApiClient"
            : settings.Naming.InterfaceName;
        ungroupedTitle = ungroupedTitle.CapitalizeFirstCharacter();

        var byGroup = document.Paths
            .SelectMany(x => x.Value, (k, v) => (PathItem: k, Operation: v))
            .GroupBy(x => GetGroupName(x.Operation.Value, ungroupedTitle), (k, v) => new {Key = k, Combined = v});

        Dictionary<string, StringBuilder> interfacesByGroup = new();
        foreach (var kv in byGroup)
        {
            foreach (var op in kv.Combined)
            {
                var operations = op.Operation;
                var pathItem = op.PathItem;
                var operation = operations.Value;

                var returnTypeParameter = new[] {"200", "201", "203", "206"}
                    .Where(code => operation.Responses.ContainsKey(code))
                    .Select(code => generator.GetTypeName(operation.Responses[code].ActualResponse.Schema, true, null))
                    .FirstOrDefault();

                var returnType = GetReturnType(returnTypeParameter);

                var verb = operations.Key.CapitalizeFirstCharacter();

                if (!interfacesByGroup.TryGetValue(kv.Key, out var sb))
                {
                    interfacesByGroup[kv.Key] = sb = new StringBuilder();
                    GenerateInterfaceXmlDocComments(operation, sb);
                    sb.AppendLine($$"""
                                    {{GenerateInterfaceDeclaration(GetInterfaceName(kv.Key, sb))}}
                                    {{Separator}}{
                                    """);
                }

                var operationModel = generator.CreateOperationModel(operation);
                var parameters = ParameterExtractor.GetParameters(operationModel, operation, settings);
                var parametersString = string.Join(", ", parameters);

                GenerateMethodXmlDocComments(operation, sb);

                if (operationModel.Consumes.Contains("multipart/form-data"))
                {
                    sb.AppendLine($"{Separator}{Separator}[Multipart]");
                }

                var opName = GetOperationName(op.PathItem.Key, operations.Key, operation, returnType, sb);
                sb.AppendLine($"{Separator}{Separator}[{verb}(\"{op.PathItem.Key}\")]")
                    .AppendLine($"{Separator}{Separator}{returnType} {opName}({parametersString});")
                    .AppendLine();
            }
        }

        var code = new StringBuilder();
        foreach (var kv in interfacesByGroup)
        {
            while (char.IsWhiteSpace(kv.Value[kv.Value.Length - 1]))
            {
                kv.Value.Length--;
            }

            code.AppendLine(kv.Value.ToString());
            code.AppendLine($"{Separator}}}");
            code.AppendLine();
        }

        return code.ToString();
    }

    private string GetGroupName(OpenApiOperation operation, string ungroupedTitle)
    {
        if (operation.Tags.FirstOrDefault() is string group && !string.IsNullOrWhiteSpace(group))
        {
            return group.Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace(".", string.Empty)
                .CapitalizeFirstCharacter();
        }

        return ungroupedTitle;
    }

    private string GetInterfaceName(string name,
        StringBuilder stringBuilder) =>
        StringSuffixUtils.InterfaceNameWithCounter(
            stringBuilder,
            name.CapitalizeFirstCharacter());

    private string GetOperationName(
        string name,
        string verb,
        OpenApiOperation operation,
        string returnType,
        StringBuilder stringBuilder)
        => generator
            .BaseSettings
            .OperationNameGenerator
            .GetOperationName(document, name, verb, operation);

    private string GetReturnType(string? returnTypeParameter)
    {
        return returnTypeParameter is null or "void"
            ? "Task"
            : GetConfiguredReturnType(returnTypeParameter);
    }

    private string GetConfiguredReturnType(string returnTypeParameter)
    {
        return settings.ReturnIApiResponse
            ? $"Task<IApiResponse<{WellKnownNamesspaces.TrimImportedNamespaces(returnTypeParameter)}>>"
            : $"Task<{WellKnownNamesspaces.TrimImportedNamespaces(returnTypeParameter)}>";
    }

    private void GenerateInterfaceXmlDocComments(OpenApiOperation operation, StringBuilder code)
    {
        if (!settings.GenerateXmlDocCodeComments ||
            string.IsNullOrWhiteSpace(operation.Summary))
            return;

        code.AppendLine(
            $$"""
              {{Separator}}/// <summary>
              {{Separator}}/// {{operation.Summary}}
              {{Separator}}/// </summary>
              """);
    }

    private void GenerateMethodXmlDocComments(OpenApiOperation operation, StringBuilder code)
    {
        if (!settings.GenerateXmlDocCodeComments)
            return;

        if (!string.IsNullOrWhiteSpace(operation.Description))
        {
            code.AppendLine($"{Separator}{Separator}/// <summary>");

            foreach (var line in operation.Description.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None))
                code.AppendLine($"{Separator}{Separator}/// {line.Trim()}");

            code.AppendLine($"{Separator}{Separator}/// </summary>");
        }
    }

    private string GenerateInterfaceDeclaration(string name)
    {
        var modifier = settings.TypeAccessibility.ToString().ToLowerInvariant();
        return $"{Separator}{modifier} interface I{name.CapitalizeFirstCharacter()}Api";
    }
}