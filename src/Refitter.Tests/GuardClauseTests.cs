using AwesomeAssertions;
using Refitter.Core;
using Refitter.Core.Validation;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests;

public class GuardClauseTests
{
    [Test]
    public async Task OpenApiValidator_Propagates_Cancellation_For_Remote_Document()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await OpenApiValidator.Validate(
            "https://example.com/openapi.json",
            allowRemoteReferences: true,
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task OpenApiDocumentFactory_Propagates_Cancellation_For_Remote_Document()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await OpenApiDocumentFactory.CreateAsync(
            "https://example.com/openapi.json",
            allowRemoteReferences: true,
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task RefitGenerator_CreateAsync_Throws_On_Null_Settings()
    {
        var act = async () => await RefitGenerator.CreateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Test]
    public async Task RefitGenerator_CreateAsync_Throws_On_Null_KeepSchemaPatterns()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {}
            }
            """);

        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            KeepSchemaPatterns = null!,
        };

        var act = async () => await RefitGenerator.CreateAsync(settings);

        await act.Should()
            .ThrowAsync<ArgumentNullException>()
            .WithParameterName("keepSchemaPatterns");
    }
}
