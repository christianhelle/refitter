using System.Text.RegularExpressions;
using NSwag;

namespace Refitter.Core;

internal class ReturnTypeGenerator(
    ICodeGenerationConfiguration codeGeneration,
    CustomCSharpClientGenerator generator)
    : IReturnTypeGenerator
{
    private static readonly Regex HttpResponseMessageTypeRegex = new(
        "(Task|IObservable)<HttpResponseMessage>",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly Regex ApiResponseTypeRegex = new(
        "(Task|IObservable)<(I)?ApiResponse(<[\\w<>]+>)?>",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    public string Generate(OpenApiOperation operation)
    {
        if (codeGeneration.ResponseTypeOverride.TryGetValue(operation.OperationId, out var type))
        {
            return type is null or "void"
                ? GetAsyncOperationType(true)
                : $"{GetAsyncOperationType(false)}<{TrimImportedNamespaces(type)}>";
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

    public bool IsApiResponseType(string typeName)
    {
        return HttpResponseMessageTypeRegex.IsMatch(typeName) ||
               ApiResponseTypeRegex.IsMatch(typeName);
    }

    public bool IsFileStreamResponse(OpenApiOperation operation)
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

        if (!string.IsNullOrWhiteSpace(codeGeneration.CodeGeneratorSettings?.ArrayType) &&
            schema?.Type == NJsonSchema.JsonObjectType.Array)
        {
            typeName = typeName
                .Replace("ICollection", codeGeneration.CodeGeneratorSettings!.ArrayType)
                .Replace("IEnumerable", codeGeneration.CodeGeneratorSettings!.ArrayType);
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
        return codeGeneration.ReturnIApiResponse
            ? $"{asyncType}<IApiResponse>"
            : asyncType;
    }

    private string GetConfiguredReturnType(string returnTypeParameter)
    {
        var asyncType = GetAsyncOperationType(false);
        return codeGeneration.ReturnIApiResponse
            ? $"{asyncType}<IApiResponse<{TrimImportedNamespaces(returnTypeParameter)}>>"
            : $"{asyncType}<{TrimImportedNamespaces(returnTypeParameter)}>";
    }

    private string GetAsyncOperationType(bool withVoidReturnType)
    {
        var type = withVoidReturnType ? "<Unit>" : string.Empty;
        return codeGeneration.ReturnIObservable
            ? "IObservable" + type
            : "Task";
    }

    private static string TrimImportedNamespaces(string returnTypeParameter) =>
        returnTypeParameter.StartsWith("System.Collections.Generic.", StringComparison.OrdinalIgnoreCase)
            ? returnTypeParameter.Replace("System.Collections.Generic.", string.Empty)
            : returnTypeParameter;
}
