using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Parameters, request bodies and responses referenced from components.
/// </summary>
public class ComponentReferenceTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Refs, version: v1 }
        paths:
          /r/{id}:
            parameters:
              - $ref: '#/components/parameters/Id'
            put:
              operationId: PutR
              parameters:
                - $ref: '#/components/parameters/IfMatch'
              requestBody: { $ref: '#/components/requestBodies/ItemBody' }
              responses:
                '200': { $ref: '#/components/responses/ItemResponse' }
                '404': { $ref: '#/components/responses/NotFound' }
                default: { $ref: '#/components/responses/Error' }
        components:
          parameters:
            Id: { name: id, in: path, required: true, schema: { type: string } }
            IfMatch: { name: If-Match, in: header, required: false, schema: { type: string } }
          requestBodies:
            ItemBody:
              required: true
              content: { application/json: { schema: { $ref: '#/components/schemas/Item' } } }
          responses:
            ItemResponse:
              description: ok
              content: { application/json: { schema: { $ref: '#/components/schemas/Item' } } }
            NotFound:
              description: not found
              content: { application/json: { schema: { $ref: '#/components/schemas/Problem' } } }
            Error:
              description: err
              content: { application/problem+json: { schema: { $ref: '#/components/schemas/Problem' } } }
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
    public async Task Resolves_Component_References()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<Item> PutR(string id, [Body] Item body, [Header(\"If-Match\")] string if_Match);");
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
