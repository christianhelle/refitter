using NSwag;

using System.Text;

using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class RefitInterfaceGenerator : IRefitInterfaceGenerator
{
    private const string Separator = "    ";

    private readonly RefitGeneratorSettings settings;
    private readonly OpenApiDocument document;
    private readonly CustomCSharpClientGenerator generator;

    internal RefitInterfaceGenerator(
        RefitGeneratorSettings settings,
        OpenApiDocument document,
        CustomCSharpClientGenerator generator)
    {
        this.settings = settings;
        this.document = document;
        this.generator = generator;
        generator.BaseSettings.OperationNameGenerator = new OperationNameGenerator(document);
    }

    public virtual string GenerateCode()
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
                GenerateForMultipartFormData(operationModel, code);
                GenerateAcceptHeaders(operations, operation, code);

                code.AppendLine($"{Separator}{Separator}[{verb}(\"{kv.Key}\")]")
                    .AppendLine($"{Separator}{Separator}{returnType} {name}({parametersString});")
                    .AppendLine();
            }
        }

        return code.ToString();
    }

    protected static void GenerateForMultipartFormData(CSharpOperationModel operationModel, StringBuilder code)
    {
        if (operationModel.Consumes.Contains("multipart/form-data"))
        {
            code.AppendLine($"{Separator}{Separator}[Multipart]");
        }
    }

    protected void GenerateAcceptHeaders(
        KeyValuePair<string, OpenApiOperation> operations, 
        OpenApiOperation operation, 
        StringBuilder code)
    {
        if (settings.AddAcceptHeaders && document.SchemaType is >= NJsonSchema.SchemaType.OpenApi3)
        {
            //Generate header "Accept"
            var contentTypes = operations.Value.Responses.Select(pair => operation.Responses[pair.Key].Content.Keys);
            
            //remove duplicates
            var uniqueContentTypes = contentTypes
                .GroupBy(x => x)
                .SelectMany(y => y.First())
                .Distinct()
                .ToList();

            if (uniqueContentTypes.Any())
            {
                code.AppendLine($"{Separator}{Separator}[Headers(\"Accept: {string.Join(", ", uniqueContentTypes)}\")]");
            }
        }
    }

    protected string GetReturnType(string? returnTypeParameter)
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

    protected void GenerateMethodXmlDocComments(OpenApiOperation operation, StringBuilder code)
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
            ? IdentifierUtils.Sanitize(document.Info?.Title ?? "ApiClient")
            : settings.Naming.InterfaceName;

        var modifier = settings.TypeAccessibility.ToString().ToLowerInvariant();
        return $"""
                {Separator}{GetGeneratedCodeAttribute()}
                {Separator}{modifier} interface I{title.CapitalizeFirstCharacter()}
                """;
    }

    protected void GenerateInterfaceXmlDocComments(OpenApiOperation operation, StringBuilder code)
    {
        if (!settings.GenerateXmlDocCodeComments ||
            string.IsNullOrWhiteSpace(operation.Summary))
            return;

        code.AppendLine(
            $"""
             {Separator}/// <summary>
             {Separator}/// {operation.Summary}
             {Separator}/// </summary>
             """);
    }

    protected string GetGeneratedCodeAttribute() =>
        $"""
         [System.CodeDom.Compiler.GeneratedCode("Refitter", "{GetType().Assembly.GetName().Version}")]
         """;
}
