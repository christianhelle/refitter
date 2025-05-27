using System.Diagnostics;
using System.Reflection;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Refitter.MSBuild;

public class RefitterGenerateTask : MSBuildTask
{
    public string ProjectFileDirectory { get; set; }

    public bool DisableLogging { get; set; }

    public override bool Execute()
    {
        TryLogCommandLine($"Starting {nameof(RefitterGenerateTask)}");
        TryLogCommandLine($"Looking for .refitter files under {ProjectFileDirectory}");

        var files = Directory.GetFiles(
            ProjectFileDirectory,
            "*.refitter",
            SearchOption.AllDirectories);

        TryLogCommandLine($"Found {files.Length} .refitter files...");

        foreach (var file in files)
        {
            TryLogCommandLine($"Processing {file}");
            TryExecuteRefitter(file);
        }

        return true;
    }

    private void TryExecuteRefitter(string file)
    {
        try
        {
            StartProcess(file);
        }
        catch (Exception e)
        {
            TryLogErrorFromException(e);
        }
    }

    private void StartProcess(string file)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var packageFolder = Path.GetDirectoryName(assembly.Location);
        var seperator = Path.DirectorySeparatorChar;
        
        // Try to find the appropriate Refitter binary for the current .NET SDK version
        var refitterDllPaths = new[]
        {
            $"{packageFolder}{seperator}..{seperator}net9.0{seperator}refitter.dll",
            $"{packageFolder}{seperator}..{seperator}net8.0{seperator}refitter.dll",
            $"{packageFolder}{seperator}..{seperator}refitter.dll"  // Fallback to the original path
        };
        
        var refitterDll = refitterDllPaths.FirstOrDefault(File.Exists) 
                         ?? refitterDllPaths.Last(); // Use last as fallback even if it doesn't exist
        
        TryLogCommandLine($"Using Refitter binary: {refitterDll}");

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
}
