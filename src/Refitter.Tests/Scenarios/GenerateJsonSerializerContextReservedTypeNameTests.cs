using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

[TestClass]
public class GenerateJsonSerializerContextReservedTypeNameTests
{
    private const string OpenApiSpec = """
        {
          "openapi": "3.0.0",
          "info": {
            "title": "Instance API",
            "version": "1.0.0"
          },
          "paths": {
            "/api/options": {
              "get": {
                "operationId": "getOptions",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/Options"
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
              "Options": {
                "type": "object",
                "properties": {
                  "name": {
                    "type": "string"
                  }
                }
              }
            }
          }
        }
        """;

    [TestMethod]
    public async Task Generated_Code_Renames_TypeInfo_Property_For_Options()
    {
        var generatedCode = await GenerateCode();

        generatedCode.Should().Contain(
            "[global::System.Text.Json.Serialization.JsonSerializable(typeof(Options), TypeInfoPropertyName = \"OptionsTypeInfo\")]");
    }

    [TestCategory("Integration")]
    [TestMethod]
    public async Task Generated_Code_With_Options_Contract_Can_Build()
    {
        var generatedCode = await GenerateCode();

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateJsonSerializerContext = true,
                CodeGeneratorSettings = new CodeGeneratorSettings
                {
                    GenerateNullableReferenceTypes = true
                }
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
