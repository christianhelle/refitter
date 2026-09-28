using AwesomeAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;


public class FlattenPrimitiveAllOfMutatorTests
{
    [Test]
    public async Task Mutate_WithSingleStringAllOf_CollapsesToString()
    {
        var document = ApiDocumentLoader.Load("""
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
            """, null, isYaml: false);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        var parent = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["parent"].ActualSchema;

        parent.Type.Should().Be(ApiObjectTypes.String);
        parent.AllOf.Should().BeEmpty();
        parent.Description.Should().Be("the parent id");
    }

    [Test]
    [Arguments("integer", "int64")]
    [Arguments("number", "double")]
    [Arguments("boolean", null)]
    public async Task Mutate_WithSinglePrimitiveAllOf_CollapsesToPrimitive(
        string jsonType,
        string? format)
    {
        var expectedType = ApiObjectTypeExtensions.Parse(jsonType);
        var formatJson = format == null ? "" : $", \"format\": \"{format}\"";
        var document = ApiDocumentLoader.Load($$"""
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
            """, null, isYaml: false);

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
        var document = ApiDocumentLoader.Load("""
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
            """, null, isYaml: false);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        var priority = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["priority"].ActualSchema;

        priority.Type.Should().Be(ApiObjectTypes.String);
        priority.AllOf.Should().BeEmpty();
        priority.IsEnumeration.Should().BeTrue();
        priority.Enumeration.Should().BeEquivalentTo(new[] { "low", "medium", "high" });
    }

    [Test]
    public async Task Mutate_WithRefToPrimitiveAllOf_CollapsesToPrimitive()
    {
        var document = ApiDocumentLoader.Load("""
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
            """, null, isYaml: false);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        var id = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["id"].ActualSchema;

        id.Type.Should().Be(ApiObjectTypes.String);
        id.AllOf.Should().BeEmpty();
    }

    [Test]
    public async Task Mutate_WithRefToObjectAllOf_LeavesSchemaUnchanged()
    {
        var document = ApiDocumentLoader.Load("""
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
            """, null, isYaml: false);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        var actual = document.Components!.Schemas["TestModel"].ActualSchema;
        actual.Type.Should().Be(ApiObjectTypes.Object);
        actual.Properties.Should().ContainKey("name");
    }

    [Test]
    public async Task Mutate_WithObjectPropertiesAndAllOf_LeavesSchemaUnchanged()
    {
        var document = ApiDocumentLoader.Load("""
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
            """, null, isYaml: false);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        document.Components!.Schemas["TestModel"].ActualSchema.AllOf
            .Should().HaveCount(1);
    }

    [Test]
    public async Task Mutate_WithMultipleAllOfItems_LeavesSchemaUnchanged()
    {
        var document = ApiDocumentLoader.Load("""
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
            """, null, isYaml: false);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        document.Components!.Schemas["TestModel"].ActualSchema.AllOf
            .Should().HaveCount(2);
    }

    [Test]
    public async Task Mutate_WithSingleEnumAllOf_PreservesEnumerationNames()
    {
        var document = ApiDocumentLoader.Load("""
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
                          {
                            "type": "string",
                            "enum": ["low", "medium", "high"],
                            "x-enumNames": ["Low", "Medium", "High"]
                          }
                        ]
                      }
                    }
                  }
                }
              }
            }
            """, null, isYaml: false);

        new FlattenPrimitiveAllOfMutator().Mutate(document);

        var priority = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["priority"].ActualSchema;

        priority.EnumerationNames.Should().BeEquivalentTo(new[] { "Low", "Medium", "High" });
    }
}
