using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Schemas named like BCL types (Task, Object, String, File) must still produce compilable code.
/// </summary>
public class SchemaNamedLikeBclTypeTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: SchemaNames, version: v1 }
        paths:
          /a:
            get:
              operationId: GetA
              responses:
                '200':
                  description: OK
                  content:
                    application/json:
                      schema: { $ref: '#/components/schemas/Task' }
          /b:
            post:
              operationId: PostB
              requestBody:
                content:
                  application/json:
                    schema: { $ref: '#/components/schemas/Object' }
              responses:
                '200':
                  description: OK
                  content:
                    application/json:
                      schema: { $ref: '#/components/schemas/String' }
          /c:
            get:
              operationId: GetC
              responses:
                '200':
                  description: OK
                  content:
                    application/json:
                      schema: { $ref: '#/components/schemas/File' }
        components:
          schemas:
            Task: { type: object, properties: { id: { type: integer } } }
            Object: { type: object, properties: { id: { type: integer } } }
            String: { type: object, properties: { value: { type: string } } }
            File: { type: object, properties: { name: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Return_Types_For_Schemas_Named_Like_Bcl_Types()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<Task> GetA();");
        generatedCode.Should().Contain("Task<String> PostB(");
        generatedCode.Should().Contain("Task<File> GetC();");
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
