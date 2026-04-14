using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class PropertyNamingPolicyTests
{
    private const string OpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Property Naming Policy API",
            "version": "1.0.0"
          },
          "paths": {
            "/nodes": {
              "get": {
                "operationId": "GetNode",
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/RecursiveNode"
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
              "RecursiveNode": {
                "type": "object",
                "properties": {
                  "node_id": {
                    "format": "int32"
                  },
                  "class": {
                    "type": "string"
                  },
                  "1st-node": {
                    "type": "string"
                  },
                  "child_count": {
                    "type": "integer"
                  },
                  "next_node": {
                    "$ref": "#/components/schemas/RecursiveNode"
                  },
                  "children": {
                    "type": "array",
                    "items": {
                      "$ref": "#/components/schemas/RecursiveNode"
                    }
                  },
                  "named_nodes": {
                    "type": "object",
                    "additionalProperties": {
                      "$ref": "#/components/schemas/RecursiveNode"
                    }
                  },
                  "external_node": {
                    "$ref": "#/components/schemas/RecursiveExternalNode"
                  }
                }
              },
              "RecursiveExternalNode": {
                "type": "object",
                "properties": {
                  "external_id": {
                    "type": "integer"
                  },
                  "next_node": {
                    "$ref": "#/components/schemas/RecursiveExternalNode"
                  }
                }
              }
            }
          }
        }
        """;

    private const string DigitPrefixedPropertyOpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Digit Prefixed Property API",
            "version": "1.0.0"
          },
          "paths": {
            "/responses": {
              "get": {
                "operationId": "GetResponse",
                "responses": {
                  "200": {
                    "description": "Success",
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
              "TestResponse": {
                "type": "object",
                "properties": {
                  "42_question_field": {
                    "$ref": "#/components/schemas/42Question"
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
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code_With_Default_PascalCase_Property_Naming()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Default_PascalCase_Property_Naming_Remains_Unchanged()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public int NodeId { get; set; }");
        generatedCode.Should().Contain("""[JsonPropertyName("node_id")]""");
    }

    [Test]
    public async Task PreserveOriginal_Emits_Raw_Valid_Identifiers()
    {
        string generatedCode = await GenerateCode(PropertyNamingPolicy.PreserveOriginal);
        generatedCode.Should().Contain("public int node_id { get; set; }");
    }

    [Test]
    public async Task PreserveOriginal_Escapes_Reserved_Keywords()
    {
        string generatedCode = await GenerateCode(PropertyNamingPolicy.PreserveOriginal);
        generatedCode.Should().Contain("public string @class { get; set; }");
    }

    [Test]
    public async Task PreserveOriginal_Minimally_Sanitizes_Invalid_Identifiers()
    {
        string generatedCode = await GenerateCode(PropertyNamingPolicy.PreserveOriginal);
        generatedCode.Should().Contain("_1st_node { get; set; }");
    }

    [Test]
    public async Task PreserveOriginal_Generated_Code_With_Recursive_Schemas_Can_Build()
    {
        string generatedCode = await GenerateCode(PropertyNamingPolicy.PreserveOriginal);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Default_PascalCase_Property_Naming_Prefixes_Digit_Prefixed_Properties()
    {
        string generatedCode = await GenerateCodeFromSpec(DigitPrefixedPropertyOpenApiSpec);
        generatedCode.Should().Contain("""[JsonPropertyName("42_question_field")]""");
        generatedCode.Should().Contain("_42QuestionField { get; set; }");
    }

    [Test]
    public async Task Default_PascalCase_Digit_Prefixed_Properties_Can_Build()
    {
        string generatedCode = await GenerateCodeFromSpec(DigitPrefixedPropertyOpenApiSpec);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(
        PropertyNamingPolicy propertyNamingPolicy = PropertyNamingPolicy.PascalCase)
    {
        return await GenerateCodeFromSpec(OpenApiSpec, propertyNamingPolicy);
    }

    private static async Task<string> GenerateCodeFromSpec(
        string openApiSpec,
        PropertyNamingPolicy propertyNamingPolicy = PropertyNamingPolicy.PascalCase)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(openApiSpec);

        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                PropertyNamingPolicy = propertyNamingPolicy,
            };

            var sut = await RefitGenerator.CreateAsync(settings);
            return sut.Generate();
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
