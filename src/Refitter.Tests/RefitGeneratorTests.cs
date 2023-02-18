using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Resources;
using Xunit;

namespace Refitter.Tests;

public class RefitGeneratorTests
{
    [Theory]
    [InlineData(SwaggerPetstoreVersions.JsonV2, "Swagger.json")]
    [InlineData(SwaggerPetstoreVersions.JsonV3, "Swagger.json")]
    [InlineData(SwaggerPetstoreVersions.YamlV2, "Swagger.yaml")]
    [InlineData(SwaggerPetstoreVersions.YamlV3, "Swagger.yaml")]
    public async Task Can_Generate_Code(SwaggerPetstoreVersions version, string filename)
    {
        var swaggerFile = await CreateSwaggerFile(EmbeddedResources.GetSwaggerPetstore(version), filename);
        var generator = new RefitGenerator();
        var result = await generator.Generate(swaggerFile, "GeneratedCode");
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        var swaggerFile = await CreateSwaggerFile(EmbeddedResources.SwaggerPetstoreJsonV3, "Swagger.json");
        var generator = new RefitGenerator();
        var result = await generator.Generate(swaggerFile, "GeneratedCode");
        
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