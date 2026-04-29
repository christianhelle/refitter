using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Regression;

/// <summary>
/// Regression tests for Issue #1014: Internal enum JsonConverter injection
/// Validates that both internal and public enums receive JsonConverter attributes
/// </summary>
public class EnumConverterInjectionTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Enum Test API"",
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
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Pet"": {
        ""type"": ""object"",
        ""properties"": {
          ""status"": {
            ""$ref"": ""#/components/schemas/PetStatus""
          },
          ""category"": {
            ""$ref"": ""#/components/schemas/PetCategory""
          },
          ""visibility"": {
            ""$ref"": ""#/components/schemas/PetVisibility""
          }
        }
      },
      ""PetStatus"": {
        ""type"": ""string"",
        ""enum"": [""Available"", ""Pending"", ""Sold""]
      },
      ""PetCategory"": {
        ""type"": ""string"",
        ""enum"": [""Dog"", ""Cat"", ""Bird""]
      },
      ""PetVisibility"": {
        ""type"": ""string"",
        ""enum"": [""Public"", ""Private"", ""Hidden""]
      }
    }
  }
}
";

    [Test]
    public async Task Internal_Enums_Should_Have_JsonConverter()
    {
        string generatedCode = await GenerateCodeWithInternalTypes();

        // All enums should have JsonConverter attribute, regardless of visibility
        generatedCode.Should().Contain("[JsonConverter(typeof(JsonStringEnumConverter))]");

        // Find internal enum declarations
        generatedCode.Should().Contain("internal enum PetStatus");
        generatedCode.Should().Contain("internal enum PetCategory");
        generatedCode.Should().Contain("internal enum PetVisibility");

        // Each internal enum should have its own JsonConverter
        var converterCount = System.Text.RegularExpressions.Regex.Matches(
            generatedCode,
            @"\[JsonConverter\(typeof\(JsonStringEnumConverter\)\)\][\s\r\n]+internal enum"
        ).Count;

        converterCount.Should().BeGreaterThanOrEqualTo(3,
            "all three internal enums should have JsonConverter attributes");
    }

    [Test]
    public async Task Public_Enums_Should_Have_JsonConverter()
    {
        string generatedCode = await GenerateCodeWithPublicTypes();

        // Public enums should also have JsonConverter
        generatedCode.Should().Contain("public enum PetStatus");
        generatedCode.Should().Contain("[JsonConverter(typeof(JsonStringEnumConverter))]");

        var converterCount = System.Text.RegularExpressions.Regex.Matches(
            generatedCode,
            @"\[JsonConverter\(typeof\(JsonStringEnumConverter\)\)\][\s\r\n]+public enum"
        ).Count;

        converterCount.Should().BeGreaterThanOrEqualTo(3,
            "all three public enums should have JsonConverter attributes");
    }

    [Test]
    public async Task Multiple_Internal_Enums_Should_All_Have_Converter()
    {
        string generatedCode = await GenerateCodeWithInternalTypes();

        // Ensure all three enums get the attribute, not just the first one
        var internalEnumMatches = System.Text.RegularExpressions.Regex.Matches(
            generatedCode,
            @"\[JsonConverter[^\]]+\][\s\r\n]+internal enum (PetStatus|PetCategory|PetVisibility)"
        );

        internalEnumMatches.Count.Should().Be(3,
            "regex should match all internal enums, not just some");

        // Verify we found all three enum names
        var enumNames = internalEnumMatches
            .Select(m => m.Groups[1].Value)
            .ToHashSet();

        enumNames.Should().Contain("PetStatus");
        enumNames.Should().Contain("PetCategory");
        enumNames.Should().Contain("PetVisibility");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Internal_Enums()
    {
        string generatedCode = await GenerateCodeWithInternalTypes();

        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue("generated code with internal enums should compile");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Public_Enums()
    {
        string generatedCode = await GenerateCodeWithPublicTypes();

        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue("generated code with public enums should compile");
    }

    private static async Task<string> GenerateCodeWithInternalTypes()
    {
        return await GenerateCode(TypeAccessibility.Internal);
    }

    private static async Task<string> GenerateCodeWithPublicTypes()
    {
        return await GenerateCode(TypeAccessibility.Public);
    }

    private static async Task<string> GenerateCode(TypeAccessibility typeAccessibility)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            TypeAccessibility = typeAccessibility
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
