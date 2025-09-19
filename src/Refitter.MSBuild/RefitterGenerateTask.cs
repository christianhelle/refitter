using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Refitter.MSBuild;

public class RefitterGenerateTask : MSBuildTask
{
    public string ProjectFileDirectory { get; set; }

    public bool DisableLogging { get; set; }

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

        TryLogCommandLine($"Found {files.Length} .refitter files...");

        var generatedFiles = new List<string>();

        foreach (var file in files)
        {
            TryLogCommandLine($"Processing {file}");
            var generated = TryExecuteRefitter(file);
            if (generated != null)
            {
                generatedFiles.AddRange(generated);
            }
        }

        GeneratedFiles = generatedFiles.Select(f => new Microsoft.Build.Utilities.TaskItem(f)).ToArray();
        TryLogCommandLine($"Generated {GeneratedFiles.Length} files");

        return true;
    }

    private List<string>? TryExecuteRefitter(string file)
    {
        try
        {
            return StartProcess(file);
        }
        catch (Exception e)
        {
            TryLogErrorFromException(e);
            return null;
        }
    }

    private List<string> StartProcess(string file)
    {
        var expectedFiles = GetExpectedGeneratedFiles(file);
        var assembly = Assembly.GetExecutingAssembly();
        var packageFolder = Path.GetDirectoryName(assembly.Location);
        var separator = Path.DirectorySeparatorChar;
        var refitterDll = $"{packageFolder}{separator}..{separator}net8.0{separator}refitter.dll";

        List<string> installedRuntimes = GetInstalledDotnetRuntimes();
        if (installedRuntimes.Any(r => r.StartsWith("Microsoft.NETCore.App 9.")))
        {
            // Use .NET 9 version if available
            refitterDll = $"{packageFolder}{separator}..{separator}net9.0{separator}refitter.dll";
            TryLogCommandLine("Detected .NET 9 runtime. Using .NET 9 version of Refitter.");
        }
        else
        {
            TryLogCommandLine("Using .NET 8 version of Refitter.");
        }

        var args = $"{refitterDll} --settings-file {file}";
        if (DisableLogging)
        {
            args += " --no-logging";
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
        process.WaitForExit();

        // Return the list of files that should have been generated
        return expectedFiles.Where(File.Exists).ToList();
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

    /// <summary>
    /// Determines the expected output files that should be generated from a .refitter configuration file
    /// </summary>
    /// <param name="refitterFilePath">Path to the .refitter configuration file</param>
    /// <returns>List of expected output file paths</returns>
    private List<string> GetExpectedGeneratedFiles(string refitterFilePath)
    {
        try
        {
            var refitterFileDirectory = Path.GetDirectoryName(refitterFilePath) ?? string.Empty;
            var refitterContent = File.ReadAllText(refitterFilePath);

            // Parse JSON properties using simple regex patterns
            var outputFolder = ExtractJsonStringValue(refitterContent, "outputFolder");
            var outputFilename = ExtractJsonStringValue(refitterContent, "outputFilename");
            var generateMultipleFiles = ExtractJsonBoolValue(refitterContent, "generateMultipleFiles");
            var contractsOutputFolder = ExtractJsonStringValue(refitterContent, "contractsOutputFolder");
            var hasDependencyInjectionSettings = refitterContent.Contains("\"dependencyInjectionSettings\"");

            // If contractsOutputFolder is specified, automatically enable multiple files
            bool hasContractsOutputFolder = !string.IsNullOrWhiteSpace(contractsOutputFolder);
            generateMultipleFiles = generateMultipleFiles || hasContractsOutputFolder;

            // Default output filename based on .refitter filename if not specified
            if (string.IsNullOrWhiteSpace(outputFilename))
            {
                var refitterFileName = Path.GetFileNameWithoutExtension(refitterFilePath);
                outputFilename = $"{refitterFileName}.cs";
            }

            var generatedFiles = new List<string>();

            if (generateMultipleFiles)
            {
                // Multiple files mode
                var baseOutputFolder = !string.IsNullOrWhiteSpace(outputFolder) ? outputFolder : "./Generated";
                var interfaceOutputPath = Path.GetFullPath(Path.Combine(refitterFileDirectory, baseOutputFolder, "RefitInterfaces.cs"));
                var contractsOutputPath = Path.GetFullPath(Path.Combine(refitterFileDirectory,
                    !string.IsNullOrWhiteSpace(contractsOutputFolder) ? contractsOutputFolder : baseOutputFolder,
                    "Contracts.cs"));
                var diOutputPath = Path.GetFullPath(Path.Combine(refitterFileDirectory, baseOutputFolder, "DependencyInjection.cs"));

                generatedFiles.Add(interfaceOutputPath);
                generatedFiles.Add(contractsOutputPath);

                // DependencyInjection.cs is only generated if dependencyInjectionSettings are specified
                if (hasDependencyInjectionSettings)
                {
                    generatedFiles.Add(diOutputPath);
                }
            }
            else
            {
                // Single file mode
                string outputPath;
                if (!string.IsNullOrWhiteSpace(outputFolder))
                {
                    // outputFolder is specified
                    outputPath = Path.GetFullPath(Path.Combine(refitterFileDirectory, outputFolder, outputFilename));
                }
                else
                {
                    // No outputFolder specified - generate in current directory (default behavior)
                    outputPath = Path.GetFullPath(Path.Combine(refitterFileDirectory, outputFilename));
                }
                generatedFiles.Add(outputPath);
            }

            TryLogCommandLine($"Expected generated files: {string.Join(", ", generatedFiles)}");
            return generatedFiles;
        }
        catch (Exception ex)
        {
            TryLogError($"Error parsing .refitter file {refitterFilePath}: {ex.Message}");
            return new List<string>();
        }
    }

    private void TryLogErrorFromException(Exception e)
    {
        try
        {
            Log.LogErrorFromException(e);
        }
        catch
        {
            // Ignore
        }
    }

    private void TryLogCommandLine(string text)
    {
        try
        {
            Log.LogCommandLine(text);
        }
        catch
        {
            // ignore
        }
    }

    private void TryLogError(string text)
    {
        try
        {
            Log.LogError(text);
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// Extracts a string value from a JSON property using regex pattern matching
    /// </summary>
    /// <param name="json">The JSON content to search</param>
    /// <param name="propertyName">The name of the property to extract</param>
    /// <returns>The extracted string value, or null if not found</returns>
    private static string? ExtractJsonStringValue(string json, string propertyName)
    {
        // Simple regex to extract string values from JSON
        // Pattern: "propertyName": "value" or "propertyName":"value"
        var pattern = $@"""{Regex.Escape(propertyName)}""\s*:\s*""([^""]*)""";
        var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Extracts a boolean value from a JSON property using regex pattern matching
    /// </summary>
    /// <param name="json">The JSON content to search</param>
    /// <param name="propertyName">The name of the property to extract</param>
    /// <returns>True if the property value is "true", false otherwise</returns>
    private static bool ExtractJsonBoolValue(string json, string propertyName)
    {
        // Simple regex to extract boolean values from JSON
        // Pattern: "propertyName": true or "propertyName":false
        var pattern = $@"""{Regex.Escape(propertyName)}""\s*:\s*(true|false)";
        var match = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
        return match.Success && string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
    }
}
