using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Operations without operationId, including the root path and hyphenated path parameters.
/// </summary>
public class MissingOperationIdTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: NoOpIds, version: v1 }
        paths:
          /users:
            get:
              responses: { '200': { description: ok, content: { application/json: { schema: { type: array, items: { type: string } } } } } }
            post:
              requestBody: { content: { application/json: { schema: { type: object } } } }
              responses: { '201': { description: ok } }
          /users/{id}:
            get:
              parameters: [ { name: id, in: path, required: true, schema: { type: integer } } ]
              responses: { '200': { description: ok } }
          /users/{id}/friends/{friend-id}:
            delete:
              parameters:
                - { name: id, in: path, required: true, schema: { type: integer } }
                - { name: friend-id, in: path, required: true, schema: { type: integer } }
              responses: { '204': { description: ok } }
          /:
            get:
              responses: { '200': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Method_Names_From_Paths()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task UsersGet(int id);");
        generatedCode.Should().Contain("Task Index();");
        generatedCode.Should().Contain("[AliasAs(\"friend-id\")] int friend_id");
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
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces_By_Tag()
    {
        var generatedCode = await GenerateCode(settings => settings.MultipleInterfaces = MultipleInterfaces.ByTag);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    [Skip("https://github.com/christianhelle/refitter/issues/1266")]
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces_By_Endpoint()
    {
        var generatedCode = await GenerateCode(settings => settings.MultipleInterfaces = MultipleInterfaces.ByEndpoint);
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
