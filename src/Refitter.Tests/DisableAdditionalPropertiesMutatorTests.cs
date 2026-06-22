using FluentAssertions;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;


public class DisableAdditionalPropertiesMutatorTests
{
    [Test]
    public async Task Mutate_WithGenerateDefaultAdditionalPropertiesFalse_SetsAllowAdditionalPropertiesToFalse()
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
                      "id": { "type": "integer" }
                    }
                  }
                }
              }
            }
            """);

        var sut = new DisableAdditionalPropertiesMutator(generateDefaultAdditionalProperties: false);
        sut.Mutate(document);

        document.Components!.Schemas["TestModel"].ActualSchema.AllowAdditionalProperties
            .Should().BeFalse();
    }

    [Test]
    public async Task Mutate_WithGenerateDefaultAdditionalPropertiesTrue_DoesNotChangeAllowAdditionalProperties()
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
                      "id": { "type": "integer" }
                    }
                  }
                }
              }
            }
            """);

        var expected = document.Components!.Schemas["TestModel"].ActualSchema.AllowAdditionalProperties;

        var sut = new DisableAdditionalPropertiesMutator(generateDefaultAdditionalProperties: true);
        sut.Mutate(document);

        document.Components!.Schemas["TestModel"].ActualSchema.AllowAdditionalProperties
            .Should().Be(expected);
    }

    [Test]
    public async Task Mutate_WithNoComponentsSchemas_DoesNotThrow()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {}
            }
            """);

        var sut = new DisableAdditionalPropertiesMutator(generateDefaultAdditionalProperties: false);
        var act = () => sut.Mutate(document);

        act.Should().NotThrow();
    }

    [Test]
    public async Task Mutate_WithGenerateDefaultAdditionalPropertiesFalse_AppliesToAllSchemas()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "ModelA": {
                    "type": "object",
                    "properties": { "a": { "type": "string" } }
                  },
                  "ModelB": {
                    "type": "object",
                    "properties": { "b": { "type": "integer" } }
                  }
                }
              }
            }
            """);

        var sut = new DisableAdditionalPropertiesMutator(generateDefaultAdditionalProperties: false);
        sut.Mutate(document);

        document.Components!.Schemas["ModelA"].ActualSchema.AllowAdditionalProperties
            .Should().BeFalse();
        document.Components!.Schemas["ModelB"].ActualSchema.AllowAdditionalProperties
            .Should().BeFalse();
    }
}
