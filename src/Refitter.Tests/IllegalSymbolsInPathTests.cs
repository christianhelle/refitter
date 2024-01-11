using FluentAssertions;

using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.Resources;

using Xunit;

namespace Refitter.Tests;

public class IllegalSymbolsInPathTests
{
    [Fact]
    public async Task Compiler_Could_Build_Generated_Code_From_Swagger_With_Illegal_Symbols()
    {
        var generateCode = await GenerateCode("SwaggerIllegalPaths");
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(
        string filename,
        RefitGeneratorSettings? settings = null)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(EmbeddedResources.SwaggerIllegalPathsJsonV3, filename);
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