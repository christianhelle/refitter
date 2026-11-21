using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// A request body declared as a $ref to components/requestBodies must get the same Content-Type header
/// as one declared inline under the operation (#1430).
/// </summary>
[TestClass]
public class ReferencedRequestBodyContentTypeTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.3
        info: {title: T, version: "1.0"}
        paths:
          /items:
            post:
              operationId: createItem
              requestBody: {$ref: "#/components/requestBodies/ItemBody"}
              responses:
                "200": {description: OK}
        components:
          requestBodies:
            ItemBody:
              required: true
              content:
                application/json:
                  schema: {$ref: "#/components/schemas/Item"}
          schemas:
            Item:
              type: object
              properties: {name: {type: string}}
        """;

    [TestMethod]
    public async Task Referenced_Request_Body_Gets_Content_Type_Header()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Headers(\"Content-Type: application/json\")]");
        generatedCode.Should().Contain("Task CreateItem([Body] Item body);");
    }

    [TestMethod]
    public async Task Referenced_Request_Body_Respects_Disabled_Content_Type_Headers()
    {
        var generatedCode = await GenerateCode(s => s.AddContentTypeHeaders = false);
        generatedCode.Should().NotContain("Content-Type");
    }

    [TestMethod]
    [TestCategory("Integration")]
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
