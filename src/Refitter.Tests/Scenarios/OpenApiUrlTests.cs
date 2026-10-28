#if !DEBUG
using Refitter.Tests.TestUtilities;
using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;

namespace Refitter.Tests.Scenarios;


[TestClass]
public class OpenApiUrlTests
{
    [TestMethod]
    [DataRow("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/api-with-examples.json")]
    [DataRow("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/callback-example.json")]
    [DataRow("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/link-example.json")]
    [DataRow("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore-expanded.json")]
    [DataRow("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/uspto.json")]
    [DataRow("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/ingram-micro.json")]
    public async Task Can_Build_Generated_Code_From_OpenApi_v3_Url_Json(string url)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = url };
        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue(url);
    }

    [TestMethod]
    [DataRow("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/api-with-examples.yaml")]
    [DataRow("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/callback-example.yaml")]
    [DataRow("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/link-example.yaml")]
    [DataRow("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore-expanded.yaml")]
    [DataRow("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/uspto.yaml")]
    public async Task Can_Build_Generated_Code_From_OpenApi_v3_Url_Yaml(string url)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = url };
        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue(url);
    }
}
#endif
