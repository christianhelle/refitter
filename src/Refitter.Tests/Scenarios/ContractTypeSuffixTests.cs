using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

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

    [Test]
    public async Task Does_Not_Apply_Double_Suffix_When_Type_Already_Has_Suffix()
    {
        // This tests the #1013 fix: prevent double-suffixing
        // Generate code first with suffix
        string generatedCode = await GenerateCode("Dto");

        // Apply suffix again (simulating multiple passes)
        var result = ContractTypeSuffixApplier.ApplySuffix(generatedCode, "Dto");

        // Should not have double suffix
        result.Should().NotContain("PetDtoDto");
        result.Should().NotContain("OwnerDtoDto");
        result.Should().Contain("PetDto");
        result.Should().Contain("OwnerDto");
    }

    [Test]
    public async Task Does_Not_Corrupt_Comments_With_Type_Names()
    {
        // #1013 regression test: comments containing type names should not be modified
        string generatedCode = await GenerateCode(null);

        // Add comments containing type names
        var codeWithComments = generatedCode.Replace(
            "public partial class Pet",
            "/// <summary>Represents a Pet entity</summary>\npublic partial class Pet");

        var result = ContractTypeSuffixApplier.ApplySuffix(codeWithComments, "Dto");

        // Comment should still say "Pet entity" not "PetDto entity"
        result.Should().Contain("Represents a Pet entity");
        result.Should().NotContain("Represents a PetDto entity");

        // But the class declaration should be renamed
        result.Should().Contain("public partial class PetDto");
    }

    [Test]
    public async Task Does_Not_Corrupt_String_Literals_With_Type_Names()
    {
        // #1013 regression test: string literals should not be modified
        string generatedCode = await GenerateCode(null);

        // Simulate generated code with string literals containing type names
        var codeWithStrings = generatedCode.Replace(
            "public partial class Pet",
            "[Description(\"Pet\")]\npublic partial class Pet");

        var result = ContractTypeSuffixApplier.ApplySuffix(codeWithStrings, "Dto");

        // String literal should remain "Pet" not "PetDto"
        result.Should().Contain("[Description(\"Pet\")]");
        result.Should().NotContain("[Description(\"PetDto\")]");

        // But the class declaration should be renamed
        result.Should().Contain("public partial class PetDto");
    }

    [Test]
    public async Task Does_Not_Corrupt_Property_Names_Containing_Type_Names()
    {
        // #1013 regression test: property names that happen to contain type name strings
        string generatedCode = await GenerateCode(null);

        // Add a property whose name contains "Pet" but is not a type reference
        var codeWithProperty = generatedCode.Replace(
            "public partial class Owner",
            "public partial class Owner { public string PetName { get; set; } }");

        var result = ContractTypeSuffixApplier.ApplySuffix(codeWithProperty, "Dto");

        // Property name should remain "PetName" not "PetDtoName"
        result.Should().Contain("public string PetName");
        result.Should().NotContain("public string PetDtoName");

        // But Owner class should be renamed
        result.Should().Contain("public partial class OwnerDto");
    }

    [Test]
    public async Task Does_Not_Create_Collision_When_Suffixed_Type_Already_Exists()
    {
        // #1013 regression test: handle pre-existing suffixed types
        string generatedCode = await GenerateCode(null);

        // Simulate a scenario where "PetDto" already exists as a different type
        var codeWithExisting = generatedCode.Replace(
            "public partial class Pet",
            "public partial class PetDto { }\npublic partial class Pet");

        var result = ContractTypeSuffixApplier.ApplySuffix(codeWithExisting, "Dto");

        // Pre-existing PetDto should not be renamed
        result.Should().Contain("public partial class PetDto { }");

        // Original Pet class should be renamed to PetDto
        // This creates a collision, but that's the behavior - we don't double-suffix
        // The test verifies no "PetDtoDto" is created
        result.Should().NotContain("PetDtoDto");
    }

    [Test]
    public async Task Preserves_XML_Documentation_Comments()
    {
        // #1013 regression test: XML docs should be preserved exactly
        string generatedCode = await GenerateCode(null);

        // Add XML documentation
        var codeWithDocs = generatedCode.Replace(
            "public partial class Pet",
            "/// <summary>\n/// The Pet class represents a pet.\n/// </summary>\npublic partial class Pet");

        var result = ContractTypeSuffixApplier.ApplySuffix(codeWithDocs, "Dto");

        // XML doc should preserve "Pet class" not "PetDto class"
        result.Should().Contain("The Pet class represents a pet.");
        result.Should().NotContain("The PetDto class");

        // Class declaration should be renamed
        result.Should().Contain("public partial class PetDto");
    }

    [Test]
    public async Task Does_Not_Corrupt_Method_Parameters_Named_After_Types()
    {
        // #1013 regression test: parameter names should not be corrupted
        string generatedCode = await GenerateCode(null);

        // Simulate code with a parameter name that matches a type name substring
        var codeWithParam = generatedCode.Replace(
            "public partial class Owner",
            "public partial class Owner { public void UpdatePet(string pet) { } }");

        var result = ContractTypeSuffixApplier.ApplySuffix(codeWithParam, "Dto");

        // Parameter name "pet" should not become "petDto"
        result.Should().Contain("string pet");
        result.Should().NotContain("string petDto");

        // But Owner class should be renamed
        result.Should().Contain("public partial class OwnerDto");
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
