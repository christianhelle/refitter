using System.Diagnostics;
using System.Text;

namespace Refitter.Tests.Build;

public static class BuildHelper
{
    public static bool BuildCSharp(params string[] generatedCode)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        var projectFile = Path.Combine(path, "Project.csproj");
        File.WriteAllText(projectFile, ProjectFileContents.Net70App);

        for (int i = 0; i < generatedCode.Length; i++)
        {
            File.WriteAllText(Path.Combine(path, $"Generated_{i}.cs"), generatedCode[i]);
        }

        var processStartInfo = new ProcessStartInfo(GetDotNetCli(), $"build \"{projectFile}\"");
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;
        processStartInfo.UseShellExecute = false;
        processStartInfo.CreateNoWindow = true;

        var process = new Process();
        process.StartInfo = processStartInfo;

        var output = new StringBuilder();
        process.OutputDataReceived += (_, args) => output.AppendLine(args.Data);

        var errors = new StringBuilder();
        process.ErrorDataReceived += (_, args) => errors.AppendLine(args.Data);

        bool startResult = process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        var result = startResult && process.ExitCode == 0;
        if (!result)
            throw new BuildFailedException(errors.ToString(), output.ToString());

        return result;
    }

    private static string GetDotNetCli()
    {
        return Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX
            ? "dotnet"
            : "dotnet.exe";
    }
}

public class BuildFailedException(string Errors, string Output)
    : Exception($"Build failed{Crlf}Errors:{Crlf}{Errors}{Crlf}Output:{Crlf}{Output}")
{
    private static readonly string Crlf = Environment.NewLine;
}
