using System.Text;
using System.Text.RegularExpressions;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class RefitInterfaceGenerator : IRefitInterfaceGenerator
{
    protected const string Separator = "    ";

    protected readonly RefitGeneratorSettings settings;
    protected readonly OpenApiDocument document;
    protected readonly CustomCSharpClientGenerator generator;
    protected readonly XmlDocumentationGenerator docGenerator;

    internal RefitInterfaceGenerator(
        RefitGeneratorSettings settings,
        OpenApiDocument document,
        CustomCSharpClientGenerator generator,
        XmlDocumentationGenerator docGenerator)
    {
        this.settings = settings;
        this.document = document;
        this.generator = generator;
        this.docGenerator = docGenerator;
        generator.BaseSettings.OperationNameGenerator = new OperationNameGenerator(document, settings);
    }

    public virtual IEnumerable<GeneratedCode> GenerateCode()
    {
        var interfaceDeclaration = GenerateInterfaceDeclaration(out var interfaceName);
        yield return new GeneratedCode(
            interfaceName,
            $$"""
              {{interfaceDeclaration}}
              {{Separator}}{
              {{GenerateInterfaceBody(out var dynamicQuerystringParameters)}}
              {{Separator}}}
              {{dynamicQuerystringParameters}}
              """);
    }

    private string GenerateInterfaceBody(out string? dynamicQuerystringParameters)
    {
        var code = new StringBuilder();
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
                var operationName = GenerateOperationName(kv.Key, verb, operation);
                var dynamicQuerystringParameterType = operationName + "QueryParams";

                var operationModel = generator.CreateOperationModel(operation);
                var parameters = ParameterExtractor.GetParameters(operationModel, operation, settings, dynamicQuerystringParameterType, out var operationDynamicQuerystringParameters).ToList();

                var hasDynamicQuerystringParameter = !string.IsNullOrWhiteSpace(operationDynamicQuerystringParameters);
                if (hasDynamicQuerystringParameter)
                    dynamicQuerystringParametersCodeBuilder.AppendLine(operationDynamicQuerystringParameters);

                var parametersString = string.Join(", ", parameters);
                var hasApizrRequestOptionsParameter = settings.ApizrSettings?.WithRequestOptions == true;
                var hasCancellationToken = settings.UseCancellationTokens && !hasApizrRequestOptionsParameter;

                if (settings.GenerateXmlDocCodeComments)
                {
                    this.docGenerator.AppendMethodDocumentation(operationModel, IsApiResponseType(returnType), hasDynamicQuerystringParameter, hasApizrRequestOptionsParameter, hasCancellationToken, code);
                }

                GenerateObsoleteAttribute(operation, code);
                GenerateForMultipartFormData(operationModel, code);
                GenerateHeaders(operations, operation, operationModel, code);

                code.AppendLine($"{Separator}{Separator}[{verb}(\"{kv.Key}\")]")
                    .AppendLine($"{Separator}{Separator}{returnType} {operationName}({parametersString});")
                    .AppendLine();

                if (parametersString.Contains("?") && settings is { OptionalParameters: true, ApizrSettings: not null })
                {
                    if (settings.GenerateXmlDocCodeComments)
                    {
                        this.docGenerator.AppendMethodDocumentation(operationModel, IsApiResponseType(returnType), false, hasApizrRequestOptionsParameter, hasCancellationToken, code);
                    }
                    GenerateObsoleteAttribute(operation, code);
                    GenerateForMultipartFormData(operationModel, code);
                    GenerateHeaders(operations, operation, operationModel, code);

                    parametersString = string.Join(", ", parameters.Where(parameter => !parameter.Contains("?")));

                    code.AppendLine($"{Separator}{Separator}[{verb}(\"{kv.Key}\")]")
                        .AppendLine($"{Separator}{Separator}{returnType} {operationName}({parametersString});")
                        .AppendLine();
                }
            }
        }

        dynamicQuerystringParameters = dynamicQuerystringParametersCodeBuilder.ToString();

        return code.ToString();
    }

    protected string GetTypeName(OpenApiOperation operation)
    {
        if (settings.ResponseTypeOverride.TryGetValue(operation.OperationId, out var type))
        {
            return type is null or "void" ? GetAsyncOperationType(true) : $"{GetAsyncOperationType(false)}<{WellKnownNamespaces.TrimImportedNamespaces(type)}>";
        }

        // Check if response is a file stream
        if (IsFileStreamResponse(operation))
        {
            return $"{GetAsyncOperationType(false)}<HttpResponseMessage>";
        }

        // First check for explicit success status codes
        var successCodes = new[] { "200", "201", "203", "206" };
        var returnTypeParameter = successCodes
            .Where(operation.Responses.ContainsKey)
            .Select(code => GetTypeName(code, operation))
            .FirstOrDefault();

        // If no explicit success codes found, check for 2XX range
        if (returnTypeParameter == null && operation.Responses.ContainsKey("2XX"))
        {
            returnTypeParameter = GetTypeName("2XX", operation);
        }

        // If no success codes or ranges found, check for default response
        if (returnTypeParameter == null && operation.Responses.ContainsKey("default"))
        {
            returnTypeParameter = GetTypeName("default", operation);
        }

        return GetReturnType(returnTypeParameter);
    }

    /// <summary>
    /// Determines if the operation response is a file stream (binary content).
    /// </summary>
    /// <param name="operation">The OpenAPI operation to check.</param>
    /// <returns>True if the response is a file stream, false otherwise.</returns>
    private static bool IsFileStreamResponse(OpenApiOperation operation)
    {
        var successCodes = new[] { "200", "201", "203", "206", "2XX" };

        foreach (var code in successCodes)
        {
            if (!operation.Responses.TryGetValue(code, out var apiResponse))
                continue;

            var response = apiResponse.ActualResponse;

            if (response.Content?.Any() != true)
                continue;

            foreach (var contentEntry in response.Content)
            {
                if (IsFileContentType(contentEntry.Key))
                {
                    var schema = contentEntry.Value?.Schema;
                    if (schema?.Format == "binary" || schema?.Type == NJsonSchema.JsonObjectType.File)
                        return true;
                }
            }
        }

        return false;
    }

    private static bool IsFileContentType(string contentType)
    {
        return
            contentType.StartsWith("application/octet-stream", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("application/vnd", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("application/zip", StringComparison.OrdinalIgnoreCase) ||
            contentType.StartsWith("application/gzip", StringComparison.OrdinalIgnoreCase) ||
            (contentType.StartsWith("application/x-", StringComparison.OrdinalIgnoreCase) &&
             !contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase));
    }

    private string GetTypeName(string code, OpenApiOperation operation)
    {
        var schema = operation.Responses[code].ActualResponse.Schema;
        var typeName = generator.GetTypeName(schema, false, null);

        if (!string.IsNullOrWhiteSpace(settings.CodeGeneratorSettings?.ArrayType) &&
            schema?.Type == NJsonSchema.JsonObjectType.Array)
        {
            typeName = typeName
                .Replace("ICollection", settings.CodeGeneratorSettings!.ArrayType)
                .Replace("IEnumerable", settings.CodeGeneratorSettings!.ArrayType);
        }

        return typeName;
    }

    protected string GenerateOperationName(
        string path,
        string verb,
        OpenApiOperation operation,
        bool capitalizeFirstCharacter = false)
    {
        const string operationNamePlaceholder = "{operationName}";

        var operationName = generator
            .BaseSettings
            .OperationNameGenerator
            .GetOperationName(document, path, verb, operation);

        if (capitalizeFirstCharacter)
            operationName = operationName.CapitalizeFirstCharacter();

        if (settings.OperationNameTemplate?.Contains(operationNamePlaceholder) ?? false)
        {
            operationName = settings.OperationNameTemplate!
                .Replace(operationNamePlaceholder, operationName);
        }

        return operationName;
    }

    protected static void GenerateForMultipartFormData(CSharpOperationModel operationModel, StringBuilder code)
    {
        if (operationModel.Consumes.Contains("multipart/form-data"))
        {
            code.AppendLine($"{Separator}{Separator}[Multipart]");
        }
    }

    protected void GenerateHeaders(
        KeyValuePair<string, OpenApiOperation> operations,
        OpenApiOperation operation,
        CSharpOperationModel operationModel,
        StringBuilder code)
    {
        var headers = new List<string>();

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
                headers.Add($"\"Accept: {string.Join(", ", uniqueContentTypes)}\"");
            }
        }

        if (settings.AddContentTypeHeaders && document.SchemaType is >= NJsonSchema.SchemaType.OpenApi3)
        {
            var uniqueContentTypes = operations.Value.RequestBody?.Content.Keys ?? Array.Empty<string>();
            var contentType =
                uniqueContentTypes.FirstOrDefault(c => c.Equals("application/json", StringComparison.OrdinalIgnoreCase)) ??
                uniqueContentTypes.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(contentType) && !operationModel.Consumes.Contains("multipart/form-data"))
            {
                headers.Add($"\"Content-Type: {contentType}\"");
            }
        }

        if (headers.Any())
        {
            code.AppendLine($"{Separator}{Separator}[Headers({string.Join(", ", headers)})]");
        }
    }

    protected string GetReturnType(string? returnTypeParameter)
    {
        return returnTypeParameter is null or "void"
            ? GetDefaultReturnType()
            : GetConfiguredReturnType(returnTypeParameter);
    }

    private string GetDefaultReturnType()
    {
        var asyncType = GetAsyncOperationType(true);
        return settings.ReturnIApiResponse
            ? $"{asyncType}<IApiResponse>"
            : asyncType;
    }

    /// <summary>
    /// Checks if the given return type is derived from <c>ApiResponse</c> or its interface.
    /// </summary>
    /// <param name="typeName">The name of the type to check.</param>
    /// <returns>True if the type is an ApiResponse Task or similar, false otherwise.</returns>
    protected static bool IsApiResponseType(string typeName)
    {
        // Check for HttpResponseMessage
        if (Regex.IsMatch(
            typeName,
            "(Task|IObservable)<HttpResponseMessage>",
            RegexOptions.None,
            TimeSpan.FromSeconds(1)))
        {
            return true;
        }

        return Regex.IsMatch(
            typeName,
            "(Task|IObservable)<(I)?ApiResponse(<[\\w<>]+>)?>",
            RegexOptions.None,
            TimeSpan.FromSeconds(1));
    }

    private string GetConfiguredReturnType(string returnTypeParameter)
    {
        var asyncType = GetAsyncOperationType(false);
        return settings.ReturnIApiResponse
            ? $"{asyncType}<IApiResponse<{WellKnownNamespaces.TrimImportedNamespaces(returnTypeParameter)}>>"
            : $"{asyncType}<{WellKnownNamespaces.TrimImportedNamespaces(returnTypeParameter)}>";
    }

    private string GetAsyncOperationType(bool withVoidReturnType)
    {
        var type = withVoidReturnType ? "<Unit>" : string.Empty;
        return settings.ReturnIObservable
            ? "IObservable" + type
            : "Task";
    }

    protected void GenerateObsoleteAttribute(OpenApiOperation operation, StringBuilder code)
    {
        if (operation.IsDeprecated)
        {
            code.AppendLine($"{Separator}{Separator}[System.Obsolete]");
        }
    }

    private string GenerateInterfaceDeclaration(out string interfaceName)
    {
        var title = settings.Naming.UseOpenApiTitle
            ? (document.Info?.Title ?? NamingSettings.DefaultInterfaceName).Sanitize()
            : settings.Naming.InterfaceName;

        interfaceName = $"I{title.CapitalizeFirstCharacter()}";

        var inheritance = settings.GenerateDisposableClients
            ? " : IDisposable"
            : null;

        var modifier = settings.TypeAccessibility.ToString().ToLowerInvariant();
        var code = new StringBuilder();
        docGenerator.AppendInterfaceDocumentation(document, code);
        code.Append($"""
                {Separator}{GetGeneratedCodeAttribute()}
                {Separator}{modifier} partial interface {interfaceName}{inheritance}
                """);
        return code.ToString();
    }

    protected string GetGeneratedCodeAttribute() =>
        $"""
         [System.CodeDom.Compiler.GeneratedCode("Refitter", "{GetType().Assembly.GetName().Version}")]
         """;
}
