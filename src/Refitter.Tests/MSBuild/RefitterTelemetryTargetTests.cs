using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using AwesomeAssertions;

namespace Refitter.Tests.MSBuild;

/// <summary>
/// Runs the telemetry target from Refitter.SourceGenerator.targets out of the packed NuGet package, or out of the same
/// layout built from the build output when the generator was not packed.
/// Only opted-out builds are run so that the tests never send telemetry.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class RefitterTelemetryTargetTests
{
    private const string TaskName = "RefitterTelemetryTask";
    private string workspace = null!;

    [TestInitialize]
    public void Initialize()
    {
        workspace = Path.Combine(Path.GetTempPath(), "refitter-telemetry-target-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(workspace, "build"));
        Directory.CreateDirectory(Path.Combine(workspace, "analyzers", "dotnet", "cs"));

        if (FindSourceGeneratorPackage() is { } package)
        {
            ExtractPackage(package, workspace);
        }
        else
        {
            File.Copy(
                Path.Combine(FindRepositoryRoot(), "src", "Refitter.SourceGenerator", "Refitter.SourceGenerator.targets"),
                Path.Combine(workspace, "build", "Refitter.SourceGenerator.targets"));
            CopySourceGeneratorPackageAssemblies(Path.Combine(workspace, "analyzers", "dotnet", "cs"));
        }

        File.WriteAllText(Path.Combine(workspace, "petstore.refitter"), """{ "openApiPath": "./openapi.json" }""");
        File.WriteAllText(
            Path.Combine(workspace, "consumer.proj"),
            """
            <Project>
              <ItemGroup>
                <AdditionalFiles Include="petstore.refitter" />
              </ItemGroup>
              <Target Name="Build" />
              <Import Project="build/Refitter.SourceGenerator.targets" />
            </Project>
            """);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // MSBuild can keep the loaded task assembly open for a moment after the build exits
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                Directory.Delete(workspace, true);
                return;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(500);
            }
        }
    }

    [TestMethod]
    public void Target_Runs_Task_After_Build_And_Task_Honours_Dotnet_Telemetry_Opt_Out()
    {
        var output = RunBuild(string.Empty, telemetryOptOut: "1");

        output.Should().Contain($"Task \"{TaskName}\"");
        StampFileExists().Should().BeFalse();
    }

    [TestMethod]
    public void Target_Is_Skipped_When_RefitterNoLogging_Is_Set()
    {
        var output = RunBuild("-p:RefitterNoLogging=true", telemetryOptOut: "1");

        output.Should().NotContain($"Task \"{TaskName}\"");
        StampFileExists().Should().BeFalse();
    }

    [TestMethod]
    public void Target_Is_Skipped_During_Design_Time_Build()
    {
        var output = RunBuild("-p:DesignTimeBuild=true", telemetryOptOut: "1");

        output.Should().NotContain($"Task \"{TaskName}\"");
        StampFileExists().Should().BeFalse();
    }

    private bool StampFileExists() =>
        File.Exists(Path.Combine(workspace, "refitter.sourcegenerator.telemetry"));

    private string RunBuild(string arguments, string telemetryOptOut)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"msbuild consumer.proj -t:Build -v:d -nologo -nodeReuse:false {arguments}",
            WorkingDirectory = workspace,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = telemetryOptOut;
        startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";

        // Keep code coverage out of the child build. It would instrument the generator assembly, whose
        // never-run copy of the Refitter.Core sources then hides the real Refitter.Core coverage.
        startInfo.Environment["CORECLR_ENABLE_PROFILING"] = "0";
        startInfo.Environment["DOTNET_ENABLE_PROFILING"] = "0";

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        process.WaitForExit(120000).Should().BeTrue();

        var result = output.GetAwaiter().GetResult() + error.GetAwaiter().GetResult();
        process.ExitCode.Should().Be(0, result);
        return result;
    }

    // Release builds pack the generator (GeneratePackageOnBuild), so test the package that ships when it exists.
    // Only a package written after the generator was built comes from this build; an older one is left over.
    private static string? FindSourceGeneratorPackage()
    {
        var assembly = GetSourceGeneratorAssembly();
        var builtAt = File.GetLastWriteTimeUtc(assembly);
        var packageDirectory = Path.GetDirectoryName(Path.GetDirectoryName(assembly))!;
        return Directory.Exists(packageDirectory)
            ? Directory.GetFiles(packageDirectory, "Refitter.SourceGenerator.*.nupkg")
                .Where(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
                .Where(path => File.GetLastWriteTimeUtc(path) >= builtAt)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
            : null;
    }

    private static void ExtractPackage(string package, string destination)
    {
        var root = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
        using var archive = ZipFile.OpenRead(package);
        foreach (var entry in archive.Entries.Where(entry =>
                     entry.Name.Length > 0 &&
                     (entry.FullName.StartsWith("analyzers/dotnet/cs/", StringComparison.Ordinal) ||
                      entry.FullName.StartsWith("build/", StringComparison.Ordinal))))
        {
            // Never write outside the workspace, whatever the entry name contains
            var path = Path.GetFullPath(Path.Combine(root, entry.FullName));
            if (!path.StartsWith(root, StringComparison.Ordinal))
                throw new InvalidOperationException($"Package entry '{entry.FullName}' is outside the package");

            entry.ExtractToFile(path, overwrite: true);
        }
    }

    [TestMethod]
    public void Package_Ships_The_Task_Dependencies_Next_To_The_Generator()
    {
        var package = FindSourceGeneratorPackage();
        if (package is null)
        {
            Assert.Inconclusive("The generator is only packed in Release builds");
            return;
        }

        using var archive = ZipFile.OpenRead(package);
        archive.Entries
            .Select(entry => entry.FullName)
            .Should()
            .Contain(
            [
                "analyzers/dotnet/cs/Refitter.SourceGenerator.dll",
                "analyzers/dotnet/cs/Microsoft.OpenApi.dll",
                "analyzers/dotnet/cs/Microsoft.ApplicationInsights.dll",
                "build/Refitter.SourceGenerator.targets",
            ]);
    }

    // The package ships the generator's dependencies next to it, and MSBuild may need them to load the task
    // assembly (for example to resolve the base types of its public types), so lay them out the same way.
    // The compiler provides Roslyn, so the package leaves it out.
    private static void CopySourceGeneratorPackageAssemblies(string analyzersDirectory)
    {
        var buildOutput = Path.GetDirectoryName(GetSourceGeneratorAssembly())!;
        foreach (var assembly in Directory.GetFiles(buildOutput, "*.dll"))
        {
            var fileName = Path.GetFileName(assembly);
            if (!fileName.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
                File.Copy(assembly, Path.Combine(analyzersDirectory, fileName));
        }
    }

    // The task ships inside the generator assembly, so load it from the built generator rather than the linked sources
    private static string GetSourceGeneratorAssembly() =>
        typeof(RefitterTelemetryTargetTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RefitterSourceGeneratorAssembly")
            .Value!;

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "src", "Refitter.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Unable to locate the repository root");
    }
}
