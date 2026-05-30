namespace Refitter;

/// <summary>
/// Thin filesystem sink for planned output. Owns the "ensure directory exists,
/// then write the file" mechanics so the command code no longer interleaves
/// directory creation with reporting. The <c>GeneratedFile:</c> marker stays
/// with the caller (see <see cref="GenerateCommand.FormatGeneratedFileMarker"/>)
/// because it is observable stdout, not a filesystem concern.
/// </summary>
internal static class FileWriter
{
    public static async Task WriteAsync(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(path, content);
    }

    public static Task WriteAsync(PlannedFile file) => WriteAsync(file.Path, file.Content);
}
