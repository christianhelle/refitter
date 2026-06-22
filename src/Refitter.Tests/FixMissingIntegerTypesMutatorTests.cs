using FluentAssertions;
using NJsonSchema;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;


public class FixMissingIntegerTypesMutatorTests
{
    [Test]
    public async Task Mutate_WithFormatInt32AndNoType_SetsTypeToInteger()
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
                      "formattedId": { "format": "int32" }
                    }
                  }
                }
              }
            }
            """);

        var sut = new FixMissingIntegerTypesMutator();
        sut.Mutate(document);

        var schema = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["formattedId"]
            .ActualSchema;

        schema.Type.Should().Be(JsonObjectType.Integer);
    }

    [Test]
    public async Task Mutate_WithFormatInt64AndNoType_SetsTypeToInteger()
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
                      "bigId": { "format": "int64" }
                    }
                  }
                }
              }
            }
            """);

        var sut = new FixMissingIntegerTypesMutator();
        sut.Mutate(document);

        var schema = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["bigId"]
            .ActualSchema;

        schema.Type.Should().Be(JsonObjectType.Integer);
    }

    [Test]
    public async Task Mutate_WithFormatFloatAndNoType_SetsTypeToNumber()
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
                      "price": { "format": "float" }
                    }
                  }
                }
              }
            }
            """);

        var sut = new FixMissingIntegerTypesMutator();
        sut.Mutate(document);

        var schema = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["price"]
            .ActualSchema;

        schema.Type.Should().Be(JsonObjectType.Number);
    }

    [Test]
    public async Task Mutate_WithFormatDoubleAndNoType_SetsTypeToNumber()
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
                      "score": { "format": "double" }
                    }
                  }
                }
              }
            }
            """);

        var sut = new FixMissingIntegerTypesMutator();
        sut.Mutate(document);

        var schema = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["score"]
            .ActualSchema;

        schema.Type.Should().Be(JsonObjectType.Number);
    }

    [Test]
    public async Task Mutate_WithFormatInt32AndExistingType_DoesNotChangeType()
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

        var sut = new FixMissingIntegerTypesMutator();
        sut.Mutate(document);

        var schema = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["count"]
            .ActualSchema;

        schema.Type.Should().Be(JsonObjectType.Integer);
    }

    [Test]
    public async Task Mutate_WithoutFormat_DoesNotChangeType()
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
                      "name": { "type": "string" }
                    }
                  }
                }
              }
            }
            """);

        var expectedType = document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["name"]
            .ActualSchema.Type;

        var sut = new FixMissingIntegerTypesMutator();
        sut.Mutate(document);

        document.Components!.Schemas["TestModel"]
            .ActualSchema.Properties["name"]
            .ActualSchema.Type.Should().Be(expectedType);
    }
}
