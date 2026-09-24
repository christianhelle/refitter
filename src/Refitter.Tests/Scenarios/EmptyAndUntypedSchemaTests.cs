using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Empty schemas, media types without a schema, */* content and untyped arrays.
/// </summary>
public class EmptyAndUntypedSchemaTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Empty, version: v1 }
        paths:
          /e:
            post:
              operationId: PostE
              requestBody:
                content:
                  application/json:
                    schema: {}
              responses:
                '200':
                  description: ok
                  content:
                    application/json:
                      schema: {}
          /f:
            get:
              operationId: GetF
              responses:
                '200':
                  description: ok
                  content:
                    application/json: {}
          /g:
            get:
              operationId: GetG
              responses:
                '200':
                  description: ok
                  content:
                    '*/*':
                      schema: { $ref: '#/components/schemas/Empty' }
        components:
          schemas:
            Empty: { type: object }
            Weird:
              type: object
              properties:
                anything: {}
                arrOfAny: { type: array, items: {} }
                noItems: { type: array }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Maps_Untyped_Schemas_To_Object()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<object> PostE([Body(BodySerializationMethod.Serialized)] object body);");
        generatedCode.Should().Contain("Task GetF();");
        generatedCode.Should().Contain("Task<Empty> GetG();");
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
