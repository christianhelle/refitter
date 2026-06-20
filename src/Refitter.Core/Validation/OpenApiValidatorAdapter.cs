using System.Threading;
using System.Threading.Tasks;

namespace Refitter.Core.Validation;

public sealed class OpenApiValidatorAdapter : IValidator
{
    public async Task<OpenApiValidationResult> ValidateAsync(string openApiPath, CancellationToken cancellationToken = default)
    {
        return await OpenApiValidator.Validate(openApiPath, cancellationToken);
    }
}
