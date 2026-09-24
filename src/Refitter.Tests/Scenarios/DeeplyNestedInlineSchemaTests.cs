using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Deeply nested inline objects, arrays and enums in request and response bodies.
/// </summary>
public class DeeplyNestedInlineSchemaTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Inline, version: v1 }
        paths:
          /i:
            post:
              operationId: PostI
              requestBody:
                content:
                  application/json:
                    schema:
                      type: object
                      properties:
                        level1:
                          type: object
                          properties:
                            level2:
                              type: array
                              items:
                                type: object
                                properties:
                                  level3: { type: string, enum: [x, y] }
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema:
                        type: object
                        properties:
                          result:
                            type: object
                            properties:
                              status: { type: string, enum: [x, y] }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Named_Types_For_Inline_Schemas()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public ICollection<Level2> Level2 { get; set; }");
        generatedCode.Should().Contain("public enum Level2Level3");
        generatedCode.Should().Contain("public enum ResultStatus");
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
