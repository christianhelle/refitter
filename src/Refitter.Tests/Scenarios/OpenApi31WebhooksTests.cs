using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// OpenAPI 3.1 webhooks are ignored while regular paths are still generated.
/// </summary>
public class OpenApi31WebhooksTests
{
    private const string OpenApiSpec = """
        openapi: 3.1.0
        info: { title: Webhooks, version: v1 }
        paths:
          /ping:
            get:
              operationId: Ping
              responses: { '204': { description: ok } }
        webhooks:
          newPet:
            post:
              requestBody:
                content:
                  application/json:
                    schema: { $ref: '#/components/schemas/Pet' }
              responses: { '200': { description: ok } }
        components:
          schemas:
            Pet: { type: object, properties: { name: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Paths_But_Not_Webhooks()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task Ping();");
        generatedCode.Should().NotContain("[Post(\"newPet\")]");
        generatedCode.Should().NotContain("NewPet(");
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
