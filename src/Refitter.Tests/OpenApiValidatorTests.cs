using FluentAssertions;
using Refitter.Tests.Resources;
using Refitter.Validation;
using TUnit.Core;

namespace Refitter.Tests;

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
}
