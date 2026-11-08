using NSwag;
using NSwag.CodeGeneration.CSharp.Models;

namespace Refitter.Core;

internal sealed class HeaderParameterExtractor
{
    public IEnumerable<MethodParameter> Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings)
    {
        var headerParameters = new List<MethodParameter>();

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
                    var variableName = ParameterNaming.GetVariableName(p);
                    return new MethodParameter(ParameterTypeResolver.GetHeaderParameterType(p, settings), variableName)
                    {
                        Attributes = [GetHeaderAttribute(p.Name)],
                    };
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
                    headerParameters.Add(
                        new MethodParameter("string", ParameterNaming.ReplaceUnsafeCharacters(securityScheme.Name))
                        {
                            Attributes = [GetHeaderAttribute(securityScheme.Name)],
                        });
                }
                else if (securityScheme is { Type: OpenApiSecuritySchemeType.Http }
                    && string.Equals(securityScheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase))
                {
                    headerParameters.Add(
                        new MethodParameter("string", "bearerToken")
                        {
                            Attributes = [GetHeaderAttribute("Authorization: Bearer")],
                        });
                }
            }
        }

        return headerParameters;
    }

    private static string GetHeaderAttribute(string name) =>
        $"Header(\"{ParameterNaming.EscapeString(name)}\")";
}
