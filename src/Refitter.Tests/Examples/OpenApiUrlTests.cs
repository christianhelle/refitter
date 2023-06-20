#if !DEBUG
using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class OpenApiUrlTests
{
    [Theory]
    [InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/api-with-examples.json")]
    [InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/callback-example.json")]
    [InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/link-example.json")]
    //[InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore-expanded.json")]
    [InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/uspto.json")]
    [InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/hubspot-events.json")]
    [InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/hubspot-webhooks.json")]
    [InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/ingram-micro.json")]
    public async Task Can_Build_Generated_Code_From_OpenApi_v3_Url_Json(string url)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = url };
        var sut = await RefitGenerator.CreateAsync(settings);
        var generateCode = sut.Generate();
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/api-with-examples.yaml")]
    [InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/callback-example.yaml")]
    [InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/link-example.yaml")]
    //[InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore-expanded.yaml")]
    [InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/uspto.yaml")]
    public async Task Can_Build_Generated_Code_From_OpenApi_v3_Url_Yaml(string url)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = url };
        var sut = await RefitGenerator.CreateAsync(settings);
        var generateCode = sut.Generate();
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }
}
#endif