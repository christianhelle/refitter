using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class ContractTypeSuffixTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Pet Store API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/api/pets"": {
      ""get"": {
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
    },
    ""/api/owners/{ownerId}"": {
      ""get"": {
        ""operationId"": ""GetOwner"",
        ""parameters"": [
          {
            ""name"": ""ownerId"",
            ""in"": ""path"",
            ""required"": true,
            ""schema"": {
              ""type"": ""integer"",
              ""format"": ""int64""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/Owner""
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
          },
          ""owner"": {
            ""$ref"": ""#/components/schemas/Owner""
          }
        }
      },
      ""PetRequest"": {
        ""type"": ""object"",
        ""properties"": {
          ""name"": {
            ""type"": ""string""
          },
          ""ownerId"": {
            ""type"": ""integer"",
            ""format"": ""int64""
          }
        }
      },
      ""Owner"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""integer"",
            ""format"": ""int64""
          },
          ""name"": {
            ""type"": ""string""
          }
        }
      },
      ""PetStatus"": {
        ""type"": ""string"",
        ""enum"": [""Available"", ""Pending"", ""Sold""]
      }
    }
  }
}
";

    [Test]
    public async Task Can_Generate_Code_With_Suffix()
    {
        string generatedCode = await GenerateCode("Dto");
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generated_Code_Contains_Dto_Suffix_For_Classes()
    {
        string generatedCode = await GenerateCode("Dto");
        generatedCode.Should().Contain("public partial class PetDto");
        generatedCode.Should().Contain("public partial class PetRequestDto");
        generatedCode.Should().Contain("public partial class OwnerDto");
    }

    [Test]
    public async Task Generated_Code_Contains_Dto_Suffix_For_Enums()
    {
        string generatedCode = await GenerateCode("Dto");
        generatedCode.Should().Contain("public enum PetStatusDto");
    }

    [Test]
    public async Task Generated_Code_Uses_Suffixed_Types_In_Properties()
    {
        string generatedCode = await GenerateCode("Dto");
        generatedCode.Should().Contain("public PetStatusDto");
        generatedCode.Should().Contain("public OwnerDto");
    }

    [Test]
    public async Task Generated_Code_Uses_Suffixed_Types_In_Method_Signatures()
    {
        string generatedCode = await GenerateCode("Dto");
        generatedCode.Should().Contain("Task<ICollection<PetDto>>");
        generatedCode.Should().Contain("Task<PetDto>");
        generatedCode.Should().Contain("Task<OwnerDto>");
        generatedCode.Should().Contain("[Body] PetRequestDto");
    }

    [Test]
    public async Task Can_Generate_Code_With_Contract_Suffix()
    {
        string generatedCode = await GenerateCode("Contract");
        generatedCode.Should().Contain("public partial class PetContract");
        generatedCode.Should().Contain("public partial class OwnerContract");
        generatedCode.Should().Contain("public enum PetStatusContract");
    }

    [Test]
    public async Task Can_Generate_Code_With_Model_Suffix()
    {
        string generatedCode = await GenerateCode("Model");
        generatedCode.Should().Contain("public partial class PetModel");
        generatedCode.Should().Contain("public partial class OwnerModel");
        generatedCode.Should().Contain("public enum PetStatusModel");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Suffix()
    {
        string generatedCode = await GenerateCode("Dto");
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Does_Not_Apply_Suffix_When_Not_Specified()
    {
        string generatedCode = await GenerateCode(null);
        generatedCode.Should().Contain("public partial class Pet");
        generatedCode.Should().NotContain("public partial class PetDto");
        generatedCode.Should().Contain("public partial class Owner");
        generatedCode.Should().NotContain("public partial class OwnerDto");
    }

    [Test]
    public async Task Does_Not_Apply_Suffix_When_Empty()
    {
        string generatedCode = await GenerateCode(string.Empty);
        generatedCode.Should().Contain("public partial class Pet");
        generatedCode.Should().NotContain("public partial class PetDto");
    }

    private static async Task<string> GenerateCode(string? contractTypeSuffix)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ContractTypeSuffix = contractTypeSuffix
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
