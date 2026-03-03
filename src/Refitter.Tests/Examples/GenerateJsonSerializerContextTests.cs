using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class GenerateJsonSerializerContextTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""JsonSerializerContext API"",
    ""version"": ""1.0.0""
  },
  ""servers"": [
    {
      ""url"": ""https://api.example.com""
    }
  ],
  ""paths"": {
    ""/api/users"": {
      ""get"": {
        ""operationId"": ""getUsers"",
        ""summary"": ""Get users"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""$ref"": ""#/components/schemas/User""
                  }
                }
              }
            }
          }
        }
      },
      ""post"": {
        ""operationId"": ""createUser"",
        ""summary"": ""Create user"",
        ""requestBody"": {
          ""required"": true,
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/User""
              }
            }
          }
        },
        ""responses"": {
          ""201"": {
            ""description"": ""Created"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/User""
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
      ""User"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""name"": {
            ""type"": ""string""
          },
          ""email"": {
            ""type"": ""string"",
            ""format"": ""email""
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
        string generatedCode = await GenerateCode(generateJsonSerializerContext: true);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode(generateJsonSerializerContext: true);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Contains_JsonSerializerContext()
    {
        string generatedCode = await GenerateCode(generateJsonSerializerContext: true);
        generatedCode.Should().Contain("JsonSerializerContext");
    }

    [Test]
    public async Task Generated_Code_Contains_JsonSerializable_Attributes()
    {
        string generatedCode = await GenerateCode(generateJsonSerializerContext: true);
        generatedCode.Should().Contain("JsonPropertyName");
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_JsonSerializerContext_When_Disabled()
    {
        string generatedCode = await GenerateCode(generateJsonSerializerContext: false);
        generatedCode.Should().NotContain("[JsonSerializable(typeof(");
    }

    private static async Task<string> GenerateCode(bool generateJsonSerializerContext = false)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateJsonSerializerContext = generateJsonSerializerContext
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
