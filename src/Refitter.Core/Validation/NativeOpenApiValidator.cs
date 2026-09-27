namespace Refitter.Core.Validation;

/// <summary>
/// Validates an OpenAPI specification file without Microsoft.OpenApi. It reproduces the diagnostics and
/// statistics of the Microsoft.OpenApi based <see cref="OpenApiValidator"/>, which it will replace.
/// </summary>
internal static class NativeOpenApiValidator
{
    public static Task<OpenApiValidationResult> Validate(
        string openApiFile,
        bool allowRemoteReferences = false,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
