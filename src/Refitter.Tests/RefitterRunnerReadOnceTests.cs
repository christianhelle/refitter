using AwesomeAssertions;
using Microsoft.OpenApi.Reader;
using Refitter.Core;
using Refitter.Core.Validation;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests;

/// <summary>
/// A run reads the OpenAPI document once, and generates and validates from what it read.
/// </summary>
[TestClass]
public class RefitterRunnerReadOnceTests
{
    private const string Spec =
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{\"/pets\":{\"get\":" +
        "{\"operationId\":\"GetPets\",\"responses\":{\"200\":{\"description\":\"ok\"}}}}}}";

    [TestMethod]
    public async Task RunAsync_Downloads_Remote_Specification_Once()
    {
        using var server = new LoopbackHttpServer(Spec);
        var settings = new RefitGeneratorSettings { OpenApiPath = server.BaseUrl + "openapi.json" };

        var result = await new RefitterRunner().RunAsync(settings, validator: new OpenApiValidatorAdapter());

        result.ExitCode.Should().Be(0);
        result.GeneratedFiles.Should().NotBeEmpty();
        server.RequestCount.Should().Be(1);
    }

    [TestMethod]
    public async Task RunAsync_Passes_Path_To_Custom_Validator()
    {
        var folder = TestDirectory.CreateFolder();
        var path = Path.Combine(folder, "spec.json");
        await File.WriteAllTextAsync(path, Spec);
        var validator = new RecordingValidator();

        var result = await new RefitterRunner().RunAsync(
            new RefitGeneratorSettings { OpenApiPath = path },
            validator: validator);

        result.ExitCode.Should().Be(0);
        validator.Paths.Should().Equal(path);
    }

    private sealed class RecordingValidator : IValidator
    {
        public List<string> Paths { get; } = [];

        public Task<OpenApiValidationResult> ValidateAsync(
            string openApiPath,
            bool allowRemoteReferences = false,
            CancellationToken cancellationToken = default)
        {
            Paths.Add(openApiPath);
            return Task.FromResult(new OpenApiValidationResult(new OpenApiDiagnostic(), new OpenApiStats()));
        }
    }
}
