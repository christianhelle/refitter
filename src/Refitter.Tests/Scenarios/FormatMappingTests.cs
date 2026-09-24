using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// String, integer and number formats in parameters and properties, including unknown formats.
/// </summary>
public class FormatMappingTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Formats, version: v1 }
        paths:
          /f/{d}:
            get:
              operationId: GetF
              parameters:
                - { name: d, in: path, required: true, schema: { type: string, format: date } }
                - { name: dt, in: query, schema: { type: string, format: date-time } }
                - { name: t, in: query, schema: { type: string, format: time } }
                - { name: dur, in: query, schema: { type: string, format: duration } }
                - { name: u, in: query, schema: { type: string, format: uuid } }
                - { name: uri, in: query, schema: { type: string, format: uri } }
                - { name: b, in: query, schema: { type: string, format: byte } }
                - { name: i64, in: query, schema: { type: integer, format: int64 } }
                - { name: f, in: query, schema: { type: number, format: float } }
                - { name: dec, in: query, schema: { type: number, format: decimal } }
                - { name: ids, in: query, schema: { type: array, items: { type: string, format: uuid } } }
                - { name: dates, in: query, schema: { type: array, items: { type: string, format: date-time } } }
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/F' } } }
        components:
          schemas:
            F:
              type: object
              properties:
                byte: { type: string, format: byte }
                binary: { type: string, format: binary }
                email: { type: string, format: email }
                ipv4: { type: string, format: ipv4 }
                hostname: { type: string, format: hostname }
                password: { type: string, format: password }
                uint: { type: integer, format: uint64 }
                unknownFmt: { type: string, format: something-custom }
                intUnknownFmt: { type: integer, format: weird }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Maps_Formats_To_CSharp_Types()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("System.DateTimeOffset d");
        generatedCode.Should().Contain("[Query] System.TimeSpan? dur");
        generatedCode.Should().Contain("[Query] System.Guid? u");
        generatedCode.Should().Contain("[Query] System.Uri uri");
        generatedCode.Should().Contain("[Query] byte[] b");
        generatedCode.Should().Contain("[Query] long? i64");
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

    private static async Task<string> GenerateCode(Action<RefitGeneratorSettings>? configure = null)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        configure?.Invoke(settings);

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
