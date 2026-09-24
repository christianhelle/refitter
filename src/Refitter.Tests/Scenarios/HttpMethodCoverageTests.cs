using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// HEAD, OPTIONS, PATCH (json-patch), PUT and DELETE operations sharing path-level parameters.
/// </summary>
public class HttpMethodCoverageTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Methods, version: v1 }
        paths:
          /r/{id}:
            parameters:
              - { name: id, in: path, required: true, schema: { type: string } }
            head:
              operationId: HeadR
              responses: { '200': { description: ok } }
            options:
              operationId: OptionsR
              responses: { '200': { description: ok } }
            patch:
              operationId: PatchR
              requestBody:
                content:
                  application/json-patch+json:
                    schema: { type: array, items: { type: object, properties: { op: { type: string }, path: { type: string }, value: {} } } }
              responses: { '204': { description: ok } }
            delete:
              operationId: DeleteR
              responses: { '204': { description: ok } }
            put:
              operationId: PutR
              requestBody:
                content:
                  application/json:
                    schema: { type: object, properties: { a: { type: string } } }
              responses: { '204': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_All_Supported_Http_Methods()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Head(\"/r/{id}\")]");
        generatedCode.Should().Contain("[Options(\"/r/{id}\")]");
        generatedCode.Should().Contain("[Patch(\"/r/{id}\")]");
        generatedCode.Should().Contain("[Put(\"/r/{id}\")]");
        generatedCode.Should().Contain("[Delete(\"/r/{id}\")]");
        generatedCode.Should().Contain("Task HeadR(string id);");
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
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces_By_Tag()
    {
        var generatedCode = await GenerateCode(settings => settings.MultipleInterfaces = MultipleInterfaces.ByTag);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_With_IApiResponse()
    {
        var generatedCode = await GenerateCode(settings => settings.ReturnIApiResponse = true);
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
