using System.Threading;
using System.Threading.Tasks;

namespace Refitter.Core.Validation;

/// <summary>
/// Adapts the static <see cref="OpenApiValidator"/> to the <see cref="IValidator"/> interface.
/// </summary>
public sealed class OpenApiValidatorAdapter : IValidator
{
    /// <inheritdoc />
    public async Task<OpenApiValidationResult> ValidateAsync(string openApiPath, CancellationToken cancellationToken = default)
    {
        return await OpenApiValidator.Validate(openApiPath, cancellationToken);
    }
}
