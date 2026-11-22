using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

[TestClass]
public class CallbackReferenceSanitizerTests
{
    [TestMethod]
    public void Replaces_Local_Callback_Reference_With_Empty_Callback()
    {
        var json = """
            {
              "paths": { "/a": { "post": { "callbacks": { "onEvent": { "$ref": "#/components/callbacks/OnEvent" } } } } },
              "components": { "callbacks": { "OnEvent": { "{$request.body#/url}": { "post": {} } } } }
            }
            """;

        var result = JObject.Parse(OpenApiJsonNormalizer.NormalizeDocument(json));

        ((JObject)result["paths"]!["/a"]!["post"]!["callbacks"]!["onEvent"]!).Count.Should().Be(0);
        result["components"]!["callbacks"]!["OnEvent"]!["{$request.body#/url}"].Should().NotBeNull();
    }

    [TestMethod]
    public void Replaces_Reference_Chain_Inside_Components()
    {
        var json = """{ "components": { "callbacks": { "Alias": { "$ref": "#/components/callbacks/OnEvent" } } } }""";

        var result = JObject.Parse(OpenApiJsonNormalizer.NormalizeDocument(json));

        ((JObject)result["components"]!["callbacks"]!["Alias"]!).Count.Should().Be(0);
    }

    [TestMethod]
    public void Keeps_Inline_Callback()
    {
        var json = """{ "callbacks": { "onEvent": { "{$request.body#/url}": { "post": {} } } } }""";

        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
    }

    [TestMethod]
    public void Keeps_Reference_To_Another_Document()
    {
        var json = """{ "callbacks": { "onEvent": { "$ref": "other.yaml#/components/callbacks/OnEvent" } } }""";

        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
    }

    [TestMethod]
    public void Keeps_Schema_Property_Named_Callbacks()
    {
        var json = """{ "properties": { "callbacks": { "$ref": "#/components/callbacks/OnEvent" } } }""";

        OpenApiJsonNormalizer.NormalizeDocument(json).Should().BeSameAs(json);
    }
}
