using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Deprecated operations, parameters and properties with allowEmptyValue and allowReserved.
/// </summary>
public class DeprecatedParameterTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Deprecated, version: v1 }
        paths:
          /d:
            get:
              operationId: GetD
              deprecated: true
              parameters:
                - { name: old, in: query, deprecated: true, schema: { type: string } }
                - { name: allowEmpty, in: query, allowEmptyValue: true, schema: { type: string } }
                - { name: reserved, in: query, allowReserved: true, schema: { type: string } }
              responses: { '200': { description: ok, content: { application/json: { schema: { $ref: '#/components/schemas/D' } } } } }
        components:
          schemas:
            D:
              type: object
              properties:
                old: { type: string, deprecated: true }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Deprecated_Members()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<D> GetD([Query] string old, [Query] string allowEmpty, [Query] string reserved);");
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
