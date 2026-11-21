using Refitter.Core;

namespace Refitter;

/// <summary>
/// Writes planned files to disk and reports progress via <see cref="IGenerationReporter"/>.
/// </summary>
public class CliFileWriter : IFileWriter
{
    private readonly IGenerationReporter _reporter;

    /// <summary>
    /// Initializes a new instance of the <see cref="CliFileWriter"/> class.
    /// </summary>
    /// <param name="reporter">The generation reporter for progress reporting.</param>
    public CliFileWriter(IGenerationReporter reporter)
    {
        _reporter = reporter ?? throw new ArgumentNullException(nameof(reporter));
    }

    /// <inheritdoc />
    public async Task WriteAsync(PlannedFile file, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dir = Path.GetDirectoryName(file.Path);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Never write through a symbolic link: a pre-existing link at the output path (for example one
        // that ships in a cloned repository) would redirect the write to an arbitrary file.
        // See GHSA-7q2f-m7jf-3gp6.
        var target = new FileInfo(file.Path);
        if (target.LinkTarget != null)
            throw new InvalidOperationException($"Refusing to write '{file.Path}': it is a symbolic link.");

        // Write to a temporary file and rename it over the target, so that a link created after the
        // check above is replaced instead of followed.
        var tempPath = Path.Combine(
            string.IsNullOrWhiteSpace(dir) ? Directory.GetCurrentDirectory() : dir,
            "." + Path.GetFileName(file.Path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await File.WriteAllTextAsync(tempPath, file.Content, cancellationToken);
            File.Move(tempPath, file.Path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }

        _reporter.ReportFileWritten(file.Path);
    }
}
