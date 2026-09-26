using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Multipart bodies composed with allOf include the properties of every member (#1277).
/// </summary>
public class MultipartAllOfTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: MultipartAllOf, version: v1 }
        paths:
          /files:
            post:
              operationId: PostFiles
              requestBody:
                content:
                  multipart/form-data:
                    schema:
                      allOf:
                        - $ref: '#/components/schemas/Files'
                        - $ref: '#/components/schemas/Files'
                        - allOf:
                            - $ref: '#/components/schemas/Tags'
                        - type: object
                          properties: { note: { type: string } }
              responses: { '204': { description: ok } }
          /empty:
            post:
              operationId: PostEmpty
              requestBody:
                content:
                  multipart/form-data: {}
              responses: { '204': { description: ok } }
        components:
          schemas:
            Files:
              type: object
              properties:
                attachments: { type: array, items: { type: string, format: binary } }
            Tags:
              type: object
              properties:
                labels: { type: array, items: { type: string } }
                ids: { type: array, items: { type: integer } }
                raw: { type: array }
        """;

    [Test]
    public async Task Includes_Properties_From_All_AllOf_Members_Once()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task PostFiles(IEnumerable<StreamPart> attachments, IEnumerable<string> labels, IEnumerable<int> ids, IEnumerable<object> raw, string note);");
    }

    [Test]
    public async Task Generates_Multipart_Operation_Without_Schema()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task PostEmpty(");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = swaggerFile });
        return sut.Generate();
    }
}
