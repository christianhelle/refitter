using System.Runtime.Serialization;

namespace Refitter.Validation;

[Serializable]
public class OpenApiValidationException : Exception
{
    public OpenApiValidationResult ValidationResult { get; } = null!;

    public OpenApiValidationException(
        OpenApiValidationResult validationResult) 
        : base("OpenAPI validation failed")
    {
        ValidationResult = validationResult;
    }
    
    protected OpenApiValidationException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}