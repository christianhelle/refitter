using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// An operation callback that references components/callbacks (#1426).
/// </summary>
[TestClass]
public class CallbackReferenceTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.3
        info: {title: T, version: "1.0"}
        paths:
          /subscribe:
            post:
              operationId: subscribe
              callbacks:
                onEvent:
                  $ref: "#/components/callbacks/OnEvent"
              responses:
                "200": {description: OK}
        components:
          callbacks:
            OnEvent:
              "{$request.body#/url}":
                post:
                  requestBody:
                    content:
                      application/json:
                        schema: {type: object}
                  responses:
                    "200": {description: OK}
        """;

    [TestMethod]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task Subscribe(");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = swaggerFile });
        return sut.Generate();
    }
}
