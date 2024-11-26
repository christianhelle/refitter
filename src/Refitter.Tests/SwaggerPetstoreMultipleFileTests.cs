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
            assert: generatorOutput =>
            {
                generatorOutput.Files.Should().NotBeNullOrEmpty();
                generatorOutput.Files.Should().HaveCountGreaterOrEqualTo(2);
                foreach ((_, string content) in generatorOutput.Files)
                {
                    content.Should().NotBeNullOrWhiteSpace();
                }
            });
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_DependencyInjection(SampleOpenSpecifications version, string filename)
    {
        await GenerateCode(
            version,
            filename,
            new RefitGeneratorSettings { DependencyInjectionSettings = new DependencyInjectionSettings() },
            generatorOutput =>
            {
                generatorOutput.Files.Should().NotBeNullOrEmpty();
                generatorOutput.Files.Should().HaveCountGreaterOrEqualTo(3);
                foreach ((_, string content) in generatorOutput.Files)
                {
                    content.Should().NotBeNullOrWhiteSpace();
                }
            });
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Apizr(SampleOpenSpecifications version, string filename)
    {
        await GenerateCode(
            version,
            filename,
            new RefitGeneratorSettings { ApizrSettings = new ApizrSettings { WithRegistrationHelper = true } },
            generatorOutput =>
            {
                generatorOutput.Files.Should().NotBeNullOrEmpty();
                generatorOutput.Files.Should().HaveCountGreaterOrEqualTo(3);
                foreach ((_, string content) in generatorOutput.Files)
                {
                    content.Should().NotBeNullOrWhiteSpace();
                }
            });
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
#if !DEBUG
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
#endif
    public async Task Can_Build_Generate_Code(SampleOpenSpecifications version, string filename)
    {
        await GenerateCode(
            version,
            filename,
            assert: generatorOutput =>
            {
                BuildHelper
                    .BuildCSharp(generatorOutput.Files.Select(code => code.Content).ToArray())
                    .Should()
                    .BeTrue();
            });
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
#if !DEBUG
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
#endif
    public async Task Can_Build_Generate_Code_With_IDisposable(SampleOpenSpecifications version, string filename)
    {
        await GenerateCode(
            version,
            filename,
            new RefitGeneratorSettings { GenerateDisposableClients = true },
            assert: generatorOutput =>
            {
                BuildHelper
                    .BuildCSharp(generatorOutput.Files.Select(code => code.Content).ToArray())
                    .Should()
                    .BeTrue();
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
            settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateMultipleFiles = true,
                Naming = new NamingSettings
                {
                    UseOpenApiTitle = false,
                    InterfaceName = "PetStore"
                }
            };
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
