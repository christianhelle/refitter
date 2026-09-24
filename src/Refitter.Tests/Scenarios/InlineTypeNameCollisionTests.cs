using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Inline enum and object names that collide with existing component schema names.
/// </summary>
public class InlineTypeNameCollisionTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: InlineCollide, version: v1 }
        paths:
          /p:
            get:
              operationId: GetP
              responses: { '200': { description: ok, content: { application/json: { schema: { $ref: '#/components/schemas/Pet' } } } } }
        components:
          schemas:
            Pet:
              type: object
              properties:
                status: { type: string, enum: [a, b] }
                kind: { type: object, properties: { x: { type: string } } }
            PetStatus: { type: object, properties: { y: { type: string } } }
            PetKind: { type: string, enum: [c, d] }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Renames_Colliding_Inline_Types()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public PetStatus2 Status { get; set; }");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
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
