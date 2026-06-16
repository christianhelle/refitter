namespace Refitter.Core.Validation;

public class OpenApiValidationException : Exception
{
    public OpenApiValidationResult ValidationResult { get; }

    public OpenApiValidationException(
        OpenApiValidationResult validationResult)
        : base("OpenAPI validation failed")
    {
        ValidationResult = validationResult;
    }
}
