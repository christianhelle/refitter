using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using Xunit;

namespace Refitter.Tests.Examples;

public class IntegerWithoutFormatTests
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
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "$ref": "#/components/schemas/TestModel"
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
              "TestModel": {
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
                  }
                }
              }
            }
          }
        }
        """;

    [Fact]
    public async Task Default_Generates_Integer_Without_Format_As_Int()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("int IntegerWithoutFormat");
    }

    [Fact]
    public async Task Can_Generate_Integer_Without_Format_As_Long()
    {
        string generatedCode = await GenerateCodeWithLongIntegers();
        generatedCode.Should().Contain("long IntegerWithoutFormat");
    }

    [Fact]
    public async Task Always_Respects_Explicit_Int32_Format()
    {
        string generatedCode = await GenerateCodeWithLongIntegers();
        generatedCode.Should().Contain("int IntegerWithInt32Format");
    }

    [Fact]
    public async Task Always_Respects_Explicit_Int64_Format()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("long IntegerWithInt64Format");
    }

    [Fact]
    public async Task Can_Build_Generated_Code_With_Default()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Fact]
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
                IntegerType = "long"
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
