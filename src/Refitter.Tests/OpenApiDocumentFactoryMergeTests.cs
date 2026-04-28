using System.Reflection;
using FluentAssertions;
using NJsonSchema;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

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
}
