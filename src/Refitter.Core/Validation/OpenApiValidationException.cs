namespace Refitter.Core.Validation;

/// <summary>
/// Exception thrown when OpenAPI validation fails.
/// </summary>
/// <param name="validationResult">The validation result that caused the failure.</param>
public class OpenApiValidationException(
    OpenApiValidationResult validationResult)
    : Exception("OpenAPI validation failed")
{
    /// <summary>
    /// Gets the validation result that caused this exception.
    /// </summary>
    public OpenApiValidationResult ValidationResult { get; } = validationResult;
}
