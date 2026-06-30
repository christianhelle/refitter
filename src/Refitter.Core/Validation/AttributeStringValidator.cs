using System.Linq;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Refitter.Core.Validation;

/// <summary>
/// Rejects OpenAPI paths and header names containing characters that could break out of the
/// generated Refit attribute string literals (e.g. quotes, backslashes, newlines). Generated
/// code escapes these characters, but rejecting them gives a clear error unless validation is
/// skipped. See GHSA-3fhm-p725-h3g3.
/// </summary>
internal static class AttributeStringValidator
{
    internal static bool ContainsUnsafeCharacters(string? value) =>
        value != null && value.Any(c => c is '"' or '\\' || char.IsControl(c));

    internal static void Validate(OpenApiDocument? document, OpenApiDiagnostic diagnostic)
    {
        if (document == null)
            return;

        // Validate security scheme names for API-key schemes with header location
        if (document.Components?.SecuritySchemes != null)
        {
            foreach (var securityScheme in document.Components.SecuritySchemes)
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

        if (document.Paths == null)
            return;

        // Accept/Content-Type headers are only emitted from content map keys for OpenAPI 3.0+,
        // so only reject unsafe content-type keys for those documents to avoid Swagger 2.0 false positives.
        var validateContentTypes = diagnostic.SpecificationVersion != OpenApiSpecVersion.OpenApi2_0;

        foreach (var path in document.Paths)
        {
            if (ContainsUnsafeCharacters(path.Key))
            {
                diagnostic.Errors.Add(new OpenApiError(
                    path.Key,
                    $"Path '{path.Key}' contains illegal characters (quotes, backslashes, or control characters) and is rejected to prevent code injection into Refit attributes. Use --skip-validation to bypass."));
            }

            if (path.Value?.Operations == null)
                continue;

            foreach (var operation in path.Value.Operations.Values)
            {
                if (operation == null)
                    continue;

                if (operation.Parameters != null)
                {
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

                if (validateContentTypes)
                {
                    ValidateContentTypeKeys(operation, diagnostic);
                }
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
