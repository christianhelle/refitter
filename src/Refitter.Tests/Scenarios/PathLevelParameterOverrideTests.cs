using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Operation-level parameters override path-level parameters with the same name and location.
/// </summary>
public class PathLevelParameterOverrideTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Override, version: v1 }
        paths:
          /r/{id}:
            parameters:
              - { name: id, in: path, required: true, schema: { type: string } }
              - { name: verbose, in: query, schema: { type: boolean } }
            get:
              operationId: GetR
              parameters:
                - { name: id, in: path, required: true, schema: { type: integer, format: int64 } }
                - { name: verbose, in: query, schema: { type: boolean } }
              responses: { '200': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Uses_Operation_Level_Parameter_Definition()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task GetR(long id, [Query] bool? verbose);");
        generatedCode.Should().NotContain("string id");
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
