using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Path and query parameters whose names only differ by case must not collide when Refit binds the route (#1338).
/// </summary>
public class CaseInsensitiveRouteParameterTests
{
    private const string OpenApiSpec = """
        {
          "openapi": "3.0.1",
          "info": { "title": "Repro", "version": "1.0" },
          "paths": {
            "/api/parent/{id}/child": {
              "get": {
                "operationId": "GetSomething",
                "parameters": [
                  { "name": "id", "in": "path", "required": true, "schema": { "type": "integer", "format": "int32" } },
                  { "name": "ID", "in": "query", "schema": { "type": "integer", "format": "int32" } }
                ],
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Renames_Route_Placeholder_Colliding_With_Query_Parameter()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Get(\"/api/parent/{id2}/child\")]");
        generatedCode.Should().Contain("Task GetSomething([AliasAs(\"id2\")] int id, [Query, AliasAs(\"ID\")] int? iD);");
    }

    [Test]
    [Arguments(RefitClient.Reflection)]
    [Arguments(RefitClient.Generated)]
    public async Task Refit_Sends_Path_And_Query_Parameters(RefitClient client)
    {
        var generatedCode = await GenerateCode();

        var request = await RefitRequestCapture.SendAsync(client, generatedCode, "IRepro", "GetSomething", 1, 2);

        request.RequestUri!.PathAndQuery.Should().Be("/api/parent/1/child?ID=2");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
