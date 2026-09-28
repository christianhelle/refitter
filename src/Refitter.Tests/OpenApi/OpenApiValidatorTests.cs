using AwesomeAssertions;
using Refitter.Core.Validation;
using Refitter.Tests.Resources;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;


public class OpenApiValidatorTests
{
    [Test]
    public async Task Validate_Should_Return_Valid_Result_For_Valid_OpenAPI_V3_Spec()
    {
        var openApiSpec = EmbeddedResources.GetSwaggerPetstore(SampleOpenSpecifications.SwaggerPetstoreJsonV3);
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, openApiSpec);
            var result = await OpenApiValidator.Validate(tempFile);

            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Diagnostics.Should().NotBeNull();
            result.Diagnostics.Errors.Should().BeEmpty();
            result.Statistics.Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Test]
    public async Task Validate_Should_Return_Valid_Result_For_Valid_OpenAPI_V2_Spec()
    {
        var openApiSpec = EmbeddedResources.GetSwaggerPetstore(SampleOpenSpecifications.SwaggerPetstoreJsonV2);
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, openApiSpec);
            var result = await OpenApiValidator.Validate(tempFile);

            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Diagnostics.Should().NotBeNull();
            result.Diagnostics.Errors.Should().BeEmpty();
            result.Statistics.Should().NotBeNull();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Test]
    public async Task Validate_Should_Collect_Statistics()
    {
        var openApiSpec = EmbeddedResources.GetSwaggerPetstore(SampleOpenSpecifications.SwaggerPetstoreJsonV3);
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, openApiSpec);
            var result = await OpenApiValidator.Validate(tempFile);

            result.Statistics.Should().NotBeNull();
            result.Statistics.OperationCount.Should().BeGreaterThan(0);
            result.Statistics.PathItemCount.Should().BeGreaterThan(0);
            result.Statistics.SchemaCount.Should().BeGreaterThan(0);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Test]
    public async Task Validate_Should_Return_Diagnostics_With_Zero_Errors_For_Valid_Spec()
    {
        var openApiSpec = EmbeddedResources.GetSwaggerPetstore(SampleOpenSpecifications.SwaggerPetstoreJsonV3);
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, openApiSpec);
            var result = await OpenApiValidator.Validate(tempFile);

            result.Diagnostics.Errors.Count.Should().Be(0);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Test]
    public async Task Validate_Should_Parse_Remote_OpenApi_Document()
    {
        var openApiSpec = EmbeddedResources.GetSwaggerPetstore(SampleOpenSpecifications.SwaggerPetstoreJsonV3);
        await using var server = new LocalHttpServer(openApiSpec);

        var result = await OpenApiValidator.Validate(server.Url);

        result.IsValid.Should().BeTrue();
        result.Diagnostics.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task Validate_Should_Throw_When_The_Specification_Version_Is_Not_Supported()
    {
        var openApiPath = await SwaggerFileHelper.CreateSwaggerJsonFile(
            """
            { "openapi": "4.0.0", "info": { "title": "Test API", "version": "1.0.0" }, "paths": {} }
            """);

        var act = () => OpenApiValidator.Validate(openApiPath);

        var exception = await act.Should().ThrowAsync<UnsupportedSpecificationVersionException>()
            .WithMessage("OpenAPI specification version '4.0.0' is not supported.");
        exception.Which.SpecificationVersion.Should().Be("4.0.0");
    }

    [Test]
    public async Task Validate_Should_Throw_InvalidOperationException_When_Remote_Download_Fails()
    {
        var act = () => OpenApiValidator.Validate("http://127.0.0.1:1/openapi.json");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to download OpenAPI document*");
    }

    /// <summary>
    /// Microsoft.OpenApi overflowed the stack on such a cycle, crashing the process.
    /// </summary>
    [Test]
    public async Task Validate_Should_Treat_A_Cycle_Of_Parameter_References_As_Unresolved()
    {
        var openApiPath = await SwaggerFileHelper.CreateSwaggerFile(
            """
            openapi: 3.0.1
            info:
              title: Parameter references in a cycle
              version: 1.0.0
            paths:
              /pets:
                get:
                  parameters:
                    - $ref: '#/components/parameters/A'
                  responses:
                    '200':
                      description: OK
            components:
              parameters:
                A:
                  $ref: '#/components/parameters/B'
                B:
                  $ref: '#/components/parameters/A'
            """);

        var result = await OpenApiValidator.Validate(openApiPath);

        result.Diagnostics.Errors.Should().BeEmpty();
        result.Statistics.ParameterCount.Should().Be(0);
    }

    [Test]
    public async Task Validate_Should_Count_Components_Merged_From_Allowed_Remote_Documents()
    {
        await using var server = new LocalHttpServer(
            """
            {
              "openapi": "3.0.1",
              "info": { "title": "Remote", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Pet": { "type": "object", "properties": { "name": { "type": "string" } } }
                }
              }
            }
            """);
        var openApiPath = await SwaggerFileHelper.CreateSwaggerFile(
            $$"""
            openapi: 3.0.1
            info:
              title: Local
              version: 1.0.0
            paths:
              /pets:
                get:
                  responses:
                    '200':
                      description: OK
                      content:
                        application/json:
                          schema:
                            $ref: '{{server.Url}}#/components/schemas/Pet'
            """);

        var result = await OpenApiValidator.Validate(openApiPath, allowRemoteReferences: true);

        result.Diagnostics.Errors.Should().BeEmpty();
        result.Statistics.SchemaCount.Should().Be(2);
    }

    [Test]
    public async Task Validate_Should_Throw_InvalidOperationException_When_Remote_Server_Returns_An_Error()
    {
        var openApiSpec = EmbeddedResources.GetSwaggerPetstore(SampleOpenSpecifications.SwaggerPetstoreJsonV3);
        await using var server = new LocalHttpServer(openApiSpec, statusCode: 404, statusText: "Not Found");

        var act = () => OpenApiValidator.Validate(server.Url);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to download OpenAPI document*");
    }
}
