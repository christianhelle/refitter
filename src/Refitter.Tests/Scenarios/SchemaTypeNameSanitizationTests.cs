using System.Text.RegularExpressions;
using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

public class SchemaTypeNameSanitizationTests
{
    private const string SanitizedDtoName = "LookUpErnResponse";

    private const string OpenApiSpec = """
        {
          "swagger": "2.0",
          "info": {
            "title": "Issue 1083 Regression API",
            "version": "1.0.0"
          },
          "paths": {
            "/lookup-ern": {
              "get": {
                "operationId": "LookUpErn",
                "responses": {
                  "200": {
                    "description": "Success",
                    "schema": {
                      "$ref": "#/definitions/LookUpErnResponse."
                    }
                  }
                }
              }
            }
          },
          "definitions": {
            "LookUpErnResponse.": {
              "type": "object",
              "properties": {
                "ern": {
                  "type": "string"
                }
              }
            }
          }
        }
        """;

    private const string OpenApiSpecWithCollision = """
        {
          "swagger": "2.0",
          "info": {
            "title": "Issue 1083 Collision API",
            "version": "1.0.0"
          },
          "paths": {
            "/lookup-ern": {
              "get": {
                "operationId": "LookUpErn",
                "responses": {
                  "200": {
                    "description": "Success",
                    "schema": {
                      "$ref": "#/definitions/LookUpErnResponse."
                    }
                  }
                }
              }
            },
            "/lookup-ern-direct": {
              "get": {
                "operationId": "LookUpErnDirect",
                "responses": {
                  "200": {
                    "description": "Success",
                    "schema": {
                      "$ref": "#/definitions/LookUpErnResponse"
                    }
                  }
                }
              }
            }
          },
          "definitions": {
            "LookUpErnResponse.": {
              "type": "object",
              "properties": {
                "ern": {
                  "type": "string"
                }
              }
            },
            "LookUpErnResponse": {
              "type": "object",
              "properties": {
                "id": {
                  "type": "integer"
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Issue1083_Sanitizes_Trailing_Dot_Schema_Name_In_Contract_And_Method_Signature()
    {
        string generatedCode = await GenerateCode();

        Regex.IsMatch(generatedCode, @"partial class\s*(?:\r?\n|\{)").Should().BeFalse();
        generatedCode.Should().NotContain("Task<>");
        generatedCode.Should().NotContain("LookUpErnResponse.");
        generatedCode.Should().Contain($"partial class {SanitizedDtoName}");
        generatedCode.Should().Contain($"Task<{SanitizedDtoName}> LookUpErn(");
    }

    [Test]
    public async Task Issue1083_Dotted_Schema_Name_Generated_Code_Can_Build()
    {
        string generatedCode = await GenerateCode();

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Issue1083_Preserves_Normal_Schema_Name_When_Malformed_Name_Normalizes_To_Same_Type()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithCollision);

        generatedCode.Should().Contain("partial class LookUpErnResponse");
        generatedCode.Should().Contain("partial class LookUpErnResponse2");
        generatedCode.Should().Contain("Task<LookUpErnResponse2> LookUpErn(");
        generatedCode.Should().Contain("Task<LookUpErnResponse> LookUpErnDirect(");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
        => await GenerateCode(OpenApiSpec);

    private static async Task<string> GenerateCode(string openApiSpec)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(openApiSpec);

        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile
            };

            var sut = await RefitGenerator.CreateAsync(settings);
            return sut.Generate();
        }
        finally
        {
            DeleteSwaggerFile(swaggerFile);
        }
    }

    private static void DeleteSwaggerFile(string swaggerFile)
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
