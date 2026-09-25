using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Default values containing quotes, backslashes, newlines, extreme integers, exponents and GUIDs.
/// </summary>
public class DefaultValueEdgeCaseTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Defaults, version: v1 }
        paths:
          /d:
            get:
              operationId: GetD
              parameters:
                - { name: q, in: query, schema: { type: string, default: 'he said "hi" \ back' } }
                - { name: n, in: query, schema: { type: integer, default: -5 } }
                - { name: f, in: query, schema: { type: number, default: 1.5 } }
                - { name: b, in: query, schema: { type: boolean, default: true } }
                - { name: e, in: query, schema: { type: string, enum: [asc, desc], default: desc } }
                - { name: dt, in: query, schema: { type: string, format: date-time, default: '2020-01-01T00:00:00Z' } }
                - { name: arr, in: query, schema: { type: array, items: { type: string }, default: [a, b] } }
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/D' } } }
        components:
          schemas:
            D:
              type: object
              properties:
                s: { type: string, default: "line1\nline2 \"quoted\" {braces}" }
                e: { type: string, enum: [x, y], default: y }
                i: { type: integer, format: int64, default: 9223372036854775807 }
                d: { type: number, default: 1e10 }
                obj: { type: object, default: {} }
                u: { type: string, format: uuid, default: 00000000-0000-0000-0000-000000000000 }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Escapes_Default_Values()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("= \"line1\\nline2 \\\"quoted\\\" {braces}\";");
        generatedCode.Should().Contain("= 9223372036854775807L;");
        generatedCode.Should().Contain("= new System.Guid(\"00000000-0000-0000-0000-000000000000\");");
    }

    [Test]
    public async Task Escapes_Optional_Parameter_Default_Values()
    {
        var generatedCode = await GenerateCode(settings => settings.OptionalParameters = true);
        generatedCode.Should().Contain("[Query] string? q = \"he said \\\"hi\\\" \\\\ back\"");
        generatedCode.Should().Contain("[Query] int? n = -5");
        generatedCode.Should().Contain("[Query] bool? b = true");
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
    public async Task Can_Build_Generated_Code_With_Optional_Parameters()
    {
        var generatedCode = await GenerateCode(settings => settings.OptionalParameters = true);
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
