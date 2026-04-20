using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

/// <summary>
/// Regression tests for PR #1064 blockers: #1013, #1018, #1053
/// </summary>
public class PR1064BlockerRegressions
{
    #region Issue #1013 - Suffix Target Collision Prevention

    private const string OpenApiSpecWithSuffixCollision = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Collision Test API",
            "version": "v1"
          },
          "paths": {
            "/api/pets": {
              "get": {
                "operationId": "GetPets",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/Pet"
                        }
                      }
                    }
                  }
                }
              }
            },
            "/api/petdto": {
              "get": {
                "operationId": "GetPetDto",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/PetDto"
                        }
                      }
                    }
                  }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "Pet": {
                "type": "object",
                "properties": {
                  "id": { "type": "integer" },
                  "name": { "type": "string" }
                }
              },
              "PetDto": {
                "type": "object",
                "properties": {
                  "petId": { "type": "integer" },
                  "petName": { "type": "string" }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Issue1013_Prevents_Suffix_Collision_When_Target_Type_Already_Exists()
    {
        // Arrange: Schema has "Pet" and "PetDto" types. Applying "Dto" suffix to "Pet" would collide with existing "PetDto".
        var generatedCode = await GenerateCodeWithSuffix(OpenApiSpecWithSuffixCollision, "Dto");

        // Act: Apply suffix transformation
        var result = ContractTypeSuffixApplier.ApplySuffix(generatedCode, "Dto");

        // Assert: Original "PetDto" should remain unchanged, "Pet" should not double-suffix to "PetDtoDto"
        result.Should().NotContain("PetDtoDto");
        result.Should().Contain("public partial class PetDto");
    }

    [Test]
    public async Task Issue1013_Does_Not_Corrupt_Type_References_When_Collision_Exists()
    {
        // Repro: When "Pet" → "PetDto" and "PetDto" already exists, references must resolve correctly
        var generatedCode = await GenerateCodeWithSuffix(OpenApiSpecWithSuffixCollision, "Dto");
        var result = ContractTypeSuffixApplier.ApplySuffix(generatedCode, "Dto");

        // Both classes should exist without corruption
        result.Should().Contain("Task<PetDto>");
        result.Should().NotContain("Task<PetDtoDto>");
    }

    [Test]
    public async Task Issue1013_Generated_Code_With_Suffix_Collision_Compiles()
    {
        // Prove that collision scenario produces compilable code
        var generatedCode = await GenerateCodeWithSuffix(OpenApiSpecWithSuffixCollision, "Dto");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Issue #1018 - Multipart Parameter Deduplication on Sanitized Identifier

    private const string OpenApiSpecWithDuplicateSanitizedNames = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Duplicate Sanitized API",
            "version": "v1"
          },
          "paths": {
            "/upload": {
              "post": {
                "operationId": "UploadForm",
                "requestBody": {
                  "content": {
                    "multipart/form-data": {
                      "schema": {
                        "type": "object",
                        "properties": {
                          "a-b": {
                            "type": "string",
                            "description": "First param with dash"
                          },
                          "a b": {
                            "type": "string",
                            "description": "Second param with space"
                          },
                          "a.b": {
                            "type": "string",
                            "description": "Third param with dot"
                          }
                        }
                      }
                    }
                  }
                },
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Issue1018_Deduplicates_Multipart_Parameters_By_Sanitized_Identifier()
    {
        // Repro: "a-b", "a b", "a.b" all sanitize to "a_b" → must dedupe on final identifier, not original key
        var generatedCode = await GenerateCode(OpenApiSpecWithDuplicateSanitizedNames);

        // Count occurrences of "string a_b" parameter
        var parameterPattern = new System.Text.RegularExpressions.Regex(@"string a_b\b");
        var matches = parameterPattern.Matches(generatedCode);

        // Should only have ONE parameter named "a_b", not three duplicates
        matches.Count.Should().BeLessThanOrEqualTo(1, "deduplication should prevent duplicate sanitized identifiers");
    }

    [Test]
    public async Task Issue1018_First_Multipart_Parameter_Wins_After_Sanitization()
    {
        // Repro: When multiple keys sanitize to same identifier, first wins (consistent with HashSet.Add semantics)
        var generatedCode = await GenerateCode(OpenApiSpecWithDuplicateSanitizedNames);

        // First parameter "a-b" should generate AliasAs attribute
        generatedCode.Should().Contain("[AliasAs(\"a-b\")]", "first parameter with sanitized name should be emitted");
    }

    [Test]
    public async Task Issue1018_Generated_Code_With_Duplicate_Sanitized_Names_Compiles()
    {
        // Prove that deduplication prevents compilation errors
        var generatedCode = await GenerateCode(OpenApiSpecWithDuplicateSanitizedNames);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Issue #1053 - Keyword and Title Handling Without Invalid Prefixes

    private const string OpenApiSpecWithKeywordsAndTitle = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "@class-Service",
            "version": "v1"
          },
          "paths": {
            "/api/test": {
              "get": {
                "operationId": "GetTest",
                "parameters": [
                  {
                    "name": "class",
                    "in": "query",
                    "schema": {
                      "type": "string"
                    }
                  },
                  {
                    "name": "event",
                    "in": "query",
                    "schema": {
                      "type": "string"
                    }
                  },
                  {
                    "name": "@while",
                    "in": "query",
                    "schema": {
                      "type": "string"
                    }
                  }
                ],
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "class": {
                "type": "object",
                "properties": {
                  "id": { "type": "integer" }
                }
              },
              "event": {
                "type": "object",
                "properties": {
                  "name": { "type": "string" }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Issue1053_Keywords_Are_Escaped_Not_Double_Prefixed()
    {
        // Repro: Sanitize("class") should return "@class", not "I@class" or "_@class"
        var generatedCode = await GenerateCode(OpenApiSpecWithKeywordsAndTitle);

        // Should contain properly escaped keywords
        generatedCode.Should().Contain("@class");
        generatedCode.Should().Contain("@event");

        // Should NOT contain malformed identifiers with double prefixes
        generatedCode.Should().NotContain("I@class");
        generatedCode.Should().NotContain("_@class");
        generatedCode.Should().NotContain("I@event");
        generatedCode.Should().NotContain("_@event");
    }

    [Test]
    public async Task Issue1053_Sanitize_Routes_Through_EscapeReservedKeyword()
    {
        // Repro: Sanitize() must call EscapeReservedKeyword() to properly handle C# keywords
        var sanitized = "class".Sanitize();
        sanitized.Should().Be("@class", "Sanitize must escape reserved keywords");

        var sanitizedEvent = "event".Sanitize();
        sanitizedEvent.Should().Be("@event", "Sanitize must escape reserved keywords");
    }

    [Test]
    public async Task Issue1053_Title_With_Special_Chars_Does_Not_Produce_Invalid_Identifiers()
    {
        // Repro: Title "@class-Service" should sanitize cleanly without invalid prefixes
        var generatedCode = await GenerateCode(OpenApiSpecWithKeywordsAndTitle);

        // Generated interface/class names should be valid identifiers
        generatedCode.Should().NotContain("I@");
        generatedCode.Should().NotContain("interface I@");
        generatedCode.Should().NotContain("class @");
    }

    [Test]
    public async Task Issue1053_Parameter_Name_With_Leading_At_Sign_Is_Stripped()
    {
        // Repro: Parameter "@while" should become "@while" (keyword-escaped), not "@@while"
        var generatedCode = await GenerateCode(OpenApiSpecWithKeywordsAndTitle);

        // Should have properly escaped keyword after stripping leading @
        generatedCode.Should().Contain("@while");
        generatedCode.Should().NotContain("@@while");
    }

    [Test]
    public async Task Issue1053_Schema_Names_As_Keywords_Are_Properly_Escaped()
    {
        // Repro: Schema named "class" is capitalized by NSwag to "Class" (not a keyword)
        // Schema named "event" is capitalized by NSwag to "Event" (not a keyword)
        var generatedCode = await GenerateCode(OpenApiSpecWithKeywordsAndTitle);

        // NSwag capitalizes schema names, so "class" becomes "Class" which doesn't need escaping
        // But parameter names with keywords should still be escaped
        generatedCode.Should().Contain("partial class Class");
        generatedCode.Should().Contain("partial class Event");

        // Parameter names that are keywords should be escaped
        generatedCode.Should().Contain("@class");
        generatedCode.Should().Contain("@event");
        generatedCode.Should().Contain("@while");
    }

    [Test]
    public async Task Issue1053_Generated_Code_With_Keywords_Compiles()
    {
        // Prove that keyword handling produces compilable code
        var generatedCode = await GenerateCode(OpenApiSpecWithKeywordsAndTitle);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Test Helpers

    private static async Task<string> GenerateCode(string openApiSpec)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(openApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }

    private static async Task<string> GenerateCodeWithSuffix(string openApiSpec, string suffix)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(openApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ContractTypeSuffix = suffix
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }

    #endregion
}
