using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class OperationNameGeneratorTypesTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Pet Store API"",
    ""version"": ""1.0.0""
  },
  ""servers"": [
    {
      ""url"": ""https://petstore.swagger.io/v2""
    }
  ],
  ""paths"": {
    ""/pets"": {
      ""get"": {
        ""tags"": [""Pets""],
        ""operationId"": ""listPets"",
        ""summary"": ""List all pets"",
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
        ""operationId"": ""createPet"",
        ""summary"": ""Create a pet"",
        ""requestBody"": {
          ""required"": true,
          ""content"": {
            ""application/json"": {
              ""schema"": {
                ""$ref"": ""#/components/schemas/Pet""
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
    },
    ""/pets/{petId}"": {
      ""get"": {
        ""tags"": [""Pets""],
        ""operationId"": ""getPetById"",
        ""summary"": ""Get a pet by ID"",
        ""parameters"": [
          {
            ""name"": ""petId"",
            ""in"": ""path"",
            ""required"": true,
            ""schema"": {
              ""type"": ""integer"",
              ""format"": ""int32""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
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
    },
    ""/store/orders"": {
      ""get"": {
        ""tags"": [""Store""],
        ""operationId"": ""listOrders"",
        ""summary"": ""List all orders"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""$ref"": ""#/components/schemas/Order""
                  }
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
            ""format"": ""int32""
          },
          ""name"": {
            ""type"": ""string""
          },
          ""tag"": {
            ""type"": ""string""
          }
        }
      },
      ""Order"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""petId"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          },
          ""quantity"": {
            ""type"": ""integer"",
            ""format"": ""int32""
          }
        }
      }
    }
  }
}
";

    [Test]
    public async Task Can_Generate_Code_With_Default()
    {
        string generatedCode = await GenerateCode(OperationNameGeneratorTypes.Default);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Default()
    {
        string generatedCode = await GenerateCode(OperationNameGeneratorTypes.Default);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(OperationNameGeneratorTypes.Default)]
    [Arguments(OperationNameGeneratorTypes.MultipleClientsFromOperationId)]
    [Arguments(OperationNameGeneratorTypes.MultipleClientsFromPathSegments)]
    [Arguments(OperationNameGeneratorTypes.MultipleClientsFromFirstTagAndOperationId)]
    [Arguments(OperationNameGeneratorTypes.MultipleClientsFromFirstTagAndOperationName)]
    [Arguments(OperationNameGeneratorTypes.MultipleClientsFromFirstTagAndPathSegments)]
    [Arguments(OperationNameGeneratorTypes.SingleClientFromOperationId)]
    [Arguments(OperationNameGeneratorTypes.SingleClientFromPathSegments)]
    public async Task Can_Generate_Code_For_Each_OperationNameGenerator_Type(OperationNameGeneratorTypes generatorType)
    {
        string generatedCode = await GenerateCode(generatorType);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Arguments(OperationNameGeneratorTypes.Default)]
    [Arguments(OperationNameGeneratorTypes.MultipleClientsFromOperationId)]
    [Arguments(OperationNameGeneratorTypes.SingleClientFromOperationId)]
    public async Task Can_Build_Generated_Code_For_OperationId_Based_Generators(OperationNameGeneratorTypes generatorType)
    {
        string generatedCode = await GenerateCode(generatorType);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Contains_Multiple_Interfaces_For_MultipleClientsFromOperationId()
    {
        string generatedCode = await GenerateCode(OperationNameGeneratorTypes.MultipleClientsFromOperationId);
        generatedCode.Should().Contain("interface IPetStoreAPI");
        generatedCode.Should().Contain("ListPets");
        generatedCode.Should().Contain("ListOrders");
    }

    [Test]
    public async Task Generated_Code_Contains_Single_Interface_For_SingleClientFromOperationId()
    {
        string generatedCode = await GenerateCode(OperationNameGeneratorTypes.SingleClientFromOperationId);
        generatedCode.Should().Contain("interface IPetStoreAPI");
        generatedCode.Should().Contain("ListPets");
        generatedCode.Should().Contain("CreatePet");
        generatedCode.Should().Contain("GetPetById");
    }

    [Test]
    public async Task Generated_Code_Contains_Multiple_Interfaces_With_Tag_Based_Naming_For_FirstTag()
    {
        string generatedCode = await GenerateCode(OperationNameGeneratorTypes.MultipleClientsFromFirstTagAndOperationId);
        generatedCode.Should().Contain("interface IPetStoreAPI");
        generatedCode.Should().Contain("ListPets");
        generatedCode.Should().Contain("ListOrders");
    }

    private static async Task<string> GenerateCode(OperationNameGeneratorTypes generatorType = OperationNameGeneratorTypes.Default)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                OperationNameGenerator = generatorType
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
