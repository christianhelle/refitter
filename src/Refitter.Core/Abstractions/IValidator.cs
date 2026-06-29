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
    /// <param name="allowRemoteReferences">When false, remote and out-of-tree <c>$ref</c> references are rejected.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <summary>
/// Validates an OpenAPI specification file.
/// </summary>
/// <param name="openApiPath">The path to the OpenAPI specification file.</param>
/// <param name="allowRemoteReferences">Whether remote and out-of-tree <c>$ref</c> references are accepted.</param>
/// <param name="cancellationToken">The token used to cancel the operation.</param>
/// <returns>An <see cref="OpenApiValidationResult"/> containing diagnostics and element counts.</returns>
    Task<OpenApiValidationResult> ValidateAsync(string openApiPath, bool allowRemoteReferences = false, CancellationToken cancellationToken = default);
}
