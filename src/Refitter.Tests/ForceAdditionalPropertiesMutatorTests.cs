using AwesomeAssertions;
using NSwag;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class ForceAdditionalPropertiesMutatorTests
{
    private const string ClosedSchemasSpec = """
        {
          "openapi": "3.0.1",
          "info": { "title": "Test", "version": "1.0" },
          "paths": {},
          "components": {
            "schemas": {
              "ModelA": {
                "type": "object",
                "properties": { "a": { "type": "string" } },
                "additionalProperties": false
              },
              "ModelB": {
                "type": "object",
                "properties": { "b": { "type": "integer" } },
                "additionalProperties": false
              },
              "Labels": {
                "type": "object",
                "properties": { "name": { "type": "string" } },
                "additionalProperties": { "type": "string" }
              }
            }
          }
        }
        """;

    [TestMethod]
    public async Task Mutate_WithForceAdditionalPropertiesTrue_SetsAllowAdditionalPropertiesToTrue()
    {
        var document = await OpenApiDocument.FromJsonAsync(ClosedSchemasSpec);

        var sut = new ForceAdditionalPropertiesMutator(forceAdditionalProperties: true);
        sut.Mutate(document);

        document.Components!.Schemas["ModelA"].ActualSchema.AllowAdditionalProperties
            .Should().BeTrue();
        document.Components!.Schemas["ModelB"].ActualSchema.AllowAdditionalProperties
            .Should().BeTrue();
    }

    [TestMethod]
    public async Task Mutate_WithForceAdditionalPropertiesFalse_DoesNotChangeAllowAdditionalProperties()
    {
        var document = await OpenApiDocument.FromJsonAsync(ClosedSchemasSpec);

        var sut = new ForceAdditionalPropertiesMutator(forceAdditionalProperties: false);
        sut.Mutate(document);

        document.Components!.Schemas["ModelA"].ActualSchema.AllowAdditionalProperties
            .Should().BeFalse();
        document.Components!.Schemas["ModelB"].ActualSchema.AllowAdditionalProperties
            .Should().BeFalse();
    }

    [TestMethod]
    public async Task Mutate_WithForceAdditionalPropertiesTrue_KeepsSchemaValuedAdditionalProperties()
    {
        var document = await OpenApiDocument.FromJsonAsync(ClosedSchemasSpec);
        var expected = document.Components!.Schemas["Labels"].ActualSchema.AdditionalPropertiesSchema;

        var sut = new ForceAdditionalPropertiesMutator(forceAdditionalProperties: true);
        sut.Mutate(document);

        document.Components!.Schemas["Labels"].ActualSchema.AdditionalPropertiesSchema
            .Should().NotBeNull().And.BeSameAs(expected);
    }

    [TestMethod]
    public async Task Mutate_WithNoComponentsSchemas_DoesNotThrow()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {}
            }
            """);

        var sut = new ForceAdditionalPropertiesMutator(forceAdditionalProperties: true);
        var act = () => sut.Mutate(document);

        act.Should().NotThrow();
    }
}
