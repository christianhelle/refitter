using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using Xunit;

namespace Refitter.Tests.Examples;

public class OptionalParametersWithDefaultValuesEdgeCasesTests
{
    [Fact]
    public async Task String_Default_Values_Should_Be_Escaped()
    {
        const string openApiSpec =
            """
            {
              "openapi": "3.0.1",
              "info": {
                "title": "Test API",
                "version": "v1"
              },
              "paths": {
                "/api/test": {
                  "get": {
                    "operationId": "TestEscaping",
                    "parameters": [
                      {
                        "name": "quotedString",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "string",
                          "default": "value with \"quotes\""
                        }
                      },
                      {
                        "name": "backslashString",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "string",
                          "default": "path\\to\\file"
                        }
                      },
                      {
                        "name": "newlineString",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "string",
                          "default": "line1\nline2"
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
              }
            }
            """;

        string generatedCode = await GenerateCode(openApiSpec);

        // Verify strings are properly escaped
        generatedCode.Should().Contain("string? quotedString = \"value with \\\"quotes\\\"\"");
        generatedCode.Should().Contain("string? backslashString = \"path\\\\to\\\\file\"");
        generatedCode.Should().Contain("string? newlineString = \"line1\\nline2\"");
    }

    [Fact]
    public async Task Float_Default_Values_Should_Have_Type_Suffix()
    {
        const string openApiSpec =
            """
            {
              "openapi": "3.0.1",
              "info": {
                "title": "Test API",
                "version": "v1"
              },
              "paths": {
                "/api/test": {
                  "get": {
                    "operationId": "TestFloat",
                    "parameters": [
                      {
                        "name": "floatValue",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "number",
                          "format": "float",
                          "default": 1.5
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
              }
            }
            """;

        string generatedCode = await GenerateCode(openApiSpec);

        // Verify float has 'f' suffix
        generatedCode.Should().Contain("float? floatValue = 1.5f");
    }

    [Fact]
    public async Task Decimal_Default_Values_Should_Have_Type_Suffix()
    {
        const string openApiSpec =
            """
            {
              "openapi": "3.0.1",
              "info": {
                "title": "Test API",
                "version": "v1"
              },
              "paths": {
                "/api/test": {
                  "get": {
                    "operationId": "TestDecimal",
                    "parameters": [
                      {
                        "name": "decimalValue",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "number",
                          "format": "decimal",
                          "default": 99.99
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
              }
            }
            """;

        string generatedCode = await GenerateCode(openApiSpec);

        // Verify decimal has 'm' suffix
        generatedCode.Should().Contain("decimal? decimalValue = 99.99m");
    }

    [Fact]
    public async Task Generated_Code_With_Escaped_Strings_Should_Build()
    {
        const string openApiSpec =
            """
            {
              "openapi": "3.0.1",
              "info": {
                "title": "Test API",
                "version": "v1"
              },
              "paths": {
                "/api/test": {
                  "get": {
                    "operationId": "TestBuild",
                    "parameters": [
                      {
                        "name": "escapedString",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "string",
                          "default": "value with \"quotes\" and \\backslashes\\"
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
              }
            }
            """;

        string generatedCode = await GenerateCode(openApiSpec);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Fact]
    public async Task Generated_Code_With_Float_Decimal_Should_Build()
    {
        const string openApiSpec =
            """
            {
              "openapi": "3.0.1",
              "info": {
                "title": "Test API",
                "version": "v1"
              },
              "paths": {
                "/api/test": {
                  "get": {
                    "operationId": "TestBuild",
                    "parameters": [
                      {
                        "name": "floatValue",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "number",
                          "format": "float",
                          "default": 1.5
                        }
                      },
                      {
                        "name": "decimalValue",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "number",
                          "format": "decimal",
                          "default": 99.99
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
              }
            }
            """;

        string generatedCode = await GenerateCode(openApiSpec);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(string openApiSpec)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(openApiSpec);
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
