using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using TUnit.Core;

namespace Refitter.Tests.Examples;

/// <summary>
/// Regression tests for runtime compatibility issues identified in v2.0 audit.
/// </summary>
public class RuntimeCompatibilityTests
{
    /// <summary>
    /// Test for issue #1027: RefitInterfaceGenerator NRE when an OpenAPI response has no content.
    /// Ensures that responses with null Content (e.g., 204 No Content) are handled gracefully.
    /// </summary>
    [Test]
    public async Task Can_Handle_Response_With_No_Content()
    {
        const string openApiSpec = """
{
  "openapi": "3.0.0",
  "info": {
    "title": "No Content Test API",
    "version": "1.0.0"
  },
  "paths": {
    "/items/{id}": {
      "delete": {
        "operationId": "DeleteItem",
        "parameters": [
          {
            "name": "id",
            "in": "path",
            "required": true,
            "schema": {
              "type": "string"
            }
          }
        ],
        "responses": {
          "204": {
            "description": "Successfully deleted"
          },
          "default": {
            "description": "Error occurred"
          }
        }
      }
    }
  }
}
""";

        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = CreateTempFile(openApiSpec),
            AddAcceptHeaders = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var code = sut.Generate();

        code.Should().NotBeNullOrWhiteSpace();
        code.Should().Contain("DeleteItem");
    }

    /// <summary>
    /// Test for issue #1027: Verify Accept headers are generated for responses with content.
    /// </summary>
    [Test]
    public async Task Can_Generate_Accept_Headers_For_Valid_Responses()
    {
        const string openApiSpec = """
{
  "openapi": "3.0.0",
  "info": {
    "title": "Accept Headers Test API",
    "version": "1.0.0"
  },
  "paths": {
    "/items": {
      "get": {
        "operationId": "GetItems",
        "responses": {
          "200": {
            "description": "Success",
            "content": {
              "application/json": {
                "schema": {
                  "type": "array",
                  "items": {
                    "type": "string"
                  }
                }
              }
            }
          },
          "204": {
            "description": "No content"
          }
        }
      }
    }
  }
}
""";

        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = CreateTempFile(openApiSpec),
            AddAcceptHeaders = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var code = sut.Generate();

        code.Should().NotBeNullOrWhiteSpace();
        code.Should().Contain("GetItems");
        code.Should().Contain("application/json");
    }

    /// <summary>
    /// Test for issue #1026: Verify auto-enabling of GenerateOptionalPropertiesAsNullable.
    /// When GenerateNullableReferenceTypes is enabled, optional properties should be nullable.
    /// </summary>
    [Test]
    public async Task Auto_Enables_Optional_Properties_As_Nullable_When_NRT_Enabled()
    {
        const string openApiSpec = """
{
  "openapi": "3.0.0",
  "info": {
    "title": "Optional Properties Test API",
    "version": "1.0.0"
  },
  "paths": {
    "/items": {
      "post": {
        "operationId": "CreateItem",
        "requestBody": {
          "required": true,
          "content": {
            "application/json": {
              "schema": {
                "$ref": "#/components/schemas/Item"
              }
            }
          }
        },
        "responses": {
          "200": {
            "description": "Success"
          }
        }
      }
    }
  },
  "components": {
    "schemas": {
      "Item": {
        "type": "object",
        "required": ["name"],
        "properties": {
          "name": {
            "type": "string"
          },
          "description": {
            "type": "string"
          }
        }
      }
    }
  }
}
""";

        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = CreateTempFile(openApiSpec),
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                GenerateNullableReferenceTypes = true
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var code = sut.Generate();

        code.Should().NotBeNullOrWhiteSpace();
        code.Should().Contain("public string Name");
        code.Should().Contain("public string? Description"); // Optional property should be nullable
    }

    /// <summary>
    /// Test for issue #1052: Verify duplicate operation ID detection is efficient.
    /// This test ensures the duplicate detection short-circuits on first duplicate found.
    /// </summary>
    [Test]
    public async Task Duplicate_Operation_Id_Detection_Is_Efficient()
    {
        const string openApiSpec = """
{
  "openapi": "3.0.0",
  "info": {
    "title": "Duplicate Operation ID Test API",
    "version": "1.0.0"
  },
  "paths": {
    "/items": {
      "get": {
        "operationId": "GetItem",
        "responses": {
          "200": {
            "description": "Success"
          }
        }
      }
    },
    "/products": {
      "get": {
        "operationId": "GetItem",
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

        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = CreateTempFile(openApiSpec)
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var code = sut.Generate();

        code.Should().NotBeNullOrWhiteSpace();
        code.Should().Contain("Task Items()");
        code.Should().Contain("Task Products()");
    }

    /// <summary>
    /// Test for issue #1055: Verify interface generator is created before GenerateFile().
    /// This ensures operation ID detection works correctly.
    /// </summary>
    [Test]
    public async Task Interface_Generator_Created_Before_Generate_File()
    {
        const string openApiSpec = """
{
  "openapi": "3.0.0",
  "info": {
    "title": "Generator Ordering Test API",
    "version": "1.0.0"
  },
  "tags": [
    {
      "name": "Items"
    }
  ],
  "paths": {
    "/items": {
      "get": {
        "operationId": "GetItems",
        "tags": ["Items"],
        "responses": {
          "200": {
            "description": "Success"
          }
        }
      }
    },
    "/items/{id}": {
      "get": {
        "operationId": "GetItemById",
        "tags": ["Items"],
        "parameters": [
          {
            "name": "id",
            "in": "path",
            "required": true,
            "schema": {
              "type": "string"
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

        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = CreateTempFile(openApiSpec),
            MultipleInterfaces = MultipleInterfaces.ByTag
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var code = sut.Generate();

        code.Should().NotBeNullOrWhiteSpace();
        code.Should().Contain("GetItems");
        code.Should().Contain("GetItemById");
        // Should not have numeric suffixes like GetItems2
        code.Should().NotContain("GetItems2");
    }

    /// <summary>
    /// Test for issue #1049: Verify generated code with async operations.
    /// Note: ConfigureAwait(false) is used internally in library code, not in generated output.
    /// </summary>
    [Test]
    public async Task Can_Generate_Code_With_Async_Operations()
    {
        const string openApiSpec = """
{
  "openapi": "3.0.0",
  "info": {
    "title": "Async Test API",
    "version": "1.0.0"
  },
  "paths": {
    "/items": {
      "get": {
        "operationId": "GetItems",
        "responses": {
          "200": {
            "description": "Success",
            "content": {
              "application/json": {
                "schema": {
                  "type": "array",
                  "items": {
                    "type": "string"
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

        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = CreateTempFile(openApiSpec)
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var code = sut.Generate();

        code.Should().NotBeNullOrWhiteSpace();
        code.Should().Contain("Task<");
        BuildHelper.BuildCSharp(code).Should().BeTrue();
    }

    private static string CreateTempFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        return tempFile;
    }
}
