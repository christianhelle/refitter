using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Refitter.Core.Validation;

/// <summary>
/// Rejects OpenAPI paths, header names, and content-type keys containing characters that could break
/// out of the generated Refit attribute string literals (e.g. quotes, backslashes, newlines). Generated
/// code escapes these characters, but rejecting them gives a clear error unless validation is skipped.
/// See GHSA-3fhm-p725-h3g3 (path), GHSA-58x9-vjvp-6mx8 (header name), GHSA-p32v-8v8j-j534 (content-type)
/// and GHSA-7hgj-q592-v892 (network-path route).
/// </summary>
internal static class AttributeStringValidator
{
    internal static bool ContainsUnsafeCharacters(string? value) =>
        value != null && value.Any(c => c is '"' or '\\' or LineSeparator or ParagraphSeparator || char.IsControl(c));

    // C# ends a string literal at these Unicode line terminators, which are not control characters
    private const char LineSeparator = (char)0x2028;
    private const char ParagraphSeparator = (char)0x2029;

    internal static void Validate(
        OpenApiDocument? document,
        OpenApiDiagnostic diagnostic,
        IDictionary<string, IOpenApiSecurityScheme>? externalSecuritySchemes = null)
    {
        if (document == null)
            return;

        ValidateSecuritySchemes(document.Components?.SecuritySchemes, diagnostic);
        ValidateSecuritySchemes(externalSecuritySchemes, diagnostic);

        if (document.Paths == null)
            return;

        // Accept/Content-Type headers are only emitted from content map keys for OpenAPI 3.0+,
        // so only reject unsafe content-type keys for those documents to avoid Swagger 2.0 false positives.
        var validateContentTypes = diagnostic.SpecificationVersion != OpenApiSpecVersion.OpenApi2_0;

        foreach (var path in document.Paths)
        {
            ValidatePath(path.Key, diagnostic);

            if (path.Value?.Operations == null)
                continue;

            foreach (var operation in path.Value.Operations.Values)
            {
                if (operation == null)
                    continue;

                ValidateHeaderParameters(operation, diagnostic);

                if (validateContentTypes)
                    ValidateContentTypeKeys(operation, diagnostic);
            }
        }
    }

    private static void ValidateSecuritySchemes(
        IDictionary<string, IOpenApiSecurityScheme>? securitySchemes,
        OpenApiDiagnostic diagnostic)
    {
        if (securitySchemes == null)
            return;

        // Validate security scheme names for API-key schemes with header location
        foreach (var securityScheme in securitySchemes)
        {
            if (securityScheme.Value?.Type == SecuritySchemeType.ApiKey
                && securityScheme.Value.In == ParameterLocation.Header
                && ContainsUnsafeCharacters(securityScheme.Value.Name))
            {
                diagnostic.Errors.Add(new OpenApiError(
                    securityScheme.Key,
                    $"Security scheme '{securityScheme.Key}' has header name '{securityScheme.Value.Name}' containing illegal characters and is rejected to prevent code injection into Refit attributes. Use --skip-validation to bypass."));
            }
        }
    }

    private static void ValidatePath(string path, OpenApiDiagnostic diagnostic)
    {
        if (!RouteValidator.IsRelativeRoute(path))
        {
            diagnostic.Errors.Add(new OpenApiError(
                path,
                $"Path '{path}' must start with a single '/' and is rejected because a path starting with two slashes or a backslash would be resolved as a different host. Correct the path in the OpenAPI document."));
        }

        if (ContainsUnsafeCharacters(path))
        {
            diagnostic.Errors.Add(new OpenApiError(
                path,
                $"Path '{path}' contains illegal characters (quotes, backslashes, control characters, or Unicode line separators) and is rejected to prevent code injection into Refit attributes. Use --skip-validation to bypass."));
        }
    }

    private static void ValidateHeaderParameters(OpenApiOperation operation, OpenApiDiagnostic diagnostic)
    {
        if (operation.Parameters == null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            if (parameter.In == ParameterLocation.Header && ContainsUnsafeCharacters(parameter.Name))
            {
                diagnostic.Errors.Add(new OpenApiError(
                    parameter.Name ?? string.Empty,
                    $"Header parameter name '{parameter.Name}' contains illegal characters and is rejected to prevent code injection into Refit attributes. Use --skip-validation to bypass."));
            }
        }
    }

    private static void ValidateContentTypeKeys(OpenApiOperation operation, OpenApiDiagnostic diagnostic)
    {
        if (operation.RequestBody?.Content != null)
        {
            foreach (var contentType in operation.RequestBody.Content.Keys)
            {
                AddContentTypeErrorIfUnsafe(contentType, diagnostic);
            }
        }

        if (operation.Responses == null)
            return;

        foreach (var response in operation.Responses.Values)
        {
            if (response?.Content == null)
                continue;

            foreach (var contentType in response.Content.Keys)
            {
                AddContentTypeErrorIfUnsafe(contentType, diagnostic);
            }
        }
    }

    private static void AddContentTypeErrorIfUnsafe(string contentType, OpenApiDiagnostic diagnostic)
    {
        if (ContainsUnsafeCharacters(contentType))
        {
            diagnostic.Errors.Add(new OpenApiError(
                contentType,
                $"Content type '{contentType}' contains illegal characters and is rejected to prevent code injection into Refit attributes. Use --skip-validation to bypass."));
        }
    }
}
