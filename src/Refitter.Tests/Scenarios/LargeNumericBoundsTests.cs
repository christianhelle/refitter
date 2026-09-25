using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// minimum/maximum values larger than decimal.MaxValue, as emitted for double.MaxValue ranges (#1273).
/// </summary>
public class LargeNumericBoundsTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Bounds, version: v1 }
        paths:
          /m:
            get:
              operationId: GetM
              responses: { '200': { description: ok, content: { application/json: { schema: { $ref: '#/components/schemas/M' } } } } }
        components:
          schemas:
            M:
              type: object
              properties:
                value: { type: number, format: double, minimum: 0, maximum: 1.7976931348623157e308 }
        """;

    [Test]
    [Skip("https://github.com/christianhelle/refitter/issues/1273")]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Category("Integration")]
    [Skip("https://github.com/christianhelle/refitter/issues/1273")]
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
