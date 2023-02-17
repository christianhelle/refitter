using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Resources;
using Xunit;

namespace Refitter.Tests;

public class RefitGeneratorTests
{
    [Fact]
    public async Task Can_Generate_Code()
    {
        var swaggerFile = await CreateSwaggerFile();
        var generator = new RefitGenerator();
        var result = await generator.Generate(swaggerFile);
        result.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        var swaggerFile = await CreateSwaggerFile();
        var generator = new RefitGenerator();
        var result = await generator.Generate(swaggerFile);
        
        BuildHelper
            .BuildCSharp(result)
            .Should()
            .BeTrue();
    }

    private static async Task<string> CreateSwaggerFile()
    {
        var contents = EmbeddedResources.SwaggerPetstoreJsonV3;
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, "Swagger.json");
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}