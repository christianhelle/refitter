using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class EnumDescriptionsSanitizerTests
{
    [Test]
    [Arguments("x-enumDescriptions")]
    [Arguments("x-enum-descriptions")]
    public void Converts_Object_To_Array_In_Enum_Order(string keyword)
    {
        var json = $$"""
            {
              "type": "string",
              "enum": ["pending", "done"],
              "{{keyword}}": { "done": "Finished.", "pending": "Not started yet." }
            }
            """;

        var result = JObject.Parse(EnumDescriptionsSanitizer.Sanitize(json));

        result[keyword]!.ToObject<string[]>().Should().Equal("Not started yet.", "Finished.");
        result["enum"]!.ToObject<string[]>().Should().Equal("pending", "done");
    }

    [Test]
    [Arguments("""[1, 2]""", """{ "2": "Two", "1": "One" }""", "One", "Two")]
    [Arguments("""[true, false]""", """{ "false": "No", "true": "Yes" }""", "Yes", "No")]
    [Arguments("""["a", null]""", """{ "a": "A", "null": "None" }""", "A", "None")]
    public void Matches_Non_String_Enum_Values_By_Their_Json_Text(
        string enumValues,
        string descriptions,
        string first,
        string second)
    {
        var json = $$"""{ "enum": {{enumValues}}, "x-enumDescriptions": {{descriptions}} }""";

        var result = JObject.Parse(EnumDescriptionsSanitizer.Sanitize(json));

        result["x-enumDescriptions"]!.ToObject<string[]>().Should().Equal(first, second);
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
