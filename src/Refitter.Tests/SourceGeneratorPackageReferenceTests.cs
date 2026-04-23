using System.IO.Compression;
using System.Xml.Linq;
using FluentAssertions;

namespace Refitter.Tests;

public class SourceGeneratorPackageReferenceTests
{
    [Test]
    public void Packed_SourceGenerator_Package_Should_Not_Expose_Generator_Implementation_Dependencies()
    {
        var workspace = CreateWorkspace();

        try
        {
            var version = $"2.0.0-test.{Guid.NewGuid():N}";
            var packagePath = PackSourceGeneratorPackage(workspace, version);

            using var archive = ZipFile.OpenRead(packagePath);
            using var nuspecStream = archive.Entries
                .Single(entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
                .Open();

            var document = XDocument.Load(nuspecStream);
            var ns = document.Root!.Name.Namespace;
            var dependencyIds = document
                .Descendants(ns + "dependency")
                .Select(element => element.Attribute("id")?.Value)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToArray();

            dependencyIds.Should().NotContain(id => id!.Equals("Refit", StringComparison.OrdinalIgnoreCase));
            dependencyIds.Should().NotContain(id => id!.Equals("OasReader", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void Packed_SourceGenerator_Package_Should_Only_Ship_Its_Own_Analyzer_Assets()
    {
        var workspace = CreateWorkspace();

        try
        {
            var version = $"2.0.0-test.{Guid.NewGuid():N}";
            var packagePath = PackSourceGeneratorPackage(workspace, version);

            using var archive = ZipFile.OpenRead(packagePath);
            var entries = archive.Entries.Select(entry => entry.FullName).ToArray();

            entries.Should().Contain("analyzers/dotnet/cs/Refitter.SourceGenerator.dll");
            entries.Should().Contain("build/Refitter.SourceGenerator.props");
            entries.Should().NotContain(entry =>
                entry.StartsWith("analyzers/dotnet/cs/", StringComparison.OrdinalIgnoreCase) &&
                !entry.Equals("analyzers/dotnet/cs/Refitter.SourceGenerator.dll", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteWorkspace(workspace);
        }
    }

    private static string PackSourceGeneratorPackage(string workspace, string version)
    {
        var repoRoot = GetRepositoryRoot();
        var projectFile = Path.Combine(repoRoot, "src", "Refitter.SourceGenerator", "Refitter.SourceGenerator.csproj");
        var packageOutputPath = Path.Combine(workspace, "packages");
        Directory.CreateDirectory(packageOutputPath);
        BuildSourceGeneratorProject(repoRoot, projectFile);

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"pack \"{projectFile}\" -c Release --no-build --no-restore -p:PackageVersion={version} -p:PackageOutputPath=\"{packageOutputPath}\"",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        process.Should().NotBeNull();
        var output = process!.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        process.ExitCode.Should().Be(0, $"dotnet pack should succeed{Environment.NewLine}{output}{Environment.NewLine}{error}");

        var packagePath = Path.Combine(packageOutputPath, $"Refitter.SourceGenerator.{version}.nupkg");
        File.Exists(packagePath).Should().BeTrue("dotnet pack should produce the expected nupkg");
        return packagePath;
    }

    private static void BuildSourceGeneratorProject(string repoRoot, string projectFile)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectFile}\" -c Release",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        process.Should().NotBeNull();
        var output = process!.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        process.ExitCode.Should().Be(0, $"dotnet build should succeed before pack{Environment.NewLine}{output}{Environment.NewLine}{error}");
    }

    private static string CreateWorkspace()
    {
        var workspace = Path.Combine(AppContext.BaseDirectory, "SourceGeneratorPackageReferenceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        return workspace;
    }

    private static void DeleteWorkspace(string workspace)
    {
        if (Directory.Exists(workspace))
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "README.md")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("tests should run from within the repository workspace");
        return directory!.FullName;
    }
}
