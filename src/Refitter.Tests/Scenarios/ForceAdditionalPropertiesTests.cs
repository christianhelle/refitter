using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

[TestClass]
public class ForceAdditionalPropertiesTests
{
    private const string OpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Closed Schemas API",
            "version": "1.0.0"
          },
          "paths": {
            "/api/v3/config/mediamanagement": {
              "get": {
                "operationId": "getMediaManagementConfig",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/MediaManagementConfigResource"
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
              "MediaManagementConfigResource": {
                "type": "object",
                "properties": {
                  "id": {
                    "type": "integer",
                    "format": "int32"
                  },
                  "recycleBin": {
                    "type": "string",
                    "nullable": true
                  },
                  "quality": {
                    "$ref": "#/components/schemas/QualityResource"
                  }
                },
                "additionalProperties": false
              },
              "QualityResource": {
                "type": "object",
                "properties": {
                  "name": {
                    "type": "string",
                    "nullable": true
                  }
                },
                "additionalProperties": false
              }
            }
          }
        }
        """;

    [TestMethod]
    public async Task Closed_Schemas_Do_Not_Get_AdditionalProperties_By_Default()
    {
        string generatedCode = await GenerateCode(forceAdditionalProperties: false);
        generatedCode.Should().NotContain("JsonExtensionData");
        generatedCode.Should().NotContain("AdditionalProperties");
    }

    [TestMethod]
    public async Task Closed_Schemas_Get_AdditionalProperties_When_Forced()
    {
        string generatedCode = await GenerateCode(forceAdditionalProperties: true);
        generatedCode.Should().Contain("JsonExtensionData");
        generatedCode.Should().Contain("IDictionary<string, object> AdditionalProperties");
    }

    [TestMethod]
    public async Task Forced_AdditionalProperties_Take_Precedence_Over_Skipping_Defaults()
    {
        string generatedCode = await GenerateCode(
            forceAdditionalProperties: true,
            generateDefaultAdditionalProperties: false);
        generatedCode.Should().Contain("IDictionary<string, object> AdditionalProperties");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Can_Build_Generated_Code_When_Forced()
    {
        string generatedCode = await GenerateCode(forceAdditionalProperties: true);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(
        bool forceAdditionalProperties,
        bool generateDefaultAdditionalProperties = true)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            GenerateDefaultAdditionalProperties = generateDefaultAdditionalProperties,
            ForceAdditionalProperties = forceAdditionalProperties,
        };
        var generator = await RefitGenerator.CreateAsync(settings);
        return generator.Generate();
    }
}
