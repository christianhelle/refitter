using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal class HeaderParameterExtractor : IParameterTypeExtractor
{
    public IEnumerable<string> Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings)
    {
        var headerParameters = new List<string>();

        if (settings.GenerateOperationHeaders)
        {
            var ignoredHeaders = settings.IgnoredOperationHeaders
                .Select(h => h.Trim())
                .Where(h => !string.IsNullOrEmpty(h))
                .ToArray();

            var anyIgnoredHeaders = ignoredHeaders.Any();

            headerParameters = operationModel.Parameters
                .Where(p => p.Kind == OpenApiParameterKind.Header && p.IsHeader)
                .Where(p => !anyIgnoredHeaders || !ignoredHeaders.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .Select(p =>
                {
                    var variableName = ParameterShared.GetVariableName(p);
                    return $"{ParameterShared.JoinAttributes($"Header(\"{ParameterShared.EscapeString(p.Name)}\")")}{ParameterShared.GetParameterType(p, settings)} {variableName}";
                })
                .ToList();
        }

        if (settings.AuthenticationHeaderStyle == AuthenticationHeaderStyle.Parameter)
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
                    headerParameters.Add($"[Header(\"{ParameterShared.EscapeString(securityScheme.Name)}\")] string {ParameterShared.ReplaceUnsafeCharacters(securityScheme.Name)}");
                }
                else if (securityScheme is { Type: OpenApiSecuritySchemeType.Http }
                    && string.Equals(securityScheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase))
                {
                    headerParameters.Add(@"[Header(""Authorization: Bearer"")] string bearerToken");
                }
            }
        }

        return headerParameters;
    }
}
