using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class TrimUnusedSchemaAliasRegressionTests
{
    private const string AliasSafeTrimOpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Trim Unused Schema Alias Regression API",
            "version": "1.0.0"
          },
          "paths": {
            "/test": {
              "post": {
                "operationId": "DoTest",
                "requestBody": {
                  "required": true,
                  "content": {
                    "application/json": {
                      "schema": {
                        "$ref": "#/components/schemas/TestRequest"
                      }
                    },
                    "multipart/form-data": {
                      "schema": {
                        "$ref": "#/components/schemas/TestRequest"
                      }
                    }
                  }
                },
                "responses": {
                  "201": {
                    "description": "Created",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/TestResponse"
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
              "TestRequest": {
                "type": "object",
                "properties": {
                  "request_param2": {
                    "type": "array",
                    "items": {
                      "$ref": "#/components/schemas/RequestParamArrayItem"
                    }
                  },
                  "request_param3": {
                    "$ref": "#/components/schemas/RequestParamArrayItem"
                  }
                }
              },
              "TestResponse": {
                "type": "object",
                "properties": {
                  "item": {
                    "$ref": "#/components/schemas/RequestParamArrayItem"
                  }
                }
              },
              "RequestParamArrayItem": {
                "$ref": "#/components/schemas/RequestParamArrayItemInternal"
              },
              "RequestParamArrayItemInternal": {
                "type": "object",
                "required": [
                  "param1"
                ],
                "properties": {
                  "param1": {
                    "type": "number"
                  }
                }
              }
            }
          }
        }
        """;

    private const string CombinedOpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Combined Trim And Identifier Regression API",
            "version": "1.0.0"
          },
          "paths": {
            "/test": {
              "post": {
                "operationId": "DoTest",
                "requestBody": {
                  "required": true,
                  "content": {
                    "application/json": {
                      "schema": {
                        "$ref": "#/components/schemas/TestRequest"
                      }
                    },
                    "multipart/form-data": {
                      "schema": {
                        "$ref": "#/components/schemas/TestRequest"
                      }
                    }
                  }
                },
                "responses": {
                  "201": {
                    "description": "Created",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/TestResponse"
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
              "TestRequest": {
                "type": "object",
                "properties": {
                  "42_question_field": {
                    "$ref": "#/components/schemas/42Question"
                  },
                  "request_param2": {
                    "type": "array",
                    "items": {
                      "$ref": "#/components/schemas/RequestParamArrayItem"
                    }
                  },
                  "request_param3": {
                    "$ref": "#/components/schemas/RequestParamArrayItem"
                  }
                }
              },
              "42Question": {
                "type": "object",
                "properties": {
                  "answer_text": {
                    "type": "string"
                  }
                }
              },
              "TestResponse": {
                "type": "object",
                "properties": {
                  "item": {
                    "$ref": "#/components/schemas/RequestParamArrayItem"
                  }
                }
              },
              "RequestParamArrayItem": {
                "$ref": "#/components/schemas/RequestParamArrayItemInternal"
              },
              "RequestParamArrayItemInternal": {
                "type": "object",
                "required": [
                  "param1"
                ],
                "properties": {
                  "param1": {
                    "type": "number"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code_When_TrimUnusedSchema_Uses_Aliased_Component_Schemas()
    {
        string generatedCode = await GenerateCode(AliasSafeTrimOpenApiSpec);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task TrimUnusedSchema_With_Aliased_Component_Schemas_Can_Build()
    {
        string generatedCode = await GenerateCode(AliasSafeTrimOpenApiSpec);
        generatedCode.Should().Contain("RequestParamArrayItem");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Combined_Alias_Trim_And_Digit_Prefixed_Property_Regression_Can_Build()
    {
        string generatedCode = await GenerateCode(CombinedOpenApiSpec);
        generatedCode.Should().Contain("""[JsonPropertyName("42_question_field")]""");
        generatedCode.Should().Contain("_42QuestionField { get; set; }");
        generatedCode.Should().Contain("RequestParamArrayItem");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Combined_Regression_Can_Generate_From_Settings_File_Content()
    {
        string generatedCode = await GenerateCodeFromSettingsFileContent(CombinedOpenApiSpec);
        generatedCode.Should().Contain("""[JsonPropertyName("42_question_field")]""");
        generatedCode.Should().Contain("_42QuestionField { get; set; }");
        generatedCode.Should().Contain("RequestParamArrayItem");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(string openApiSpec)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(openApiSpec);

        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                TrimUnusedSchema = true
            };

            var sut = await RefitGenerator.CreateAsync(settings);
            return sut.Generate();
        }
        finally
        {
            DeleteSwaggerFile(swaggerFile);
        }
    }

    private static async Task<string> GenerateCodeFromSettingsFileContent(string openApiSpec)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(openApiSpec);

        try
        {
            var settingsFileContent = $$"""
                {
                  "openApiPath": "{{swaggerFile.Replace("\\", "\\\\", StringComparison.Ordinal)}}",
                  "trimUnusedSchema": true
                }
                """;

            var settings = Serializer.Deserialize<RefitGeneratorSettings>(settingsFileContent);
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
