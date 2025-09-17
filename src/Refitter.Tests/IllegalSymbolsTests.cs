using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.Resources;
using Xunit;

namespace Refitter.Tests;

public class IllegalSymbolsTests
{
    [Fact]
    public async Task Illegal_Symbols_In_Paths__Should_Build_Successfully()
    {
        var generateCode = await GenerateCode(EmbeddedResources.SwaggerIllegalPathsJsonV3);
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task Illegal_Symbols_In_Title__Should_Build_Successfully()
    {
        var generateCode = await GenerateCode(EmbeddedResources.SwaggerIllegalSymbolsInTitleJsonV3);
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(
        string content,
        RefitGeneratorSettings? settings = null)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(content, Guid.NewGuid().ToString());
        if (settings is null)
        {
            settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        }
        else
        {
            settings.OpenApiPath = swaggerFile;
        }

        var sut = await RefitGenerator.CreateAsync(settings);

        return sut.Generate();
    }
}
