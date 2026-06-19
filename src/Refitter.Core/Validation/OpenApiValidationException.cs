namespace Refitter.Core.Validation;

public class OpenApiValidationException(
    OpenApiValidationResult validationResult)
    : Exception("OpenAPI validation failed")
{
    public OpenApiValidationResult ValidationResult { get; } = validationResult;
}
