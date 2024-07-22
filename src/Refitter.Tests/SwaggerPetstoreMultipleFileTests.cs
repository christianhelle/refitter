using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.Resources;
using Xunit;

namespace Refitter.Tests;

public class SwaggerPetstoreMultipleFileTests
{
    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code(SampleOpenSpecifications version, string filename)
    {
        await GenerateCode(
            version,
            filename,
            assert: c =>
            {
                c.Files.Should().NotBeNullOrEmpty();
                foreach ((_, string content) in c.Files)
                {
                    content.Should().NotBeNullOrWhiteSpace();
                }
            });
    }

    private static async Task GenerateCode(
        SampleOpenSpecifications version,
        string filename,
        RefitGeneratorSettings? settings = null,
        Action<GeneratorOutput>? assert = null)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(EmbeddedResources.GetSwaggerPetstore(version), filename);
        if (settings is null)
        {
            settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile, GenerateMultipleFiles = true };
        }
        else
        {
            settings.OpenApiPath = swaggerFile;
            settings.GenerateMultipleFiles = true;
        }

        var sut = await RefitGenerator.CreateAsync(settings);
        var output = sut.GenerateMultipleFiles();
        assert?.Invoke(output);
    }
}
