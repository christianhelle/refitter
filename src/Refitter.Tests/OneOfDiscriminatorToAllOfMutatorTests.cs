using FluentAssertions;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class OneOfDiscriminatorToAllOfMutatorTests
{
    [Test]
    public async Task Mutate_WithOneOfAndDiscriminator_AddsAllOfToSubtypes()
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
                      { "$ref": "#/components/schemas/Car" },
                      { "$ref": "#/components/schemas/Truck" }
                    ],
                    "discriminator": { "propertyName": "type" }
                  },
                  "Car": {
                    "type": "object",
                    "properties": { "wheels": { "type": "integer" } }
                  },
                  "Truck": {
                    "type": "object",
                    "properties": { "capacity": { "type": "number" } }
                  }
                }
              }
            }
            """);

        var sut = new OneOfDiscriminatorToAllOfMutator();
        sut.Mutate(document);

        var vehicle = document.Components!.Schemas["Vehicle"].ActualSchema;
        var car = document.Components!.Schemas["Car"].ActualSchema;
        var truck = document.Components!.Schemas["Truck"].ActualSchema;

        vehicle.OneOf.Should().BeEmpty();
        vehicle.Type.Should().Be(NJsonSchema.JsonObjectType.Object);

        car.AllOf.Should().Contain(a => a.HasReference && a.ActualSchema == vehicle);
        truck.AllOf.Should().Contain(a => a.HasReference && a.ActualSchema == vehicle);
    }

    [Test]
    public async Task Mutate_WithAnyOfAndDiscriminator_AddsAllOfToSubtypes()
    {
        var document = await OpenApiDocument.FromJsonAsync("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Payment": {
                    "anyOf": [
                      { "$ref": "#/components/schemas/CreditCard" },
                      { "$ref": "#/components/schemas/BankTransfer" }
                    ],
                    "discriminator": { "propertyName": "type" }
                  },
                  "CreditCard": {
                    "type": "object",
                    "properties": { "number": { "type": "string" } }
                  },
                  "BankTransfer": {
                    "type": "object",
                    "properties": { "account": { "type": "string" } }
                  }
                }
              }
            }
            """);

        var sut = new OneOfDiscriminatorToAllOfMutator();
        sut.Mutate(document);

        var payment = document.Components!.Schemas["Payment"].ActualSchema;
        var creditCard = document.Components!.Schemas["CreditCard"].ActualSchema;
        var bankTransfer = document.Components!.Schemas["BankTransfer"].ActualSchema;

        payment.AnyOf.Should().BeEmpty();
        payment.Type.Should().Be(NJsonSchema.JsonObjectType.Object);

        creditCard.AllOf.Should().Contain(a => a.HasReference && a.ActualSchema == payment);
        bankTransfer.AllOf.Should().Contain(a => a.HasReference && a.ActualSchema == payment);
    }

    [Test]
    public async Task Mutate_WithoutDiscriminator_DoesNotChangeSchemas()
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
                    "properties": { "name": { "type": "string" } }
                  }
                }
              }
            }
            """);

        var expectedOneOf = document.Components!.Schemas["TestModel"].ActualSchema.OneOf.Count;

        var sut = new OneOfDiscriminatorToAllOfMutator();
        sut.Mutate(document);

        document.Components!.Schemas["TestModel"].ActualSchema.OneOf.Count
            .Should().Be(expectedOneOf);
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

        var sut = new OneOfDiscriminatorToAllOfMutator();
        var act = () => sut.Mutate(document);

        act.Should().NotThrow();
    }

    [Test]
    public async Task Mutate_WithDiscriminatorButNoUnionSchemas_DoesNotChange()
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
                    "discriminator": { "propertyName": "type" },
                    "properties": { "name": { "type": "string" } }
                  }
                }
              }
            }
            """);

        var sut = new OneOfDiscriminatorToAllOfMutator();
        sut.Mutate(document);

        document.Components!.Schemas["Base"].ActualSchema.Type
            .Should().Be(NJsonSchema.JsonObjectType.Object);
    }
}
