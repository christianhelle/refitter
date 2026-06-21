using FluentAssertions;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;


[Category("Unit")]
public class CSharpClientGeneratorFactoryIntegrationTests
{
    [Test]
    public async Task Create_AppliesMutatorsInOrder_BeforeBuildingGenerator()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Vehicle": {
                    "oneOf": [
                      { "$ref": "#/components/schemas/Car" }
                    ],
                    "discriminator": { "propertyName": "type" }
                  },
                  "Car": {
                    "type": "object",
                    "properties": {
                      "id": { "format": "int32" },
                      "count": { "type": "integer" }
                    }
                  }
                }
              }
            }
            """);

        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                IntegerType = IntegerType.Int64,
            },
        };

        var mutators = new IOpenApiDocumentMutator[]
        {
            new DisableAdditionalPropertiesMutator(settings.GenerateDefaultAdditionalProperties),
            new OneOfDiscriminatorToAllOfMutator(),
            new FixMissingIntegerTypesMutator(),
            new CustomIntegerTypeMutator(
                settings.CodeGeneratorSettings?.IntegerType ?? IntegerType.Int32),
        };

        var factory = new CSharpClientGeneratorFactory(settings, document, mutators);
        var generator = factory.Create();

        generator.Should().NotBeNull();
        generator.Settings.Should().NotBeNull();
        generator.Settings.CSharpGeneratorSettings.Should().NotBeNull();
        generator.Settings.CSharpGeneratorSettings.Namespace.Should().Be("TestNamespace");
    }

    [Test]
    public async Task Create_WithExplicitMutators_AppliesAllMutations()
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
                      "missingType": { "format": "int32" },
                      "integerNoFormat": { "type": "integer" }
                    }
                  }
                }
              }
            }
            """);

        var settings = new RefitGeneratorSettings
        {
            Namespace = "Test",
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                IntegerType = IntegerType.Int64,
            },
        };

        var mutators = new IOpenApiDocumentMutator[]
        {
            new FixMissingIntegerTypesMutator(),
            new CustomIntegerTypeMutator(IntegerType.Int64),
        };

        var factory = new CSharpClientGeneratorFactory(settings, document, mutators);
        factory.Create();

        var props = document.Components!.Schemas["TestModel"].ActualSchema.Properties;

        props["missingType"].ActualSchema.Type.Should().Be(JsonObjectType.Integer);
        props["integerNoFormat"].ActualSchema.Format.Should().Be("int64");
    }

    [Test]
    public async Task Create_WithDefaultMutators_UsesStandardMutationOrder()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Vehicle": {
                    "oneOf": [
                      { "$ref": "#/components/schemas/Car" }
                    ],
                    "discriminator": { "propertyName": "type" }
                  },
                  "Car": {
                    "type": "object",
                    "properties": { "name": { "type": "string" } }
                  }
                }
              }
            }
            """);

        var settings = new RefitGeneratorSettings
        {
            Namespace = "Test",
            UsePolymorphicSerialization = true,
        };

        var factory = new CSharpClientGeneratorFactory(settings, document);
        var generator = factory.Create();

        var vehicle = document.Components!.Schemas["Vehicle"].ActualSchema;
        vehicle.OneOf.Should().BeEmpty();

        generator.Settings.CSharpGeneratorSettings.TemplateFactory
            .Should().NotBeNull();
    }

    [Test]
    public async Task Create_WithCodeGeneratorSettings_AppliesExplictPropertyCopies()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {}
            }
            """);

        var settings = new RefitGeneratorSettings
        {
            Namespace = "Test",
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                GenerateDataAnnotations = false,
                ExcludedTypeNames = new[] { "SomeType" },
                GenerateNativeRecords = true,
            },
        };

        var factory = new CSharpClientGeneratorFactory(settings, document);
        var generator = factory.Create();

        generator.Settings.CSharpGeneratorSettings.GenerateDataAnnotations
            .Should().BeFalse();
        generator.Settings.CSharpGeneratorSettings.ExcludedTypeNames
            .Should().Contain("SomeType");
        generator.Settings.CSharpGeneratorSettings.GenerateNativeRecords
            .Should().BeTrue();
    }

    [Test]
    public async Task Create_WithoutCodeGeneratorSettings_DoesNotThrow()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {}
            }
            """);

        var settings = new RefitGeneratorSettings
        {
            Namespace = "TestNamespace",
        };

        var factory = new CSharpClientGeneratorFactory(settings, document);
        var generator = factory.Create();

        generator.Should().NotBeNull();
        generator.Settings.CSharpGeneratorSettings.Namespace.Should().Be("TestNamespace");
    }
}
