using FluentAssertions;
using Microsoft.OpenApi.Reader;
using Refitter.Core;
using Refitter.Core.Validation;

namespace Refitter.Tests;

public class RefitterRunnerTests
{
    private static string CreateTempDirectory() =>
        Path.Combine(
            AppContext.BaseDirectory,
            "RefitterRunnerTests",
            Guid.NewGuid().ToString("N"));

    private static string CreateOpenApiSpec(string directory, string fileName = "spec.json")
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(
            path,
            """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test API", "version": "1.0.0" },
              "paths": {
                "/pets": {
                  "get": {
                    "operationId": "GetPets",
                    "responses": { "200": { "description": "ok" } }
                  }
                }
              }
            }
            """);
        return path;
    }

    [Test]
    public async Task RunAsync_Should_Generate_Code_And_Return_PlannedFiles()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var openApiPath = CreateOpenApiSpec(workspace);
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
            };

            var runner = new RefitterRunner();
            var result = await runner.RunAsync(
                settings,
                writer: null,
                validator: null,
                cancellationToken: default);

            result.ExitCode.Should().Be(0);
            result.GeneratedFiles.Should().NotBeEmpty();
            result.GeneratedFiles[0].Content.Should().Contain("TestNamespace");
            result.GeneratedFiles[0].Content.Should().Contain("GetPets");
            result.Warnings.Should().BeEmpty();
            result.Diagnostics.Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_Should_Call_Writer_When_Provided()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var openApiPath = CreateOpenApiSpec(workspace);
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
            };

            var writer = new MockFileWriter();
            var runner = new RefitterRunner();
            var result = await runner.RunAsync(
                settings,
                writer: writer,
                validator: null,
                cancellationToken: default);

            result.ExitCode.Should().Be(0);
            writer.WrittenFiles.Should().NotBeEmpty();
            writer.WrittenFiles[0].Content.Should().Contain("TestNamespace");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_Should_Not_Call_Writer_When_Null()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var openApiPath = CreateOpenApiSpec(workspace);
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
            };

            var runner = new RefitterRunner();
            var result = await runner.RunAsync(
                settings,
                writer: null,
                validator: null,
                cancellationToken: default);

            result.ExitCode.Should().Be(0);
            result.GeneratedFiles.Should().NotBeEmpty();
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_Should_Call_Validator_When_Provided()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var openApiPath = CreateOpenApiSpec(workspace);
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
            };

            var validator = new MockValidator();
            var runner = new RefitterRunner();
            var result = await runner.RunAsync(
                settings,
                writer: null,
                validator: validator,
                cancellationToken: default);

            result.ExitCode.Should().Be(0);
            validator.CallCount.Should().Be(1);
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_Should_Not_Call_Validator_When_Null()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var openApiPath = CreateOpenApiSpec(workspace);
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
            };

            var runner = new RefitterRunner();
            var result = await runner.RunAsync(
                settings,
                writer: null,
                validator: null,
                cancellationToken: default);

            result.ExitCode.Should().Be(0);
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_Should_Detect_UseIsoDateFormat_Warning()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var openApiPath = CreateOpenApiSpec(workspace);
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
                UseIsoDateFormat = true,
                CodeGeneratorSettings = new CodeGeneratorSettings
                {
                    DateFormat = "yyyy-MM-dd"
                },
            };

            var runner = new RefitterRunner();
            var result = await runner.RunAsync(
                settings,
                writer: null,
                validator: null,
                cancellationToken: default);

            result.ExitCode.Should().Be(0);
            result.Warnings.Should().Contain(w =>
                w.Title == "Date Format Override" &&
                w.Description.Contains("useIsoDateFormat"));
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_Should_Detect_Deprecated_Polly_Warning()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var openApiPath = CreateOpenApiSpec(workspace);
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
#pragma warning disable CS0618
                DependencyInjectionSettings = new DependencyInjectionSettings
                {
                    UsePolly = true,
                },
#pragma warning restore CS0618
            };

            var runner = new RefitterRunner();
            var result = await runner.RunAsync(
                settings,
                writer: null,
                validator: null,
                cancellationToken: default);

            result.ExitCode.Should().Be(0);
            result.Warnings.Should().Contain(w =>
                w.Title == "Deprecated Setting" &&
                w.Description.Contains("usePolly"));
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_Should_Detect_Both_Warnings()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var openApiPath = CreateOpenApiSpec(workspace);
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
                UseIsoDateFormat = true,
                CodeGeneratorSettings = new CodeGeneratorSettings
                {
                    DateFormat = "yyyy-MM-dd"
                },
#pragma warning disable CS0618
                DependencyInjectionSettings = new DependencyInjectionSettings
                {
                    UsePolly = true,
                },
#pragma warning restore CS0618
            };

            var runner = new RefitterRunner();
            var result = await runner.RunAsync(
                settings,
                writer: null,
                validator: null,
                cancellationToken: default);

            result.ExitCode.Should().Be(0);
            result.Warnings.Should().HaveCount(2);
            result.Warnings.Should().Contain(w => w.Title == "Date Format Override");
            result.Warnings.Should().Contain(w => w.Title == "Deprecated Setting");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_With_MultipleFiles_Should_Generate_Multiple_PlannedFiles()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var openApiPath = CreateOpenApiSpec(workspace);
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateMultipleFiles = true,
                GenerateContracts = false,
            };

            var runner = new RefitterRunner();
            var result = await runner.RunAsync(
                settings,
                writer: null,
                validator: null,
                cancellationToken: default);

            result.ExitCode.Should().Be(0);
            result.GeneratedFiles.Should().NotBeEmpty();
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_With_NonExistent_Spec_Should_Return_NonZero()
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = "C:\\nonexistent\\spec.json",
            Namespace = "TestNamespace",
            GenerateContracts = false,
            GenerateClients = true,
        };

        var runner = new RefitterRunner();
        var result = await runner.RunAsync(
            settings,
            writer: null,
            validator: null,
            cancellationToken: default);

        result.ExitCode.Should().NotBe(0);
        result.Diagnostics.Should().Contain(d => d.IsError);
    }

    [Test]
    public async Task RunAsync_With_Validation_Errors_Should_Return_Diagnostics()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var openApiPath = CreateOpenApiSpec(workspace);
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = openApiPath,
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
            };

            var diagnostic = new OpenApiDiagnostic();
            diagnostic.Errors.Add(new Microsoft.OpenApi.OpenApiError("test", "Something went wrong"));
            var validationResult = new OpenApiValidationResult(diagnostic, new OpenApiStats());
            var validator = new MockValidator(validationResult);

            var runner = new RefitterRunner();
            var result = await runner.RunAsync(
                settings,
                writer: null,
                validator: validator,
                cancellationToken: default);

            result.ExitCode.Should().NotBe(0);
            result.Diagnostics.Should().Contain(d => d.IsError && d.Message.Contains("Something went wrong"));
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task RunAsync_With_MultipleOpenApiPaths_Should_Validate_All()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var spec1 = CreateOpenApiSpec(workspace, "spec1.json");
            var spec2 = CreateOpenApiSpec(workspace, "spec2.json");
            var settings = new RefitGeneratorSettings
            {
                OpenApiPaths = new[] { spec1, spec2 },
                Namespace = "TestNamespace",
                GenerateContracts = false,
                GenerateClients = true,
            };

            var validator = new MockValidator();
            var runner = new RefitterRunner();
            var result = await runner.RunAsync(
                settings,
                writer: null,
                validator: validator,
                cancellationToken: default);

            result.ExitCode.Should().Be(0);
            validator.CallCount.Should().Be(2);
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    private sealed class MockFileWriter : IFileWriter
    {
        public List<PlannedFile> WrittenFiles { get; } = [];

        public Task WriteAsync(PlannedFile file, CancellationToken cancellationToken = default)
        {
            WrittenFiles.Add(file);
            return Task.CompletedTask;
        }
    }

    private sealed class MockValidator : IValidator
    {
        private readonly OpenApiValidationResult _result;

        public int CallCount { get; private set; }

        public MockValidator(OpenApiValidationResult? result = null)
        {
            _result = result ?? new OpenApiValidationResult(
                new OpenApiDiagnostic(),
                new OpenApiStats());
        }

        public Task<OpenApiValidationResult> ValidateAsync(
            string openApiPath,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }
}
