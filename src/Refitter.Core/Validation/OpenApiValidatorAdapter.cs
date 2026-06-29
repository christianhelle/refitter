using System.Threading;
using System.Threading.Tasks;

namespace Refitter.Core.Validation;

/// <summary>
/// Adapts the static <see cref="OpenApiValidator"/> to the <see cref="IValidator"/> interface.
/// </summary>
public sealed class OpenApiValidatorAdapter : IValidator
{
    /// <summary>
    /// Validates an OpenAPI specification file.
    /// </summary>
    /// <param name="openApiPath">The path to the OpenAPI definition.</param>
    /// <param name="allowRemoteReferences">Whether remote <c>$ref</c> references are allowed.</param>
    /// <param name="cancellationToken">The token to observe while validating.</param>
    /// <returns>The validation result for the specified OpenAPI file.</returns>
    public async Task<OpenApiValidationResult> ValidateAsync(string openApiPath, bool allowRemoteReferences = false, CancellationToken cancellationToken = default)
    {
        return await OpenApiValidator.Validate(openApiPath, allowRemoteReferences, cancellationToken);
    }
}
