using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Path templates with extensions, adjacent parameters, trailing slashes and literal query strings.
/// </summary>
public class PathTemplateEdgeCaseTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: PathEdge, version: v1 }
        servers: [ { url: 'https://example.com/{basePath}', variables: { basePath: { default: v2 } } } ]
        paths:
          /files/{path}.json:
            get:
              operationId: GetFileJson
              parameters: [ { name: path, in: path, required: true, schema: { type: string } } ]
              responses: { '204': { description: ok } }
          /a/{x}-{y}:
            get:
              operationId: GetXY
              parameters:
                - { name: x, in: path, required: true, schema: { type: integer } }
                - { name: y, in: path, required: true, schema: { type: integer } }
              responses: { '204': { description: ok } }
          /trailing/:
            get:
              operationId: GetTrailing
              responses: { '204': { description: ok } }
          /query?fixed=1:
            get:
              operationId: GetFixedQuery
              responses: { '204': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Preserves_Path_Templates()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Get(\"/files/{path}.json\")]");
        generatedCode.Should().Contain("Task GetXY(int x, int y);");
        generatedCode.Should().Contain("[Get(\"/trailing/\")]");
        generatedCode.Should().Contain("[Get(\"/query?fixed=1\")]");
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
