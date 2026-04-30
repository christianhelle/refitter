using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Refitter.SourceGenerators.Tests;

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
    public void Packed_SourceGenerator_Package_Should_Ship_Required_Analyzer_Assets()
    {
        var workspace = CreateWorkspace();

        try
        {
            var version = $"2.0.0-test.{Guid.NewGuid():N}";
            var packagePath = PackSourceGeneratorPackage(workspace, version);

            using var archive = ZipFile.OpenRead(packagePath);
            var entries = archive.Entries.Select(entry => entry.FullName).ToArray();

            entries.Should().Contain("analyzers/dotnet/cs/Refitter.SourceGenerator.dll");
            entries.Should().Contain("analyzers/dotnet/cs/H.Generators.Extensions.dll");
            entries.Should().Contain("analyzers/dotnet/cs/NSwag.Core.dll");
            entries.Should().Contain("analyzers/dotnet/cs/OasReader.dll");
            entries.Should().Contain("analyzers/dotnet/cs/Refit.dll");
            entries.Should().Contain("build/Refitter.SourceGenerator.props");
        }
        finally
        {
            DeleteWorkspace(workspace);
        }
    }

    [Test]
    public void Packed_SourceGenerator_Package_Should_Load_Analyzer_With_Packaged_Dependencies()
    {
        var workspace = CreateWorkspace();

        try
        {
            var version = $"2.0.0-test.{Guid.NewGuid():N}";
            var packagePath = PackSourceGeneratorPackage(workspace, version);
            var extractPath = Path.Combine(workspace, "extracted");
            ZipFile.ExtractToDirectory(packagePath, extractPath);

            var analyzerPath = Path.Combine(
                extractPath,
                "analyzers",
                "dotnet",
                "cs",
                "Refitter.SourceGenerator.dll");

            File.Exists(analyzerPath).Should().BeTrue("the package should contain the source generator analyzer");

            var loadContext = new AnalyzerAssemblyLoadContext(analyzerPath);
            try
            {
                var assembly = loadContext.LoadFromAssemblyPath(analyzerPath);
                var generatorType = assembly.GetType("Refitter.SourceGenerator.RefitterSourceGenerator", throwOnError: true)!;
                var generator = (IIncrementalGenerator)Activator.CreateInstance(generatorType)!;

                var openApiPath = Path.Combine(workspace, "openapi.json");
                File.WriteAllText(openApiPath, MinimalOpenApiDocument);

                var refitterPath = Path.Combine(workspace, "package.refitter");
                var refitterJson = $$"""
                    {
                      "openApiPath": "{{EscapeJsonString(openApiPath)}}",
                      "namespace": "PackageAnalyzerLoadability",
                      "naming": {
                        "useOpenApiTitle": false,
                        "interfaceName": "PackageApi"
                      }
                    }
                    """;

                var compilation = CSharpCompilation.Create(
                    "PackageAnalyzerLoadability",
                    [CSharpSyntaxTree.ParseText("namespace PackageAnalyzerLoadability; public sealed class Stub { }")],
                    GetMetadataReferences(),
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                GeneratorDriver driver = CSharpGeneratorDriver.Create(
                    [generator.AsSourceGenerator()],
                    additionalTexts: [new StubAdditionalText(refitterPath, SourceText.From(refitterJson, Encoding.UTF8))],
                    parseOptions: CSharpParseOptions.Default);

                driver = driver.RunGenerators(compilation);
                var runResult = driver.GetRunResult();
                var diagnostics = runResult.Diagnostics
                    .Concat(runResult.Results.SelectMany(result => result.Diagnostics))
                    .ToArray();

                diagnostics.Should().NotContain(diagnostic =>
                    diagnostic.Id == "CS8785" ||
                    diagnostic.Id == "AD0001" ||
                    diagnostic.Id == "REFITTER000");

                runResult.Results
                    .SelectMany(result => result.GeneratedSources)
                    .Should()
                    .Contain(source => source.HintName.EndsWith(".g.cs", StringComparison.Ordinal));
            }
            finally
            {
                loadContext.Unload();
                for (var i = 0; i < 5; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }
        finally
        {
            DeleteWorkspace(workspace);
        }
    }

    private static string PackSourceGeneratorPackage(string workspace, string version)
    {
        using var mutex = new Mutex(false, "RefitterSourceGeneratorPackageReferenceTests");
        mutex.WaitOne(TimeSpan.FromMinutes(3)).Should().BeTrue("package tests build and pack the same project output");

        try
        {
            var repoRoot = GetRepositoryRoot();
            var projectFile = Path.Combine(repoRoot, "src", "Refitter.SourceGenerator", "Refitter.SourceGenerator.csproj");
            var packageOutputPath = Path.Combine(workspace, "packages");

            Directory.CreateDirectory(packageOutputPath);

            var startInfo = CreateDotNetStartInfo(
                repoRoot,
                "pack",
                projectFile,
                "-c",
                "Release",
                "--no-build",
                "--no-restore",
                $"-p:PackageVersion={version}",
                $"-p:PackageOutputPath={packageOutputPath}");

            using var process = System.Diagnostics.Process.Start(startInfo);
            process.Should().NotBeNull();
            var outputTask = process!.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            Task.WaitAll(outputTask, errorTask);
            var output = outputTask.Result;
            var error = errorTask.Result;

            process.ExitCode.Should().Be(0, $"dotnet pack should succeed{Environment.NewLine}{output}{Environment.NewLine}{error}");

            var packagePath = Path.Combine(packageOutputPath, $"Refitter.SourceGenerator.{version}.nupkg");
            File.Exists(packagePath).Should().BeTrue("dotnet pack should produce the expected nupkg");
            return packagePath;
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private static string CreateWorkspace()
    {
        var workspace = Path.Combine(AppContext.BaseDirectory, "SourceGeneratorPackageReferenceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        return workspace;
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences() =>
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location)
    ];

    private static string EscapeJsonString(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static void DeleteWorkspace(string workspace)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (Directory.Exists(workspace))
                {
                    Directory.Delete(workspace, recursive: true);
                }

                return;
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(200);
            }
            catch (UnauthorizedAccessException) when (attempt < 4)
            {
                Thread.Sleep(200);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
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

    private static System.Diagnostics.ProcessStartInfo CreateDotNetStartInfo(string repoRoot, params string[] arguments)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private const string MinimalOpenApiDocument = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Package analyzer loadability",
            "version": "1.0.0"
          },
          "paths": {
            "/ping": {
              "get": {
                "operationId": "Ping",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "type": "string"
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    private sealed class StubAdditionalText(string path, SourceText text) : AdditionalText
    {
        public override string Path => path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) => text;
    }

    private sealed class AnalyzerAssemblyLoadContext(string analyzerPath) : AssemblyLoadContext(isCollectible: true)
    {
        private readonly AssemblyDependencyResolver resolver = new(analyzerPath);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name is null ||
                assemblyName.Name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) ||
                assemblyName.Name == "System.Collections.Immutable")
            {
                return null;
            }

            var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath is null
                ? null
                : LoadFromAssemblyPath(assemblyPath);
        }
    }
}
