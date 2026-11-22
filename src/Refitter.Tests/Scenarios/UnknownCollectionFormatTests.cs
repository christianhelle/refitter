using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// A Swagger 2.0 parameter with an unrecognised collectionFormat (#1424).
/// </summary>
[TestClass]
public class UnknownCollectionFormatTests
{
    private const string OpenApiSpec = """
        swagger: "2.0"
        info: {title: T, version: "1.0"}
        host: example.com
        paths:
          /items:
            get:
              operationId: getItems
              parameters:
                - name: ids
                  in: query
                  type: array
                  items: {type: string}
                  collectionFormat: bogus
              responses:
                "200": {description: OK}
        """;

    [TestMethod]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task GetItems(");
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
