
namespace Refitter.Core;

internal sealed class HeaderParameterExtractor
{
    public IEnumerable<string> Extract(
        OperationModel operationModel,
        ApiOperation operation,
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
                .Where(p => p.Kind == ApiParameterKind.Header && p.IsHeader)
                .Where(p => !anyIgnoredHeaders || !ignoredHeaders.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .Select(p =>
                {
                    var variableName = ParameterNaming.GetVariableName(p);
                    return $"{ParameterAttributeFormatter.JoinAttributes($"Header(\"{ParameterNaming.EscapeString(p.Name)}\")")}{ParameterTypeResolver.GetHeaderParameterType(p, settings)} {variableName}";
                })
                .ToList();
        }

        if (settings.AuthenticationHeaderStyle == AuthenticationHeaderStyle.Parameter)
        {
            var document = operation.Parent!.Parent!;
            foreach (var securitySchemeName in operationModel.Security.SelectMany(x => x.Keys))
            {
                if ((settings.SecurityScheme != null && securitySchemeName != settings.SecurityScheme) ||
                    !document.SecurityDefinitions.TryGetValue(securitySchemeName, out var securityScheme))
                {
                    continue;
                }

                if (securityScheme.Type == ApiSecuritySchemeType.ApiKey
                    && securityScheme.In == ApiSecurityApiKeyLocation.Header
                    && !operationModel.Parameters.Any(p => p.Kind == ApiParameterKind.Header && p.IsHeader && p.Name == securityScheme.Name))
                {
                    headerParameters.Add($"[Header(\"{ParameterNaming.EscapeString(securityScheme.Name!)}\")] string {ParameterNaming.ReplaceUnsafeCharacters(securityScheme.Name!)}");
                }
                else if (securityScheme is { Type: ApiSecuritySchemeType.Http }
                    && string.Equals(securityScheme.Scheme, "bearer", StringComparison.OrdinalIgnoreCase))
                {
                    headerParameters.Add(@"[Header(""Authorization: Bearer"")] string bearerToken");
                }
            }
        }

        return headerParameters;
    }
}
