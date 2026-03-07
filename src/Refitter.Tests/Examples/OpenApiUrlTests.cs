#if !DEBUG
using Refitter.Tests.TestUtilities;
using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class OpenApiUrlTests
{
    [Test]
    [Arguments("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/api-with-examples.json")]
    [Arguments("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/callback-example.json")]
    [Arguments("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/link-example.json")]
    [Arguments("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore-expanded.json")]
    [Arguments("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/uspto.json")]
    [Arguments("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/hubspot-events.json")]
    [Arguments("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/hubspot-webhooks.json")]
    [Arguments("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/ingram-micro.json")]
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

    [Test]
    [Arguments("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/api-with-examples.yaml")]
    [Arguments("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/callback-example.yaml")]
    [Arguments("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/link-example.yaml")]
    [Arguments("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore-expanded.yaml")]
    [Arguments("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/uspto.yaml")]
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
