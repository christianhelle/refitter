using FluentAssertions;

using Refitter.Core;
using Refitter.Tests.Resources;

using Xunit;

namespace Refitter.Tests;

public class OpenApiDocumentFactoryTests
{
    [Theory]
    [InlineData("https://developers.intellihr.io/docs/v1/swagger.json")] // GZIP encoded
    [InlineData("http://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore.json")]
    public async Task Can_Build_Generated_Code_From_Url(string url)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = url };
        (await OpenApiDocumentFactory.CreateAsync(settings))
            .Should()
            .NotBeNull();
    }
    
    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Multiple_Interfaces(SampleOpenSpecifications version, string filename)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(EmbeddedResources.GetSwaggerPetstore(version), filename);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        (await OpenApiDocumentFactory.CreateAsync(settings))
            .Should()
            .NotBeNull();
    }
}