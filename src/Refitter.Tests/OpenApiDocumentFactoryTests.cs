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
    public async Task Create_From_Uri_Returns_NotNull(string url)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = url };
        (await OpenApiDocumentFactory.CreateAsync(settings.OpenApiPath))
            .Should()
            .NotBeNull();
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Create_From_File_Returns_NotNull(SampleOpenSpecifications version, string filename)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(EmbeddedResources.GetSwaggerPetstore(version), filename);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        (await OpenApiDocumentFactory.CreateAsync(settings.OpenApiPath))
            .Should()
            .NotBeNull();
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "petstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "petstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "petstore.yml")]
    public async Task Create_From_File_Detects_Format_Correctly(SampleOpenSpecifications version, string filename)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(EmbeddedResources.GetSwaggerPetstore(version), filename);
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        document.Should().NotBeNull();
        document.Info.Should().NotBeNull();
        document.Info.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_From_File_With_External_References_Returns_NotNull()
    {
        var spec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""object""
                }
              }
            }
          }
        }
      }
    }
  }
}";
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        document.Should().NotBeNull();
        document.Info.Title.Should().Be("Test API");
    }

    [Fact]
    public async Task Create_From_Yaml_File_Without_Extension_Returns_NotNull()
    {
        var spec = @"openapi: 3.0.0
info:
  title: Test API
  version: 1.0.0
paths:
  /test:
    get:
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: object";
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.yaml");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        document.Should().NotBeNull();
        document.Info.Title.Should().Be("Test API");
    }

    [Theory]
    [InlineData("https://petstore.swagger.io/v2/swagger.json")]
    [InlineData("http://petstore.swagger.io/v2/swagger.json")]
    public async Task Create_From_Http_Url_Returns_NotNull(string url)
    {
        var document = await OpenApiDocumentFactory.CreateAsync(url);
        document.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_Handles_Missing_Info_Fields()
    {
        var spec = @"{
  ""openapi"": ""3.0.0"",
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    }
  }
}";
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "minimal.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        document.Should().NotBeNull();
    }
}
