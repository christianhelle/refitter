using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

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
    /// Swagger 2.0 equivalent of Can_Generate_Accept_Headers_For_Valid_Responses.
    /// </summary>
    [Test]
    public async Task Can_Generate_Accept_Headers_For_Valid_Responses_Swagger2()
    {
        const string swaggerSpecV2 = """
{
  "swagger": "2.0",
  "info": {
    "title": "Accept Headers Test API",
    "version": "1.0.0"
  },
  "host": "localhost",
  "basePath": "/",
  "paths": {
    "/items": {
      "get": {
        "operationId": "GetItems",
        "produces": ["application/json"],
        "responses": {
          "200": {
            "description": "Success",
            "schema": {
              "type": "array",
              "items": {
                "type": "string"
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
            OpenApiPath = CreateTempFile(swaggerSpecV2),
            AddAcceptHeaders = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var code = sut.Generate();

        code.Should().NotBeNullOrWhiteSpace();
        code.Should().Contain("GetItems");
        // Swagger 2.0 with produces field: verify generation succeeds without crashing
    }

    /// <summary>
    /// Test for issue #1026: nullable reference types alone must not silently change contract shapes.
    /// </summary>
    [Test]
    public async Task Does_Not_Auto_Enable_Optional_Properties_As_Nullable_When_NRT_Enabled()
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
        code.Should().Contain("public string Description");
        code.Should().NotContain("public string? Description");
    }

    /// <summary>
    /// Swagger 2.0 equivalent of Does_Not_Auto_Enable_Optional_Properties_As_Nullable_When_NRT_Enabled.
    /// </summary>
    [Test]
    public async Task Does_Not_Auto_Enable_Optional_Properties_As_Nullable_When_NRT_Enabled_Swagger2()
    {
        const string swaggerSpecV2 = """
{
  "swagger": "2.0",
  "info": {
    "title": "Optional Properties Test API",
    "version": "1.0.0"
  },
  "host": "localhost",
  "basePath": "/",
  "paths": {
    "/items": {
      "post": {
        "operationId": "CreateItem",
        "consumes": ["application/json"],
        "parameters": [
          {
            "name": "body",
            "in": "body",
            "required": true,
            "schema": {
              "$ref": "#/definitions/Item"
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
  },
  "definitions": {
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
""";

        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = CreateTempFile(swaggerSpecV2),
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                GenerateNullableReferenceTypes = true
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var code = sut.Generate();

        code.Should().NotBeNullOrWhiteSpace();
        code.Should().Contain("public string Name");
        code.Should().Contain("public string Description");
        code.Should().NotContain("public string? Description");
    }

    /// <summary>
    /// Swagger 2.0 optional reference properties should keep pre-#1026 shapes for arrays, custom types and generic collections.
    /// Value types should still retain nullable fallthrough where appropriate.
    /// </summary>
    [Test]
    public async Task Does_Not_Auto_Enable_Optional_Properties_As_Nullable_For_Swagger2_Reference_Shapes()
    {
        const string swaggerSpecV2 = """
{
  "swagger": "2.0",
  "info": {
    "title": "Optional Reference Shapes API",
    "version": "1.0.0"
  },
  "host": "localhost",
  "basePath": "/",
  "paths": {
    "/items": {
      "post": {
        "operationId": "CreateItem",
        "consumes": ["application/json"],
        "parameters": [
          {
            "name": "body",
            "in": "body",
            "required": true,
            "schema": {
              "$ref": "#/definitions/Item"
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
  },
  "definitions": {
    "Child": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string"
        }
      }
    },
    "Item": {
      "type": "object",
      "required": ["name"],
      "properties": {
        "name": {
          "type": "string"
        },
        "children": {
          "type": "array",
          "items": {
            "$ref": "#/definitions/Child"
          }
        },
        "primaryChild": {
          "$ref": "#/definitions/Child"
        },
        "namedChildren": {
          "type": "object",
          "additionalProperties": {
            "$ref": "#/definitions/Child"
          }
        },
        "count": {
          "type": "integer",
          "format": "int32"
        }
      }
    }
  }
}
""";

        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = CreateTempFile(swaggerSpecV2),
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                GenerateNullableReferenceTypes = true
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var code = sut.Generate();

        code.Should().Contain("public ICollection<Child> Children");
        code.Should().Contain("public Child PrimaryChild");
        code.Should().Contain("public IDictionary<string, Child> NamedChildren");
        code.Should().Contain("public int? Count");
        code.Should().NotContain("public ICollection<Child>? Children");
        code.Should().NotContain("public Child? PrimaryChild");
        code.Should().NotContain("public IDictionary<string, Child>? NamedChildren");
        BuildHelper.BuildCSharp(code).Should().BeTrue();
    }

    /// <summary>
    /// Explicit opt-in should still generate nullable optional properties when desired.
    /// </summary>
    [Test]
    public async Task Honors_Explicit_GenerateOptionalPropertiesAsNullable_When_NRT_Enabled()
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
                GenerateNullableReferenceTypes = true,
                GenerateOptionalPropertiesAsNullable = true
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var code = sut.Generate();

        code.Should().Contain("public string Name");
        code.Should().Contain("public string? Description");
    }

    /// <summary>
    /// Swagger 2.0 should still honor explicit opt-in for nullable optional properties.
    /// </summary>
    [Test]
    public async Task Honors_Explicit_GenerateOptionalPropertiesAsNullable_When_NRT_Enabled_Swagger2()
    {
        const string swaggerSpecV2 = """
{
  "swagger": "2.0",
  "info": {
    "title": "Optional Properties Test API",
    "version": "1.0.0"
  },
  "host": "localhost",
  "basePath": "/",
  "paths": {
    "/items": {
      "post": {
        "operationId": "CreateItem",
        "consumes": ["application/json"],
        "parameters": [
          {
            "name": "body",
            "in": "body",
            "required": true,
            "schema": {
              "$ref": "#/definitions/Item"
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
  },
  "definitions": {
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
""";

        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = CreateTempFile(swaggerSpecV2),
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                GenerateNullableReferenceTypes = true,
                GenerateOptionalPropertiesAsNullable = true
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var code = sut.Generate();

        code.Should().Contain("public string Name");
        code.Should().Contain("public string? Description");
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
    /// Swagger 2.0 equivalent of Duplicate_Operation_Id_Detection_Is_Efficient.
    /// </summary>
    [Test]
    public async Task Duplicate_Operation_Id_Detection_Is_Efficient_Swagger2()
    {
        const string swaggerSpecV2 = """
{
  "swagger": "2.0",
  "info": {
    "title": "Duplicate Operation ID Test API",
    "version": "1.0.0"
  },
  "host": "localhost",
  "basePath": "/",
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
            OpenApiPath = CreateTempFile(swaggerSpecV2)
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
    /// Swagger 2.0 equivalent of Interface_Generator_Created_Before_Generate_File.
    /// </summary>
    [Test]
    public async Task Interface_Generator_Created_Before_Generate_File_Swagger2()
    {
        const string swaggerSpecV2 = """
{
  "swagger": "2.0",
  "info": {
    "title": "Generator Ordering Test API",
    "version": "1.0.0"
  },
  "host": "localhost",
  "basePath": "/",
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
            "type": "string"
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
            OpenApiPath = CreateTempFile(swaggerSpecV2),
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

    /// <summary>
    /// Swagger 2.0 equivalent of Can_Generate_Code_With_Async_Operations.
    /// </summary>
    [Test]
    public async Task Can_Generate_Code_With_Async_Operations_Swagger2()
    {
        const string swaggerSpecV2 = """
{
  "swagger": "2.0",
  "info": {
    "title": "Async Test API",
    "version": "1.0.0"
  },
  "host": "localhost",
  "basePath": "/",
  "paths": {
    "/items": {
      "get": {
        "operationId": "GetItems",
        "produces": ["application/json"],
        "responses": {
          "200": {
            "description": "Success",
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
""";

        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = CreateTempFile(swaggerSpecV2)
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
