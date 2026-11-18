using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

// Settings values written into C# string literals must be escaped. Each payload closes the literal and puts an
// #error directive on its own line, so the generated code only compiles if the payload stays inside the literal.
[TestClass]
public class SettingsLiteralInjectionTests
{
    private const string Payload = "x\"))]\n#error INJECTED\n//";
    private const string EscapedPayload = "x\\\"))]\\n#error INJECTED\\n//";

    private const string Spec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/pets":{"get":{"operationId":"getPets",
           "parameters":[
             {"name":"day","in":"query","schema":{"type":"string","format":"date"}},
             {"name":"at","in":"query","schema":{"type":"string","format":"date-time"}}],
           "responses":{"200":{"description":"ok"}}}}}}
        """;

    [TestMethod]
    public async Task DependencyInjection_BaseUrl_Is_Escaped()
    {
        string generatedCode = await GenerateCode(s => s.DependencyInjectionSettings = new DependencyInjectionSettings { BaseUrl = Payload });
        generatedCode.Should().Contain($"new Uri(\"{EscapedPayload}\")");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task DependencyInjection_BaseUrl_Injection_Is_Inert_And_Compiles()
    {
        string generatedCode = await GenerateCode(s => s.DependencyInjectionSettings = new DependencyInjectionSettings { BaseUrl = Payload });
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [TestMethod]
    public async Task Apizr_BaseUrl_Is_Escaped()
    {
        string generatedCode = await GenerateCode(ConfigureApizr);
        generatedCode.Should().Contain($".WithBaseAddress(\"{EscapedPayload}\"");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Apizr_BaseUrl_Injection_Is_Inert_And_Compiles()
    {
        string generatedCode = await GenerateCode(ConfigureApizr);
        BuildHelper.BuildApizrCSharp(generatedCode).Should().BeTrue();
    }

    [TestMethod]
    public async Task Date_Formats_Are_Escaped()
    {
        string generatedCode = await GenerateCode(ConfigureDateFormats);
        generatedCode.Should().Contain($"Query(Format = \"d{EscapedPayload}\")");
        generatedCode.Should().Contain($"Query(Format = \"dt{EscapedPayload}\")");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Date_Format_Injection_Is_Inert_And_Compiles()
    {
        string generatedCode = await GenerateCode(ConfigureDateFormats);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static void ConfigureApizr(RefitGeneratorSettings settings)
    {
        settings.ApizrSettings = new ApizrSettings { WithRegistrationHelper = true };
        settings.DependencyInjectionSettings = new DependencyInjectionSettings { BaseUrl = Payload };
    }

    private static void ConfigureDateFormats(RefitGeneratorSettings settings) =>
        settings.CodeGeneratorSettings = new CodeGeneratorSettings
        {
            DateFormat = "d" + Payload,
            DateTimeFormat = "dt" + Payload,
        };

    private static async Task<string> GenerateCode(Action<RefitGeneratorSettings> configure)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(Spec);
        RefitGeneratorSettings settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        configure(settings);
        RefitGenerator generator = await RefitGenerator.CreateAsync(settings);
        return generator.Generate();
    }
}
