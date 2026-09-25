using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Schema names containing brackets, dots, leading digits and dashes.
/// </summary>
public class SchemaNameSanitizationEdgeCaseTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: SchemaNames, version: v1 }
        paths:
          /s:
            get:
              operationId: GetS
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/Page[User]' } } }
          /t:
            get:
              operationId: GetT
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/My.Namespace.Type' } } }
          /u:
            get:
              operationId: GetU
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/123Type' } } }
          /v:
            get:
              operationId: GetV
              responses:
                '200':
                  description: ok
                  content: { application/json: { schema: { $ref: '#/components/schemas/type-with-dashes' } } }
        components:
          schemas:
            'Page[User]': { type: object, properties: { items: { type: array, items: { $ref: '#/components/schemas/User' } } } }
            User: { type: object, properties: { id: { type: string } } }
            My.Namespace.Type: { type: object, properties: { id: { type: string } } }
            123Type: { type: object, properties: { id: { type: string } } }
            type-with-dashes: { type: object, properties: { id: { type: string } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Sanitizes_Schema_Names()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<PageOfUser> GetS();");
        generatedCode.Should().Contain("Task<_123Type> GetU();");
        generatedCode.Should().Contain("Task<TypeWithDashes> GetV();");
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
