using Refitter.Core;

namespace Refitter;

/// <summary>
/// Writes planned files under a different root directory, preserving each file's path
/// relative to a base directory (usually the directory of the <c>.refitter</c> file).
/// Used by <c>--output-root</c> so that, for example, every inner build of a
/// multi-targeted project can write to its own intermediate output directory.
/// </summary>
public sealed class OutputRootFileWriter : IFileWriter
{
    /// <summary>
    /// Replaces <c>..</c> segments so rebased files can never escape the output root.
    /// </summary>
    public const string ParentDirectorySegment = "__";

    private readonly IFileWriter inner;
    private readonly string baseDirectory;
    private readonly string outputRoot;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputRootFileWriter"/> class.
    /// </summary>
    /// <param name="inner">The writer that performs the actual write.</param>
    /// <param name="baseDirectory">The directory that planned paths are made relative to.</param>
    /// <param name="outputRoot">The directory that relative paths are rebased under.</param>
    public OutputRootFileWriter(IFileWriter inner, string baseDirectory, string outputRoot)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.baseDirectory = Path.GetFullPath(baseDirectory);
        this.outputRoot = Path.GetFullPath(outputRoot);
    }

    /// <inheritdoc />
    public Task WriteAsync(PlannedFile file, CancellationToken cancellationToken = default) =>
        inner.WriteAsync(file with { Path = Rebase(file.Path, baseDirectory, outputRoot) }, cancellationToken);

    /// <summary>
    /// Maps <paramref name="path"/> to the same relative location under <paramref name="outputRoot"/>.
    /// </summary>
    /// <param name="path">The planned output path (absolute, or relative to the current directory).</param>
    /// <param name="baseDirectory">The directory that <paramref name="path"/> is made relative to.</param>
    /// <param name="outputRoot">The directory to rebase under.</param>
    /// <returns>The absolute rebased path.</returns>
    public static string Rebase(string path, string baseDirectory, string outputRoot)
    {
        var relativePath = Path.GetRelativePath(Path.GetFullPath(baseDirectory), Path.GetFullPath(path));
        if (Path.IsPathRooted(relativePath))
        {
            // Different drive or volume: keep the path below its root
            relativePath = relativePath.Substring(Path.GetPathRoot(relativePath)!.Length);
        }

        var segments = relativePath
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
            .Where(segment => segment != ".")
            .Select(segment => segment == ".." ? ParentDirectorySegment : segment)
            .ToArray();

        return Path.Combine([Path.GetFullPath(outputRoot), .. segments]);
    }
}
