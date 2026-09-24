using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// text/plain, text/csv, image/png and vendor JSON content types.
/// </summary>
public class NonJsonContentTypeTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Content, version: v1 }
        paths:
          /text:
            get:
              operationId: GetText
              responses:
                '200': { description: ok, content: { text/plain: { schema: { type: string } } } }
          /csv:
            get:
              operationId: GetCsv
              responses:
                '200': { description: ok, content: { text/csv: { schema: { type: string, format: binary } } } }
          /img:
            put:
              operationId: PutImage
              requestBody:
                content:
                  image/png: { schema: { type: string, format: binary } }
              responses: { '204': { description: ok } }
          /text-body:
            post:
              operationId: PostText
              requestBody:
                content:
                  text/plain: { schema: { type: string } }
              responses: { '204': { description: ok } }
          /vendor:
            get:
              operationId: GetVendor
              responses:
                '200': { description: ok, content: { application/vnd.api+json: { schema: { $ref: '#/components/schemas/V' } } } }
        components:
          schemas:
            V: { type: object, properties: { a: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Methods_For_Non_Json_Content()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<string> GetText();");
        generatedCode.Should().Contain("Task PutImage(StreamPart body);");
        generatedCode.Should().Contain("Task PostText([Body] string body);");
        generatedCode.Should().Contain("Task<V> GetVendor();");
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
