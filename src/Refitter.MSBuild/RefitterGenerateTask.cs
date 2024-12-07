using System.Diagnostics;
using System.Reflection;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Refitter.MSBuild;

public class RefitterGenerateTask : MSBuildTask
{
    public string ProjectFileDirectory { get; set; }

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
        var refitterDll = $"{packageFolder}\\..\\refitter.dll";
        TryLogCommandLine("Starting " + refitterDll);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"{refitterDll} --settings-file {file}",
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
