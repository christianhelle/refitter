using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Request and response bodies offered in JSON, XML and form-urlencoded at the same time.
/// </summary>
public class MultipleRequestContentTypeTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: MultiContent, version: v1 }
        paths:
          /p:
            post:
              operationId: PostP
              requestBody:
                required: true
                content:
                  application/json: { schema: { $ref: '#/components/schemas/P' } }
                  application/xml: { schema: { $ref: '#/components/schemas/P' } }
                  application/x-www-form-urlencoded: { schema: { $ref: '#/components/schemas/P' } }
              responses:
                '200':
                  description: ok
                  content:
                    application/xml: { schema: { $ref: '#/components/schemas/P' } }
                    application/json: { schema: { $ref: '#/components/schemas/P' } }
        components:
          schemas:
            P: { type: object, properties: { a: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Single_Body_Parameter()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<P> PostP([Body] P body);");
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
