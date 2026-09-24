using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Required/optional combined with nullable for parameters, enums and properties.
/// </summary>
public class RequiredNullableCombinationTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Required, version: v1 }
        paths:
          /r:
            post:
              operationId: PostR
              parameters:
                - { name: reqNullable, in: query, required: true, schema: { type: integer, nullable: true } }
                - { name: optNonNull, in: query, schema: { type: integer } }
                - { name: reqEnum, in: query, required: true, schema: { $ref: '#/components/schemas/Color' } }
                - { name: optEnum, in: query, schema: { $ref: '#/components/schemas/Color' } }
              requestBody:
                required: false
                content: { application/json: { schema: { $ref: '#/components/schemas/R' } } }
              responses: { '204': { description: ok } }
        components:
          schemas:
            Color: { type: string, enum: [red, green] }
            R:
              type: object
              required: [a, b, c, missing]
              properties:
                a: { type: string, nullable: true }
                b: { type: array, items: { type: string }, nullable: true }
                c: { $ref: '#/components/schemas/Color' }
                d: { type: string, format: date-time, nullable: true }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Nullable_Parameters()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Query] int? reqNullable");
        generatedCode.Should().Contain("[Query] Color reqEnum");
        generatedCode.Should().Contain("[Query] Color? optEnum");
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
