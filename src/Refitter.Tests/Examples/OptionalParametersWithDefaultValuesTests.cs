using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class OptionalParametersWithDefaultValuesTests
{
    private const string OpenApiSpec =
        """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Test API",
            "version": "v1"
          },
          "paths": {
            "/api/schedule/list": {
              "get": {
                "tags": ["Schedule"],
                "operationId": "List",
                "parameters": [
                  {
                    "name": "start",
                    "in": "query",
                    "required": true,
                    "schema": {
                      "type": "string",
                      "format": "date"
                    }
                  },
                  {
                    "name": "end",
                    "in": "query",
                    "required": true,
                    "schema": {
                      "type": "string",
                      "format": "date"
                    }
                  },
                  {
                    "name": "includeCancelled",
                    "in": "query",
                    "required": false,
                    "schema": {
                      "type": "boolean",
                      "default": true
                    }
                  },
                  {
                    "name": "pageSize",
                    "in": "query",
                    "required": false,
                    "schema": {
                      "type": "integer",
                      "format": "int32",
                      "default": 10
                    }
                  },
                  {
                    "name": "filter",
                    "in": "query",
                    "required": false,
                    "schema": {
                      "type": "string",
                      "default": "active"
                    }
                  }
                ],
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": {
                          "type": "array",
                          "items": {
                            "type": "object"
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generated_Code_Should_Have_Optional_Parameters_With_Default_Values()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("bool? includeCancelled = true");
        generatedCode.Should().Contain("int? pageSize = 10");
        generatedCode.Should().Contain("string? filter = \"active\"");
    }

    [Test]
    public async Task Generated_Code_Should_Have_Required_Parameters_Without_Defaults()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Query] System.DateTimeOffset start");
        generatedCode.Should().Contain("[Query] System.DateTimeOffset end");
        generatedCode.Should().NotContain("System.DateTimeOffset start =");
        generatedCode.Should().NotContain("System.DateTimeOffset end =");
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            OptionalParameters = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var code = sut.Generate();
        return code;
    }
}
