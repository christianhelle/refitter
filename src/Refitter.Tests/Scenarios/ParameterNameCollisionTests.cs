using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Parameters with the same name in different locations (path, query, header, body).
/// </summary>
public class ParameterNameCollisionTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Collisions, version: v1 }
        paths:
          /c/{body}:
            post:
              operationId: PostC
              parameters:
                - { name: body, in: path, required: true, schema: { type: string } }
                - { name: cancellationToken, in: query, schema: { type: string } }
                - { name: Body, in: header, schema: { type: string } }
              requestBody:
                content: { application/json: { schema: { type: object, properties: { x: { type: string } } } } }
              responses: { '204': { description: ok } }
          /dup:
            get:
              operationId: GetDup
              parameters:
                - { name: id, in: query, schema: { type: string } }
                - { name: id, in: header, schema: { type: string } }
              responses: { '204': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Disambiguates_Parameters_By_Location()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[AliasAs(\"body\")] string bodyPath");
        generatedCode.Should().Contain("[Header(\"Body\")] string bodyHeader");
        generatedCode.Should().Contain("[Query, AliasAs(\"id\")] string idQuery");
        generatedCode.Should().Contain("[Header(\"id\")] string idHeader");
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
