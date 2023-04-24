using System;
using System.Text;
using NSwag;

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
    }

    public string GenerateRefitInterface()
    {
        var code = new StringBuilder();
        code.AppendLine(GenerateInterfaceDeclaration());
        code.AppendLine(GenerateInterfaceBody());
        return code.ToString();
    }

    private string GenerateInterfaceBody()
    {
        var code = new StringBuilder();
        foreach (var kv in document.Paths)
        {
            foreach (var operations in kv.Value)
            {
                var operation = operations.Value;

                var returnTypeParameter = operation.Responses.ContainsKey("200")
                    ? generator.GetTypeName(operation.Responses["200"].Schema, true, null)
                    : null;

                var returnType = GetReturnType(returnTypeParameter);

                var verb = operations.Key.CapitalizeFirstCharacter();

                var name = generator.BaseSettings.OperationNameGenerator
                    .GetOperationName(document, kv.Key, verb, operation)
                    .CapitalizeFirstCharacter()
                    .ConvertKebabCaseToPascalCase();

                var parameters = ParameterExtractor.GetParameters(generator, operation);
                var parametersString = string.Join(", ", parameters);

                GenerateMethodXmlDocComments(operation, code);

                code.AppendLine($"{Separator}{Separator}[{verb}(\"{kv.Key}\")]")
                    .AppendLine($"{Separator}{Separator}{returnType} {name}({parametersString});")
                    .AppendLine();
            }
        }

        code.AppendLine($"{Separator}}}");
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

        var code = new StringBuilder();
        var modifier = settings.TypeAccessibility.ToString().ToLowerInvariant();
        code.AppendLine($"{Separator}{modifier} interface I{title.CapitalizeFirstCharacter()}")
            .AppendLine($"{Separator}{{");
        return code.ToString();
    }
}