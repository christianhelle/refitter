using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// OpenAPI 3 documents that reference sections of another specification version, e.g. a Swagger 2.0
/// style top-level <c>responses</c> section, like https://developers.intellihr.io/docs/v1/swagger.json does.
/// </summary>
public class ExtensionDataReferenceTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.3
        info: { title: Refs, version: v1 }
        paths:
          /items:
            get:
              operationId: GetItems
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/Item' } } }
                '400': { $ref: '#/responses/errors' }
        responses:
          errors:
            description: errors
            content: { application/json: { schema: { $ref: '#/components/schemas/Problem' } } }
        components:
          schemas:
            Item: { type: object, properties: { id: { type: string } } }
            Problem: { type: object, properties: { title: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Resolves_Responses_Outside_Components()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<Item> GetItems();");
        generatedCode.Should().Contain("class Problem");
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
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
