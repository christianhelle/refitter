using FluentAssertions;
using NJsonSchema;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;


public class CustomIntegerTypeMutatorTests
{
    [Test]
    public async Task Mutate_WithInt64_AddsInt64FormatToIntegerSchemasWithoutFormat()
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
                      "count": { "type": "integer" }
                    }
                  }
                }
              }
            }
            """);

        var sut = new CustomIntegerTypeMutator(IntegerType.Int64);
        sut.Mutate(document);

        var schema = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["count"]
            .ActualSchema;

        schema.Format.Should().Be("int64");
    }

    [Test]
    public async Task Mutate_WithInt32_DoesNotAddFormat()
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
                      "count": { "type": "integer" }
                    }
                  }
                }
              }
            }
            """);

        var sut = new CustomIntegerTypeMutator(IntegerType.Int32);
        sut.Mutate(document);

        var schema = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["count"]
            .ActualSchema;

        schema.Format.Should().BeNullOrEmpty();
    }

    [Test]
    public async Task Mutate_WithInt64_DoesNotChangeIntegerSchemaWithExistingFormat()
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
                      "count": { "type": "integer", "format": "int32" }
                    }
                  }
                }
              }
            }
            """);

        var sut = new CustomIntegerTypeMutator(IntegerType.Int64);
        sut.Mutate(document);

        var schema = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["count"]
            .ActualSchema;

        schema.Format.Should().Be("int32");
    }

    [Test]
    public async Task Mutate_WithInt64_OnlyAffectsIntegerSchemas()
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
                      "count": { "type": "integer" },
                      "name": { "type": "string" },
                      "price": { "type": "number" }
                    }
                  }
                }
              }
            }
            """);

        var sut = new CustomIntegerTypeMutator(IntegerType.Int64);
        sut.Mutate(document);

        var props = document.Components!.Schemas["TestModel"].ActualSchema.Properties;
        props["count"].ActualSchema.Format.Should().Be("int64");
        props["name"].ActualSchema.Format.Should().BeNullOrEmpty();
        props["price"].ActualSchema.Format.Should().BeNullOrEmpty();
    }

    [Test]
    public async Task Mutate_WithInt64_AppliesToArrayItemSchemas()
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
                      "items": {
                        "type": "array",
                        "items": { "type": "integer" }
                      }
                    }
                  }
                }
              }
            }
            """);

        var sut = new CustomIntegerTypeMutator(IntegerType.Int64);
        sut.Mutate(document);

        var items = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["items"]
            .ActualSchema.Item!.ActualSchema;

        items.Format.Should().Be("int64");
    }
}
