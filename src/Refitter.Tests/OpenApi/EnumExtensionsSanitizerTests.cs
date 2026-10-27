using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

[TestClass]
public class EnumExtensionsSanitizerTests
{
    [TestMethod]
    [DataRow("x-enumDescriptions")]
    [DataRow("x-enum-descriptions")]
    public void Converts_Object_To_Array_In_Enum_Order(string keyword)
    {
        var json = $$"""
            {
              "type": "string",
              "enum": ["pending", "done"],
              "{{keyword}}": { "done": "Finished.", "pending": "Not started yet." }
            }
            """;

        var result = JObject.Parse(EnumExtensionsSanitizer.Sanitize(json));

        result[keyword]!.ToObject<string[]>().Should().Equal("Not started yet.", "Finished.");
        result["enum"]!.ToObject<string[]>().Should().Equal("pending", "done");
    }

    [TestMethod]
    [DataRow("x-enumNames")]
    [DataRow("x-enum-names")]
    [DataRow("x-enum-varnames")]
    public void Converts_Names_Object_To_Array_In_Enum_Order(string keyword)
    {
        var json = $$"""{ "type": "integer", "enum": [0, 1], "{{keyword}}": { "1": "Done", "0": "Pending" } }""";

        var result = JObject.Parse(EnumExtensionsSanitizer.Sanitize(json));

        result[keyword]!.ToObject<string[]>().Should().Equal("Pending", "Done");
    }

    [TestMethod]
    [DataRow("x-enumNames")]
    [DataRow("x-enum-names")]
    [DataRow("x-enum-varnames")]
    public void Uses_Enum_Value_As_Name_For_Enum_Values_Without_One(string keyword)
    {
        var json = $$"""{ "enum": [0, "b", null], "{{keyword}}": { "0": "Zero" } }""";

        var result = JObject.Parse(EnumExtensionsSanitizer.Sanitize(json));

        result[keyword]!.ToObject<string[]>().Should().Equal("Zero", "b", "null");
    }

    [TestMethod]
    [DataRow("""[1, 2]""", """{ "2": "Two", "1": "One" }""", "One", "Two")]
    [DataRow("""[true, false]""", """{ "false": "No", "true": "Yes" }""", "Yes", "No")]
    [DataRow("""["a", null]""", """{ "a": "A", "null": "None" }""", "A", "None")]
    public void Matches_Non_String_Enum_Values_By_Their_Json_Text(
        string enumValues,
        string descriptions,
        string first,
        string second)
    {
        var json = $$"""{ "enum": {{enumValues}}, "x-enumDescriptions": {{descriptions}} }""";

        var result = JObject.Parse(EnumExtensionsSanitizer.Sanitize(json));

        result["x-enumDescriptions"]!.ToObject<string[]>().Should().Equal(first, second);
    }

    [TestMethod]
    public void Uses_Empty_Description_For_Enum_Values_Without_One()
    {
        const string json = """
            { "enum": ["a", "b", "c"], "x-enumDescriptions": { "b": "B", "unknown": "U" } }
            """;

        var result = JObject.Parse(EnumExtensionsSanitizer.Sanitize(json));

        result["x-enumDescriptions"]!.ToObject<string[]>().Should().Equal("", "B", "");
    }

    [TestMethod]
    [DataRow("""{ "type": "string", "x-enumDescriptions": { "a": "A" } }""")]
    [DataRow("""{ "type": "string", "enum": "a", "x-enumDescriptions": { "a": "A" } }""")]
    public void Removes_Object_Without_Enum_Array(string json)
    {
        var result = JObject.Parse(EnumExtensionsSanitizer.Sanitize(json));

        result.ContainsKey("x-enumDescriptions").Should().BeFalse();
        result["type"]!.Value<string>().Should().Be("string");
    }

    [TestMethod]
    [DataRow("x-enumDescriptions", "properties")]
    [DataRow("x-enumNames", "properties")]
    [DataRow("x-enum-varnames", "patternProperties")]
    [DataRow("x-enumNames", "schemas")]
    [DataRow("x-enumNames", "definitions")]
    [DataRow("x-enumNames", "$defs")]
    [DataRow("x-enumNames", "dependentSchemas")]
    [DataRow("x-enumDescriptions", "responses")]
    [DataRow("x-enumDescriptions", "parameters")]
    [DataRow("x-enumDescriptions", "examples")]
    [DataRow("x-enumDescriptions", "requestBodies")]
    [DataRow("x-enumDescriptions", "headers")]
    [DataRow("x-enumDescriptions", "securitySchemes")]
    [DataRow("x-enumDescriptions", "securityDefinitions")]
    [DataRow("x-enumDescriptions", "links")]
    [DataRow("x-enumDescriptions", "callbacks")]
    [DataRow("x-enumDescriptions", "pathItems")]
    [DataRow("x-enumDescriptions", "webhooks")]
    [DataRow("x-enumDescriptions", "content")]
    [DataRow("x-enumDescriptions", "encoding")]
    [DataRow("x-enumDescriptions", "variables")]
    public void Does_Not_Change_Schemas_Named_Like_Extensions(string keyword, string map)
    {
        var json = $$"""{ "type": "object", "{{map}}": { "{{keyword}}": { "type": "string" } } }""";

        EnumExtensionsSanitizer.Sanitize(json).Should().BeSameAs(json);
    }

    [TestMethod]
    [DataRow("""[1e2, 2.50]""", """{ "100": "Hundred", "2.5": "Half" }""")]
    [DataRow("""[100, 2.5]""", """{ "1e2": "Hundred", "2.50": "Half" }""")]
    [DataRow("""[1e2, 2.50]""", """{ "1e2": "Hundred", "2.50": "Half" }""")]
    public void Matches_Numeric_Enum_Values_Written_Differently(string enumValues, string descriptions)
    {
        var json = $$"""{ "enum": {{enumValues}}, "x-enumDescriptions": {{descriptions}} }""";

        var result = JObject.Parse(EnumExtensionsSanitizer.Sanitize(json));

        result["x-enumDescriptions"]!.ToObject<string[]>().Should().Equal("Hundred", "Half");
    }

    [TestMethod]
    public void Ignores_Non_Numeric_Keys_When_Matching_Numeric_Enum_Values()
    {
        const string json = """
            { "enum": [1, 2.5], "x-enumDescriptions": { "other": "Other", "1e0": "One" } }
            """;

        var result = JObject.Parse(EnumExtensionsSanitizer.Sanitize(json));

        result["x-enumDescriptions"]!.ToObject<string[]>().Should().Equal("One", "");
    }

    [TestMethod]
    public void Does_Not_Match_Distinct_Numbers_That_Round_To_The_Same_Double()
    {
        const string json = """
            {
              "enum": [9007199254740992, 9007199254740993],
              "x-enumNames": { "9.007199254740992e15": "First", "9.007199254740993e15": "Second" }
            }
            """;

        var result = JObject.Parse(EnumExtensionsSanitizer.Sanitize(json));

        result["x-enumNames"]!.ToObject<string[]>().Should().Equal("First", "Second");
    }

    [TestMethod]
    [DataRow("responses")]
    [DataRow("properties")]
    [DataRow("schemas")]
    public void Converts_Extensions_On_Schema_Properties_Named_Like_Maps(string propertyName)
    {
        var json = $$"""
            {
              "properties": {
                "{{propertyName}}": { "enum": ["a"], "x-enumDescriptions": { "a": "A" } },
                "other": { "type": "string", "x-enumNames": { "a": "A" } }
              }
            }
            """;

        var result = JObject.Parse(EnumExtensionsSanitizer.Sanitize(json));

        result["properties"]![propertyName]!["x-enumDescriptions"]!.ToObject<string[]>().Should().Equal("A");
        ((JObject)result["properties"]!["other"]!).ContainsKey("x-enumNames").Should().BeFalse();
    }

    [TestMethod]
    public void Keeps_Named_Map_Entries_Under_Schema_Properties_Named_Like_Maps()
    {
        const string json = """
            { "properties": { "responses": { "type": "object", "properties": { "x-enumNames": { "type": "string" } } } } }
            """;

        EnumExtensionsSanitizer.Sanitize(json).Should().BeSameAs(json);
    }

    [TestMethod]
    public void Keeps_High_Precision_Numbers_When_Converting()
    {
        const string json = """
            {
              "properties": {
                "status": { "enum": ["a"], "x-enumDescriptions": { "a": "A" } },
                "amount": { "type": "number", "default": 0.1000000000000000055, "example": 12345678901234567.89 }
              }
            }
            """;

        var result = EnumExtensionsSanitizer.Sanitize(json);

        result.Should().Contain("\"default\":0.1000000000000000055");
        result.Should().Contain("\"example\":12345678901234567.89");
    }

    [TestMethod]
    public void Converts_Documents_With_Numbers_Outside_Decimal_Range()
    {
        const string json = """
            { "enum": ["a"], "x-enumDescriptions": { "a": "A" }, "example": 1e300 }
            """;

        var result = JObject.Parse(EnumExtensionsSanitizer.Sanitize(json));

        result["x-enumDescriptions"]!.ToObject<string[]>().Should().Equal("A");
        result["example"]!.Value<double>().Should().Be(1e300);
    }

    [TestMethod]
    [DataRow("x-enumDescriptions", 6)]
    [DataRow("x-enumNames", 0)]
    public void Converts_Escaped_Property_Names(string keyword, int escapedIndex)
    {
        // Built here rather than passed in, so the JSON gets a literal \uXXXX escape
        var escapedKeyword = keyword.Substring(0, escapedIndex) +
            $"\\u{(int)keyword[escapedIndex]:X4}" +
            keyword.Substring(escapedIndex + 1);
        var json = $$"""{ "enum": ["a"], "{{escapedKeyword}}": { "a": "A" } }""";
        json.Should().Contain("\\u00");

        var result = JObject.Parse(EnumExtensionsSanitizer.Sanitize(json));

        result[keyword]!.ToObject<string[]>().Should().Equal("A");
    }

    [TestMethod]
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

        var result = EnumExtensionsSanitizer.Sanitize(json);

        result.Should().Contain("\"default\":\"2020-01-01T00:00:00+02:00\"");
        result.Should().Contain("\"default\":0.1");
    }

    [TestMethod]
    [DataRow("""{ "enum": ["a", "b"], "x-enumDescriptions": ["A", "B"] }""")]
    [DataRow("""{ "type": "string", "enum": ["a", "b"] }""")]
    [DataRow("""{ "description": "x-enumDescriptions: { \"a\": \"A\" }" }""")]
    [DataRow("openapi: 3.0.1")]
    public void Returns_Same_Instance_When_Nothing_To_Convert(string content)
    {
        EnumExtensionsSanitizer.Sanitize(content).Should().BeSameAs(content);
    }

    [TestMethod]
    public void Returns_Same_Instance_When_Escapes_Hide_Nothing_To_Convert()
    {
        const string json = "{ \"description\": \"caf\\u00e9\", \"maximum\": 0.1000000000000000055 }";

        EnumExtensionsSanitizer.Sanitize(json).Should().BeSameAs(json);
    }
}
