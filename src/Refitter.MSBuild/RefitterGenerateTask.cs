using System.Diagnostics;
using System.Reflection;
using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Refitter.MSBuild;

public class RefitterGenerateTask : MSBuildTask
{
    internal const string GeneratedFileMarker = "GeneratedFile: ";
    private static readonly System.Threading.AsyncLocal<Func<List<string>>?> InstalledDotnetRuntimesProviderOverride = new();
    private static readonly System.Threading.AsyncLocal<Func<ProcessStartInfo, Action<string?>, Action<string?>, ProcessExecutionResult>?> ProcessRunnerOverride = new();
    private static readonly System.Threading.AsyncLocal<int?> ProcessTimeoutMillisecondsOverride = new();
    private static readonly System.Threading.AsyncLocal<Action<Process>?> ProcessTerminatorOverride = new();
    private static readonly System.Threading.AsyncLocal<Func<string, bool>?> FileExistsOverride = new();

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

    internal sealed class ProcessExecutionResult
    {
        public ProcessExecutionResult(bool timedOut, int exitCode, Exception? terminationException = null)
        {
            TimedOut = timedOut;
            ExitCode = exitCode;
            TerminationException = terminationException;
        }

        public bool TimedOut { get; }

        public int ExitCode { get; }

        public Exception? TerminationException { get; }
    }

    internal static Func<List<string>> InstalledDotnetRuntimesProvider
    {
        get => InstalledDotnetRuntimesProviderOverride.Value ?? GetInstalledDotnetRuntimes;
        set => InstalledDotnetRuntimesProviderOverride.Value = value;
    }

    internal static Func<ProcessStartInfo, Action<string?>, Action<string?>, ProcessExecutionResult> ProcessRunner
    {
        get => ProcessRunnerOverride.Value ?? RunProcess;
        set => ProcessRunnerOverride.Value = value;
    }

    internal static int ProcessTimeoutMilliseconds
    {
        get => ProcessTimeoutMillisecondsOverride.Value ?? 300000;
        set => ProcessTimeoutMillisecondsOverride.Value = value;
    }

    internal static Action<Process> ProcessTerminator
    {
        get => ProcessTerminatorOverride.Value ?? DefaultTerminateProcess;
        set => ProcessTerminatorOverride.Value = value;
    }

    internal static Func<string, bool> FileExists
    {
        get => FileExistsOverride.Value ?? File.Exists;
        set => FileExistsOverride.Value = value;
    }

    public string ProjectFileDirectory { get; set; }

    public bool DisableLogging { get; set; }

    public bool SkipValidation { get; set; }

    public string IncludePatterns { get; set; }

    [Output]
    public ITaskItem[] GeneratedFiles { get; set; }

    internal static void ResetTestHooks()
    {
        InstalledDotnetRuntimesProviderOverride.Value = null;
        ProcessRunnerOverride.Value = null;
        ProcessTimeoutMillisecondsOverride.Value = null;
        ProcessTerminatorOverride.Value = null;
        FileExistsOverride.Value = null;
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
        var outputLines = new List<string>();

        List<string>? installedRuntimes = null;
        try
        {
            installedRuntimes = InstalledDotnetRuntimesProvider();
        }
        catch (Exception exception)
        {
            TryLogCommandLine($"Failed to inspect installed .NET runtimes: {exception.Message}. Falling back to bundled Refitter runtime selection.");
        }

        var refitterDll = ResolveRefitterDll(packageFolder, installedRuntimes, TryLogCommandLine);
        if (string.IsNullOrWhiteSpace(refitterDll) || !FileExists(refitterDll))
        {
            failed = true;
            TryLogError("Unable to locate a bundled Refitter CLI runtime for the MSBuild task.");
            return new List<string>();
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

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = Path.GetDirectoryName(file)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var processResult = ProcessRunner(
            startInfo,
            data => HandleProcessStandardOutput(data, outputLines, outputLines, TryLogCommandLine),
            data => HandleProcessErrorOutput(data, TryLogError));

        if (processResult.TimedOut)
        {
            failed = true;
            var timeoutDescription = FormatTimeout(ProcessTimeoutMilliseconds);
            if (processResult.TerminationException is null)
            {
                TryLogError($"Refitter process timed out after {timeoutDescription} and was terminated");
            }
            else
            {
                TryLogError($"Refitter process timed out after {timeoutDescription}. Failed to terminate timed-out process: {processResult.TerminationException.Message}");
            }

            return new List<string>();
        }

        // Check exit code - non-zero indicates failure
        if (processResult.ExitCode != 0)
        {
            failed = true;
            TryLogError($"Refitter process exited with code {processResult.ExitCode}");
            return new List<string>();
        }

        return ResolveGeneratedFiles(outputLines, file, out failed, TryLogError);
    }

    private static ProcessExecutionResult RunProcess(
        ProcessStartInfo startInfo,
        Action<string?> handleStandardOutput,
        Action<string?> handleErrorOutput)
    {
        using var process = new Process { StartInfo = startInfo };

        process.ErrorDataReceived += (_, args) => handleErrorOutput(args.Data);
        process.OutputDataReceived += (_, args) => handleStandardOutput(args.Data);
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        // Wait for process to exit with a reasonable timeout (5 minutes)
        // to prevent build hangs on network issues or infinite loops
        var timeoutMilliseconds = ProcessTimeoutMilliseconds;
        if (!process.WaitForExit(timeoutMilliseconds))
        {
            try
            {
                ProcessTerminator(process);
                return new ProcessExecutionResult(true, -1);
            }
            catch (Exception ex)
            {
                return new ProcessExecutionResult(true, -1, ex);
            }
        }

        process.WaitForExit();
        return new ProcessExecutionResult(false, process.ExitCode);
    }

    private static void DefaultTerminateProcess(Process process) => process.Kill();

    /// <summary>
    /// Gets the list of installed .NET runtimes by executing 'dotnet --list-runtimes'
    /// </summary>
    /// <returns>List of installed runtime strings</returns>
    private static List<string> GetInstalledDotnetRuntimes()
    {
        var installedRuntimes = new List<string>();
        var errorLines = new List<string>();
        var processResult = ProcessRunner(
            new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-runtimes",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            data => AddProcessOutputLine(data, installedRuntimes),
            data => AddProcessOutputLine(data, errorLines));

        if (processResult.TimedOut)
        {
            var timeoutDescription = FormatTimeout(ProcessTimeoutMilliseconds);
            if (processResult.TerminationException is null)
            {
                throw new TimeoutException($"dotnet --list-runtimes timed out after {timeoutDescription}");
            }

            throw new TimeoutException(
                $"dotnet --list-runtimes timed out after {timeoutDescription}. Failed to terminate timed-out process: {processResult.TerminationException.Message}",
                processResult.TerminationException);
        }

        if (processResult.ExitCode != 0)
        {
            var errorDetails = errorLines.Count > 0
                ? $"{Environment.NewLine}{string.Join(Environment.NewLine, errorLines)}"
                : string.Empty;
            throw new InvalidOperationException($"dotnet --list-runtimes exited with code {processResult.ExitCode}{errorDetails}");
        }

        return installedRuntimes;
    }

    internal static string? ResolveRefitterDll(string? packageFolder, IReadOnlyList<string>? installedRuntimes, Action<string> logCommandLine)
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

            foreach (var runtime in bundledRuntimes.Where(runtime => FileExists(runtime.Path)))
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

            if (FileExists(fallbackPath))
            {
                logCommandLine($"Falling back to bundled {GetDisplayFramework(targetFramework)} version of Refitter.");
                return fallbackPath;
            }
        }

        var coLocatedCli = Path.GetFullPath(Path.Combine(packageFolder, "refitter.dll"));
        if (FileExists(coLocatedCli))
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
        {
            return $"{timeoutMilliseconds} ms";
        }

        if (timeoutMilliseconds % 1000 == 0)
        {
            return $"{timeoutMilliseconds / 1000} seconds";
        }

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

    private static void AddProcessOutputLine(string? outputLine, ICollection<string> outputLines)
    {
        if (!string.IsNullOrWhiteSpace(outputLine))
        {
            outputLines.Add(outputLine);
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
