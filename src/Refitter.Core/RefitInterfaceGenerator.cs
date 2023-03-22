using System.Text;
using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace Refitter.Core;

public class RefitInterfaceGenerator
{
    private const string Separator = "    ";

    private readonly RefitGeneratorSettings settings;
    private readonly OpenApiDocument document;
    private readonly CSharpClientGenerator generator;

    public RefitInterfaceGenerator(
        RefitGeneratorSettings settings,
        OpenApiDocument document,
        CSharpClientGenerator generator)
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

                var returnType = returnTypeParameter is null or "void"
                    ? "Task"
                    : $"Task<{WellKnownNamesspaces.TrimImportedNamespaces(returnTypeParameter)}>";

                var verb = operations.Key.CapitalizeFirstCharacter();
                var name = operation.OperationId.CapitalizeFirstCharacter().ConvertKebabCaseToPascalCase();

                var parameters = ParameterExtractor.GetParameters(generator, operation);
                var parametersString = string.Join(", ", parameters);

                GenerateMethodXmlDocComments(operation, code);

                code.AppendLine($"{Separator}{Separator}[{verb}(\"{kv.Key.ConvertKebabCaseToCamelCase()}\")]")
                    .AppendLine($"{Separator}{Separator}{returnType} {name}({parametersString});")
                    .AppendLine();
            }
        }

        code.AppendLine($"{Separator}}}");
        return code.ToString();
    }

    private void GenerateMethodXmlDocComments(OpenApiOperation operation, StringBuilder code)
    {
        if (!settings.GenerateXmlDocCodeComments)
            return;

        if (!string.IsNullOrWhiteSpace(operation.Description))
        {
            code.AppendLine($"{Separator}{Separator}/// <summary>")
                .AppendLine($"{Separator}{Separator}/// " + operation.Description)
                .AppendLine($"{Separator}{Separator}/// </summary>");
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

        StringBuilder code = new StringBuilder();
        code.AppendLine($"{Separator}public interface I{title.CapitalizeFirstCharacter()}")
            .AppendLine($"{Separator}{{");
        return code.ToString();
    }
}