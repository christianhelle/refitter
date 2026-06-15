using System.Diagnostics;
using System.Reflection;
using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;
using Refitter.Core;

namespace Refitter.MSBuild;

public class RefitterGenerateTask : MSBuildTask
{
    internal const string GeneratedFileMarker = "GeneratedFile: ";

    internal const int DefaultProcessTimeoutMilliseconds = 300000;

    private static readonly (string TargetFramework, string RuntimePrefix)[] PreferredRuntimeOrder =
    [
        ("net10.0", "Microsoft.NETCore.App 10."),
        ("net9.0", "Microsoft.NETCore.App 9."),
        ("net8.0", "Microsoft.NETCore.App 8.")
    ];

    private static readonly string[] CompatibilityFallbackOrder =
    [
        "net8.0",
        "net9.0",
        "net10.0"
    ];

    public IGeneratorRunner? GeneratorRunner { get; set; }

    public string? RefitterDllPath { get; set; }

    public int ProcessTimeoutMilliseconds { get; set; } = DefaultProcessTimeoutMilliseconds;

    public string ProjectFileDirectory { get; set; }

    public bool DisableLogging { get; set; }

    public bool SkipValidation { get; set; }

    public string IncludePatterns { get; set; }

    [Output]
    public ITaskItem[] GeneratedFiles { get; set; }

    public RefitterGenerateTask()
    {
    }

    public RefitterGenerateTask(IGeneratorRunner generatorRunner)
    {
        GeneratorRunner = generatorRunner;
    }

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

        var generatorRunner = GeneratorRunner ?? CreateDefaultRunner();

        var generatedFiles = new List<string>();
        var hasErrors = false;

        foreach (var file in files)
        {
            TryLogCommandLine($"Processing {file}");
            var generated = TryRunGenerator(generatorRunner, file, out var failed);
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

        GeneratedFiles = generatedFiles.Select(f => new Microsoft.Build.Utilities.TaskItem(f)).ToArray<ITaskItem>();
        TryLogCommandLine($"Generated {GeneratedFiles.Length} files");

        return !hasErrors;
    }

    private IGeneratorRunner CreateDefaultRunner()
    {
        var packageFolder = GetPackageFolder();
        var refitterDll = ResolveRefitterDllForTask(packageFolder);

        if (!string.IsNullOrWhiteSpace(refitterDll) && System.IO.File.Exists(refitterDll))
        {
            TryLogCommandLine($"Using CLI adapter with {refitterDll}");
            return new Core.CliGeneratorRunner(refitterDll, ProcessTimeoutMilliseconds);
        }

        TryLogCommandLine("Using Core generator runner (in-process)");
        return new Core.CoreGeneratorRunner();
    }

    private string? GetPackageFolder()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            return Path.GetDirectoryName(assembly.Location);
        }
        catch
        {
            return null;
        }
    }

    private string? ResolveRefitterDllForTask(string? packageFolder)
    {
        if (!string.IsNullOrWhiteSpace(RefitterDllPath))
        {
            return RefitterDllPath;
        }

        if (string.IsNullOrWhiteSpace(packageFolder))
        {
            return null;
        }

        var bundledRuntimes = PreferredRuntimeOrder
            .Select(candidate => new
            {
                candidate.TargetFramework,
                candidate.RuntimePrefix,
                Path = Path.GetFullPath(Path.Combine(packageFolder, "..", candidate.TargetFramework, "refitter.dll")),
            })
            .ToArray();

        foreach (var runtime in bundledRuntimes)
        {
            if (System.IO.File.Exists(runtime.Path))
            {
                TryLogCommandLine($"Detected bundled .NET {runtime.TargetFramework.Substring(3)} version of Refitter.");
                return runtime.Path;
            }
        }

        var coLocatedCli = Path.GetFullPath(Path.Combine(packageFolder, "refitter.dll"));
        if (System.IO.File.Exists(coLocatedCli))
        {
            TryLogCommandLine("Falling back to co-located Refitter CLI.");
            return coLocatedCli;
        }

        return null;
    }

    private List<string>? TryRunGenerator(IGeneratorRunner runner, string file, out bool failed)
    {
        failed = false;
        try
        {
            return RunGenerator(runner, file, out failed);
        }
        catch (Exception e)
        {
            failed = true;
            TryLogErrorFromException(e);
            return null;
        }
    }

    private List<string>? RunGenerator(IGeneratorRunner runner, string file, out bool failed)
    {
        failed = false;

        var settingsDirectory = Path.GetDirectoryName(file);
        var json = File.ReadAllText(file);
        var settings = RefitterSettingsLoader.Load(json, settingsDirectory ?? string.Empty);

        try
        {
            var generatedFiles = runner.RunAsync(settings, SkipValidation, DisableLogging, CancellationToken.None).GetAwaiter().GetResult();
            return generatedFiles?.ToList();
        }
        catch (TimeoutException)
        {
            failed = true;
            var timeoutDescription = FormatTimeout(ProcessTimeoutMilliseconds);
            TryLogError(
                $"Refitter process timed out after {timeoutDescription} and was terminated");
            return new List<string>();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("exited with code"))
        {
            failed = true;
            TryLogError(ex.Message);
            return new List<string>();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("did not report any generated files"))
        {
            failed = true;
            TryLogError(ex.Message);
            return new List<string>();
        }
    }

    internal static string? ResolveRefitterDll(
        string? packageFolder,
        IReadOnlyList<string>? installedRuntimes,
        Action<string> logCommandLine,
        Func<string, bool> fileExists)
    {
        if (string.IsNullOrWhiteSpace(packageFolder))
        {
            return null;
        }

        var bundledRuntimes = PreferredRuntimeOrder
            .Select(candidate => new
            {
                candidate.TargetFramework,
                candidate.RuntimePrefix,
                Path = Path.GetFullPath(Path.Combine(packageFolder, "..", candidate.TargetFramework, "refitter.dll")),
            })
            .ToArray();

        if (installedRuntimes is not null)
        {
            var detectedRuntimes = installedRuntimes
                .Where(installed => !string.IsNullOrWhiteSpace(installed))
                .ToArray();

            foreach (var runtime in bundledRuntimes.Where(runtime => fileExists(runtime.Path)))
            {
                if (detectedRuntimes.Any(installed =>
                        installed.StartsWith(runtime.RuntimePrefix, StringComparison.Ordinal)))
                {
                    logCommandLine($"Detected {GetDisplayFramework(runtime.TargetFramework)} runtime. Using {GetDisplayFramework(runtime.TargetFramework)} version of Refitter.");
                    return runtime.Path;
                }
            }
        }

        foreach (var targetFramework in CompatibilityFallbackOrder)
        {
            var fallbackPath = bundledRuntimes
                .First(runtime => runtime.TargetFramework == targetFramework)
                .Path;

            if (fileExists(fallbackPath))
            {
                logCommandLine($"Falling back to bundled {GetDisplayFramework(targetFramework)} version of Refitter.");
                return fallbackPath;
            }
        }

        var coLocatedCli = Path.GetFullPath(Path.Combine(packageFolder, "refitter.dll"));
        if (fileExists(coLocatedCli))
        {
            logCommandLine("Falling back to co-located Refitter CLI.");
            return coLocatedCli;
        }

        return bundledRuntimes
            .Select(runtime => runtime.Path)
            .FirstOrDefault();
    }

    private static string FormatTimeout(int timeoutMilliseconds)
    {
        if (timeoutMilliseconds < 1000)
            return $"{timeoutMilliseconds} ms";

        if (timeoutMilliseconds % 1000 == 0)
            return $"{timeoutMilliseconds / 1000} seconds";

        return $"{timeoutMilliseconds / 1000d:0.###} seconds";
    }

    private static string GetDisplayFramework(string targetFramework) => targetFramework.Replace("net", ".NET ");

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

    internal static string[] FilterFiles(string[] files, string includePatterns, string projectFileDirectory)
    {
        if (string.IsNullOrWhiteSpace(includePatterns))
        {
            return files;
        }

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

    internal static string? ParseGeneratedFilePath(string? outputLine)
    {
        var markerLine = outputLine ?? string.Empty;
        if (string.IsNullOrWhiteSpace(markerLine))
            return null;

        if (!markerLine.StartsWith(GeneratedFileMarker, StringComparison.Ordinal))
            return null;

        var generatedFilePath = markerLine.Substring(GeneratedFileMarker.Length).Trim();
        return string.IsNullOrWhiteSpace(generatedFilePath) ? null : generatedFilePath;
    }

    internal static void HandleProcessErrorOutput(string? outputLine, Action<string> logError)
    {
        if (string.IsNullOrWhiteSpace(outputLine))
            return;

        logError(outputLine!);
    }

    internal static void HandleProcessStandardOutput(string? outputLine, ICollection<string> outputLines, object outputLinesLock, Action<string> logCommandLine)
    {
        if (string.IsNullOrWhiteSpace(outputLine))
            return;

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
