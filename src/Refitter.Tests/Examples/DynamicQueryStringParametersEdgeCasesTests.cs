using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using Xunit;

namespace Refitter.Tests.Examples;

public class DynamicQueryStringParametersEdgeCasesTests
{
    [Fact]
    public async Task Dynamic_QueryString_With_Escaped_String_Defaults()
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
                        "name": "requiredParam",
                        "in": "query",
                        "required": true,
                        "schema": {
                          "type": "string"
                        }
                      },
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

        // Verify strings are properly escaped in dynamic query string class
        generatedCode.Should().Contain("string? QuotedString { get; set; } = \"value with \\\"quotes\\\"\"");
        generatedCode.Should().Contain("string? BackslashString { get; set; } = \"path\\\\to\\\\file\"");
    }

    [Fact]
    public async Task Dynamic_QueryString_With_All_Escape_Characters()
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
                    "operationId": "TestAllEscapes",
                    "parameters": [
                      {
                        "name": "requiredParam",
                        "in": "query",
                        "required": true,
                        "schema": {
                          "type": "string"
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
                      },
                      {
                        "name": "carriageReturnString",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "string",
                          "default": "line1\rline2"
                        }
                      },
                      {
                        "name": "tabString",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "string",
                          "default": "col1\tcol2"
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

        // Verify all special characters are properly escaped
        generatedCode.Should().Contain("string? NewlineString { get; set; } = \"line1\\nline2\"");
        generatedCode.Should().Contain("string? CarriageReturnString { get; set; } = \"line1\\rline2\"");
        generatedCode.Should().Contain("string? TabString { get; set; } = \"col1\\tcol2\"");
    }

    [Fact]
    public async Task Dynamic_QueryString_With_Float_And_Decimal_Defaults()
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
                    "operationId": "TestNumeric",
                    "parameters": [
                      {
                        "name": "requiredParam",
                        "in": "query",
                        "required": true,
                        "schema": {
                          "type": "string"
                        }
                      },
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

        // Verify numeric types have proper suffixes
        generatedCode.Should().Contain("float? FloatValue { get; set; } = 1.5f");
        generatedCode.Should().Contain("decimal? DecimalValue { get; set; } = 99.99m");
    }

    [Fact]
    public async Task Dynamic_QueryString_With_Boolean_Defaults()
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
                    "operationId": "TestBool",
                    "parameters": [
                      {
                        "name": "requiredParam",
                        "in": "query",
                        "required": true,
                        "schema": {
                          "type": "string"
                        }
                      },
                      {
                        "name": "isEnabled",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "boolean",
                          "default": true
                        }
                      },
                      {
                        "name": "isDisabled",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "boolean",
                          "default": false
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

        // Verify booleans are lowercase
        generatedCode.Should().Contain("bool? IsEnabled { get; set; } = true");
        generatedCode.Should().Contain("bool? IsDisabled { get; set; } = false");
    }

    [Fact]
    public async Task Dynamic_QueryString_With_Integer_Defaults()
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
                    "operationId": "TestInt",
                    "parameters": [
                      {
                        "name": "requiredParam",
                        "in": "query",
                        "required": true,
                        "schema": {
                          "type": "string"
                        }
                      },
                      {
                        "name": "intValue",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "integer",
                          "format": "int32",
                          "default": 42
                        }
                      },
                      {
                        "name": "longValue",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "integer",
                          "format": "int64",
                          "default": 9999
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

        // Verify integers don't have suffixes
        generatedCode.Should().Contain("int? IntValue { get; set; } = 42");
        generatedCode.Should().Contain("long? LongValue { get; set; } = 9999");
    }

    [Fact]
    public async Task Dynamic_QueryString_Generated_Code_Should_Build()
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
                        "name": "requiredParam",
                        "in": "query",
                        "required": true,
                        "schema": {
                          "type": "string"
                        }
                      },
                      {
                        "name": "escapedString",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "string",
                          "default": "value with \"quotes\" and \\backslashes\\"
                        }
                      },
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
            OptionalParameters = true,
            UseDynamicQuerystringParameters = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var code = sut.Generate();
        return code;
    }
}
