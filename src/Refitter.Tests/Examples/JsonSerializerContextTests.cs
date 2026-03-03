using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class JsonSerializerContextTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/api/pets"": {
      ""get"": {
        ""tags"": [""Pets""],
        ""operationId"": ""GetPets"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""$ref"": ""#/components/schemas/Pet""
                  }
                }
              }
            }
          }
        }
      },
      ""post"": {
        ""tags"": [""Pets""],
        ""operationId"": ""CreatePet"",
        ""requestBody"": {
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/PetRequest""
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
                  ""$ref"": ""#/components/schemas/Pet""
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
      ""Pet"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""integer"",
            ""format"": ""int64""
          },
          ""name"": {
            ""type"": ""string""
          },
          ""status"": {
            ""$ref"": ""#/components/schemas/PetStatus""
          }
        }
      },
      ""PetRequest"": {
        ""type"": ""object"",
        ""properties"": {
          ""name"": {
            ""type"": ""string""
          },
          ""status"": {
            ""$ref"": ""#/components/schemas/PetStatus""
          }
        }
      },
      ""PetStatus"": {
        ""type"": ""string"",
        ""enum"": [
          ""Available"",
          ""Pending"",
          ""Sold""
        ]
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
    public async Task Generated_Code_Contains_JsonSerializerContext()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("JsonSerializerContext");
        generatedCode.Should().Contain("TestApiSerializerContext");
    }

    [Test]
    public async Task Generated_Code_Contains_JsonSerializable_Attributes()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[JsonSerializable(typeof(Pet))]");
        generatedCode.Should().Contain("[JsonSerializable(typeof(PetRequest))]");
        generatedCode.Should().Contain("[JsonSerializable(typeof(PetStatus))]");
    }

    [Test]
    public async Task Generated_Code_Contains_All_DTO_Types()
    {
        string generatedCode = await GenerateCode();

        // Verify all types are registered
        generatedCode.Should().MatchRegex(@"\[JsonSerializable\(typeof\(Pet\)\)\]");
        generatedCode.Should().MatchRegex(@"\[JsonSerializable\(typeof\(PetRequest\)\)\]");
        generatedCode.Should().MatchRegex(@"\[JsonSerializable\(typeof\(PetStatus\)\)\]");
    }

    [Test]
    public async Task JsonSerializerContext_Class_Is_Partial()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("internal partial class TestApiSerializerContext");
    }

    [Test]
    public async Task JsonSerializerContext_Inherits_From_JsonSerializerContext()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain(": JsonSerializerContext");
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
    public async Task Does_Not_Generate_JsonSerializerContext_When_Flag_Is_False()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            GenerateJsonSerializerContext = false
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().NotContain("JsonSerializerContext");
        generatedCode.Should().NotContain("[JsonSerializable");
    }

    [Test]
    public async Task Does_Not_Generate_JsonSerializerContext_When_Contracts_Not_Generated()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            GenerateJsonSerializerContext = true,
            GenerateContracts = false
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().NotContain("JsonSerializerContext");
        generatedCode.Should().NotContain("[JsonSerializable");
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            GenerateJsonSerializerContext = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
