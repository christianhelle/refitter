using System.Diagnostics;
using System.Text;
using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class GenerateJsonSerializerContextPolymorphismTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info:
          title: Animal API
          version: 1.0.0
        paths:
          /animals:
            post:
              operationId: CreateAnimal
              requestBody:
                required: true
                content:
                  application/json:
                    schema:
                      $ref: '#/components/schemas/Animal'
              responses:
                '200':
                  description: Success
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Animal'
        components:
          schemas:
            Animal:
              type: object
              required:
                - type
                - name
              discriminator:
                propertyName: type
              properties:
                type:
                  type: string
                name:
                  type: string
            Dog:
              allOf:
                - $ref: '#/components/schemas/Animal'
                - type: object
                  properties:
                    breed:
                      type: string
        """;

    private const string RuntimeProject = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net8.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>
            <RestorePackagesPath>packages</RestorePackagesPath>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Refit.HttpClientFactory" Version="10.1.6" />
          </ItemGroup>
        </Project>
        """;

    private const string RuntimeProgram = """
        using System;
        using System.Text.Json;
        using GeneratedCode;

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            Console.Error.WriteLine("Reflection-backed JSON serialization is still enabled.");
            Environment.Exit(10);
        }

        const string json = "{\"type\":\"Dog\",\"name\":\"Fido\",\"breed\":\"Collie\"}";

        var animal = JsonSerializer.Deserialize(json, AnimalsApiSerializerContext.Default.Animal);
        var dog = animal as Dog;
        if (dog is null)
        {
            Console.Error.WriteLine($"Expected Dog but got {animal?.GetType().FullName ?? "null"}.");
            Environment.Exit(11);
        }

        if (dog.Name != "Fido" || dog.Breed != "Collie")
        {
            Console.Error.WriteLine($"Unexpected values after deserialize: {dog.Name}|{dog.Breed}");
            Environment.Exit(12);
        }

        var roundTrip = JsonSerializer.Serialize<Animal>(dog, AnimalsApiSerializerContext.Default.Animal);
        if (!roundTrip.Contains("\"type\":\"Dog\"", StringComparison.Ordinal) ||
            !roundTrip.Contains("\"breed\":\"Collie\"", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"Round-trip JSON lost derived data: {roundTrip}");
            Environment.Exit(13);
        }

        var clone = JsonSerializer.Deserialize(roundTrip, AnimalsApiSerializerContext.Default.Animal);
        var clonedDog = clone as Dog;
        if (clonedDog is null || clonedDog.Breed != "Collie")
        {
            Console.Error.WriteLine($"Round-trip deserialize failed: {clone?.GetType().FullName ?? "null"}.");
            Environment.Exit(14);
        }
        """;

    [Test]
    public async Task Generated_Context_Supports_Polymorphic_Roundtrip_When_Reflection_Is_Disabled()
    {
        var generatedCode = await GenerateCode();

        generatedCode.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Animal))]");
        generatedCode.Should().Contain("[global::System.Text.Json.Serialization.JsonSerializable(typeof(Dog))]");

        var (exitCode, output) = RunGeneratedCode(generatedCode);

        exitCode.Should().Be(0, output);
    }

    private static async Task<string> GenerateCode()
    {
        var workingDirectory = CreateWorkingDirectory();
        var openApiPath = Path.Combine(workingDirectory, "openapi.yml");

        try
        {
            await File.WriteAllTextAsync(openApiPath, OpenApiSpec);

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "GeneratedCode",
                GenerateJsonSerializerContext = true,
                UsePolymorphicSerialization = true,
                Naming = new NamingSettings
                {
                    UseOpenApiTitle = false,
                    InterfaceName = "IAnimalsApi"
                }
            };

            var generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        }
        finally
        {
            DeleteDirectoryIfExists(workingDirectory);
        }
    }

    private static (int ExitCode, string Output) RunGeneratedCode(string generatedCode)
    {
        var workingDirectory = CreateWorkingDirectory();
        var projectPath = Path.Combine(workingDirectory, "RuntimeProof.csproj");
        var generatedPath = Path.Combine(workingDirectory, "GeneratedCode.cs");
        var programPath = Path.Combine(workingDirectory, "Program.cs");

        try
        {
            File.WriteAllText(projectPath, RuntimeProject);
            File.WriteAllText(generatedPath, generatedCode);
            File.WriteAllText(programPath, RuntimeProgram);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(
                    GetDotNetCli(),
                    $"run --project \"{projectPath}\" -c Release --nologo --verbosity quiet")
                {
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var output = new StringBuilder()
                .AppendLine(process.StandardOutput.ReadToEnd())
                .AppendLine(process.StandardError.ReadToEnd())
                .ToString();

            process.WaitForExit();
            return (process.ExitCode, output);
        }
        finally
        {
            DeleteDirectoryIfExists(workingDirectory);
        }
    }

    private static string CreateWorkingDirectory()
    {
        var root = Path.Combine(GetRepositoryRoot(), ".test-work", "Issue1017");

        Directory.CreateDirectory(root);

        var workingDirectory = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);
        return workingDirectory;
    }

    private static void DeleteDirectoryIfExists(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return;

        try
        {
            Directory.Delete(path, true);
        }
        catch
        {
            // Ignore cleanup errors.
        }
    }

    private static string GetDotNetCli() =>
        Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX
            ? "dotnet"
            : "dotnet.exe";

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(
            Path.GetDirectoryName(typeof(GenerateJsonSerializerContextPolymorphismTests).Assembly.Location)
                ?? AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, ".git")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")) ||
                File.Exists(Path.Combine(directory.FullName, "src", "Refitter.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Environment.CurrentDirectory;
    }
}
