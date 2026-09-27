using Refitter.Core.Validation.Model;

namespace Refitter.Core.Validation;

/// <summary>
/// Rejects OpenAPI paths, header names, and content-type keys containing characters that could break
/// out of the generated Refit attribute string literals (e.g. quotes, backslashes, newlines). Generated
/// code escapes these characters, but rejecting them gives a clear error unless validation is skipped.
/// See GHSA-3fhm-p725-h3g3 (path), GHSA-58x9-vjvp-6mx8 (header name), GHSA-p32v-8v8j-j534 (content-type).
/// </summary>
internal static class AttributeStringValidator
{
    internal static bool ContainsUnsafeCharacters(string? value) =>
        value != null && value.Any(c => c is '"' or '\\' || char.IsControl(c));

    /// <param name="document">The document to check.</param>
    /// <param name="registered">The components references resolve against.</param>
    /// <param name="diagnostics">The diagnostics to add errors to.</param>
    internal static void Validate(SpecDocument? document, SpecComponents? registered, ValidationDiagnostics diagnostics)
    {
        if (document == null)
            return;

        ValidateSecuritySchemes(document, registered, diagnostics);

        if (document.Paths == null)
            return;

        // Accept/Content-Type headers are only emitted from content map keys for OpenAPI 3.0+,
        // so only reject unsafe content-type keys for those documents to avoid Swagger 2.0 false positives.
        var validateContentTypes = diagnostics.SpecificationVersion != OpenApiSpecificationVersion.OpenApi2_0;

        foreach (var path in document.Paths)
        {
            ValidatePath(path.Key, diagnostics);

            var operations = SpecReferences.Resolve(path.Value, c => c.PathItems, registered)?.Operations;
            if (operations == null)
                continue;

            foreach (var operation in operations.Values)
            {
                ValidateHeaderParameters(operation, registered, diagnostics);

                if (validateContentTypes)
                    ValidateContentTypeKeys(operation, registered, diagnostics);
            }
        }
    }

    private static void ValidateSecuritySchemes(SpecDocument document, SpecComponents? registered, ValidationDiagnostics diagnostics)
    {
        if (document.Components?.SecuritySchemes == null)
            return;

        foreach (var securityScheme in document.Components.SecuritySchemes)
        {
            var scheme = SpecReferences.Resolve(securityScheme.Value, c => c.SecuritySchemes, registered);
            if (scheme?.Type == SpecSecuritySchemeType.ApiKey
                && scheme.In == SpecParameterLocation.Header
                && ContainsUnsafeCharacters(scheme.Name))
            {
                diagnostics.Errors.Add(new ValidationIssue(
                    securityScheme.Key,
                    $"Security scheme '{securityScheme.Key}' has header name '{scheme.Name}' containing illegal characters and is rejected to prevent code injection into Refit attributes. Use --skip-validation to bypass."));
            }
        }
    }

    private static void ValidatePath(string path, ValidationDiagnostics diagnostics)
    {
        if (ContainsUnsafeCharacters(path))
        {
            diagnostics.Errors.Add(new ValidationIssue(
                path,
                $"Path '{path}' contains illegal characters (quotes, backslashes, or control characters) and is rejected to prevent code injection into Refit attributes. Use --skip-validation to bypass."));
        }
    }

    private static void ValidateHeaderParameters(SpecOperation operation, SpecComponents? registered, ValidationDiagnostics diagnostics)
    {
        if (operation.Parameters == null)
            return;

        foreach (var reference in operation.Parameters)
        {
            var parameter = SpecReferences.Resolve(reference, c => c.Parameters, registered);
            if (parameter?.In == SpecParameterLocation.Header && ContainsUnsafeCharacters(parameter.Name))
            {
                diagnostics.Errors.Add(new ValidationIssue(
                    parameter.Name ?? string.Empty,
                    $"Header parameter name '{parameter.Name}' contains illegal characters and is rejected to prevent code injection into Refit attributes. Use --skip-validation to bypass."));
            }
        }
    }

    private static void ValidateContentTypeKeys(SpecOperation operation, SpecComponents? registered, ValidationDiagnostics diagnostics)
    {
        var requestBody = SpecReferences.Resolve(operation.RequestBody, c => c.RequestBodies, registered);
        if (requestBody?.Content != null)
        {
            foreach (var contentType in requestBody.Content.Keys)
            {
                AddContentTypeErrorIfUnsafe(contentType, diagnostics);
            }
        }

        if (operation.Responses == null)
            return;

        foreach (var reference in operation.Responses.Values)
        {
            var response = SpecReferences.Resolve(reference, c => c.Responses, registered);
            if (response?.Content == null)
                continue;

            foreach (var contentType in response.Content.Keys)
            {
                AddContentTypeErrorIfUnsafe(contentType, diagnostics);
            }
        }
    }

    private static void AddContentTypeErrorIfUnsafe(string contentType, ValidationDiagnostics diagnostics)
    {
        if (ContainsUnsafeCharacters(contentType))
        {
            diagnostics.Errors.Add(new ValidationIssue(
                contentType,
                $"Content type '{contentType}' contains illegal characters and is rejected to prevent code injection into Refit attributes. Use --skip-validation to bypass."));
        }
    }
}
