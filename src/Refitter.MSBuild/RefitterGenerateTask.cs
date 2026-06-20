using System.Diagnostics;
using Microsoft.Build.Framework;
using Refitter.Core;
using Refitter.Core.Validation;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Refitter.MSBuild;

public class RefitterGenerateTask : MSBuildTask
{
    public string ProjectFileDirectory { get; set; }

    public bool DisableLogging { get; set; }

    public bool SkipValidation { get; set; }

    public string IncludePatterns { get; set; }

    [Output]
    public ITaskItem[] GeneratedFiles { get; set; }

    public override bool Execute()
    {
        TryLogCommandLine($"Starting {nameof(RefitterGenerateTask)}");
        TryLogCommandLine($"Looking for .refitter files under {ProjectFileDirectory}");

        var files = Directory.GetFiles(
            ProjectFileDirectory,
            "*.refitter",
            SearchOption.AllDirectories);

        files = FilterFiles(files, IncludePatterns, ProjectFileDirectory);

        TryLogCommandLine($"Found {files.Length} .refitter files...");

        var generatedFiles = new List<string>();
        var hasErrors = false;

        foreach (var file in files)
        {
            TryLogCommandLine($"Processing {file}");
            try
            {
                var generated = ProcessRefitterFile(file);
                if (generated.Count > 0)
                {
                    generatedFiles.AddRange(generated);
                }
            }
            catch (Exception e)
            {
                hasErrors = true;
                TryLogError($"Failed to generate code from {file}");
                TryLogErrorFromException(e);
            }
        }

        GeneratedFiles = generatedFiles
            .Select(f => new Microsoft.Build.Utilities.TaskItem(f))
            .ToArray<ITaskItem>();

        TryLogCommandLine($"Generated {GeneratedFiles.Length} files");

        return !hasErrors;
    }

    private List<string> ProcessRefitterFile(string filePath)
    {
        var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
        var json = File.ReadAllText(filePath);
        var settings = RefitterSettingsLoader.Load(json, baseDirectory);
        RefitterSettingsLoader.ApplyDefaults(filePath, settings);

        var writer = new MsBuildFileWriter();
        IValidator? validator = SkipValidation
            ? null
            : new OpenApiValidatorAdapter();

        var runner = new RefitterRunner();
        var result = runner.RunAsync(
                settings,
                writer,
                validator,
                settingsFilePath: filePath,
                outputPath: null,
                CancellationToken.None)
            .GetAwaiter().GetResult();

        if (result.ExitCode != 0)
            throw result.Exception ?? new InvalidOperationException(
                result.Diagnostics.FirstOrDefault()?.Message ?? "Unknown error");

        return result.GeneratedFiles
            .Select(f => Path.GetFullPath(f.Path))
            .ToList();
    }

    internal static string[] FilterFiles(string[] files, string includePatterns, string projectFileDirectory)
    {
        if (string.IsNullOrWhiteSpace(includePatterns))
            return files;

        var patterns = includePatterns.Split([';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeIncludePattern)
            .ToList();

        return files.Where(file =>
        {
            var fileName = NormalizeIncludePattern(Path.GetFileName(file));
            var relativePath = string.IsNullOrWhiteSpace(projectFileDirectory)
                ? fileName
                : NormalizeIncludePattern(GetRelativePath(projectFileDirectory, file));
            var fullPath = NormalizeIncludePattern(Path.GetFullPath(file));

            return patterns.Any(pattern =>
                fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                relativePath.Equals(pattern, StringComparison.OrdinalIgnoreCase) ||
                fullPath.Equals(pattern, StringComparison.OrdinalIgnoreCase));
        }).ToArray();
    }

    private void TryLogErrorFromException(Exception e)
    {
        try
        {
            Log.LogErrorFromException(e);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to log error from exception: {ex}");
        }
    }

    private void TryLogCommandLine(string text)
    {
        try
        {
            Log.LogCommandLine(text);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to log command line: {ex}");
        }
    }

    private void TryLogError(string text)
    {
        try
        {
            Log.LogError(text);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to log error: {ex}");
        }
    }

    private static string NormalizeIncludePattern(string path)
    {
        var normalizedPath = path
            .Trim()
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        var relativePrefix = $".{Path.DirectorySeparatorChar}";
        return normalizedPath.StartsWith(relativePrefix, StringComparison.Ordinal)
            ? normalizedPath.Substring(relativePrefix.Length)
            : normalizedPath;
    }

    private static string GetRelativePath(string relativeTo, string path)
    {
        var relativeToUri = new Uri(AppendDirectorySeparator(relativeTo));
        var pathUri = new Uri(path);
        return Uri.UnescapeDataString(relativeToUri.MakeRelativeUri(pathUri).ToString())
            .Replace('/', Path.DirectorySeparatorChar);
    }

    private static string AppendDirectorySeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? path
            : path + Path.DirectorySeparatorChar;
}
