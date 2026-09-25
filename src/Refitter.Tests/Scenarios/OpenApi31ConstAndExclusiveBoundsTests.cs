using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// OpenAPI 3.1 const, numeric exclusiveMinimum/exclusiveMaximum and examples keywords.
/// </summary>
public class OpenApi31ConstAndExclusiveBoundsTests
{
    private const string OpenApiSpec = """
        openapi: 3.1.0
        info: { title: Const, version: v1 }
        paths:
          /t:
            get:
              operationId: GetT
              responses:
                '200':
                  description: OK
                  content:
                    application/json:
                      schema: { $ref: '#/components/schemas/Thing' }
        components:
          schemas:
            Thing:
              type: object
              properties:
                kind: { type: string, const: thing }
                version: { const: 2 }
                score: { type: number, exclusiveMinimum: 0, exclusiveMaximum: 100 }
                tags: { type: array, items: { type: string }, examples: [[a, b]] }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Properties_For_Const_And_Bounded_Values()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public string Kind { get; set; }");
        generatedCode.Should().Contain("public double Score { get; set; }");
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
