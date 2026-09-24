using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Discriminator property and mapping keys containing dashes, spaces and digits.
/// </summary>
public class DiscriminatorMappingEdgeCaseTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Discriminator, version: v1 }
        paths:
          /pets:
            get:
              operationId: GetPets
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema: { type: array, items: { $ref: '#/components/schemas/Pet' } }
        components:
          schemas:
            Pet:
              type: object
              required: [pet-type]
              properties:
                pet-type: { type: string }
                name: { type: string }
              discriminator:
                propertyName: pet-type
                mapping:
                  'cat-type': '#/components/schemas/Cat'
                  'dog type': '#/components/schemas/Dog'
                  '1': '#/components/schemas/Bird'
            Cat:
              allOf: [ { $ref: '#/components/schemas/Pet' }, { type: object, properties: { lives: { type: integer } } } ]
            Dog:
              allOf: [ { $ref: '#/components/schemas/Pet' }, { type: object, properties: { good: { type: boolean } } } ]
            Bird:
              allOf: [ { $ref: '#/components/schemas/Pet' }, { type: object, properties: { wings: { type: integer } } } ]
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Preserves_Discriminator_Mapping_Keys()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[JsonInheritanceConverter(typeof(Pet), \"pet-type\")]");
        generatedCode.Should().Contain("[JsonInheritanceAttribute(\"dog type\", typeof(Dog))]");
        generatedCode.Should().Contain("[JsonInheritanceAttribute(\"1\", typeof(Bird))]");
    }

    [Test]
    public async Task Preserves_Discriminator_Mapping_Keys_With_Polymorphic_Serialization()
    {
        var generatedCode = await GenerateCode(settings => settings.UsePolymorphicSerialization = true);
        generatedCode.Should().Contain("TypeDiscriminatorPropertyName = \"pet-type\"");
        generatedCode.Should().Contain("[JsonDerivedType(typeof(Dog), typeDiscriminator: \"dog type\")]");
        generatedCode.Should().Contain("[JsonDerivedType(typeof(Bird), typeDiscriminator: \"1\")]");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_With_Polymorphic_Serialization()
    {
        var generatedCode = await GenerateCode(settings => settings.UsePolymorphicSerialization = true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_With_Immutable_Records()
    {
        var generatedCode = await GenerateCode(settings => settings.ImmutableRecords = true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(Action<RefitGeneratorSettings>? configure = null)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        configure?.Invoke(settings);

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
