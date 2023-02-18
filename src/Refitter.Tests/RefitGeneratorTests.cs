using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Resources;
using Xunit;

namespace Refitter.Tests;

public class RefitGeneratorTests
{
    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code(SampleOpenSpecifications version, string filename)
    {
        var swaggerFile = await CreateSwaggerFile(EmbeddedResources.GetSwaggerPetstore(version), filename);
        var sut = new RefitGenerator();
        var result = await sut.Generate(swaggerFile, "GeneratedCode");
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code(SampleOpenSpecifications version, string filename)
    {
        var swaggerFile = await CreateSwaggerFile(EmbeddedResources.GetSwaggerPetstore(version), filename);
        var sut = new RefitGenerator();
        var result = await sut.Generate(swaggerFile, "GeneratedCode");
        BuildHelper
            .BuildCSharp(result)
            .Should()
            .BeTrue();
    }

    private static async Task<string> CreateSwaggerFile(string contents, string filename)
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}