using FluentAssertions;

using Refitter.Core;
using Refitter.Tests.Build;

using Xunit;

namespace Refitter.Tests.Examples;

public class MultiPartFormDataAliasAsTests
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
        string generateCode = await GenerateCode();
        generateCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generateCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    [Fact]
    public async Task Generated_Code_Contains_MultiPart_Attribute()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("[Multipart]");
    }

    [Fact]
    public async Task Generated_Code_Contains_AliasAs_Attribute()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("[AliasAs(\"AnimalClassFile\")]");
    }

    [Fact]
    public async Task Generated_Code_Contains_StreamPart_Parameter()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("StreamPart");
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generateCode = sut.Generate();
        return generateCode;
    }

    private static async Task<string> CreateSwaggerFile(string contents)
    {
        var filename = $"{Guid.NewGuid()}.json";
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}