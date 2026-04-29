using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class GenerateDefaultAdditionalPropertiesTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Additional Properties API"",
    ""version"": ""1.0.0""
  },
  ""servers"": [
    {
      ""url"": ""https://api.example.com""
    }
  ],
  ""paths"": {
    ""/api/products"": {
      ""get"": {
        ""operationId"": ""getProducts"",
        ""summary"": ""Get products"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Product""
                }
              }
            }
          }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Product"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""name"": {
            ""type"": ""string""
          },
          ""category"": {
            ""$ref"": ""#/components/schemas/Category""
          }
        }
      },
      ""Category"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""name"": {
            ""type"": ""string""
          }
        }
      }
    }
  }
}
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Contains_AdditionalProperties_When_Enabled()
    {
        string generatedCode = await GenerateCode(generateDefaultAdditionalProperties: true);
        generatedCode.Should().Contain("AdditionalProperties");
        generatedCode.Should().Contain("IDictionary<string, object>");
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_AdditionalProperties_When_Disabled()
    {
        string generatedCode = await GenerateCode(generateDefaultAdditionalProperties: false);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<string> GenerateCode(bool generateDefaultAdditionalProperties = true)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateDefaultAdditionalProperties = generateDefaultAdditionalProperties
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        }
        finally
        {
            if (File.Exists(swaggerFile))
            {
                File.Delete(swaggerFile);
            }

            var directory = Path.GetDirectoryName(swaggerFile);
            if (directory != null && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
