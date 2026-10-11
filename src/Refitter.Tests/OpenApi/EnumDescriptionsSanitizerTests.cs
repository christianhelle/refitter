using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class EnumDescriptionsSanitizerTests
{
    [Test]
    public void Converts_Object_To_Array_In_Enum_Order()
    {
        const string json = """
            {
              "type": "string",
              "enum": ["pending", "done"],
              "x-enumDescriptions": { "done": "Finished.", "pending": "Not started yet." }
            }
            """;

        var result = JObject.Parse(EnumDescriptionsSanitizer.Sanitize(json));

        result["x-enumDescriptions"]!.ToObject<string[]>().Should().Equal("Not started yet.", "Finished.");
        result["enum"]!.ToObject<string[]>().Should().Equal("pending", "done");
    }

    [Test]
    public void Uses_Empty_Description_For_Enum_Values_Without_One()
    {
        const string json = """
            { "enum": ["a", "b", "c"], "x-enumDescriptions": { "b": "B", "unknown": "U" } }
            """;

        var result = JObject.Parse(EnumDescriptionsSanitizer.Sanitize(json));

        result["x-enumDescriptions"]!.ToObject<string[]>().Should().Equal("", "B", "");
    }

    [Test]
    [Arguments("""{ "type": "string", "x-enumDescriptions": { "a": "A" } }""")]
    [Arguments("""{ "type": "string", "enum": "a", "x-enumDescriptions": { "a": "A" } }""")]
    public void Removes_Object_Without_Enum_Array(string json)
    {
        var result = JObject.Parse(EnumDescriptionsSanitizer.Sanitize(json));

        result.ContainsKey("x-enumDescriptions").Should().BeFalse();
        result["type"]!.Value<string>().Should().Be("string");
    }

    [Test]
    public void Keeps_Other_Values_When_Converting()
    {
        const string json = """
            {
              "properties": {
                "status": { "enum": ["a"], "x-enumDescriptions": { "a": "A" } },
                "since": { "type": "string", "format": "date-time", "default": "2020-01-01T00:00:00+02:00" },
                "ratio": { "type": "number", "default": 0.1 }
              }
            }
            """;

        var result = EnumDescriptionsSanitizer.Sanitize(json);

        result.Should().Contain("\"default\":\"2020-01-01T00:00:00+02:00\"");
        result.Should().Contain("\"default\":0.1");
    }

    [Test]
    [Arguments("""{ "enum": ["a", "b"], "x-enumDescriptions": ["A", "B"] }""")]
    [Arguments("""{ "type": "string", "enum": ["a", "b"] }""")]
    [Arguments("""{ "description": "x-enumDescriptions: { \"a\": \"A\" }" }""")]
    [Arguments("openapi: 3.0.1")]
    public void Returns_Same_Instance_When_Nothing_To_Convert(string content)
    {
        EnumDescriptionsSanitizer.Sanitize(content).Should().BeSameAs(content);
    }
}
