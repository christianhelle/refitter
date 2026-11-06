using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal static class HeaderParameterExtractor
{
    public static IEnumerable<string> Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings)
    {
        var headerParameters = settings.GenerateOperationHeaders
            ? GetOperationHeaderParameters(operationModel, settings)
            : new List<string>();

        if (settings.AuthenticationHeaderStyle == AuthenticationHeaderStyle.Parameter)
        {
            headerParameters.AddRange(GetAuthenticationHeaderParameters(operationModel, operation, settings));
        }

        return headerParameters;
    }

    private static List<string> GetOperationHeaderParameters(
        CSharpOperationModel operationModel,
        RefitGeneratorSettings settings)
    {
        var ignoredHeaders = settings.IgnoredOperationHeaders
            .Select(h => h.Trim())
            .Where(h => !string.IsNullOrEmpty(h))
            .ToArray();

        var anyIgnoredHeaders = ignoredHeaders.Any();

        return operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Header && p.IsHeader)
            .Where(p => !anyIgnoredHeaders || !ignoredHeaders.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .Select(p =>
            {
                var variableName = ParameterNaming.GetVariableName(p);
                return $"{ParameterAttributeFormatter.JoinAttributes($"Header(\"{ParameterNaming.EscapeString(p.Name)}\")")}{ParameterTypeResolver.GetHeaderParameterType(p, settings)} {variableName}";
            })
            .ToList();
    }

    private static IEnumerable<string> GetAuthenticationHeaderParameters(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings)
    {
        var document = operation.Parent.Parent;
        foreach (var securitySchemeName in operationModel.Security.SelectMany(x => x.Keys))
        {
            if ((settings.SecurityScheme != null && securitySchemeName != settings.SecurityScheme) ||
                !document.SecurityDefinitions.TryGetValue(securitySchemeName, out var securityScheme))
            {
                continue;
            }

            if (securityScheme.Type == OpenApiSecuritySchemeType.ApiKey
                && securityScheme.In == OpenApiSecurityApiKeyLocation.Header
                && !operationModel.Parameters.Any(p => p.Kind == OpenApiParameterKind.Header && p.IsHeader && p.Name == securityScheme.Name))
            {
                yield return $"[Header(\"{ParameterNaming.EscapeString(securityScheme.Name)}\")] string {ParameterNaming.ReplaceUnsafeCharacters(securityScheme.Name)}";
            }
            else if (securityScheme is { Type: OpenApiSecuritySchemeType.Http }
                && string.Equals(securityScheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase))
            {
                yield return @"[Header(""Authorization: Bearer"")] string bearerToken";
            }
        }
    }
}
