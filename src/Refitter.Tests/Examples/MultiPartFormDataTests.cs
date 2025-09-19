using FluentAssertions;
using Refitter.Tests.TestUtilities;

using Refitter.Core;
using Refitter.Tests.Build;

using Xunit;

namespace Refitter.Tests.Examples;

public class MultiPartFormDataTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.1"",  
  ""paths"": {
    ""/animals"": {
      ""post"": {
        ""tags"": [
          ""Animals""
        ],
        ""requestBody"": {
          ""content"": {
            ""multipart/form-data"": {
              ""schema"": {
                ""type"": ""object"",
                ""properties"": {
                  ""Name"": {
                    ""type"": ""string""
                  },
                  ""AnimalClassFile"": {
                    ""type"": ""string"",
                    ""format"": ""binary""
                  },
                  ""AnimalCrowdFile"": {
                    ""type"": ""string"",
                    ""format"": ""binary""
                  }
                }
              },
              ""encoding"": {
                ""Name"": {
                  ""style"": ""form""
                },
                ""AnimalClassFile"": {
                  ""style"": ""form""
                },
                ""AnimalCrowdFile"": {
                  ""style"": ""form""
                }
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
                  ""$ref"": ""#/components/schemas/AnimalResponse""
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
      ""AnimalResponse"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""string"",
            ""format"": ""uuid"",
            ""nullable"": true
          },
          ""Name"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""AnimalClassFileUri"": {
            ""type"": ""string"",
            ""nullable"": true
          },
          ""AnimalCrowdFileUri"": {
            ""type"": ""string"",
            ""nullable"": true
          }
        },
        ""additionalProperties"": false
      }
    }
  }
}
";

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task Generated_Code_Contains_MultiPart_Attribute()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Multipart]");
        generatedCode.Should().NotContain("Content-Type: multipart/form-data");
    }

    [Fact]
    public async Task Generated_Code_Contains_Correct_Parameters()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("string name, StreamPart animalClassFile, StreamPart animalCrowdFile");
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

}
