using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class IntegerFormatTests
{
    private const string OpenApiSpec =
        """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/test": {
              "get": {
                "operationId": "TestEndpoint",
                "parameters": [
                    {
                        "name": "integerWithoutFormat",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "integer"
                        }
                    },
                    {
                        "name": "integerWithInt32Format",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "integer",
                          "format": "int32"
                        }
                    },
                    {
                        "name": "integerWithInt64Format",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "integer",
                          "format": "int64"
                        }
                    }
                ],
                "requestBody": {
                  "content": {
                    "application/json": {
                      "schema": {
                        "$ref": "#/components/schemas/RequestModel"
                      }
                    }
                  }
                },
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/ResponseModel"
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
              "RequestModel": {
                "type": "object",
                "properties": {
                  "integerWithoutFormat": {
                    "type": "integer",
                    "description": "Integer without format - should default to int but optionally be long"
                  },
                  "integerWithInt32Format": {
                    "type": "integer",
                    "format": "int32"
                  },
                  "integerWithInt64Format": {
                    "type": "integer",
                    "format": "int64"
                  },
                  "additionalPropsObject": {
                    "type": "object",
                    "additionalProperties": {
                      "type": "string"
                    }
                  },
                  "allOfExample": {
                    "allOf": [
                      { "$ref": "#/components/schemas/BaseType" },
                      {
                        "type": "object",
                        "properties": {
                          "extra": { "type": "string" }
                        }
                      }
                    ]
                  },
                  "oneOfExample": {
                    "oneOf": [
                      { "$ref": "#/components/schemas/TypeA" },
                      { "$ref": "#/components/schemas/TypeB" }
                    ]
                  },
                  "anyOfExample": {
                    "anyOf": [
                      { "$ref": "#/components/schemas/TypeA" },
                      { "$ref": "#/components/schemas/TypeB" }
                    ]
                  }
                }
              },
              "ResponseModel": {
                "type": "object",
                "properties": {
                  "integerWithoutFormat": {
                    "type": "integer",
                    "description": "Integer without format - should default to int but optionally be long"
                  },
                  "integerWithInt32Format": {
                    "type": "integer",
                    "format": "int32"
                  },
                  "integerWithInt64Format": {
                    "type": "integer",
                    "format": "int64"
                  },
                  "additionalPropsObject": {
                    "type": "object",
                    "additionalProperties": {
                      "type": "integer"
                    }
                  },
                  "allOfExample": {
                    "allOf": [
                      { "$ref": "#/components/schemas/BaseType" },
                      {
                        "type": "object",
                        "properties": {
                          "extra": { "type": "integer" }
                        }
                      }
                    ]
                  },
                  "oneOfExample": {
                    "oneOf": [
                      { "$ref": "#/components/schemas/TypeA" },
                      { "$ref": "#/components/schemas/TypeB" }
                    ]
                  },
                  "anyOfExample": {
                    "anyOf": [
                      { "$ref": "#/components/schemas/TypeA" },
                      { "$ref": "#/components/schemas/TypeB" }
                    ]
                  }
                }
              },
              "BaseType": {
                "type": "object",
                "properties": {
                  "baseProp": { "type": "integer" }
                }
              },
              "TypeA": {
                "type": "object",
                "properties": {
                  "typeAProp": { "type": "integer" }
                }
              },
              "TypeB": {
                "type": "object",
                "properties": {
                  "typeBProp": { "type": "integer" }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Default_Generates_Integer_Without_Format_As_Int()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("int IntegerWithoutFormat");
    }

    [Test]
    public async Task Can_Generate_Integer_Without_Format_As_Long()
    {
        string generatedCode = await GenerateCodeWithLongIntegers();
        generatedCode.Should().Contain("long IntegerWithoutFormat");
    }

    [Test]
    public async Task Always_Respects_Explicit_Int32_Format()
    {
        string generatedCode = await GenerateCodeWithLongIntegers();
        generatedCode.Should().Contain("int IntegerWithInt32Format");
    }

    [Test]
    public async Task Always_Respects_Explicit_Int64_Format()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("long IntegerWithInt64Format");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Default()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Long()
    {
        string generatedCode = await GenerateCodeWithLongIntegers();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

    private static async Task<string> GenerateCodeWithLongIntegers()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                IntegerType = IntegerType.Int64
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

    [Test]
    public void Can_Deserialize_IntegerType_From_String_Int64()
    {
        const string json = """{"integerType": "Int64"}""";
        var settings = Serializer.Deserialize<CodeGeneratorSettings>(json);
        settings.Should().NotBeNull();
        settings!.IntegerType.Should().Be(IntegerType.Int64);
    }

    [Test]
    public void Can_Deserialize_IntegerType_From_String_Int32()
    {
        const string json = """{"integerType": "Int32"}""";
        var settings = Serializer.Deserialize<CodeGeneratorSettings>(json);
        settings.Should().NotBeNull();
        settings!.IntegerType.Should().Be(IntegerType.Int32);
    }

    [Test]
    public void Can_Deserialize_IntegerType_From_Numeric_Value()
    {
        const string json = """{"integerType": 1}""";
        var settings = Serializer.Deserialize<CodeGeneratorSettings>(json);
        settings.Should().NotBeNull();
        settings!.IntegerType.Should().Be(IntegerType.Int64);
    }

    [Test]
    public void Deserializes_Default_IntegerType_When_Not_Specified()
    {
        const string json = """{}""";
        var settings = Serializer.Deserialize<CodeGeneratorSettings>(json);
        settings.Should().NotBeNull();
        settings!.IntegerType.Should().Be(IntegerType.Int32);
    }
}
