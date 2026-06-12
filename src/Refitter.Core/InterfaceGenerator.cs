using System.Text;
using System.Text.RegularExpressions;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class InterfaceGenerator
{
    private const string Separator = "    ";
    private static readonly Regex HttpResponseMessageTypeRegex = new("(Task|IObservable)<HttpResponseMessage>", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex ApiResponseTypeRegex = new("(Task|IObservable)<(I)?ApiResponse(<[\\w<>]+>)?>", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private readonly RefitGeneratorSettings settings;
    private readonly OpenApiDocument document;
    private readonly CustomCSharpClientGenerator generator;
    private readonly XmlDocumentationGenerator docGenerator;
    private readonly IParameterExtractor parameterExtractor;

    internal InterfaceGenerator(
        RefitGeneratorSettings settings,
        OpenApiDocument document,
        CustomCSharpClientGenerator generator,
        XmlDocumentationGenerator docGenerator,
        IParameterExtractor? parameterExtractor = null)
    {
        this.settings = settings;
        this.document = document;
        this.generator = generator;
        this.docGenerator = docGenerator;
        this.parameterExtractor = parameterExtractor ?? new ParameterAggregator();
        generator.BaseSettings.OperationNameGenerator = new OperationNameGenerator(document, settings);
    }

    public IEnumerable<GeneratedCode> Generate(IInterfacePartitioning partitioning)
    {
        var operations = document.Paths
            .SelectMany(path => path.Value, (path, op) => new OpenApiOperationInfo(path.Key, op.Key, op.Value))
            .ToList();

        var groups = operations
            .GroupBy(partitioning.GetGroupKey)
            .ToList();

        var knownInterfaceIdentifiers = new HashSet<string>();
        var title = settings.Naming.UseOpenApiTitle && !string.IsNullOrWhiteSpace(document.Info?.Title)
            ? document.Info!.Title.Sanitize()
            : settings.Naming.InterfaceName;

        if (partitioning.IsSingleInterface)
        {
            var singleGroup = groups.Count > 0 ? groups[0].ToList() : new List<OpenApiOperationInfo>();
            yield return GenerateSingleInterface(singleGroup, partitioning, title, knownInterfaceIdentifiers);
        }
        else
        {
            foreach (var group in groups)
            {
                var nonDeprecatedOperations = group
                    .Where(op => settings.GenerateDeprecatedOperations || !op.Operation.IsDeprecated)
                    .ToList();

                if (nonDeprecatedOperations.Count == 0)
                    continue;

                foreach (var generatedCode in GenerateMultipleInterface(nonDeprecatedOperations, partitioning, title, knownInterfaceIdentifiers))
                {
                    yield return generatedCode;
                }
            }
        }
    }

    private GeneratedCode GenerateSingleInterface(
        List<OpenApiOperationInfo> operations,
        IInterfacePartitioning partitioning,
        string title,
        HashSet<string> knownInterfaceIdentifiers)
    {
        var baseOperationName = operations.Count > 0 ? GetBaseOperationName(operations[0]) : string.Empty;
        var rawInterfaceName = partitioning.GetInterfaceName(string.Empty, title, baseOperationName);
        var interfaceNameSuffix = partitioning.GetInterfaceNameSuffix();
        var interfaceName = IdentifierUtils.Counted(knownInterfaceIdentifiers, rawInterfaceName, interfaceNameSuffix);

        var code = new StringBuilder();
        var dynamicQuerystringParametersCodeBuilder = new StringBuilder();
        var representativeOperation = operations.Count > 0
            ? operations[0]
            : new OpenApiOperationInfo(string.Empty, string.Empty, new OpenApiOperation());
        partitioning.AppendInterfaceDocumentation(document, docGenerator, string.Empty, representativeOperation, code);
        var interfaceDeclaration = GenerateInterfaceDeclaration(interfaceName, partitioning.IsSingleInterface);
        code.AppendLine(interfaceDeclaration);
        code.AppendLine($"{Separator}{{");

        var knownMethodIdentifiers = new HashSet<string>();

        foreach (var op in operations)
        {
            var (dynamicQuerystringParams, _) = GenerateMethod(op, interfaceName, partitioning, knownMethodIdentifiers, code);
            if (!string.IsNullOrWhiteSpace(dynamicQuerystringParams))
            {
                dynamicQuerystringParametersCodeBuilder.AppendLine(dynamicQuerystringParams);
            }
        }

        code.AppendLine($"{Separator}}}");
        code.AppendLine(dynamicQuerystringParametersCodeBuilder.ToString());

        return new GeneratedCode(interfaceName, code.ToString());
    }

    private IEnumerable<GeneratedCode> GenerateMultipleInterface(
        List<OpenApiOperationInfo> operations,
        IInterfacePartitioning partitioning,
        string title,
        HashSet<string> knownInterfaceIdentifiers)
    {
        var representativeOperation = operations[0];
        var baseOperationName = GetBaseOperationName(representativeOperation);
        var groupKey = partitioning.GetGroupKey(representativeOperation);
        var rawInterfaceName = partitioning.GetInterfaceName(groupKey, title, baseOperationName);
        var interfaceNameSuffix = partitioning.GetInterfaceNameSuffix();
        var interfaceName = IdentifierUtils.Counted(knownInterfaceIdentifiers, rawInterfaceName, interfaceNameSuffix);

        var code = new StringBuilder();
        partitioning.AppendInterfaceDocumentation(document, docGenerator, groupKey, representativeOperation, code);
        var interfaceDeclaration = GenerateInterfaceDeclaration(interfaceName, partitioning.IsSingleInterface);
        code.AppendLine(interfaceDeclaration);
        code.AppendLine($"{Separator}{{");

        var knownMethodIdentifiers = new HashSet<string>();

        foreach (var op in operations)
        {
            var (dynamicQuerystringParams, dynamicQuerystringType) = GenerateMethod(op, interfaceName, partitioning, knownMethodIdentifiers, code);
            if (!string.IsNullOrWhiteSpace(dynamicQuerystringParams))
            {
                yield return new GeneratedCode(dynamicQuerystringType, dynamicQuerystringParams);
            }
        }

        code.AppendLine($"{Separator}}}");

        yield return new GeneratedCode(interfaceName, code.ToString());
    }

    private (string DynamicQuerystringParameters, string DynamicQuerystringParameterType) GenerateMethod(
        OpenApiOperationInfo op,
        string interfaceName,
        IInterfacePartitioning partitioning,
        HashSet<string> knownMethodIdentifiers,
        StringBuilder code)
    {
        var operation = op.Operation;

        if (!settings.GenerateDeprecatedOperations && operation.IsDeprecated)
        {
            return (string.Empty, string.Empty);
        }

        var returnType = GetTypeName(operation);
        var verb = op.Verb.CapitalizeFirstCharacter();
        var baseOperationName = GetBaseOperationName(op);
        var rawMethodName = partitioning.GetMethodName(op, interfaceName, baseOperationName);
        var methodName = IdentifierUtils.Counted(knownMethodIdentifiers, rawMethodName);
        knownMethodIdentifiers.Add(methodName);

        var dynamicQuerystringParameterType = partitioning.GetDynamicQuerystringParameterType(interfaceName, methodName);
        var operationModel = generator.CreateOperationModel(operation);
        var parameters = parameterExtractor.ExtractParameters(operationModel, operation, settings, dynamicQuerystringParameterType, out var operationDynamicQuerystringParameters).ToList();

        var hasDynamicQuerystringParameter = !string.IsNullOrWhiteSpace(operationDynamicQuerystringParameters);
        var parametersString = string.Join(", ", parameters);
        var hasApizrRequestOptionsParameter = settings.ApizrSettings?.WithRequestOptions == true;
        var hasCancellationToken = settings.UseCancellationTokens && !hasApizrRequestOptionsParameter;
        var isApiResponseType = IsApiResponseType(returnType);

        if (settings.GenerateXmlDocCodeComments)
        {
            docGenerator.AppendMethodDocumentation(operationModel, isApiResponseType, hasDynamicQuerystringParameter, hasApizrRequestOptionsParameter, hasCancellationToken, code);
        }

        GenerateObsoleteAttribute(operation, code);
        GenerateForMultipartFormData(operationModel, code);
        GenerateHeaders(operation, operationModel, code);

        code.AppendLine($"{Separator}{Separator}[{verb}(\"{op.Path}\")]")
            .AppendLine($"{Separator}{Separator}{returnType} {methodName}({parametersString});")
            .AppendLine();

        if (parametersString.Contains("?") && settings is { OptionalParameters: true, ApizrSettings: not null })
        {
            if (settings.GenerateXmlDocCodeComments)
            {
                docGenerator.AppendMethodDocumentation(operationModel, isApiResponseType, false, hasApizrRequestOptionsParameter, hasCancellationToken, code);
            }

            GenerateObsoleteAttribute(operation, code);
            GenerateForMultipartFormData(operationModel, code);
            GenerateHeaders(operation, operationModel, code);

            parametersString = string.Join(", ", parameters.Where(parameter => !parameter.Contains("?")));

            code.AppendLine($"{Separator}{Separator}[{verb}(\"{op.Path}\")]")
                .AppendLine($"{Separator}{Separator}{returnType} {methodName}({parametersString});")
                .AppendLine();
        }

        return (operationDynamicQuerystringParameters ?? string.Empty, dynamicQuerystringParameterType);
    }

    private string GetBaseOperationName(OpenApiOperationInfo op)
    {
        return generator
            .BaseSettings
            .OperationNameGenerator
            .GetOperationName(document, op.Path, op.Verb, op.Operation);
    }

    private string GetTypeName(OpenApiOperation operation)
    {
        if (settings.ResponseTypeOverride.TryGetValue(operation.OperationId, out var type))
        {
            return type is null or "void"
                ? GetAsyncOperationType(true)
                : $"{GetAsyncOperationType(false)}<{WellKnownNamespaces.TrimImportedNamespaces(type)}>";
        }

        if (IsFileStreamResponse(operation))
        {
            return $"{GetAsyncOperationType(false)}<HttpResponseMessage>";
        }

        var successCodes = new[] { "200", "201", "203", "206" };
        var returnTypeParameter = successCodes
            .Where(operation.Responses.ContainsKey)
            .Select(code => GetTypeName(code, operation))
            .FirstOrDefault();

        if (returnTypeParameter == null && operation.Responses.ContainsKey("2XX"))
        {
            returnTypeParameter = GetTypeName("2XX", operation);
        }

        if (returnTypeParameter == null && operation.Responses.ContainsKey("default"))
        {
            returnTypeParameter = GetTypeName("default", operation);
        }

        return GetReturnType(returnTypeParameter);
    }

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

    private string GetReturnType(string? returnTypeParameter)
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

    private static bool IsApiResponseType(string typeName)
    {
        return HttpResponseMessageTypeRegex.IsMatch(typeName) || ApiResponseTypeRegex.IsMatch(typeName);
    }

    private static void GenerateObsoleteAttribute(OpenApiOperation operation, StringBuilder code)
    {
        if (operation.IsDeprecated)
        {
            code.AppendLine($"{Separator}{Separator}[System.Obsolete]");
        }
    }

    private static void GenerateForMultipartFormData(CSharpOperationModel operationModel, StringBuilder code)
    {
        if (operationModel.Consumes.Contains("multipart/form-data"))
        {
            code.AppendLine($"{Separator}{Separator}[Multipart]");
        }
    }

    private void GenerateHeaders(
        OpenApiOperation operation,
        CSharpOperationModel operationModel,
        StringBuilder code)
    {
        var headers = new List<string>();

        if (settings.AddAcceptHeaders && document.SchemaType is >= NJsonSchema.SchemaType.OpenApi3)
        {
            var uniqueContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var response in operation.Responses.Values)
            {
                if (response.Content == null)
                    continue;

                foreach (var contentType in response.Content.Keys)
                {
                    uniqueContentTypes.Add(contentType);
                }
            }

            if (uniqueContentTypes.Any())
            {
                headers.Add($"\"Accept: {string.Join(", ", uniqueContentTypes)}\"");
            }
        }

        if (settings.AddContentTypeHeaders && document.SchemaType is >= NJsonSchema.SchemaType.OpenApi3)
        {
            var uniqueContentTypes = operation.RequestBody?.Content.Keys ?? Array.Empty<string>();
            var contentType =
                uniqueContentTypes.FirstOrDefault(c => c.Equals("application/json", StringComparison.OrdinalIgnoreCase)) ??
                uniqueContentTypes.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(contentType) && !operationModel.Consumes.Contains("multipart/form-data"))
            {
                headers.Add($"\"Content-Type: {contentType}\"");
            }
        }

        if (settings.AuthenticationHeaderStyle == AuthenticationHeaderStyle.Method)
        {
            foreach (var securitySchemeName in operationModel.Security.SelectMany(x => x.Keys))
            {
                if ((settings.SecurityScheme != null && securitySchemeName != settings.SecurityScheme) ||
                    !document.SecurityDefinitions.TryGetValue(securitySchemeName, out var securityScheme))
                {
                    continue;
                }

                if (securityScheme is { Type: OpenApiSecuritySchemeType.Http, Scheme: var scheme }
                    && string.Equals(scheme, "bearer", StringComparison.OrdinalIgnoreCase))
                {
                    headers.Add("\"Authorization: Bearer\"");
                }
            }
        }

        if (headers.Any())
        {
            code.AppendLine($"{Separator}{Separator}[Headers({string.Join(", ", headers)})]");
        }
    }

    private string GenerateInterfaceDeclaration(string interfaceName, bool isSingleInterface)
    {
        var inheritance = isSingleInterface && settings.GenerateDisposableClients
            ? " : IDisposable"
            : null;

        var modifier = settings.TypeAccessibility.ToString().ToLowerInvariant();
        return $"""
                {Separator}{GetGeneratedCodeAttribute()}
                {Separator}{modifier} partial interface {interfaceName}{inheritance}
                """;
    }

    private string GetGeneratedCodeAttribute() =>
        $"""
         [System.CodeDom.Compiler.GeneratedCode("Refitter", "{GetType().Assembly.GetName().Version}")]
         """;
}
