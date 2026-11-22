using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// An OpenAPI 3.1 tuple schema using prefixItems with items: false (#1427).
/// </summary>
[TestClass]
public class PrefixItemsBooleanItemsTests
{
    private const string OpenApiSpec = """
        openapi: 3.1.0
        info: {title: T, version: "1.0"}
        paths:
          /items:
            get:
              operationId: getItems
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema: {$ref: "#/components/schemas/Pair"}
        components:
          schemas:
            Pair:
              type: array
              prefixItems:
                - type: string
                - type: integer
              items: false
        """;

    [TestMethod]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<ICollection<object>> GetItems();");
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
