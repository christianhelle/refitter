using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests;


[TestClass]
public class PathParametersWithUrlTests
{
    [TestMethod]
    [DataRow("https://petstore3.swagger.io/api/v3/openapi.json")]
#if !DEBUG
    [DataRow("https://petstore3.swagger.io/api/v3/openapi.yaml")]
    [DataRow("https://petstore.swagger.io/v2/swagger.json")]
    [DataRow("https://petstore.swagger.io/v2/swagger.yaml")]
#endif
    public async Task Can_Generate_Code(string url)
    {
        var generatedCode = await GenerateCode(url);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    [DataRow("https://petstore3.swagger.io/api/v3/openapi.json")]
#if !DEBUG
    [DataRow("https://petstore3.swagger.io/api/v3/openapi.yaml")]
    [DataRow("https://petstore.swagger.io/v2/swagger.json")]
    [DataRow("https://petstore.swagger.io/v2/swagger.yaml")]
#endif
    public async Task Can_Build_Generated_Code(string url)
    {
        var generatedCode = await GenerateCode(url);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(string url)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = url };
        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
