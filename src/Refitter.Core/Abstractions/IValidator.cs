using System.Threading;
using System.Threading.Tasks;
using Refitter.Core.Validation;

namespace Refitter.Core;

public interface IValidator
{
    Task<OpenApiValidationResult> ValidateAsync(string openApiPath, CancellationToken cancellationToken = default);
}
