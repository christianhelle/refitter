using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Refitter.MSBuild;

public class RefitterGenerateTask : MSBuildTask
{
    internal const int DefaultTimeoutSeconds = 300;

    public string ProjectFileDirectory { get; set; }

    public bool DisableLogging { get; set; }

    public bool SkipValidation { get; set; }

    public string IncludePatterns { get; set; }

    public int TimeoutSeconds { get; set; } = DefaultTimeoutSeconds;

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

        files = FilterFiles(files, IncludePatterns);

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
        var summaryMessage = CreateSummaryMessage(GeneratedFiles.Length, hasErrors);
        if (hasErrors)
        {
            TryLogWarning(summaryMessage);
        }
        else
        {
            TryLogCommandLine(summaryMessage);
        }

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
        var outputPlan = GetOutputPlan(file);
        var outputSnapshot = CaptureOutputSnapshot(outputPlan);
        var timeoutSeconds = GetValidatedTimeoutSeconds(TimeoutSeconds);
        if (timeoutSeconds != TimeoutSeconds)
        {
            TryLogWarning(
                $"Invalid TimeoutSeconds value '{TimeoutSeconds}'. Using default timeout of {DefaultTimeoutSeconds} seconds.");
        }

        var assembly = Assembly.GetExecutingAssembly();
        var packageFolder = Path.GetDirectoryName(assembly.Location);
        List<string> installedRuntimes = GetInstalledDotnetRuntimes();
        var selectedTargetFramework = SelectRefitterTargetFramework(
            installedRuntimes,
            targetFramework => File.Exists(GetRefitterDllPath(packageFolder, targetFramework)));
        var refitterDll = GetRefitterDllPath(packageFolder, selectedTargetFramework);
        TryLogCommandLine($"Using {selectedTargetFramework} version of Refitter.");

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

        process.ErrorDataReceived += (_, args) => TryLogError(args.Data);
        process.OutputDataReceived += (_, args) => TryLogCommandLine(args.Data);
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        var timeoutMilliseconds = checked(timeoutSeconds * 1000);
        if (!process.WaitForExit(timeoutMilliseconds))
        {
            failed = true;
            try
            {
                process.Kill();
                process.WaitForExit();
                TryLogError($"Refitter process timed out after {timeoutSeconds} seconds and was terminated");
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

        return CollectGeneratedFiles(outputPlan, outputSnapshot);
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

    private RefitterOutputPlan GetOutputPlan(string refitterFilePath)
    {
        try
        {
            var refitterContent = File.ReadAllText(refitterFilePath);
            var outputPlan = GetOutputPlan(refitterFilePath, refitterContent);
            var summary = outputPlan.IsMultiFile
                ? $"Monitoring generated output directories: {string.Join(", ", outputPlan.CandidateDirectories)}"
                : $"Expected generated file: {outputPlan.SingleFilePath}";
            TryLogCommandLine(summary);
            return outputPlan;
        }
        catch (Exception ex)
        {
            TryLogError($"Error parsing .refitter file {refitterFilePath}: {ex.Message}");
            return RefitterOutputPlan.Empty;
        }
    }

    internal static int GetValidatedTimeoutSeconds(int timeoutSeconds) =>
        timeoutSeconds > 0 ? timeoutSeconds : DefaultTimeoutSeconds;

    internal static string CreateSummaryMessage(int generatedFileCount, bool hasErrors) =>
        hasErrors
            ? $"Generation partially completed: generated {generatedFileCount} files before one or more .refitter files failed."
            : $"Generated {generatedFileCount} files.";

    internal static string SelectRefitterTargetFramework(
        IEnumerable<string> installedRuntimes,
        Func<string, bool>? targetExists = null)
    {
        targetExists ??= _ => true;

        var candidates = new List<string>();
        if (installedRuntimes.Any(r => r.StartsWith("Microsoft.NETCore.App 10.", StringComparison.OrdinalIgnoreCase)))
        {
            candidates.Add("net10.0");
        }

        if (installedRuntimes.Any(r => r.StartsWith("Microsoft.NETCore.App 9.", StringComparison.OrdinalIgnoreCase)))
        {
            candidates.Add("net9.0");
        }

        candidates.Add("net8.0");

        return candidates.FirstOrDefault(targetExists) ?? "net8.0";
    }

    internal static RefitterOutputPlan GetOutputPlan(string refitterFilePath, string refitterContent)
    {
        var settings = JsonSerializer.Deserialize<RefitterTaskSettings>(refitterContent, JsonSerializerOptions)
            ?? new RefitterTaskSettings();
        var refitterFileDirectory = Path.GetDirectoryName(refitterFilePath) ?? string.Empty;
        var generateMultipleFiles = settings.GenerateMultipleFiles ||
                                    !string.IsNullOrWhiteSpace(settings.ContractsOutputFolder);

        if (!generateMultipleFiles)
        {
            var outputFilename = !string.IsNullOrWhiteSpace(settings.OutputFilename)
                ? settings.OutputFilename
                : $"{Path.GetFileNameWithoutExtension(refitterFilePath)}.cs";
            var outputFolder = !string.IsNullOrWhiteSpace(settings.OutputFolder)
                ? settings.OutputFolder
                : DefaultOutputFolder;
            var outputPath = Path.GetFullPath(Path.Combine(refitterFileDirectory, outputFolder, outputFilename));
            return RefitterOutputPlan.ForSingleFile(outputPath);
        }

        var baseOutputFolder = !string.IsNullOrWhiteSpace(settings.OutputFolder)
            ? settings.OutputFolder
            : DefaultOutputFolder;
        var candidateDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.GetFullPath(Path.Combine(refitterFileDirectory, baseOutputFolder))
        };

        var contractsOutputFolder = !string.IsNullOrWhiteSpace(settings.ContractsOutputFolder)
            ? settings.ContractsOutputFolder
            : baseOutputFolder;
        candidateDirectories.Add(Path.GetFullPath(Path.Combine(refitterFileDirectory, contractsOutputFolder)));

        return RefitterOutputPlan.ForMultipleFiles(candidateDirectories.ToArray());
    }

    internal static Dictionary<string, DateTime> CaptureOutputSnapshot(RefitterOutputPlan outputPlan)
    {
        var snapshot = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        if (!outputPlan.IsMultiFile)
        {
            return snapshot;
        }

        foreach (var directory in outputPlan.CandidateDirectories.Where(Directory.Exists))
        {
            foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.TopDirectoryOnly))
            {
                snapshot[file] = File.GetLastWriteTimeUtc(file);
            }
        }

        return snapshot;
    }

    internal static List<string> CollectGeneratedFiles(
        RefitterOutputPlan outputPlan,
        IReadOnlyDictionary<string, DateTime> outputSnapshot)
    {
        if (!outputPlan.IsMultiFile)
        {
            return !string.IsNullOrWhiteSpace(outputPlan.SingleFilePath) && File.Exists(outputPlan.SingleFilePath)
                ? new List<string> { outputPlan.SingleFilePath }
                : new List<string>();
        }

        var generatedFiles = new List<string>();
        foreach (var directory in outputPlan.CandidateDirectories.Where(Directory.Exists))
        {
            foreach (var file in Directory.GetFiles(directory, "*.cs", SearchOption.TopDirectoryOnly))
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(file);
                if (!outputSnapshot.TryGetValue(file, out var previousWriteTime) ||
                    previousWriteTime != lastWriteTime)
                {
                    generatedFiles.Add(file);
                }
            }
        }

        return generatedFiles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
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

    private void TryLogWarning(string text)
    {
        try
        {
            Log.LogWarning(text);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to log warning: {ex}");
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
    /// <param name="includePatterns">Semicolon-separated file name patterns to include (e.g. "petstore.refitter;petstore-default.refitter")</param>
    /// <returns>The filtered list of .refitter files</returns>
    internal static string[] FilterFiles(string[] files, string includePatterns)
    {
        if (string.IsNullOrWhiteSpace(includePatterns))
        {
            return files;
        }

        var patterns = includePatterns.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToList();

        return files.Where(file =>
        {
            return patterns.Any(pattern => MatchesIncludePattern(file, pattern));
        }).ToArray();
    }

    private static bool MatchesIncludePattern(string filePath, string pattern)
    {
        var normalizedPattern = pattern.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var normalizedFilePath = Path.GetFullPath(filePath)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var fileName = Path.GetFileName(normalizedFilePath);
        var containsDirectory = normalizedPattern.Contains(Path.DirectorySeparatorChar);
        var hasWildcards = normalizedPattern.Contains('*') || normalizedPattern.Contains('?');

        if (!containsDirectory)
        {
            return hasWildcards
                ? Regex.IsMatch(
                    fileName,
                    WildcardToRegex(normalizedPattern),
                    RegexOptions.IgnoreCase,
                    TimeSpan.FromSeconds(1))
                : fileName.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase);
        }

        if (!hasWildcards)
        {
            return Path.IsPathRooted(normalizedPattern)
                ? normalizedFilePath.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase)
                : normalizedFilePath.EndsWith(
                    $"{Path.DirectorySeparatorChar}{normalizedPattern}",
                    StringComparison.OrdinalIgnoreCase);
        }

        var pathPattern = Path.IsPathRooted(normalizedPattern)
            ? normalizedPattern
            : $"*{Path.DirectorySeparatorChar}{normalizedPattern}";
        return Regex.IsMatch(
            normalizedFilePath,
            WildcardToRegex(pathPattern),
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));
    }

    private static string WildcardToRegex(string pattern) =>
        $"^{Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$";

    private static string GetRefitterDllPath(string packageFolder, string targetFramework) =>
        Path.GetFullPath(Path.Combine(packageFolder, "..", targetFramework, "refitter.dll"));

    private const string DefaultOutputFolder = "./Generated";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    internal readonly struct RefitterOutputPlan
    {
        public RefitterOutputPlan(string? singleFilePath, string[] candidateDirectories)
        {
            SingleFilePath = singleFilePath;
            CandidateDirectories = candidateDirectories;
        }

        public static RefitterOutputPlan Empty { get; } = new(null, Array.Empty<string>());

        public string? SingleFilePath { get; }

        public string[] CandidateDirectories { get; }

        public bool IsMultiFile => CandidateDirectories.Length > 0;

        public static RefitterOutputPlan ForSingleFile(string outputPath) =>
            new(outputPath, Array.Empty<string>());

        public static RefitterOutputPlan ForMultipleFiles(string[] candidateDirectories) =>
            new(null, candidateDirectories);
    }

    private sealed class RefitterTaskSettings
    {
        public string? OutputFolder { get; set; }

        public string? OutputFilename { get; set; }

        public bool GenerateMultipleFiles { get; set; }

        public string? ContractsOutputFolder { get; set; }
    }
}
