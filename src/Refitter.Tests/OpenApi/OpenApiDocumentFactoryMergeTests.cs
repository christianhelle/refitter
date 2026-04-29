using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NSwag;
using Refitter.Core;
using Refitter.Tests.Resources;

namespace Refitter.Tests.OpenApi;

public class OpenApiDocumentFactoryMergeTests
{
    [Test]
    public async Task Merge_Preserves_Tags_From_Base_Document()
    {
        // Test that when the base document has tags defined, they are preserved in the merge
        var specWithTags = @"openapi: '3.0.0'
info:
  title: API With Tags
  version: '1.0'
tags:
  - name: users
    description: User operations
  - name: pets
    description: Pet operations
paths:
  /users:
    get:
      operationId: listUsers
      tags:
        - users
      responses:
        '200':
          description: Success
";

        var specWithoutTags = @"openapi: '3.0.0'
info:
  title: API Without Tags
  version: '1.0'
paths:
  /products:
    get:
      operationId: listProducts
      responses:
        '200':
          description: Success
";

        var file1 = await TestFile.CreateSwaggerFile(specWithTags, "with-tags.yaml");
        var file2 = await TestFile.CreateSwaggerFile(specWithoutTags, "without-tags.yaml");

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file2 });

        merged.Should().NotBeNull();
        merged.Tags.Should().NotBeNull();
        merged.Tags.Should().HaveCount(2);
        merged.Tags.Should().Contain(t => t.Name == "users");
        merged.Tags.Should().Contain(t => t.Name == "pets");
    }

    [Test]
    public async Task Merge_Combines_Tags_From_Multiple_Documents()
    {
        // Test that tags from both documents are merged, testing the tag name selector lambda
        var spec1 = @"openapi: '3.0.0'
info:
  title: API 1
  version: '1.0'
tags:
  - name: users
    description: User operations
paths:
  /users:
    get:
      operationId: listUsers
      tags:
        - users
      responses:
        '200':
          description: Success
";

        var spec2 = @"openapi: '3.0.0'
info:
  title: API 2
  version: '1.0'
tags:
  - name: products
    description: Product operations
  - name: orders
    description: Order operations
paths:
  /products:
    get:
      operationId: listProducts
      tags:
        - products
      responses:
        '200':
          description: Success
";

        var file1 = await TestFile.CreateSwaggerFile(spec1, "api1.yaml");
        var file2 = await TestFile.CreateSwaggerFile(spec2, "api2.yaml");

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file2 });

        merged.Should().NotBeNull();
        merged.Tags.Should().NotBeNull();
        merged.Tags.Should().HaveCount(3);
        merged.Tags.Should().Contain(t => t.Name == "users");
        merged.Tags.Should().Contain(t => t.Name == "products");
        merged.Tags.Should().Contain(t => t.Name == "orders");
    }

    [Test]
    public async Task Merge_Does_Not_Duplicate_Tags_With_Same_Name()
    {
        // Test that when multiple documents have the same tag name, it's not duplicated
        var spec1 = @"openapi: '3.0.0'
info:
  title: API 1
  version: '1.0'
tags:
  - name: shared
    description: Shared operations from API 1
paths:
  /users:
    get:
      operationId: listUsers
      tags:
        - shared
      responses:
        '200':
          description: Success
";

        var spec2 = @"openapi: '3.0.0'
info:
  title: API 2
  version: '1.0'
tags:
  - name: shared
    description: Shared operations from API 2
paths:
  /products:
    get:
      operationId: listProducts
      tags:
        - shared
      responses:
        '200':
          description: Success
";

        var file1 = await TestFile.CreateSwaggerFile(spec1, "api1-shared.yaml");
        var file2 = await TestFile.CreateSwaggerFile(spec2, "api2-shared.yaml");

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file2 });

        merged.Should().NotBeNull();
        merged.Tags.Should().NotBeNull();
        merged.Tags.Should().HaveCount(1);
        merged.Tags.Should().Contain(t => t.Name == "shared");
    }

    [Test]
    public async Task Merge_Creates_Tags_Collection_When_Base_Has_None_But_Second_Does()
    {
        // Test that when base document has no tags but second document does, tags are added
        var specWithoutTags = @"openapi: '3.0.0'
info:
  title: API Without Tags
  version: '1.0'
paths:
  /users:
    get:
      operationId: listUsers
      responses:
        '200':
          description: Success
";

        var specWithTags = @"openapi: '3.0.0'
info:
  title: API With Tags
  version: '1.0'
tags:
  - name: products
    description: Product operations
paths:
  /products:
    get:
      operationId: listProducts
      tags:
        - products
      responses:
        '200':
          description: Success
";

        var file1 = await TestFile.CreateSwaggerFile(specWithoutTags, "no-tags.yaml");
        var file2 = await TestFile.CreateSwaggerFile(specWithTags, "with-tags.yaml");

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file2 });

        merged.Should().NotBeNull();
        merged.Tags.Should().NotBeNull();
        merged.Tags.Should().HaveCount(1);
        merged.Tags.Should().Contain(t => t.Name == "products");
    }

    [Test]
    public async Task PopulateMissingRequiredFields_Handles_External_References()
    {
        // Test that external reference handling works correctly
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        var mainSpec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""operationId"": ""getTest"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""./components.json#/components/schemas/TestModel""
                }
              }
            }
          }
        }
      }
    }
  }
}";

        var componentsSpec = @"{
  ""components"": {
    ""schemas"": {
      ""TestModel"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": { ""type"": ""string"" }
        }
      }
    }
  }
}";

        var mainFile = Path.Combine(folder, "test-api.json");
        var componentsFile = Path.Combine(folder, "components.json");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(componentsFile, componentsSpec);

        var document = await OpenApiDocumentFactory.CreateAsync(mainFile);

        document.Should().NotBeNull();
        document.Info.Should().NotBeNull();
        document.Info.Title.Should().Be("Test API");
        document.Info.Version.Should().Be("1.0.0");
    }

    [Test]
    public async Task PopulateMissingRequiredFields_Preserves_Existing_Title()
    {
        // Test that when title is present, it's preserved
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        var mainSpec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""My API Spec"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""operationId"": ""getTest"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""./components.json#/components/schemas/TestModel""
                }
              }
            }
          }
        }
      }
    }
  }
}";

        var componentsSpec = @"{
  ""components"": {
    ""schemas"": {
      ""TestModel"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": { ""type"": ""string"" }
        }
      }
    }
  }
}";

        var mainFile = Path.Combine(folder, "my-api-spec.json");
        var componentsFile = Path.Combine(folder, "components.json");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(componentsFile, componentsSpec);

        var document = await OpenApiDocumentFactory.CreateAsync(mainFile);

        document.Should().NotBeNull();
        document.Info.Should().NotBeNull();
        document.Info.Title.Should().Be("My API Spec");
        document.Info.Version.Should().Be("1.0.0");
    }

    [Test]
    public async Task PopulateMissingRequiredFields_Creates_Info_With_External_Refs()
    {
        // Test that when Info is minimal with external references, it's populated
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        var mainSpec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Minimal API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""operationId"": ""getTest"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""./components.json#/components/schemas/TestModel""
                }
              }
            }
          }
        }
      }
    }
  }
}";

        var componentsSpec = @"{
  ""components"": {
    ""schemas"": {
      ""TestModel"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": { ""type"": ""string"" }
        }
      }
    }
  }
}";

        var mainFile = Path.Combine(folder, "minimal-info.json");
        var componentsFile = Path.Combine(folder, "components.json");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(componentsFile, componentsSpec);

        var document = await OpenApiDocumentFactory.CreateAsync(mainFile);

        document.Should().NotBeNull();
        document.Info.Should().NotBeNull();
        document.Info.Title.Should().NotBeNullOrEmpty();
        document.Info.Version.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Merge_Handles_Three_Documents_With_Tags()
    {
        // Test merging more than two documents with various tag scenarios
        var spec1 = @"openapi: '3.0.0'
info:
  title: API 1
  version: '1.0'
tags:
  - name: users
paths:
  /users:
    get:
      operationId: listUsers
      responses:
        '200':
          description: Success
";

        var spec2 = @"openapi: '3.0.0'
info:
  title: API 2
  version: '1.0'
tags:
  - name: products
paths:
  /products:
    get:
      operationId: listProducts
      responses:
        '200':
          description: Success
";

        var spec3 = @"openapi: '3.0.0'
info:
  title: API 3
  version: '1.0'
tags:
  - name: orders
  - name: users
paths:
  /orders:
    get:
      operationId: listOrders
      responses:
        '200':
          description: Success
";

        var file1 = await TestFile.CreateSwaggerFile(spec1, "api1.yaml");
        var file2 = await TestFile.CreateSwaggerFile(spec2, "api2.yaml");
        var file3 = await TestFile.CreateSwaggerFile(spec3, "api3.yaml");

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file2, file3 });

        merged.Should().NotBeNull();
        merged.Tags.Should().NotBeNull();
        merged.Tags.Should().HaveCount(3);
        merged.Tags.Should().Contain(t => t.Name == "users");
        merged.Tags.Should().Contain(t => t.Name == "products");
        merged.Tags.Should().Contain(t => t.Name == "orders");
    }

    [Test]
    public async Task Merge_For_NonConflicting_Documents_Returns_A_New_Document_Without_Mutating_Inputs()
    {
        const string baseSpec = """
            openapi: '3.0.0'
            info:
              title: Base API
              version: '1.0'
            tags:
              - name: users
                description: User operations
            paths:
              /users:
                get:
                  operationId: listUsersBase
                  tags:
                    - users
                  responses:
                    '200':
                      description: Base users
            components:
              schemas:
                User:
                  type: object
                  properties:
                    id:
                      type: string
            """;

        const string secondSpec = """
            openapi: '3.0.0'
            info:
              title: Second API
              version: '1.0'
            tags:
              - name: orders
                description: Order operations
            paths:
              /orders:
                get:
                  operationId: listOrders
                  tags:
                    - orders
                  responses:
                    '200':
                      description: Orders
            components:
              schemas:
                Order:
                  type: object
                  properties:
                    orderId:
                      type: string
            """;

        var baseDocument = await OpenApiYamlDocument.FromYamlAsync(baseSpec);
        var secondDocument = await OpenApiYamlDocument.FromYamlAsync(secondSpec);

        var merged = InvokeMerge(baseDocument, secondDocument);

        merged.Should().NotBeSameAs(baseDocument);
        merged.Paths.Should().ContainKeys("/users", "/orders");
        merged.Components.Schemas.Should().ContainKeys("User", "Order");
        merged.Tags.Should().Contain(t => t.Name == "users");
        merged.Tags.Should().Contain(t => t.Name == "orders");

        baseDocument.Paths.Should().ContainSingle().Which.Key.Should().Be("/users");
        baseDocument.Components.Schemas.Should().ContainSingle().Which.Key.Should().Be("User");
        baseDocument.Tags.Should().ContainSingle().Which.Name.Should().Be("users");

        secondDocument.Paths.Should().ContainSingle().Which.Key.Should().Be("/orders");
        secondDocument.Components.Schemas.Should().ContainSingle().Which.Key.Should().Be("Order");
        secondDocument.Tags.Should().ContainSingle().Which.Name.Should().Be("orders");
    }

    [Test]
    public async Task Merge_With_Equivalent_Recursive_Schema_Duplicate_Ignores_Duplicate()
    {
        const string baseSpec = """
            {
              "openapi": "3.0.0",
              "info": {
                "title": "Base API",
                "version": "1.0"
              },
              "paths": {
                "/nodes": {
                  "get": {
                    "operationId": "ListNodes",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/json": {
                            "schema": {
                              "$ref": "#/components/schemas/Node"
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
                  "Node": {
                    "type": "object",
                    "properties": {
                      "id": {
                        "type": "string"
                      },
                      "next": {
                        "$ref": "#/components/schemas/Node"
                      }
                    }
                  }
                }
              }
            }
            """;

        const string secondSpec = """
            {
              "openapi": "3.0.0",
              "info": {
                "title": "Second API",
                "version": "1.0"
              },
              "paths": {
                "/linked-list": {
                  "get": {
                    "operationId": "GetLinkedList",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/json": {
                            "schema": {
                              "$ref": "#/components/schemas/Node"
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
                  "Node": {
                    "properties": {
                      "next": {
                        "$ref": "#/components/schemas/Node"
                      },
                      "id": {
                        "type": "string"
                      }
                    },
                    "type": "object"
                  }
                }
              }
            }
            """;

        var baseDocument = await ParseJsonDocument(baseSpec);
        var secondDocument = await ParseJsonDocument(secondSpec);

        var merged = InvokeMerge(baseDocument, secondDocument);

        merged.Paths.Should().ContainKeys("/nodes", "/linked-list");
        merged.Components.Schemas.Should().ContainSingle().Which.Key.Should().Be("Node");
        baseDocument.Paths.Should().ContainSingle().Which.Key.Should().Be("/nodes");
        secondDocument.Paths.Should().ContainSingle().Which.Key.Should().Be("/linked-list");
    }

    [Test]
    public async Task Merge_With_Equivalent_Swagger2_Definition_Duplicate_Ignores_Duplicate()
    {
        var target = new Dictionary<string, JsonSchema>
        {
            ["ProblemDetails"] = await JsonSchema.FromJsonAsync("""
                {
                  "type": "object",
                  "required": [ "title" ],
                  "properties": {
                    "title": {
                      "type": "string"
                    },
                    "status": {
                      "type": "integer",
                      "format": "int32"
                    }
                  }
                }
                """)
        };
        var incoming = await JsonSchema.FromJsonAsync("""
            {
              "properties": {
                "status": {
                  "format": "int32",
                  "type": "integer"
                },
                "title": {
                  "type": "string"
                }
              },
              "required": [ "title" ],
              "type": "object"
            }
            """);

        InvokeMergeIfMissingOrThrowOnConflict(target, "ProblemDetails", incoming, "definition");

        target.Should().ContainSingle().Which.Key.Should().Be("ProblemDetails");
        target["ProblemDetails"].Properties.Should().ContainKeys("title", "status");
    }

    [Test]
    public async Task Merge_With_Collisions_Throws_And_Does_Not_Mutate_Inputs()
    {
        const string baseSpec = """
            openapi: '3.0.0'
            info:
              title: Base API
              version: '1.0'
            tags:
              - name: users
                description: User operations
            paths:
              /users:
                get:
                  operationId: listUsersBase
                  tags:
                    - users
                  responses:
                    '200':
                      description: Base users
            components:
              schemas:
                User:
                  type: object
                  properties:
                    id:
                      type: string
            """;

        const string secondSpec = """
            openapi: '3.0.0'
            info:
              title: Second API
              version: '1.0'
            tags:
              - name: orders
                description: Order operations
            paths:
              /users:
                get:
                  operationId: listUsersSecond
                  tags:
                    - users
                  responses:
                    '200':
                      description: Second users
              /orders:
                get:
                  operationId: listOrders
                  tags:
                    - orders
                  responses:
                    '200':
                      description: Orders
            components:
              schemas:
                User:
                  type: object
                  properties:
                    email:
                      type: string
                Order:
                  type: object
                  properties:
                    orderId:
                      type: string
            """;

        var baseDocument = await OpenApiYamlDocument.FromYamlAsync(baseSpec);
        var secondDocument = await OpenApiYamlDocument.FromYamlAsync(secondSpec);

        var act = () => InvokeMerge(baseDocument, secondDocument);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*duplicate path '/users'*");

        baseDocument.Paths.Should().ContainSingle().Which.Key.Should().Be("/users");
        baseDocument.Components.Schemas.Should().ContainSingle().Which.Key.Should().Be("User");
        baseDocument.Tags.Should().ContainSingle().Which.Name.Should().Be("users");

        secondDocument.Paths.Should().ContainKeys("/users", "/orders");
        secondDocument.Components.Schemas.Should().ContainKeys("User", "Order");
        secondDocument.Tags.Should().Contain(t => t.Name == "orders");
    }

    [Test]
    public async Task Merge_With_Schema_Collision_Throws_And_Does_Not_Mutate_Inputs()
    {
        const string baseSpec = """
            {
              "openapi": "3.0.0",
              "info": {
                "title": "Base API",
                "version": "1.0"
              },
              "paths": {
                "/users": {
                  "get": {
                    "operationId": "ListUsers",
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
                  "Shared": {
                    "type": "object",
                    "properties": {
                      "id": {
                        "type": "string"
                      }
                    }
                  }
                }
              }
            }
            """;

        const string secondSpec = """
            {
              "openapi": "3.0.0",
              "info": {
                "title": "Second API",
                "version": "1.0"
              },
              "paths": {
                "/orders": {
                  "get": {
                    "operationId": "ListOrders",
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
                  "Shared": {
                    "type": "object",
                    "properties": {
                      "email": {
                        "type": "string"
                      }
                    }
                  }
                }
              }
            }
            """;

        var baseDocument = await ParseJsonDocument(baseSpec);
        var secondDocument = await ParseJsonDocument(secondSpec);

        var act = () => InvokeMerge(baseDocument, secondDocument);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*duplicate schema 'Shared'*");

        baseDocument.Paths.Should().ContainSingle().Which.Key.Should().Be("/users");
        baseDocument.Components.Schemas.Should().ContainSingle().Which.Key.Should().Be("Shared");
        secondDocument.Paths.Should().ContainSingle().Which.Key.Should().Be("/orders");
        secondDocument.Components.Schemas.Should().ContainSingle().Which.Key.Should().Be("Shared");
        secondDocument.Components.Schemas["Shared"].Properties.Should().ContainKey("email");
    }

    [Test]
    public void Merge_With_Swagger2_Definition_Collision_Throws_And_Does_Not_Mutate_Dictionaries()
    {
        var target = new Dictionary<string, JsonSchema>
        {
            ["Shared"] = new()
            {
                Type = JsonObjectType.Object,
                Properties =
                {
                    ["id"] = new JsonSchemaProperty
                    {
                        Type = JsonObjectType.String
                    }
                }
            }
        };
        var incoming = new JsonSchema
        {
            Type = JsonObjectType.Object,
            Properties =
            {
                ["total"] = new JsonSchemaProperty
                {
                    Type = JsonObjectType.Integer
                }
            }
        };

        var act = () => InvokeMergeIfMissingOrThrowOnConflict(target, "Shared", incoming, "definition");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*duplicate definition 'Shared'*");

        target.Should().ContainSingle().Which.Key.Should().Be("Shared");
        target["Shared"].Properties.Should().ContainKey("id");
        incoming.Properties.Should().ContainKey("total");
    }

    [Test]
    public async Task Merge_With_Security_Scheme_Collision_Throws_And_Does_Not_Mutate_Inputs()
    {
        const string baseSpec = """
            {
              "swagger": "2.0",
              "info": {
                "title": "Base API",
                "version": "1.0"
              },
              "paths": {
                "/users": {
                  "get": {
                    "operationId": "ListUsers",
                    "responses": {
                      "200": {
                        "description": "Success"
                      }
                    }
                  }
                }
              },
              "securityDefinitions": {
                "ApiKey": {
                  "type": "apiKey",
                  "name": "X-Base-Key",
                  "in": "header"
                }
              }
            }
            """;

        const string secondSpec = """
            {
              "swagger": "2.0",
              "info": {
                "title": "Second API",
                "version": "1.0"
              },
              "paths": {
                "/orders": {
                  "get": {
                    "operationId": "ListOrders",
                    "responses": {
                      "200": {
                        "description": "Success"
                      }
                    }
                  }
                }
              },
              "securityDefinitions": {
                "ApiKey": {
                  "type": "apiKey",
                  "name": "X-Second-Key",
                  "in": "header"
                }
              }
            }
            """;

        var baseDocument = await ParseJsonDocument(baseSpec);
        var secondDocument = await ParseJsonDocument(secondSpec);

        var act = () => InvokeMerge(baseDocument, secondDocument);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*duplicate security scheme 'ApiKey'*");

        baseDocument.Paths.Should().ContainSingle().Which.Key.Should().Be("/users");
        baseDocument.SecurityDefinitions.Should().ContainSingle().Which.Key.Should().Be("ApiKey");
        baseDocument.SecurityDefinitions["ApiKey"].Name.Should().Be("X-Base-Key");
        secondDocument.Paths.Should().ContainSingle().Which.Key.Should().Be("/orders");
        secondDocument.SecurityDefinitions.Should().ContainSingle().Which.Key.Should().Be("ApiKey");
        secondDocument.SecurityDefinitions["ApiKey"].Name.Should().Be("X-Second-Key");
    }

    [Test]
    public async Task Merge_With_Equivalent_Composed_Schema_Duplicate_Ignores_Duplicate()
    {
        const string schemaJson = """
            {
              "type": "object",
              "required": [ "child", "kind" ],
              "properties": {
                "kind": {
                  "type": "string",
                  "enum": [ "basic", "advanced" ]
                },
                "child": {
                  "$ref": "#/definitions/NamedChild"
                }
              },
              "allOf": [
                {
                  "$ref": "#/definitions/BasePart"
                }
              ],
              "oneOf": [
                {
                  "$ref": "#/definitions/NamedChild"
                },
                {
                  "type": "string"
                }
              ],
              "anyOf": [
                {
                  "type": "integer"
                },
                {
                  "$ref": "#/definitions/BasePart"
                }
              ],
              "x-meta": {
                "alpha": 1,
                "zeta": null
              },
              "x-null": null,
              "definitions": {
                "NamedChild": {
                  "type": "object",
                  "required": [ "id" ],
                  "properties": {
                    "id": {
                      "type": "string"
                    }
                  }
                },
                "BasePart": {
                  "type": "object",
                  "properties": {
                    "enabled": {
                      "type": "boolean"
                    }
                  }
                }
              }
            }
            """;
        var target = new Dictionary<string, JsonSchema>
        {
            ["Shared"] = await JsonSchema.FromJsonAsync(schemaJson)
        };
        var incoming = await JsonSchema.FromJsonAsync(schemaJson);

        InvokeMergeIfMissingOrThrowOnConflict(target, "Shared", incoming, "schema");

        target.Should().ContainSingle().Which.Key.Should().Be("Shared");
        target["Shared"].RequiredProperties.Should().BeEquivalentTo(["kind", "child"]);
        target["Shared"].Properties.Should().ContainKeys("kind", "child");
        target["Shared"].Properties["kind"].Enumeration.Should().BeEquivalentTo(["basic", "advanced"]);
    }

    [Test]
    public async Task Merge_With_Base_Tags_Null_And_Swagger2_Only_Incoming_Document_Merges_Without_Creating_Tags()
    {
        const string baseSpec = """
            {
              "openapi": "3.0.0",
              "info": {
                "title": "Base API",
                "version": "1.0"
              },
              "paths": {
                "/users": {
                  "get": {
                    "operationId": "ListUsers",
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

        const string swagger2IncomingSpec = """
            {
              "swagger": "2.0",
              "info": {
                "title": "Legacy API",
                "version": "1.0"
              },
              "paths": {
                "/orders": {
                  "get": {
                    "operationId": "ListOrders",
                    "responses": {
                      "200": {
                        "description": "Success"
                      }
                    }
                  }
                }
              },
              "definitions": {
                "LegacyOrder": {
                  "type": "object",
                  "properties": {
                    "id": {
                      "type": "string"
                    }
                  }
                }
              }
            }
            """;

        var baseDocument = await ParseJsonDocument(baseSpec);
        var swagger2IncomingDocument = await ParseJsonDocument(swagger2IncomingSpec);

        var merged = InvokeMerge(baseDocument, swagger2IncomingDocument);

        merged.Tags.Should().BeEmpty();
        merged.Paths.Should().ContainKeys("/users", "/orders");
        merged.Definitions.Should().ContainKey("LegacyOrder");
        baseDocument.Tags.Should().BeEmpty();
        baseDocument.Definitions.Should().BeEmpty();
    }

    [Test]
    public void CreateCanonicalSchemaToken_Emits_Complex_Schema_Shape_Without_Nulls()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            AllowAdditionalProperties = false,
            AdditionalPropertiesSchema = new JsonSchema
            {
                Type = JsonObjectType.String
            },
            Item = new JsonSchema
            {
                Type = JsonObjectType.Integer
            },
            ExtensionData = new Dictionary<string, object?>
            {
                ["x-meta"] = new Dictionary<string, object?>
                {
                    ["beta"] = 2,
                    ["alpha"] = 1
                }
            }
        };
        schema.RequiredProperties.Add("name");
        schema.Properties["name"] = new JsonSchemaProperty
        {
            Type = JsonObjectType.String
        };
        schema.AllOf.Add(new JsonSchema { Type = JsonObjectType.String });
        schema.OneOf.Add(new JsonSchema { Type = JsonObjectType.Integer });
        schema.AnyOf.Add(new JsonSchema { Type = JsonObjectType.Boolean });
        schema.Enumeration.Add("active");
        schema.Enumeration.Add(null);

        var token = InvokeCreateCanonicalSchemaToken(schema, new HashSet<JsonSchema>());

        token.Should().BeOfType<JObject>();
        var json = (JObject)token;
        json.Should().ContainKey("type");
        json["type"]!.Value<string>().Should().Be("Object");
        json.Should().ContainKey("allowAdditionalProperties");
        json["allowAdditionalProperties"]!.Type.Should().Be(JTokenType.Boolean);
        json.Should().ContainKey("additionalProperties");
        json["additionalProperties"]!["type"]!.Value<string>().Should().Be("String");
        json.Should().ContainKey("items");
        json["items"]!["type"]!.Value<string>().Should().Be("Integer");
        json.Should().ContainKey("allOf");
        json.Should().ContainKey("oneOf");
        json.Should().ContainKey("anyOf");
        json.Should().ContainKey("required");
        json["required"]!.Should().BeOfType<JArray>().Which.Select(value => value!.Value<string>()).Should().ContainSingle().Which.Should().Be("name");
        json.Should().ContainKey("properties");
        json["properties"]!["name"]!["type"]!.Value<string>().Should().Be("String");
        json.Should().ContainKey("enum");
        json["enum"]!.Should().BeOfType<JArray>().Which.Should().HaveCount(2);
        json.Should().ContainKey("extensions");
        json["extensions"]!["x-meta"]!["alpha"]!.Value<int>().Should().Be(1);
    }

    [Test]
    public void CreateCanonicalJsonToken_Falls_Back_To_Schema_Canonicalization_For_ReferenceSchema()
    {
        var schema = new JsonSchema
        {
            Reference = new JsonSchema
            {
                Type = JsonObjectType.String
            }
        };

        var token = InvokeCreateCanonicalJsonToken(schema);

        token["type"]!.Value<string>().Should().Be("String");
    }

    [Test]
    public void CreateCanonicalSchemaToken_For_Visited_Reference_Uses_Placeholder()
    {
        var referencedSchema = new JsonSchema
        {
            Type = JsonObjectType.Object
        };
        var schema = new JsonSchema
        {
            Reference = referencedSchema
        };

        var token = InvokeCreateCanonicalSchemaToken(schema, new HashSet<JsonSchema> { referencedSchema });

        token.Should().BeOfType<JObject>();
        ((JObject)token).Properties().Should().ContainSingle();
        token["$ref"]!.Value<string>().Should().Be("#");
    }

    [Test]
    public void CreateCanonicalSchemaToken_For_Unvisited_Reference_Canonicalizes_Referenced_Schema()
    {
        var referencedSchema = new JsonSchema
        {
            Type = JsonObjectType.Object
        };
        var schema = new JsonSchema
        {
            Reference = referencedSchema
        };

        var token = InvokeCreateCanonicalSchemaToken(schema, new HashSet<JsonSchema>());

        token["type"]!.Value<string>().Should().Be("Object");
    }

    [Test]
    public void CreateCanonicalSchemaToken_When_ActualSchema_Is_Already_Visited_Uses_Placeholder()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object
        };

        var token = InvokeCreateCanonicalSchemaToken(schema, new HashSet<JsonSchema> { schema });

        token["$ref"]!.Value<string>().Should().Be("#");
    }

    [Test]
    public void GetDefinitionName_Decodes_Url_Encoded_Names()
    {
        var schema = new JsonSchema();
        ((NJsonSchema.References.IJsonReferenceBase)schema).ReferencePath = "#/definitions/Problem%20Details";

        InvokeGetDefinitionName(schema).Should().Be("Problem Details");
    }

    [Test]
    public void GetDefinitionName_Without_Path_Separator_Returns_Null()
    {
        var schema = new JsonSchema();
        ((NJsonSchema.References.IJsonReferenceBase)schema).ReferencePath = "ProblemDetails";

        InvokeGetDefinitionName(schema).Should().BeNull();
    }

    [Test]
    public async Task CreateOpenApiJson_Serializes_OpenApiDocuments_And_Pocos()
    {
        var document = await ParseJsonDocument("""
            {
              "openapi": "3.0.0",
              "info": {
                "title": "Coverage API",
                "version": "1.0"
              },
              "paths": {
                "/coverage": {
                  "get": {
                    "operationId": "GetCoverage",
                    "responses": {
                      "200": {
                        "description": "Success"
                      }
                    }
                  }
                }
              }
            }
            """);

        InvokeCreateOpenApiJson(document).Should().Contain("\"/coverage\"");
        InvokeCreateOpenApiJson(new { Name = "Lambert", Count = 2 })
            .Should()
            .Be("""{"Name":"Lambert","Count":2}""");
    }

    [Test]
    public void AreEquivalent_Returns_False_When_Canonicalization_Throws()
    {
        var left = new SelfReferencingValue();
        left.Self = left;

        var right = new SelfReferencingValue();
        right.Self = right;

        InvokeAreEquivalent(left, right).Should().BeFalse();
    }

    [Test]
    public void RemoveNullProperties_Removes_Only_Null_Values()
    {
        var json = new JObject
        {
            ["type"] = "object",
            ["format"] = JValue.CreateNull(),
            ["description"] = JValue.CreateNull(),
            ["properties"] = new JObject()
        };

        var normalized = InvokeRemoveNullProperties(json);

        normalized.Should().BeSameAs(json);
        normalized.Should().ContainKey("type");
        normalized.Should().ContainKey("properties");
        normalized.Should().NotContainKeys("format", "description");
    }

    [Test]
    public async Task AddReferencedSchemas_Does_Not_Overwrite_Existing_Named_Schema()
    {
        var rootSchema = new JsonSchema
        {
            Type = JsonObjectType.Object
        };
        var referencedChildSchema = await JsonSchema.FromJsonAsync("""
            {
              "type": "object",
              "properties": {
                "id": {
                  "type": "string"
                }
              }
            }
            """);
        var referencedChild = new JsonSchemaProperty
        {
            Reference = referencedChildSchema
        };
        ((NJsonSchema.References.IJsonReferenceBase)referencedChild).ReferencePath = "#/definitions/NamedChild";
        rootSchema.Properties["child"] = referencedChild;

        var existingDefinition = new JsonSchema
        {
            Type = JsonObjectType.Integer
        };
        var definitions = new Dictionary<string, JsonSchema>
        {
            ["NamedChild"] = existingDefinition
        };

        InvokeAddReferencedSchemas(definitions, rootSchema);

        definitions.Should().ContainSingle().Which.Value.Should().BeSameAs(existingDefinition);
    }

    [Test]
    public async Task AddReferencedSchemas_Adds_Named_Property_Schema_To_Empty_Definitions()
    {
        var rootSchema = new JsonSchema
        {
            Type = JsonObjectType.Object
        };
        var referencedChildSchema = await JsonSchema.FromJsonAsync("""
            {
              "type": "object",
              "properties": {
                "name": {
                  "type": "string"
                }
              }
            }
            """);
        var referencedChild = new JsonSchemaProperty
        {
            Reference = referencedChildSchema
        };
        ((NJsonSchema.References.IJsonReferenceBase)referencedChild).ReferencePath = "#/definitions/NamedChild";
        rootSchema.Properties["child"] = referencedChild;

        var definitions = new Dictionary<string, JsonSchema>();

        InvokeAddReferencedSchemas(definitions, rootSchema);

        definitions.Should().ContainSingle().Which.Key.Should().Be("NamedChild");
        definitions["NamedChild"].Should().BeSameAs(referencedChildSchema);
    }

    [Test]
    public void CreateCanonicalSchemaToken_Preserves_Null_And_NonNull_Extension_Values()
    {
        var schema = new JsonSchema
        {
            Type = JsonObjectType.Object,
            ExtensionData = new Dictionary<string, object?>
            {
                ["x-null"] = null,
                ["x-meta"] = new Dictionary<string, object?>
                {
                    ["beta"] = 2
                }
            }
        };

        var token = InvokeCreateCanonicalSchemaToken(schema, new HashSet<JsonSchema>());

        token["extensions"]!["x-null"]!.Type.Should().Be(JTokenType.Null);
        token["extensions"]!["x-meta"]!["beta"]!.Value<int>().Should().Be(2);
    }

    [Test]
    public void AddReferencedSchemas_Adds_Named_Item_Schemas_From_Items_Collection()
    {
        var rootSchema = new JsonSchema
        {
            Type = JsonObjectType.Array
        };
        var referencedItemSchema = new JsonSchema
        {
            Type = JsonObjectType.String
        };
        var namedItemSchema = new JsonSchemaProperty
        {
            Reference = referencedItemSchema
        };
        ((NJsonSchema.References.IJsonReferenceBase)namedItemSchema).ReferencePath = "#/definitions/NamedItem";
        rootSchema.Items.Add(namedItemSchema);

        var definitions = new Dictionary<string, JsonSchema>();

        InvokeAddReferencedSchemas(definitions, rootSchema);

        definitions.Should().ContainSingle().Which.Key.Should().Be("NamedItem");
        definitions["NamedItem"].Should().BeSameAs(referencedItemSchema);
    }

    [Test]
    public async Task ParseJsonDocument_Normalizes_Missing_Components_Schemas_To_Empty_Collections()
    {
        var document = await ParseJsonDocument("""
            {
              "openapi": "3.0.0",
              "info": {
                "title": "Components Coverage API",
                "version": "1.0"
              },
              "paths": {
                "/orders": {
                  "get": {
                    "operationId": "ListOrders",
                    "responses": {
                      "200": {
                        "description": "Success"
                      }
                    }
                  }
                }
              }
            }
            """);

        document.Components.Should().NotBeNull();
        document.Components.Schemas.Should().NotBeNull();
        document.Components.Schemas.Should().BeEmpty();
    }

    [Test]
    public async Task ParseAndCloneDocument_Normalize_Missing_Tags_To_Empty_Collections()
    {
        var parsedDocument = await ParseJsonDocument("""
            {
              "openapi": "3.0.0",
              "info": {
                "title": "Tag Coverage API",
                "version": "1.0"
              },
              "paths": {
                "/users": {
                  "get": {
                    "operationId": "ListUsers",
                    "responses": {
                      "200": {
                        "description": "Success"
                      }
                    }
                  }
                }
              }
            }
            """);
        var clonedDocument = await OpenApiDocument.FromJsonAsync(parsedDocument.ToJson());

        parsedDocument.Tags.Should().NotBeNull();
        parsedDocument.Tags.Should().BeEmpty();
        clonedDocument.Tags.Should().NotBeNull();
        clonedDocument.Tags.Should().BeEmpty();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2)]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3)]
    public async Task ParseAndCloneDocument_RoundTrips_SchemaType_And_Collections(SampleOpenSpecifications version)
    {
        var parsedDocument = await ParseJsonDocument(EmbeddedResources.GetSwaggerPetstore(version));
        var clonedDocument = await OpenApiDocument.FromJsonAsync(parsedDocument.ToJson());

        clonedDocument.SchemaType.Should().Be(parsedDocument.SchemaType);
        clonedDocument.Paths.Keys.Should().BeEquivalentTo(parsedDocument.Paths.Keys);
        clonedDocument.Tags.Select(tag => tag.Name).Should().BeEquivalentTo(parsedDocument.Tags.Select(tag => tag.Name));
        clonedDocument.Components.Schemas.Keys.Should().BeEquivalentTo(parsedDocument.Components.Schemas.Keys);
        clonedDocument.Definitions.Keys.Should().BeEquivalentTo(parsedDocument.Definitions.Keys);
        clonedDocument.SecurityDefinitions.Keys.Should().BeEquivalentTo(parsedDocument.SecurityDefinitions.Keys);
    }

    [Test]
    public async Task Merge_Preserves_OpenApi3_Base_SchemaType_For_Cloned_Document()
    {
        var baseDocument = await ParseJsonDocument("""
            {
              "openapi": "3.0.0",
              "info": {
                "title": "OpenAPI Base",
                "version": "1.0"
              },
              "paths": {
                "/users": {
                  "get": {
                    "operationId": "ListUsers",
                    "responses": {
                      "200": {
                        "description": "Success"
                      }
                    }
                  }
                }
              }
            }
            """);
        var secondDocument = await ParseJsonDocument("""
            {
              "swagger": "2.0",
              "info": {
                "title": "Swagger Incoming",
                "version": "1.0"
              },
              "paths": {
                "/orders": {
                  "get": {
                    "operationId": "ListOrders",
                    "responses": {
                      "200": {
                        "description": "Success"
                      }
                    }
                  }
                }
              }
            }
            """);

        var merged = InvokeMerge(baseDocument, secondDocument);

        merged.SchemaType.Should().Be(SchemaType.OpenApi3);
        JObject.Parse(merged.ToJson())["openapi"]!.Value<string>().Should().Be("3.0.0");
    }

    [Test]
    public async Task Merge_Preserves_Swagger2_Base_SchemaType_For_Cloned_Document()
    {
        var baseDocument = await ParseJsonDocument("""
            {
              "swagger": "2.0",
              "info": {
                "title": "Swagger Base",
                "version": "1.0"
              },
              "paths": {
                "/users": {
                  "get": {
                    "operationId": "ListUsers",
                    "responses": {
                      "200": {
                        "description": "Success"
                      }
                    }
                  }
                }
              }
            }
            """);
        var secondDocument = await ParseJsonDocument("""
            {
              "openapi": "3.0.0",
              "info": {
                "title": "OpenAPI Incoming",
                "version": "1.0"
              },
              "paths": {
                "/orders": {
                  "get": {
                    "operationId": "ListOrders",
                    "responses": {
                      "200": {
                        "description": "Success"
                      }
                    }
                  }
                }
              }
            }
            """);

        var merged = InvokeMerge(baseDocument, secondDocument);

        merged.SchemaType.Should().Be(SchemaType.Swagger2);
        JObject.Parse(merged.ToJson())["swagger"]!.Value<string>().Should().Be("2.0");
    }

    private static Task<OpenApiDocument> ParseJsonDocument(string json)
        => OpenApiDocument.FromJsonAsync(json);

    private static OpenApiDocument InvokeMerge(params OpenApiDocument[] documents)
    {
        var mergeMethod = typeof(OpenApiDocumentFactory).GetMethod("Merge", BindingFlags.NonPublic | BindingFlags.Static);

        mergeMethod.Should().NotBeNull();

        try
        {
            return (OpenApiDocument)mergeMethod!.Invoke(null, [documents])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            throw exception.InnerException;
        }
    }

    private static void InvokeMergeIfMissingOrThrowOnConflict<TValue>(
        IDictionary<string, TValue> target,
        string key,
        TValue value,
        string itemType)
    {
        var mergeMethod = typeof(OpenApiDocumentFactory)
            .GetMethod("MergeIfMissingOrThrowOnConflict", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(TValue));

        try
        {
            mergeMethod.Invoke(null, [target, key, value!, itemType]);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            throw exception.InnerException;
        }
    }

    private static bool InvokeAreEquivalent<TValue>(TValue existingValue, TValue incomingValue)
    {
        var areEquivalentMethod = typeof(OpenApiDocumentFactory)
            .GetMethod("AreEquivalent", BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(TValue));

        return (bool)areEquivalentMethod.Invoke(null, [existingValue!, incomingValue!])!;
    }

    private static JToken InvokeCreateCanonicalSchemaToken(JsonSchema schema, ISet<JsonSchema> visited)
    {
        var createCanonicalSchemaTokenMethod = typeof(OpenApiDocumentFactory)
            .GetMethod("CreateCanonicalSchemaToken", BindingFlags.NonPublic | BindingFlags.Static)!;

        return (JToken)createCanonicalSchemaTokenMethod.Invoke(null, [schema, visited])!;
    }

    private static JToken InvokeCreateCanonicalJsonToken(object value)
    {
        var createCanonicalJsonTokenMethod = typeof(OpenApiDocumentFactory)
            .GetMethod("CreateCanonicalJsonToken", BindingFlags.NonPublic | BindingFlags.Static)!;

        return (JToken)createCanonicalJsonTokenMethod.Invoke(null, [value])!;
    }

    private static void InvokeAddReferencedSchemas(IDictionary<string, JsonSchema> definitions, JsonSchema schema)
    {
        var addReferencedSchemasMethod = typeof(OpenApiDocumentFactory)
            .GetMethod("AddReferencedSchemas", BindingFlags.NonPublic | BindingFlags.Static)!;

        addReferencedSchemasMethod.Invoke(null, [definitions, schema]);
    }

    private static string? InvokeGetDefinitionName(JsonSchema schema)
    {
        var getDefinitionNameMethod = typeof(OpenApiDocumentFactory)
            .GetMethod("GetDefinitionName", BindingFlags.NonPublic | BindingFlags.Static)!;

        return (string?)getDefinitionNameMethod.Invoke(null, [schema]);
    }

    private static string InvokeCreateOpenApiJson(object value)
    {
        var createOpenApiJsonMethod = typeof(OpenApiDocumentFactory)
            .GetMethod("CreateOpenApiJson", BindingFlags.NonPublic | BindingFlags.Static)!;

        return (string)createOpenApiJsonMethod.Invoke(null, [value])!;
    }

    private static JObject InvokeRemoveNullProperties(JObject json)
    {
        var removeNullPropertiesMethod = typeof(OpenApiDocumentFactory)
            .GetMethod("RemoveNullProperties", BindingFlags.NonPublic | BindingFlags.Static)!;

        return (JObject)removeNullPropertiesMethod.Invoke(null, [json])!;
    }

    private sealed class SelfReferencingValue
    {
        public SelfReferencingValue? Self { get; set; }
    }
}
