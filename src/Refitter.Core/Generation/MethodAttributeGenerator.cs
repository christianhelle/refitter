
namespace Refitter.Core;

internal class MethodAttributeGenerator(
    RefitGeneratorSettings settings,
    ApiDocument document)
    : IMethodAttributeGenerator
{

    public string[] Generate(ApiOperation operation, OperationModel operationModel)
    {
        var attributes = new List<string>();

        if (operation.IsDeprecated)
        {
            attributes.Add("[System.Obsolete]");
        }

        if (operationModel.Consumes.Contains("multipart/form-data"))
        {
            attributes.Add("[Multipart]");
        }

        var headers = new List<string>();
        var isOpenApi3 = document.SchemaType is >= ApiSchemaType.OpenApi3;

        var acceptHeader = settings.AddAcceptHeaders && isOpenApi3 ? GetAcceptHeader(operation) : null;
        if (acceptHeader != null)
            headers.Add(acceptHeader);

        var contentTypeHeader = settings.AddContentTypeHeaders && isOpenApi3 ? GetContentTypeHeader(operation, operationModel) : null;
        if (contentTypeHeader != null)
            headers.Add(contentTypeHeader);

        if (settings.AuthenticationHeaderStyle == AuthenticationHeaderStyle.Method)
            headers.AddRange(GetAuthorizationHeaders(operationModel));

        if (headers.Any())
        {
            attributes.Add($"[Headers({string.Join(", ", headers)})]");
        }

        return attributes.ToArray();
    }

    private static string? GetAcceptHeader(ApiOperation operation)
    {
        // Responses declared as a $ref to a shared component carry no inline
        // content, so resolve through ActualResponse the way the return type
        // generator does - otherwise the Accept header is silently omitted.
        var uniqueContentTypes = new HashSet<string>(
            operation.Responses.Values.SelectMany(response => response.ActualResponse.Content.Keys),
            StringComparer.OrdinalIgnoreCase);

        return uniqueContentTypes.Any()
            ? $"\"Accept: {string.Join(", ", uniqueContentTypes.Select(ParameterNaming.EscapeString))}\""
            : null;
    }

    private static string? GetContentTypeHeader(ApiOperation operation, OperationModel operationModel)
    {
        var uniqueContentTypes = operation.RequestBody?.Content.Keys ?? Array.Empty<string>();
        var contentType =
            uniqueContentTypes.FirstOrDefault(c => c.Equals("application/json", StringComparison.OrdinalIgnoreCase)) ??
            uniqueContentTypes.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(contentType) || operationModel.Consumes.Contains("multipart/form-data"))
            return null;

        return $"\"Content-Type: {ParameterNaming.EscapeString(contentType)}\"";
    }

    private IEnumerable<string> GetAuthorizationHeaders(OperationModel operationModel) =>
        operationModel.Security
            .SelectMany(x => x.Keys)
            .Where(securitySchemeName => settings.SecurityScheme == null || securitySchemeName == settings.SecurityScheme)
            .Select(securitySchemeName => document.SecurityDefinitions.TryGetValue(securitySchemeName, out var securityScheme) ? securityScheme : null)
            .Where(securityScheme =>
                securityScheme is { Type: ApiSecuritySchemeType.Http, Scheme: var scheme } &&
                string.Equals(scheme, "bearer", StringComparison.OrdinalIgnoreCase))
            .Select(_ => "\"Authorization: Bearer\"");
}
