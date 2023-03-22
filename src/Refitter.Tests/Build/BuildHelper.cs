using System.Diagnostics;

namespace Refitter.Tests.Build;

public static class BuildHelper
{
    public static bool BuildCSharp(string generatedCode)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        var projectFile = Path.Combine(path, "Project.csproj");
        File.WriteAllText(projectFile, ProjectFileContents.Net70App);
        File.WriteAllText(Path.Combine(path, "Generated.cs"), generatedCode);
        var process = Process.Start(GetDotNetCli(), $"build \"{projectFile}\"");
        process.WaitForExit();
        return process.ExitCode == 0;
    }

    private static string GetDotNetCli()
    {
        return Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX 
            ? "dotnet" 
            : "dotnet.exe";
    }
}