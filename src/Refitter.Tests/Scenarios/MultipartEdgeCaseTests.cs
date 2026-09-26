using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Multipart bodies with JSON-encoded parts, arrays, booleans, dates, $ref schemas and allOf.
/// </summary>
public class MultipartEdgeCaseTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Multipart, version: v1 }
        paths:
          /m:
            post:
              operationId: PostM
              requestBody:
                content:
                  multipart/form-data:
                    schema:
                      type: object
                      required: [file]
                      properties:
                        file: { type: string, format: binary }
                        meta: { $ref: '#/components/schemas/Meta' }
                        ids: { type: array, items: { type: integer } }
                        flag: { type: boolean }
                        when: { type: string, format: date-time }
                    encoding:
                      meta: { contentType: application/json }
              responses: { '204': { description: ok } }
          /m2:
            post:
              operationId: PostM2
              requestBody:
                content:
                  multipart/form-data:
                    schema: { $ref: '#/components/schemas/Upload' }
              responses: { '204': { description: ok } }
          /m3:
            post:
              operationId: PostM3
              requestBody:
                content:
                  multipart/form-data:
                    schema:
                      allOf:
                        - $ref: '#/components/schemas/Upload'
                        - type: object
                          properties: { extra: { type: string } }
              responses: { '204': { description: ok } }
        components:
          schemas:
            Meta: { type: object, properties: { a: { type: string } } }
            Upload:
              type: object
              properties:
                file: { type: string, format: binary }
                name: { type: string }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Multipart_Parameters()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task PostM(StreamPart file, Meta meta, IEnumerable<int> ids, bool? flag, System.DateTimeOffset? when);");
        generatedCode.Should().Contain("Task PostM2(StreamPart file, string name);");
    }

    [Test]
    public async Task Includes_Referenced_Properties_For_AllOf_Multipart_Body()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task PostM3(StreamPart file, string name, string extra);");
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
