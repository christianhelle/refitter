using FluentAssertions;
using NJsonSchema;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;


public class FlattenPrimitiveAllOfMutatorTests
{
    [Test]
    public async Task Mutate_WithSingleStringAllOf_CollapsesToString()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "TestModel": {
                    "type": "object",
                    "properties": {
                      "parent": {
                        "allOf": [
                          { "type": "string", "description": "the parent id" }
                        ]
                      }
                    }
                  }
                }
              }
            }
            """);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        var parent = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["parent"].ActualSchema;

        parent.Type.Should().Be(JsonObjectType.String);
        parent.AllOf.Should().BeEmpty();
        parent.Description.Should().Be("the parent id");
    }

    [Test]
    [Arguments("integer", "int64", JsonObjectType.Integer)]
    [Arguments("number", "double", JsonObjectType.Number)]
    [Arguments("boolean", null, JsonObjectType.Boolean)]
    public async Task Mutate_WithSinglePrimitiveAllOf_CollapsesToPrimitive(
        string jsonType,
        string? format,
        JsonObjectType expectedType)
    {
        var formatJson = format == null ? "" : $", \"format\": \"{format}\"";
        var document = await OpenApiDocument.FromJsonAsync($$"""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "TestModel": {
                    "type": "object",
                    "properties": {
                      "value": {
                        "allOf": [
                          { "type": "{{jsonType}}"{{formatJson}} }
                        ]
                      }
                    }
                  }
                }
              }
            }
            """);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        var value = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["value"].ActualSchema;

        value.Type.Should().Be(expectedType);
        value.AllOf.Should().BeEmpty();
        if (format != null)
            value.Format.Should().Be(format);
    }

    [Test]
    public async Task Mutate_WithSingleEnumAllOf_PreservesEnumeration()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "TestModel": {
                    "type": "object",
                    "properties": {
                      "priority": {
                        "allOf": [
                          { "type": "string", "enum": ["low", "medium", "high"] }
                        ]
                      }
                    }
                  }
                }
              }
            }
            """);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        var priority = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["priority"].ActualSchema;

        priority.Type.Should().Be(JsonObjectType.String);
        priority.AllOf.Should().BeEmpty();
        priority.IsEnumeration.Should().BeTrue();
        priority.Enumeration.Should().BeEquivalentTo(new[] { "low", "medium", "high" });
    }

    [Test]
    public async Task Mutate_WithRefToPrimitiveAllOf_CollapsesToPrimitive()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Gid": { "type": "string" },
                  "TestModel": {
                    "type": "object",
                    "properties": {
                      "id": {
                        "allOf": [
                          { "$ref": "#/components/schemas/Gid" }
                        ]
                      }
                    }
                  }
                }
              }
            }
            """);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        var id = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["id"].ActualSchema;

        id.Type.Should().Be(JsonObjectType.String);
        id.AllOf.Should().BeEmpty();
    }

    [Test]
    public async Task Mutate_WithRefToObjectAllOf_LeavesSchemaUnchanged()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Base": {
                    "type": "object",
                    "properties": { "name": { "type": "string" } }
                  },
                  "TestModel": {
                    "allOf": [
                      { "$ref": "#/components/schemas/Base" }
                    ]
                  }
                }
              }
            }
            """);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        var actual = document.Components!.Schemas["TestModel"].ActualSchema;
        actual.Type.Should().Be(JsonObjectType.Object);
        actual.Properties.Should().ContainKey("name");
    }

    [Test]
    public async Task Mutate_WithObjectPropertiesAndAllOf_LeavesSchemaUnchanged()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "TestModel": {
                    "properties": { "name": { "type": "string" } },
                    "allOf": [
                      { "type": "string" }
                    ]
                  }
                }
              }
            }
            """);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        document.Components!.Schemas["TestModel"].ActualSchema.AllOf
            .Should().HaveCount(1);
    }

    [Test]
    public async Task Mutate_WithMultipleAllOfItems_LeavesSchemaUnchanged()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Base": {
                    "type": "object",
                    "properties": { "name": { "type": "string" } }
                  },
                  "TestModel": {
                    "allOf": [
                      { "$ref": "#/components/schemas/Base" },
                      { "type": "string" }
                    ]
                  }
                }
              }
            }
            """);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        document.Components!.Schemas["TestModel"].ActualSchema.AllOf
            .Should().HaveCount(2);
    }
}
