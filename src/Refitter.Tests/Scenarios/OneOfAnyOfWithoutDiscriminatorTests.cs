using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// oneOf and anyOf without a discriminator in responses, request bodies, properties and array items.
/// </summary>
public class OneOfAnyOfWithoutDiscriminatorTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: AnyOf, version: v1 }
        paths:
          /a:
            get:
              operationId: GetA
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema:
                        oneOf:
                          - $ref: '#/components/schemas/Cat'
                          - $ref: '#/components/schemas/Dog'
          /b:
            post:
              operationId: PostB
              requestBody:
                content:
                  application/json:
                    schema:
                      anyOf:
                        - { type: string }
                        - { type: integer }
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema:
                        type: object
                        properties:
                          value:
                            anyOf: [ { type: string }, { type: number }, { $ref: '#/components/schemas/Cat' } ]
                          items:
                            type: array
                            items:
                              oneOf: [ { $ref: '#/components/schemas/Cat' }, { $ref: '#/components/schemas/Dog' } ]
        components:
          schemas:
            Cat: { type: object, properties: { meow: { type: boolean } } }
            Dog: { type: object, properties: { bark: { type: boolean } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Methods_For_Untagged_Unions()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<Cat> GetA();");
        generatedCode.Should().Contain("Task<Response> PostB([Body] Body body);");
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
