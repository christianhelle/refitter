using System.Text.Json;
using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.MSBuild;

[TestClass]
public class SettingsSanitizerTests
{
    private const string Settings =
        """
        {
          // secret=hunter2 in a comment
          "openApiPath": "https://user:password@example.com/openapi.json?token=abc",
          "namespace": "Contoso.Secret",
          "generateContracts": false,
          "multipleInterfaces": "ByTag",
          "typeAccessibility": "not an enum value",
          "includeTags": [ "Pet", "Store" ],
          "responseTypeOverride": { "File_Upload": "IApiResponse" },
          "unknownApiKey": "sk-123", /* block comment */
          "token=sk-456": true,
          "dependencyInjectionSettings": {
            "baseUrl": "https://internal.example.com",
            "maxRetryCount": 3,
            "transientErrorHandler": "polly",
          },
          "apizrSettings": { "integerType": "sk789" },
        }
        """;

    [TestMethod]
    public void Sanitize_Redacts_Free_Text_Values_And_Comments()
    {
        var result = SettingsSanitizer.Sanitize(Settings);

        result.Should().NotBeNull();
        foreach (var secret in new[] { "password", "token", "hunter2", "Contoso", "sk-123", "internal.example.com", "Pet", "File_Upload", "not an enum", "sk-456", "sk789", "unknownApiKey" })
        {
            result.Should().NotContain(secret);
        }
    }

    [TestMethod]
    public void Sanitize_Keeps_Setting_Names_Booleans_Numbers_And_Enums()
    {
        var settings = Parse(SettingsSanitizer.Sanitize(Settings)!);

        settings["openApiPath"].GetString().Should().Be(SettingsSanitizer.Redacted);
        settings["namespace"].GetString().Should().Be(SettingsSanitizer.Redacted);
        settings["generateContracts"].GetBoolean().Should().BeFalse();
        settings["multipleInterfaces"].GetString().Should().Be("ByTag");
        settings["typeAccessibility"].GetString().Should().Be(SettingsSanitizer.Redacted);
        settings["includeTags"].GetString().Should().Be("[2 items]");
        settings["responseTypeOverride"].GetString().Should().Be(SettingsSanitizer.Redacted);
        settings["dependencyInjectionSettings.baseUrl"].GetString().Should().Be(SettingsSanitizer.Redacted);
        settings["dependencyInjectionSettings.maxRetryCount"].GetInt32().Should().Be(3);
        settings["dependencyInjectionSettings.transientErrorHandler"].GetString().Should().Be("Polly");
        settings.Should().NotContainKey("apizrSettings.integerType");
        settings[SettingsSanitizer.UnknownSettingsKey].GetInt32().Should().Be(3);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("not json")]
    [DataRow("[ 1, 2 ]")]
    [DataRow("{ \"openApiPath\": ")]
    [DataRow("{ \"a\": 1 } trailing")]
    [DataRow("{ \"a\": +1 }")]
    [DataRow("{ \"a\": NaN }")]
    [DataRow("{ \"a\": -Infinity }")]
    [DataRow("{ \"a\": 01 }")]
    [DataRow("{ \"a\": \"\\q\" }")]
    [DataRow("{ \"a\": \"tab\there\" }")]
    [DataRow("{ \"a\": \"\\u 1a2\" }")]
    [DataRow("{ \"a\": \"\\u12g4\" }")]
    [DataRow("{ \"a\": 1 \"b\": 2 }")]
    [DataRow("{ \"a\" 1 }")]
    [DataRow("{")]
    [DataRow("{ \"a\": 1,")]
    [DataRow("{ \"a\": [ 1,")]
    [DataRow("{ \"a")]
    public void Sanitize_Returns_Null_For_Invalid_Settings(string contents) =>
        SettingsSanitizer.Sanitize(contents).Should().BeNull();

    [TestMethod]
    [DataRow("{ \"namespace\": \"a\\nb\\rc\\td\\be\\ff\\/g\\\"h\\\\i\\u0041\" } // trailing comment")]
    [DataRow("{ \"namespace\": \"Acme\" } /* unterminated comment")]
    [DataRow("{ \"name\\u0073pace\": \"secret\" }")]
    public void Sanitize_Reads_Escapes_And_Comments(string contents)
    {
        var settings = Parse(SettingsSanitizer.Sanitize(contents)!);

        settings["namespace"].GetString().Should().Be(SettingsSanitizer.Redacted);
    }

    [TestMethod]
    [DataRow("{ \"includeTags\": [] }", "[0 items]")]
    [DataRow("{ \"includeTags\": [ \"Pet\", ] }", "[1 items]")]
    public void Sanitize_Reports_Array_Lengths(string contents, string expected)
    {
        var settings = Parse(SettingsSanitizer.Sanitize(contents)!);

        settings["includeTags"].GetString().Should().Be(expected);
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("-0")]
    [DataRow("12")]
    [DataRow("-1.5")]
    [DataRow("1e5")]
    [DataRow("1E+5")]
    [DataRow("2.5e-3")]
    public void Sanitize_Keeps_Json_Numbers(string number)
    {
        var settings = Parse(SettingsSanitizer.Sanitize($$"""{ "dependencyInjectionSettings": { "maxRetryCount": {{number}} } }""")!);

        settings["dependencyInjectionSettings.maxRetryCount"].GetRawText().Should().Be(number);
    }

    [TestMethod]
    [DataRow("1.")]
    [DataRow(".5")]
    [DataRow("1e")]
    [DataRow("1e+")]
    [DataRow("-")]
    [DataRow("--1")]
    [DataRow("1.5.2")]
    [DataRow("1x")]
    [DataRow("1-2")]
    public void Sanitize_Rejects_Malformed_Numbers(string number) =>
        SettingsSanitizer.Sanitize($$"""{ "dependencyInjectionSettings": { "maxRetryCount": {{number}} } }""").Should().BeNull();

    private static Dictionary<string, JsonElement> Parse(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
}
