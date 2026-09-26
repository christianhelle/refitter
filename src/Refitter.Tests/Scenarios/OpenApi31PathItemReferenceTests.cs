using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// OpenAPI 3.1 path items referenced from components/pathItems (#1274).
/// </summary>
public class OpenApi31PathItemReferenceTests
{
    private const string OpenApiSpec = """
        openapi: 3.1.0
        info: { title: PathItems, version: v1 }
        paths:
          /items/{id}:
            $ref: '#/components/pathItems/ItemById'
        components:
          pathItems:
            ItemById:
              get:
                operationId: GetItem
                parameters: [ { name: id, in: path, required: true, schema: { type: string } } ]
                responses: { '204': { description: ok } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Operations_From_Referenced_Path_Items()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task GetItem(string id);");
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
    public async Task Can_Build_Generated_Code_From_Json()
    {
        const string json = """
            {
              "openapi": "3.1.0",
              "info": { "title": "PathItems", "version": "v1" },
              "paths": { "/items/{id}": { "$ref": "#/components/pathItems/ItemById" } },
              "components": {
                "pathItems": {
                  "ItemById": {
                    "parameters": [ { "name": "id", "in": "path", "required": true, "schema": { "type": "string" } } ],
                    "get": { "operationId": "GetItem", "responses": { "204": { "description": "ok" } } },
                    "delete": { "operationId": "DeleteItem", "responses": { "204": { "description": "ok" } } }
                  }
                }
              }
            }
            """;
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(json);
        var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = swaggerFile });
        var generatedCode = sut.Generate();

        generatedCode.Should().Contain("Task GetItem(string id);");
        generatedCode.Should().Contain("Task DeleteItem(string id);");
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
