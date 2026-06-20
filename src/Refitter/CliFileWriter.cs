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

        await File.WriteAllTextAsync(file.Path, file.Content, cancellationToken);
        _reporter.ReportFileWritten(file.Path);
    }
}
