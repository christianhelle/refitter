using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Header-only responses, 204 with content, competing 200/201, redirects, primitive and nullable responses.
/// </summary>
public class ResponseShapeEdgeCaseTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Responses, version: v1 }
        paths:
          /headers-only:
            head:
              operationId: HeadOnly
              responses:
                '200':
                  description: ok
                  headers: { X-Count: { schema: { type: integer } } }
          /no-content-with-schema:
            delete:
              operationId: DeleteThing
              responses:
                '204': { description: gone, content: { application/json: { schema: { type: object } } } }
          /created-and-ok:
            post:
              operationId: Upsert
              responses:
                '200': { description: updated, content: { application/json: { schema: { $ref: '#/components/schemas/A' } } } }
                '201': { description: created, content: { application/json: { schema: { $ref: '#/components/schemas/B' } } } }
          /accepted:
            post:
              operationId: Accept
              responses:
                '202': { description: accepted }
                '303': { description: see other }
          /primitive:
            get:
              operationId: GetPrimitive
              responses:
                '200': { description: ok, content: { application/json: { schema: { type: integer, format: int64 } } } }
          /bool:
            get:
              operationId: GetBool
              responses:
                '200': { description: ok, content: { application/json: { schema: { type: boolean } } } }
          /nullable-ref:
            get:
              operationId: GetNullable
              responses:
                '200': { description: ok, content: { application/json: { schema: { nullable: true, allOf: [ { $ref: '#/components/schemas/A' } ] } } } }
          /dict-of-refs:
            get:
              operationId: GetDictOfRefs
              responses:
                '200': { description: ok, content: { application/json: { schema: { type: object, additionalProperties: { $ref: '#/components/schemas/A' } } } } }
        components:
          schemas:
            A: { type: object, properties: { a: { type: string } } }
            B: { type: object, properties: { b: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Return_Types_For_Response_Shapes()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task HeadOnly();");
        generatedCode.Should().Contain("Task DeleteThing();");
        generatedCode.Should().Contain("Task<A> Upsert();");
        generatedCode.Should().Contain("Task Accept();");
        generatedCode.Should().Contain("Task<long> GetPrimitive();");
        generatedCode.Should().Contain("Task<bool> GetBool();");
        generatedCode.Should().Contain("Task<A> GetNullable();");
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
