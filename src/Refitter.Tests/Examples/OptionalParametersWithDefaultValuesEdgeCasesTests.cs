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
    public async Task String_Default_Values_With_All_Escape_Characters_Should_Be_Escaped()
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
                      },
                      {
                        "name": "allSpecialChars",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "string",
                          "default": "test\n\r\t\\\""
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
        generatedCode.Should().Contain("string? carriageReturnString = \"line1\\rline2\"");
        generatedCode.Should().Contain("string? tabString = \"col1\\tcol2\"");
        generatedCode.Should().Contain("string? allSpecialChars = \"test\\n\\r\\t\\\\\\\"\"");
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

    [Fact]
    public async Task Integer_Default_Values_Should_Not_Have_Suffix()
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
        generatedCode.Should().Contain("int? intValue = 42");
        generatedCode.Should().Contain("long? longValue = 9999");
    }

    [Fact]
    public async Task Double_Default_Values_Should_Have_Decimal_Point()
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
                    "operationId": "TestDouble",
                    "parameters": [
                      {
                        "name": "doubleValue",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "number",
                          "format": "double",
                          "default": 3.14159
                        }
                      },
                      {
                        "name": "doubleIntValue",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "number",
                          "format": "double",
                          "default": 10
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

        // Verify double with decimal point stays as-is
        generatedCode.Should().Contain("double? doubleValue = 3.14159");
        // Verify double with integer value gets .0 appended
        generatedCode.Should().Contain("double? doubleIntValue = 10.0");
    }

    [Fact]
    public async Task Boolean_Default_Values_Should_Be_Lowercase()
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
        generatedCode.Should().Contain("bool? isEnabled = true");
        generatedCode.Should().Contain("bool? isDisabled = false");
    }

    [Fact]
    public async Task Empty_String_Default_Value_Should_Be_Handled()
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
                    "operationId": "TestEmpty",
                    "parameters": [
                      {
                        "name": "emptyString",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "string",
                          "default": ""
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

        // Verify empty string is properly quoted
        generatedCode.Should().Contain("string? emptyString = \"\"");
    }

    [Fact]
    public async Task Mixed_Escape_Sequences_Should_Be_Handled()
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
                    "operationId": "TestMixedEscapes",
                    "parameters": [
                      {
                        "name": "complexString",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "string",
                          "default": "path\\to\\file with \"quotes\" and\nnewlines\tand tabs"
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

        // Verify all escape sequences are properly handled in combination
        generatedCode.Should().Contain("string? complexString = \"path\\\\to\\\\file with \\\"quotes\\\" and\\nnewlines\\tand tabs\"");
    }

    [Fact]
    public async Task Long_And_ULong_Default_Values_Should_Have_Suffixes()
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
                    "operationId": "TestLong",
                    "parameters": [
                      {
                        "name": "longValue",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "integer",
                          "format": "int64",
                          "default": 3000000000
                        }
                      },
                      {
                        "name": "ulongValue",
                        "in": "query",
                        "required": false,
                        "schema": {
                          "type": "integer",
                          "format": "uint64",
                          "default": 5000000000
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

        // Verify long and ulong have proper suffixes
        generatedCode.Should().Contain("long? longValue = 3000000000L");
        generatedCode.Should().Contain("ulong? ulongValue = 5000000000UL");
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
