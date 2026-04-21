using System.Diagnostics;
using System.Reflection;
using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Refitter.MSBuild;

public class RefitterGenerateTask : MSBuildTask
{
    internal const string GeneratedFileMarker = "GeneratedFile: ";

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
            var generated = TryExecuteRefitter(file, out var failed);
            if (failed)
            {
                hasErrors = true;
                TryLogError($"Failed to generate code from {file}");
            }
            else if (generated != null)
            {
                generatedFiles.AddRange(generated);
            }
        }

        GeneratedFiles = generatedFiles.Select(f => new Microsoft.Build.Utilities.TaskItem(f)).ToArray();
        TryLogCommandLine($"Generated {GeneratedFiles.Length} files");

        // Return false if any refitter execution failed
        return !hasErrors;
    }

    private List<string>? TryExecuteRefitter(string file, out bool failed)
    {
        failed = false;
        try
        {
            return StartProcess(file, out failed);
        }
        catch (Exception e)
        {
            failed = true;
            TryLogErrorFromException(e);
            return null;
        }
    }

    private List<string> StartProcess(string file, out bool failed)
    {
        failed = false;
        var assembly = Assembly.GetExecutingAssembly();
        var packageFolder = Path.GetDirectoryName(assembly.Location);
        var separator = Path.DirectorySeparatorChar;
        var refitterDll = $"{packageFolder}{separator}..{separator}net8.0{separator}refitter.dll";
        var outputLines = new List<string>();

        List<string> installedRuntimes = GetInstalledDotnetRuntimes();
        if (installedRuntimes.Any(r => r.StartsWith("Microsoft.NETCore.App 10.")))
        {
            // Use .NET 10 version if available
            refitterDll = $"{packageFolder}{separator}..{separator}net10.0{separator}refitter.dll";
            TryLogCommandLine("Detected .NET 10 runtime. Using .NET 10 version of Refitter.");
        }
        else if (installedRuntimes.Any(r => r.StartsWith("Microsoft.NETCore.App 9.")))
        {
            // Use .NET 9 version if available
            refitterDll = $"{packageFolder}{separator}..{separator}net9.0{separator}refitter.dll";
            TryLogCommandLine("Detected .NET 9 runtime. Using .NET 9 version of Refitter.");
        }
        else
        {
            TryLogCommandLine("Using .NET 8 version of Refitter.");
        }

        var args = $"\"{refitterDll}\" --settings-file \"{file}\" --simple-output";
        if (DisableLogging)
        {
            args += " --no-logging";
        }
        if (SkipValidation)
        {
            args += " --skip-validation";
        }

        TryLogCommandLine($"Starting dotnet {args}");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = args,
                WorkingDirectory = Path.GetDirectoryName(file)!,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.ErrorDataReceived += (_, args) => HandleProcessErrorOutput(args.Data, TryLogError);
        process.OutputDataReceived += (_, args) => HandleProcessStandardOutput(args.Data, outputLines, outputLines, TryLogCommandLine);
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        // Wait for process to exit with a reasonable timeout (5 minutes)
        // to prevent build hangs on network issues or infinite loops
        const int timeoutMilliseconds = 300000; // 5 minutes
        if (!process.WaitForExit(timeoutMilliseconds))
        {
            failed = true;
            try
            {
                process.Kill();
                TryLogError($"Refitter process timed out after {timeoutMilliseconds / 1000} seconds and was terminated");
            }
            catch (Exception ex)
            {
                TryLogError($"Failed to terminate timed-out process: {ex.Message}");
            }
            return new List<string>();
        }

        process.WaitForExit();

        // Check exit code - non-zero indicates failure
        if (process.ExitCode != 0)
        {
            failed = true;
            TryLogError($"Refitter process exited with code {process.ExitCode}");
            return new List<string>();
        }

        return ResolveGeneratedFiles(outputLines, file, out failed, TryLogError);
    }

    /// <summary>
    /// Gets the list of installed .NET runtimes by executing 'dotnet --list-runtimes'
    /// </summary>
    /// <returns>List of installed runtime strings</returns>
    private static List<string> GetInstalledDotnetRuntimes()
    {
        var installedRuntimes = new List<string>();
        using (var process = new Process())
        {
            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = "--list-runtimes";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            using (var reader = process.StandardOutput)
            {
                var output = reader.ReadToEnd();
                installedRuntimes.AddRange(output?.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries));
            }
            process.WaitForExit();
        }

        return installedRuntimes;
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

    /// <summary>
    /// Filters the list of .refitter files based on include patterns
    /// </summary>
    /// <param name="files">The list of .refitter files to filter</param>
    /// <param name="includePatterns">Semicolon-separated file names or project-relative paths to include (e.g. "petstore.refitter;apis\petstore-default.refitter")</param>
    /// <param name="projectFileDirectory">The root project directory used when matching relative paths.</param>
    /// <returns>The filtered list of .refitter files</returns>
    internal static string[] FilterFiles(string[] files, string includePatterns, string projectFileDirectory)
    {
        if (string.IsNullOrWhiteSpace(includePatterns))
        {
            return files;
        }

        var patterns = includePatterns.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
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

    internal static string? ParseGeneratedFilePath(string? outputLine)
    {
        var markerLine = outputLine ?? string.Empty;
        if (string.IsNullOrWhiteSpace(markerLine))
        {
            return null;
        }

        if (!markerLine.StartsWith(GeneratedFileMarker, StringComparison.Ordinal))
        {
            return null;
        }

        var generatedFilePath = markerLine.Substring(GeneratedFileMarker.Length).Trim();
        return string.IsNullOrWhiteSpace(generatedFilePath) ? null : generatedFilePath;
    }

    internal static void HandleProcessErrorOutput(string? outputLine, Action<string> logError)
    {
        if (string.IsNullOrWhiteSpace(outputLine))
        {
            return;
        }

        logError(outputLine!);
    }

    internal static void HandleProcessStandardOutput(string? outputLine, ICollection<string> outputLines, object outputLinesLock, Action<string> logCommandLine)
    {
        if (string.IsNullOrWhiteSpace(outputLine))
        {
            return;
        }

        lock (outputLinesLock)
        {
            outputLines.Add(outputLine!);
        }

        logCommandLine(outputLine!);
    }

    internal static List<string> ResolveGeneratedFiles(IEnumerable<string?> outputLines, string settingsFilePath, out bool failed, Action<string> logError)
    {
        var existingGeneratedFiles = ResolveGeneratedFiles(outputLines, settingsFilePath, out var errorMessage);
        failed = errorMessage is not null;
        if (failed)
        {
            logError(errorMessage!);
        }

        return existingGeneratedFiles;
    }

    internal static List<string> ResolveGeneratedFiles(IEnumerable<string?> outputLines, string settingsFilePath, out string? errorMessage)
    {
        var existingGeneratedFiles = outputLines
            .Select(ParseGeneratedFilePath)
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Select(path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        errorMessage = existingGeneratedFiles.Count == 0
            ? $"Refitter did not report any generated files for {settingsFilePath}"
            : null;

        return existingGeneratedFiles;
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
