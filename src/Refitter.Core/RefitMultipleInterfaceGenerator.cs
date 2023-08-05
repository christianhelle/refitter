using System.Collections.Specialized;
using System.Text;

using NSwag;
using NSwag.CodeGeneration.OperationNameGenerators;

namespace Refitter.Core;

internal class RefitMultipleInterfaceGenerator : IRefitInterfaceGenerator
{
    private const string Separator = "    ";

    private readonly RefitGeneratorSettings settings;
    private readonly OpenApiDocument document;
    private readonly CustomCSharpClientGenerator generator;

    internal RefitMultipleInterfaceGenerator(
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
        var code = new StringBuilder();
        foreach (var kv in document.Paths)
        {
            foreach (var operations in kv.Value)
            {
                var operation = operations.Value;

                var returnTypeParameter = new[] { "200", "201", "203", "206" }
                    .Where(code => operation.Responses.ContainsKey(code))
                    .Select(code => generator.GetTypeName(operation.Responses[code].ActualResponse.Schema, true, null))
                    .FirstOrDefault();

                var returnType = GetReturnType(returnTypeParameter);

                var verb = operations.Key.CapitalizeFirstCharacter();

                GenerateInterfaceXmlDocComments(operation, code);
                code.AppendLine($$"""
                                  {{GenerateInterfaceDeclaration(GetInterfaceName(kv, verb, operation, code))}}
                                  {{Separator}}{
                                  """);

                var operationModel = generator.CreateOperationModel(operation);
                var parameters = ParameterExtractor.GetParameters(operationModel, operation, settings);
                var parametersString = string.Join(", ", parameters);

                GenerateMethodXmlDocComments(operation, code);

                if (operationModel.Consumes.Contains("multipart/form-data"))
                {
                    code.AppendLine($"{Separator}{Separator}[Multipart]");
                }

                code.AppendLine($"{Separator}{Separator}[{verb}(\"{kv.Key}\")]")
                    .AppendLine($"{Separator}{Separator}{returnType} Execute({parametersString});")
                    .AppendLine($"{Separator}}}")
                    .AppendLine();
            }
        }

        return code.ToString();
    }

    private string GetInterfaceName(
        KeyValuePair<string, OpenApiPathItem> kv,
        string verb,
        OpenApiOperation operation,
        StringBuilder stringBuilder) =>
        StringSuffixUtils.InterfaceNameWithCounter(
            stringBuilder,
            generator
                .BaseSettings
                .OperationNameGenerator
                .GetOperationName(document, kv.Key, verb, operation));

    

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

            foreach (var line in operation.Description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                code.AppendLine($"{Separator}{Separator}/// {line.Trim()}");

            code.AppendLine($"{Separator}{Separator}/// </summary>");
        }
    }

    private string GenerateInterfaceDeclaration(string name)
    {
        var modifier = settings.TypeAccessibility.ToString().ToLowerInvariant();
        return $"{Separator}{modifier} interface I{name.CapitalizeFirstCharacter()}Endpoint";
    }
}