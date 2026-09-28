using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

public class SchemaTraversalTests
{
    [Test]
    public async Task SchemaWalker_Visits_Schemas_Reached_Through_Definitions()
    {
        var document = ApiDocumentLoader.Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Root": {
                    "type": "object",
                    "definitions": {
                      "Nested": { "type": "string" }
                    }
                  }
                }
              }
            }
            """, null, isYaml: false);

        var visited = new List<ApiSchema>();
        SchemaWalker.TraverseDocumentSchemas(document, visited.Add);

        visited.Select(s => s.Type).Should().Contain(ApiObjectTypes.String);
    }

    [Test]
    public async Task SchemaWalker_Visits_Schemas_Reached_Through_Tuple_Items()
    {
        var document = ApiDocumentLoader.Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Root": { "type": "array" }
                }
              }
            }
            """, null, isYaml: false);

        // Tuple validation is not expressible in OpenAPI 3.0, so build the item schemas directly
        var tupleItem = new ApiSchema { Type = ApiObjectTypes.String };
        document.Components.Schemas["Root"].Items.Add(tupleItem);

        var visited = new List<ApiSchema>();
        SchemaWalker.TraverseDocumentSchemas(document, visited.Add);

        visited.Should().Contain(tupleItem);
    }

    [Test]
    public async Task SchemaWalker_Visits_Schemas_Reached_Through_Path_Parameters()
    {
        var document = ApiDocumentLoader.Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/pets": {
                  "parameters": [
                    {
                      "name": "filter",
                      "in": "query",
                      "schema": { "type": "boolean" }
                    }
                  ]
                }
              }
            }
            """, null, isYaml: false);

        var visited = new List<ApiSchema>();
        SchemaWalker.TraverseDocumentSchemas(document, visited.Add);

        visited.Select(s => s.Type).Should().Contain(ApiObjectTypes.Boolean);
    }

    [Test]
    public async Task SchemaCleaner_Keeps_Schemas_Reached_Through_Additional_Properties()
    {
        var document = ApiDocumentLoader.Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/pets": {
                  "get": {
                    "operationId": "GetPets",
                    "responses": {
                      "200": {
                        "description": "ok",
                        "content": {
                          "application/json": {
                            "schema": { "$ref": "#/components/schemas/Root" }
                          }
                        }
                      }
                    }
                  }
                }
              },
              "components": {
                "schemas": {
                  "Root": {
                    "type": "object",
                    "additionalProperties": { "$ref": "#/components/schemas/Value" }
                  },
                  "Value": { "type": "string" },
                  "Unused": { "type": "integer" }
                }
              }
            }
            """, null, isYaml: false);

        new SchemaCleaner(document, []).RemoveUnreferencedSchema();

        document.Components.Schemas.Should().ContainKey("Root");
        document.Components.Schemas.Should().ContainKey("Value");
        document.Components.Schemas.Should().NotContainKey("Unused");
    }

    [Test]
    public async Task SchemaCleaner_Keeps_Schemas_Reached_Through_Dictionary_Key()
    {
        var document = ApiDocumentLoader.Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/pets": {
                  "get": {
                    "operationId": "GetPets",
                    "responses": {
                      "200": {
                        "description": "ok",
                        "content": {
                          "application/json": {
                            "schema": { "$ref": "#/components/schemas/Root" }
                          }
                        }
                      }
                    }
                  }
                }
              },
              "components": {
                "schemas": {
                  "Root": { "type": "object" },
                  "Key": { "type": "string", "enum": [ "a", "b" ] },
                  "Unused": { "type": "integer" }
                }
              }
            }
            """, null, isYaml: false);

        // x-dictionaryKey has no OpenAPI 3.0 representation, so wire it up directly
        document.Components.Schemas["Root"].DictionaryKey = document.Components.Schemas["Key"];

        new SchemaCleaner(document, []).RemoveUnreferencedSchema();

        document.Components.Schemas.Should().ContainKey("Root");
        document.Components.Schemas.Should().ContainKey("Key");
        document.Components.Schemas.Should().NotContainKey("Unused");
    }

    [Test]
    public async Task SchemaCleaner_Ignores_Response_Content_Without_Schema()
    {
        var document = ApiDocumentLoader.Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/pets": {
                  "get": {
                    "operationId": "GetPets",
                    "responses": {
                      "200": {
                        "description": "ok",
                        "content": {
                          "application/json": {}
                        }
                      }
                    }
                  }
                }
              },
              "components": {
                "schemas": {
                  "Unused": { "type": "integer" }
                }
              }
            }
            """, null, isYaml: false);

        document.GetOperations()
            .Single()
            .Operation.ActualResponses["200"]
            .Content["application/json"]
            .Schema.Should().BeNull();

        var act = () => new SchemaCleaner(document, []).RemoveUnreferencedSchema();

        act.Should().NotThrow();
        document.Components.Schemas.Should().NotContainKey("Unused");
    }

    [Test]
    public async Task SchemaCleaner_Keeps_Schemas_Reached_Through_Path_Item_Parameters()
    {
        var document = ApiDocumentLoader.Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/pets": {
                  "parameters": [
                    {
                      "name": "filter",
                      "in": "query",
                      "schema": { "$ref": "#/components/schemas/Filter" }
                    }
                  ]
                }
              },
              "components": {
                "schemas": {
                  "Filter": { "type": "string", "enum": [ "a", "b" ] },
                  "Unused": { "type": "integer" }
                }
              }
            }
            """, null, isYaml: false);

        new SchemaCleaner(document, []).RemoveUnreferencedSchema();

        document.Components.Schemas.Should().ContainKey("Filter");
        document.Components.Schemas.Should().NotContainKey("Unused");
    }

    [Test]
    public async Task SchemaCleaner_Keeps_Schemas_Reached_Through_Tuple_Items()
    {
        var document = await CreateDocumentWithReferencedRootAsync();

        // Tuple validation is not expressible in OpenAPI 3.0, so wire up the item schema directly
        document.Components.Schemas["Root"].Items.Add(
            new ApiSchema { Reference = document.Components.Schemas["Value"] });

        new SchemaCleaner(document, []).RemoveUnreferencedSchema();

        document.Components.Schemas.Should().ContainKey("Value");
        document.Components.Schemas.Should().NotContainKey("Unused");
    }

    [Test]
    public async Task SchemaCleaner_Keeps_Schemas_Reached_Through_Definitions()
    {
        var document = await CreateDocumentWithReferencedRootAsync();

        document.Components.Schemas["Root"].Definitions["Nested"] =
            new ApiSchema { Reference = document.Components.Schemas["Value"] };

        new SchemaCleaner(document, []).RemoveUnreferencedSchema();

        document.Components.Schemas.Should().ContainKey("Value");
        document.Components.Schemas.Should().NotContainKey("Unused");
    }

    private static Task<ApiDocument> CreateDocumentWithReferencedRootAsync() =>
        Task.FromResult(ApiDocumentLoader.Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/pets": {
                  "get": {
                    "operationId": "GetPets",
                    "responses": {
                      "200": {
                        "description": "ok",
                        "content": {
                          "application/json": {
                            "schema": { "$ref": "#/components/schemas/Root" }
                          }
                        }
                      }
                    }
                  }
                }
              },
              "components": {
                "schemas": {
                  "Root": { "type": "object" },
                  "Value": { "type": "string" },
                  "Unused": { "type": "integer" }
                }
              }
            }
            """, null, isYaml: false));
}
