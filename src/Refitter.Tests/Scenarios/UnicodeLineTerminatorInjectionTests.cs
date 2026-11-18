using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

// C# ends a string literal at U+2028 and U+2029 just like at CR or LF. Each payload puts an #error directive
// between two of these separators, so the generated code only compiles if the payload stays inside the
// string literal of the Refit attribute or parameter default it is written into.
[TestClass]
public class UnicodeLineTerminatorInjectionTests
{
    private const string Payload = "a{LS}#error INJECTED{LS}//";

    private const string PathSpec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/pets/{PAYLOAD}":{"get":{"operationId":"getPets","responses":{"200":{"description":"ok"}}}}}}
        """;

    private const string HeaderNameSpec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/pets":{"get":{"operationId":"getPets",
           "parameters":[{"name":"{PAYLOAD}","in":"header","schema":{"type":"string"}}],
           "responses":{"200":{"description":"ok"}}}}}}
        """;

    private const string QueryNameSpec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/pets":{"get":{"operationId":"getPets",
           "parameters":[{"name":"{PAYLOAD}","in":"query","schema":{"type":"string"}}],
           "responses":{"200":{"description":"ok"}}}}}}
        """;

    private const string QueryDefaultSpec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/pets":{"get":{"operationId":"getPets",
           "parameters":[{"name":"q","in":"query","schema":{"type":"string","default":"{PAYLOAD}"}}],
           "responses":{"200":{"description":"ok"}}}}}}
        """;

    private const string FormFieldSpec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/pets":{"post":{"operationId":"createPet",
           "requestBody":{"content":{"multipart/form-data":{"schema":{"type":"object","properties":{"{PAYLOAD}":{"type":"string"}}}}}},
           "responses":{"200":{"description":"ok"}}}}}}
        """;

    private const string ContentTypeSpec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/pets":{"post":{"operationId":"createPet",
           "requestBody":{"content":{"text/{PAYLOAD}":{"schema":{"type":"string"}}}},
           "responses":{"200":{"description":"ok","content":{"application/{PAYLOAD}":{"schema":{"type":"string"}}}}}}}}}
        """;

    private const string SecuritySchemeSpec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/pets":{"get":{"operationId":"getPets","security":[{"key":[]}],"responses":{"200":{"description":"ok"}}}}},
         "components":{"securitySchemes":{"key":{"type":"apiKey","in":"header","name":"{PAYLOAD}"}}}}
        """;

    public static IEnumerable<object[]> Cases()
    {
        foreach (string separator in new[] { "\\u2028", "\\u2029" })
        {
            yield return ["path", separator];
            yield return ["header-name", separator];
            yield return ["query-name", separator];
            yield return ["query-default", separator];
            yield return ["form-field", separator];
            yield return ["content-type", separator];
            yield return ["security-scheme", separator];
        }
    }

    [TestMethod]
    [DynamicData(nameof(Cases))]
    public async Task Unicode_Line_Terminators_Are_Escaped(string scenario, string separator)
    {
        string generatedCode = await GenerateCode(scenario, separator);
        char rawSeparator = (char)Convert.ToInt32(separator.Substring(2), 16);
        generatedCode.Should().NotContain(rawSeparator + "#error INJECTED");
        generatedCode.Should().Contain(separator + "#error INJECTED" + separator);
    }

    [TestMethod]
    [TestCategory("Integration")]
    [DynamicData(nameof(Cases))]
    public async Task Unicode_Line_Terminator_Injection_Is_Inert_And_Compiles(string scenario, string separator)
    {
        string generatedCode = await GenerateCode(scenario, separator);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(string scenario, string separator)
    {
        string spec = scenario switch
        {
            "path" => PathSpec,
            "header-name" => HeaderNameSpec,
            "query-name" => QueryNameSpec,
            "query-default" => QueryDefaultSpec,
            "form-field" => FormFieldSpec,
            "content-type" => ContentTypeSpec,
            "security-scheme" => SecuritySchemeSpec,
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null),
        };

        string json = spec.Replace("{PAYLOAD}", Payload).Replace("{LS}", separator);
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(json);
        RefitGeneratorSettings settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            OptionalParameters = scenario == "query-default",
            AuthenticationHeaderStyle = scenario == "security-scheme"
                ? AuthenticationHeaderStyle.Parameter
                : AuthenticationHeaderStyle.None,
        };
        RefitGenerator generator = await RefitGenerator.CreateAsync(settings);
        return generator.Generate();
    }
}
