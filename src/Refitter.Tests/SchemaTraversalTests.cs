using AwesomeAssertions;
using NJsonSchema;
using NSwag;
using Refitter.Core;

namespace Refitter.Tests;

public class SchemaTraversalTests
{
    [Test]
    public async Task SchemaWalker_Visits_Schemas_Reached_Through_Definitions()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
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
            """);

        var visited = new List<JsonSchema>();
        SchemaWalker.TraverseDocumentSchemas(document, visited.Add);

        visited.Select(s => s.Type).Should().Contain(JsonObjectType.String);
    }

    [Test]
    public async Task SchemaWalker_Visits_Schemas_Reached_Through_Tuple_Items()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
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
            """);

        // Tuple validation is not expressible in OpenAPI 3.0, so build the item schemas directly
        var tupleItem = new JsonSchema { Type = JsonObjectType.String };
        document.Components.Schemas["Root"].Items.Add(tupleItem);

        var visited = new List<JsonSchema>();
        SchemaWalker.TraverseDocumentSchemas(document, visited.Add);

        visited.Should().Contain(tupleItem);
    }

    [Test]
    public async Task SchemaWalker_Visits_Schemas_Reached_Through_Path_Parameters()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
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
            """);

        var visited = new List<JsonSchema>();
        SchemaWalker.TraverseDocumentSchemas(document, visited.Add);

        visited.Select(s => s.Type).Should().Contain(JsonObjectType.Boolean);
    }

    [Test]
    public async Task SchemaCleaner_Keeps_Schemas_Reached_Through_Dictionary_Key()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
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
            """);

        new SchemaCleaner(document, []).RemoveUnreferencedSchema();

        document.Components.Schemas.Should().ContainKey("Root");
        document.Components.Schemas.Should().ContainKey("Value");
        document.Components.Schemas.Should().NotContainKey("Unused");
    }

    [Test]
    public async Task SchemaCleaner_Handles_Null_Schema_Argument()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {}
            }
            """);

        var cleaner = new SchemaCleaner(document, []);

        var act = () => cleaner.RemoveUnreferencedSchema();

        act.Should().NotThrow();
    }
}
