using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// A schema named like the generated interface collides with it (#1270).
/// </summary>
public class InterfaceNameCollisionTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Pet, version: v1 }
        paths:
          /p:
            get:
              operationId: GetPet
              responses: { '200': { description: ok, content: { application/json: { schema: { $ref: '#/components/schemas/IPet' } } } } }
        components:
          schemas:
            IPet: { type: object, properties: { a: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Category("Integration")]
    [Skip("https://github.com/christianhelle/refitter/issues/1270")]
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
