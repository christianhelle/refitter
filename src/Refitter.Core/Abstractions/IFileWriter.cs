using System.Threading;
using System.Threading.Tasks;

namespace Refitter.Core;

/// <summary>
/// Writes a <see cref="PlannedFile"/> to disk, creating directories as needed.
/// Each distribution form (CLI, MSBuild, Source Generator) provides its own adapter
/// that integrates with its own reporting/progress pipeline.
/// </summary>
public interface IFileWriter
{
    /// <summary>
    /// Writes the planned file to disk, creating directories as necessary.
    /// </summary>
    /// <param name="file">The planned file to write.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    Task WriteAsync(PlannedFile file, CancellationToken cancellationToken = default);
}
