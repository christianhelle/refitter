using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// allOf with several references plus nullable and description-wrapped references.
/// </summary>
public class AllOfCompositionTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: AllOf, version: v1 }
        paths:
          /a:
            get:
              operationId: GetA
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/Combined' } } }
        components:
          schemas:
            Base1: { type: object, properties: { a: { type: string } } }
            Base2: { type: object, properties: { b: { type: string } } }
            Combined:
              allOf:
                - $ref: '#/components/schemas/Base1'
                - $ref: '#/components/schemas/Base2'
                - type: object
                  properties:
                    c: { type: string }
                    nullableRef:
                      nullable: true
                      allOf: [ { $ref: '#/components/schemas/Base1' } ]
                    refWithDescription:
                      description: overridden
                      allOf: [ { $ref: '#/components/schemas/Base2' } ]
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Composed_Contract()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public partial class Combined : Base1");
        generatedCode.Should().Contain("public Base1 NullableRef { get; set; }");
        generatedCode.Should().Contain("public Base2 RefWithDescription { get; set; }");
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
