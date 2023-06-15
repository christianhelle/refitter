using NSwag;
using System;
using System.Text;

namespace Refitter.Core;

public class RefitInterfaceGenerator
{
    private const string Separator = "    ";

    private readonly RefitGeneratorSettings settings;
    private readonly OpenApiDocument document;
    private readonly CustomCSharpClientGenerator generator;

    public RefitInterfaceGenerator(
        RefitGeneratorSettings settings,
        OpenApiDocument document,
        CustomCSharpClientGenerator generator)
    {
        this.settings = settings;
        this.document = document;
        this.generator = generator;
        generator.BaseSettings.OperationNameGenerator = new OperationNameGenerator(document);
    }

    public string GenerateRefitInterface()
    {
        return $$"""
                {{GenerateInterfaceDeclaration()}}
                {{Separator}}{
                {{GenerateInterfaceBody()}}
                {{Separator}}}
                """;
    }

    private string GenerateInterfaceBody()
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

                var name = generator.BaseSettings.OperationNameGenerator
                    .GetOperationName(document, kv.Key, verb, operation);

                var operationModel = generator.CreateOperationModel(operation);
                var parameters = ParameterExtractor.GetParameters(operationModel, operation, settings);
                var parametersString = string.Join(", ", parameters);

                GenerateMethodXmlDocComments(operation, code);

                if (operationModel.Consumes.Contains("multipart/form-data"))
                {
                    code.AppendLine($"{Separator}{Separator}[Multipart]");
                }

                code.AppendLine($"{Separator}{Separator}[{verb}(\"{kv.Key}\")]")
                    .AppendLine($"{Separator}{Separator}{returnType} {name}({parametersString});")
                    .AppendLine();
            }
        }

        return code.ToString();
    }

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

    private string GenerateInterfaceDeclaration()
    {
        var title = settings.Naming.UseOpenApiTitle
            ? document.Info?.Title?
                  .Replace(" ", string.Empty)
                  .Replace("-", string.Empty)
                  .Replace(".", string.Empty) ??
              "ApiClient"
            : settings.Naming.InterfaceName;

        var modifier = settings.TypeAccessibility.ToString().ToLowerInvariant();
        return $"{Separator}{modifier} interface I{title.CapitalizeFirstCharacter()}";
    }
}
