using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Refitter.Core;

namespace Refitter.MSBuild;

/// <summary>
/// Writes planned files to disk from within an MSBuild task.
/// Creates directories as needed. No logging dependencies — the calling task
/// handles reporting.
/// </summary>
public class MsBuildFileWriter : IFileWriter
{
    /// <inheritdoc />
    public Task WriteAsync(PlannedFile file, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dir = Path.GetDirectoryName(file.Path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(file.Path, file.Content);
        return Task.CompletedTask;
    }
}
