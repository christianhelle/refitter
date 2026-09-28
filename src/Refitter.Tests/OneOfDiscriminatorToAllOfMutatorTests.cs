using AwesomeAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;


public class OneOfDiscriminatorToAllOfMutatorTests
{
    [Test]
    public async Task Mutate_WithOneOfAndDiscriminator_AddsAllOfToSubtypes()
    {
        var document = ApiDocumentLoader.Load("""
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
            """, null, isYaml: false);

        var sut = new OneOfDiscriminatorToAllOfMutator();
        sut.Mutate(document);

        var vehicle = document.Components!.Schemas["Vehicle"].ActualSchema;
        var car = document.Components!.Schemas["Car"].ActualSchema;
        var truck = document.Components!.Schemas["Truck"].ActualSchema;

        vehicle.OneOf.Should().BeEmpty();
        vehicle.Type.Should().Be(ApiObjectTypes.Object);

        car.AllOf.Should().Contain(a => a.HasReference && a.ActualSchema == vehicle);
        truck.AllOf.Should().Contain(a => a.HasReference && a.ActualSchema == vehicle);
    }

    [Test]
    public async Task Mutate_WithAnyOfAndDiscriminator_AddsAllOfToSubtypes()
    {
        var document = ApiDocumentLoader.Load("""
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
            """, null, isYaml: false);

        var sut = new OneOfDiscriminatorToAllOfMutator();
        sut.Mutate(document);

        var payment = document.Components!.Schemas["Payment"].ActualSchema;
        var creditCard = document.Components!.Schemas["CreditCard"].ActualSchema;
        var bankTransfer = document.Components!.Schemas["BankTransfer"].ActualSchema;

        payment.AnyOf.Should().BeEmpty();
        payment.Type.Should().Be(ApiObjectTypes.Object);

        creditCard.AllOf.Should().Contain(a => a.HasReference && a.ActualSchema == payment);
        bankTransfer.AllOf.Should().Contain(a => a.HasReference && a.ActualSchema == payment);
    }

    [Test]
    public async Task Mutate_WithoutDiscriminator_DoesNotChangeSchemas()
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
                    "properties": { "name": { "type": "string" } }
                  }
                }
              }
            }
            """, null, isYaml: false);

        var expectedOneOf = document.Components!.Schemas["TestModel"].ActualSchema.OneOf.Count;

        var sut = new OneOfDiscriminatorToAllOfMutator();
        sut.Mutate(document);

        document.Components!.Schemas["TestModel"].ActualSchema.OneOf.Count
            .Should().Be(expectedOneOf);
    }

    [Test]
    public async Task Mutate_WithNoComponentsSchemas_DoesNotThrow()
    {
        var document = ApiDocumentLoader.Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {}
            }
            """, null, isYaml: false);

        var sut = new OneOfDiscriminatorToAllOfMutator();
        var act = () => sut.Mutate(document);

        act.Should().NotThrow();
    }

    [Test]
    public async Task Mutate_WithDiscriminatorButNoUnionSchemas_DoesNotChange()
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
                    "discriminator": { "propertyName": "type" },
                    "properties": { "name": { "type": "string" } }
                  }
                }
              }
            }
            """, null, isYaml: false);

        var sut = new OneOfDiscriminatorToAllOfMutator();
        sut.Mutate(document);

        document.Components!.Schemas["Base"].ActualSchema.Type
            .Should().Be(ApiObjectTypes.Object);
    }

    [Test]
    public void Mutate_WithoutComponents_DoesNothing()
    {
        var document = new ApiDocument
        {
            Info = new() { Title = "Test", Version = "1.0" }
        };

        var sut = new OneOfDiscriminatorToAllOfMutator();

        var act = () => sut.Mutate(document);

        act.Should().NotThrow();
    }

    [Test]
    public async Task Mutate_Does_Not_Duplicate_Existing_AllOf_Inheritance()
    {
        // Two union members keep the schema model from collapsing Vehicle.ActualSchema onto the
        // single referenced subtype, so the mutator actually inspects the subtypes.
        var document = ApiDocumentLoader.Load("""
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
                    "allOf": [
                      { "$ref": "#/components/schemas/Vehicle" }
                    ]
                  },
                  "Truck": {
                    "type": "object",
                    "properties": { "capacity": { "type": "number" } }
                  }
                }
              }
            }
            """, null, isYaml: false);

        var vehicle = document.Components!.Schemas["Vehicle"].ActualSchema;

        var sut = new OneOfDiscriminatorToAllOfMutator();
        sut.Mutate(document);

        var car = document.Components!.Schemas["Car"].ActualSchema;
        var truck = document.Components!.Schemas["Truck"].ActualSchema;

        car.AllOf.Should().HaveCount(1);
        car.AllOf.Should().OnlyContain(a => a.HasReference && a.ActualSchema == vehicle);
        truck.AllOf.Should().HaveCount(1);
        truck.AllOf.Should().OnlyContain(a => a.HasReference && a.ActualSchema == vehicle);
    }

    [Test]
    public async Task Mutate_Without_Discriminator_Does_Not_Add_Inheritance()
    {
        var document = ApiDocumentLoader.Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Vehicle": {
                    "oneOf": [
                      { "$ref": "#/components/schemas/Car" }
                    ]
                  },
                  "Car": { "type": "object" }
                }
              }
            }
            """, null, isYaml: false);

        var sut = new OneOfDiscriminatorToAllOfMutator();
        sut.Mutate(document);

        var car = document.Components!.Schemas["Car"].ActualSchema;
        car.AllOf.Should().BeEmpty();
    }
}
