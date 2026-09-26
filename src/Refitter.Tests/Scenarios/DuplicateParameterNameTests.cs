using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Path parameters differing by case and path/multipart name clashes produce duplicate parameters (#1269).
/// </summary>
public class DuplicateParameterNameTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: DupParams, version: v1 }
        paths:
          /search:
            get:
              operationId: Search
              parameters:
                - { name: cancellationToken, in: query, schema: { type: string } }
              responses: { '204': { description: ok } }
          /v1/{Id}/items/{id}:
            get:
              operationId: GetItem
              parameters:
                - { name: Id, in: path, required: true, schema: { type: string } }
                - { name: id, in: path, required: true, schema: { type: string } }
              responses: { '204': { description: ok } }
          /upload/{id}:
            post:
              operationId: Upload
              parameters: [ { name: id, in: path, required: true, schema: { type: string } } ]
              requestBody:
                content:
                  multipart/form-data:
                    schema: { type: object, properties: { id: { type: string }, file: { type: string, format: binary } } }
              responses: { '204': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Renames_Duplicate_Parameters_And_Keeps_Wire_Names()
    {
        var generatedCode = await GenerateCode(settings => settings.UseCancellationTokens = true);
        generatedCode.Should().Contain("Task Search([AliasAs(\"cancellationToken\")] [Query] string cancellationToken2, CancellationToken cancellationToken = default);");
        generatedCode.Should().Contain("Task GetItem([AliasAs(\"Id\")] string idPath, [AliasAs(\"id\")] string idPath2, CancellationToken cancellationToken = default);");
        generatedCode.Should().Contain("[AliasAs(\"id\")] string id2");
        generatedCode.Should().Contain("<param name=\"cancellationToken2\">");
        generatedCode.Should().Contain("<param name=\"idPath2\">");
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
    public async Task Can_Build_Generated_Code_With_Cancellation_Tokens()
    {
        var generatedCode = await GenerateCode(settings => settings.UseCancellationTokens = true);
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
