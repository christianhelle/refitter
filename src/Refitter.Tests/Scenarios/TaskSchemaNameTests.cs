using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// A schema named Task shadows System.Threading.Tasks.Task for void operations (#1275).
/// </summary>
public class TaskSchemaNameTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Tasks, version: v1 }
        paths:
          /tasks:
            get:
              operationId: GetTasks
              responses: { '200': { description: ok, content: { application/json: { schema: { type: array, items: { $ref: '#/components/schemas/Task' } } } } } }
            delete:
              operationId: DeleteTasks
              responses: { '204': { description: ok } }
        components:
          schemas:
            Task: { type: object, properties: { id: { type: integer } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Category("Integration")]
    [Skip("https://github.com/christianhelle/refitter/issues/1275")]
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
