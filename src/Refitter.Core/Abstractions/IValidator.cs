using System.Threading;
using System.Threading.Tasks;
using Refitter.Core.Validation;

namespace Refitter.Core;

/// <summary>
/// Validates an OpenAPI specification file and returns validation diagnostics and statistics.
/// Each distribution form (CLI, MSBuild, Source Generator) can provide its own adapter.
/// </summary>
public interface IValidator
{
    /// <summary>
    /// Validates the specified OpenAPI specification file.
    /// </summary>
    /// <param name="openApiPath">The path to the OpenAPI specification file.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An <see cref="OpenApiValidationResult"/> containing diagnostics and element counts.</returns>
    Task<OpenApiValidationResult> ValidateAsync(string openApiPath, CancellationToken cancellationToken = default);
}
