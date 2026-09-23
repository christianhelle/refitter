using System.Diagnostics;
using System.Text;
using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using TUnit.Core;

namespace Refitter.Tests;

public class ConstantsCompatibilityTests
{
    [Test]
#pragma warning disable CS0618
    public void RestoredCompatibilityConstantsKeepTheirPublicValues()
    {
        FileExtensionConstants.GeneratedCSharp.Should().Be(".g.cs");
        FileExtensionConstants.CSharp.Should().Be(".cs");
        ContentTypeConstants.Json.Should().Be("application/json");
        ContentTypeConstants.Xml.Should().Be("application/xml");
        PackageConstants.Polly.Should().Be("Polly");
        PackageConstants.Akavache.Should().Be("Akavache");
        PackageConstants.MonkeyCache.Should().Be("MonkeyCache");
        PackageConstants.AutoMapper.Should().Be("AutoMapper");
        PackageConstants.Mapster.Should().Be("Mapster");
        PackageConstants.MediatR.Should().Be("MediatR");
        DotNetTypeConstants.DateTimeOffset.Should().Be("System.DateTimeOffset");
        DotNetTypeConstants.TimeSpan.Should().Be("System.TimeSpan");
        DotNetTypeConstants.Dictionary.Should().Be("System.Collections.Generic.Dictionary");
        FilenameConstants.DefaultOutput.Should().Be("Output.cs");
        FilenameConstants.DefaultSettingsFile.Should().Be(".refitter");
    }
#pragma warning restore CS0618

    [Category("Integration")]
    [Test]
    public void RestoredCompatibilityConstantsCanStillBeConsumedByDownstreamCode()
        => BuildCompatibilityConsumer().Should().BeTrue();

    private static bool BuildCompatibilityConsumer()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);

        var projectFile = Path.Combine(path, "Project.csproj");
        File.WriteAllText(projectFile, $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <Reference Include="Refitter.Core">
                  <HintPath>{{typeof(FileExtensionConstants).Assembly.Location}}</HintPath>
                </Reference>
              </ItemGroup>
            </Project>
            """);

        File.WriteAllText(Path.Combine(path, "Consumer.cs"), """
            using Refitter.Core;

            public static class Consumer
            {
                public static string Value => string.Join("|",
                    FileExtensionConstants.GeneratedCSharp,
                    FileExtensionConstants.CSharp,
                    ContentTypeConstants.Json,
                    ContentTypeConstants.Xml,
                    PackageConstants.Polly,
                    PackageConstants.Akavache,
                    PackageConstants.MonkeyCache,
                    PackageConstants.AutoMapper,
                    PackageConstants.Mapster,
                    PackageConstants.MediatR,
                    DotNetTypeConstants.DateTimeOffset,
                    DotNetTypeConstants.TimeSpan,
                    DotNetTypeConstants.Dictionary,
                    FilenameConstants.DefaultOutput,
                    FilenameConstants.DefaultSettingsFile);
            }
            """);

        var processStartInfo = new ProcessStartInfo("dotnet", $"build \"{projectFile}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        var output = new StringBuilder();
        process.OutputDataReceived += (_, args) => output.AppendLine(args.Data);

        var errors = new StringBuilder();
        process.ErrorDataReceived += (_, args) => errors.AppendLine(args.Data);

        var started = process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (!(started && process.ExitCode == 0))
            throw new BuildFailedException(errors.ToString(), output.ToString());

        try
        {
            Directory.Delete(path, true);
        }
        catch
        {
            // Ignore cleanup errors
        }

        return true;
    }
}
