using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.Reader;

namespace Refitter.Core.Validation;

/// <summary>
/// Represents the result of an OpenAPI document validation.
/// </summary>
/// <param name="Diagnostics">The diagnostics collected during validation.</param>
/// <param name="Statistics">The element counts collected during validation.</param>
[ExcludeFromCodeCoverage]
public record OpenApiValidationResult(
    OpenApiDiagnostic Diagnostics,
    OpenApiStats Statistics)
{
    /// <summary>
    /// Gets whether the validation passed with no errors.
    /// </summary>
    public bool IsValid => Diagnostics.Errors.Count == 0;

    /// <summary>
    /// Throws an <see cref="OpenApiValidationException"/> if validation failed.
    /// </summary>
    /// <exception cref="OpenApiValidationException">Thrown when <see cref="IsValid"/> is <c>false</c>.</exception>
    public void ThrowIfInvalid()
    {
        if (!IsValid)
            throw new OpenApiValidationException(this);
    }
}
