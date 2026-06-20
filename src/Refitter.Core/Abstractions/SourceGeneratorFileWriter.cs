using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Refitter.Core;

/// <summary>
/// Writes planned files to disk with content-equality checking.
/// Skips writing when the file already exists with identical content,
/// avoiding unnecessary disk writes during incremental / design-time builds.
/// </summary>
public class SourceGeneratorFileWriter : IFileWriter
{
    /// <inheritdoc />
    public Task WriteAsync(PlannedFile file, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dir = Path.GetDirectoryName(file.Path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var existingContent = File.Exists(file.Path)
            ? File.ReadAllText(file.Path, Encoding.UTF8)
            : null;

        if (existingContent is null || !string.Equals(existingContent, file.Content, System.StringComparison.Ordinal))
        {
            File.WriteAllText(file.Path, file.Content, Encoding.UTF8);
        }

        return Task.CompletedTask;
    }
}
