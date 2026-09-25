using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Deprecated schemas used in operation signatures raise CS0612 in the generated code (#1278).
/// </summary>
public class DeprecatedSchemaTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Deprecated, version: v1 }
        paths:
          /d:
            get:
              operationId: GetD
              responses: { '200': { description: ok, content: { application/json: { schema: { $ref: '#/components/schemas/D' } } } } }
        components:
          schemas:
            D:
              type: object
              deprecated: true
              properties:
                old: { type: string }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Category("Integration")]
    [Skip("https://github.com/christianhelle/refitter/issues/1278")]
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
