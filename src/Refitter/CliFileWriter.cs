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

        await WriteSafelyAsync(file.Path, file.Content, cancellationToken);

        _reporter.ReportFileWritten(file.Path);
    }

    /// <summary>
    /// Writes a file without following symbolic links at the file path or in its directory chain.
    /// Shared by generated code and the <c>.refitter</c> settings file. See GHSA-7q2f-m7jf-3gp6.
    /// </summary>
    internal static async Task WriteSafelyAsync(string path, string content, CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        EnsureNoLinkedDirectories(dir);

        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Never write through a symbolic link: a pre-existing link at the output path (for example one
        // that ships in a cloned repository) would redirect the write to an arbitrary file.
        // See GHSA-7q2f-m7jf-3gp6.
        var target = new FileInfo(path);
        if (target.LinkTarget != null)
            throw new InvalidOperationException($"Refusing to write '{path}': it is a symbolic link.");

        // Write to a temporary file and rename it over the target, so that a link created after the
        // check above is replaced instead of followed. The temporary name is short and fixed-length so
        // it cannot exceed filesystem filename limits when the output filename is long.
        var tempPath = Path.Combine(
            string.IsNullOrWhiteSpace(dir) ? Directory.GetCurrentDirectory() : dir,
            "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await File.WriteAllTextAsync(tempPath, content, cancellationToken);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    /// <summary>
    /// Refuses to proceed when an existing directory in the chain below the output root is a symbolic link.
    /// The output root is the current working directory, and components above it are deliberately not
    /// checked because users may work inside a linked home or checkout. For a target outside the working
    /// directory only the final directory is checked: every other component the CLI would create does not
    /// exist yet, and existing ancestors were chosen by the user through an explicit absolute path.
    /// </summary>
    private static void EnsureNoLinkedDirectories(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return;

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Directory.GetCurrentDirectory()));
        var current = Path.TrimEndingDirectorySeparator(directory);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var insideRoot = current.StartsWith(root + Path.DirectorySeparatorChar, comparison);

        if (!insideRoot)
        {
            ThrowIfLinkedDirectory(current);
            return;
        }

        while (!string.Equals(current, root, comparison))
        {
            ThrowIfLinkedDirectory(current);
            var parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent))
                break;

            current = parent;
        }
    }

    private static void ThrowIfLinkedDirectory(string directory)
    {
        var info = new DirectoryInfo(directory);
        if (info.Exists && info.LinkTarget != null)
            throw new InvalidOperationException(
                $"Refusing to write below '{directory}': it is a symbolic link to a directory.");
    }
}
