using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.RegressionTests;

/// <summary>
/// Regression tests for Issue #1013: ContractTypeSuffix regex corruption
/// Validates that suffix replacement doesn't corrupt XML comments, string literals, or partial name matches
/// </summary>
public class Issue1013_ContractSuffixCorruptionTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Pet Store API"",
    ""version"": ""v1"",
    ""description"": ""API for managing Pet and PetResponse resources""
  },
  ""paths"": {
    ""/api/pets"": {
      ""get"": {
        ""operationId"": ""GetPets"",
        ""summary"": ""Gets a list of Pet objects"",
        ""description"": ""Returns all Pet records from the database"",
        ""responses"": {
          ""200"": {
            ""description"": ""Returns a Pet array"",
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
      }
    },
    ""/api/pets/{id}/response"": {
      ""get"": {
        ""operationId"": ""GetPetResponse"",
        ""summary"": ""Gets a PetResponse object"",
        ""description"": ""Returns a PetResponse for the given Pet ID"",
        ""parameters"": [
          {
            ""name"": ""id"",
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
            ""description"": ""Returns a PetResponse object"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""#/components/schemas/PetResponse""
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
        ""description"": ""Represents a Pet in the system"",
        ""properties"": {
          ""id"": {
            ""type"": ""integer"",
            ""format"": ""int64"",
            ""description"": ""The Pet identifier""
          },
          ""name"": {
            ""type"": ""string"",
            ""description"": ""The Pet name""
          }
        }
      },
      ""PetResponse"": {
        ""type"": ""object"",
        ""description"": ""Response object containing Pet data"",
        ""properties"": {
          ""pet"": {
            ""$ref"": ""#/components/schemas/Pet"",
            ""description"": ""The Pet object in this response""
          },
          ""message"": {
            ""type"": ""string"",
            ""description"": ""Message about the Pet operation""
          }
        }
      }
    }
  }
}
";

    [Test]
    public async Task Should_Not_Corrupt_Comments_Or_Descriptions()
    {
        string generatedCode = await GenerateCode("DTO");

        // XML comments and descriptions should NOT have "Pet" replaced with "PetDTO"
        // Correct: "Gets a list of Pet objects" or "Gets a list of PetDTO objects"
        // Wrong: "Gets a list of PETDTO objects" or other malformed text

        // Ensure we don't corrupt natural language in comments
        generatedCode.Should().NotContain("PETDTO");
        generatedCode.Should().NotContain("PetDTOResponse"); // Should be PetResponseDTO

        // Type declarations should have suffix
        generatedCode.Should().Contain("class PetDTO");
        generatedCode.Should().Contain("class PetResponseDTO");
    }

    [Test]
    public async Task Should_Not_Double_Suffix()
    {
        string generatedCode = await GenerateCode("DTO");

        // If suffix is applied twice, we'd see PetDTODTO
        generatedCode.Should().NotContain("DTODTO");
        generatedCode.Should().NotContain("PetDTODTO");
        generatedCode.Should().NotContain("PetResponseDTODTO");
    }

    [Test]
    public async Task Partial_Name_Should_Not_Corrupt_Longer_Names()
    {
        string generatedCode = await GenerateCode("DTO");

        // "Pet" should not corrupt "PetResponse" into "PetDTOResponse"
        // Both should get suffix: PetDTO and PetResponseDTO
        generatedCode.Should().Contain("class PetDTO");
        generatedCode.Should().Contain("class PetResponseDTO");
        generatedCode.Should().NotContain("PetDTOResponse");
        generatedCode.Should().NotContain("class PetResponse "); // without suffix
    }

    [Test]
    public async Task Should_Apply_Suffix_To_Type_Declarations_Only()
    {
        string generatedCode = await GenerateCode("Contract");

        // Type declarations should have suffix
        generatedCode.Should().Contain("class PetContract");
        generatedCode.Should().Contain("class PetResponseContract");

        // But we should still be able to talk about the domain concept "Pet" in comments
        // This test validates that word-boundary regex doesn't over-match
        generatedCode.Should().NotContain("class Pet "); // ensure it's suffixed
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Suffix()
    {
        string generatedCode = await GenerateCode("DTO");

        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue("generated code with ContractTypeSuffix should compile without errors");
    }

    private static async Task<string> GenerateCode(string suffix)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ContractTypeSuffix = suffix
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
