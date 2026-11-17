using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

// Regression tests for GHSA-h924-jfhp-9vjr. NJsonSchema writes numeric and boolean defaults and integer
// enum values into the generated code as they are. Each payload is a JSON string that ends the statement
// and puts an #error directive on its own line, so the generated code only compiles if it is not emitted.
[TestClass]
public class SchemaValueInjectionTests
{
    private const string PropertyDefaultSpec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/p":{"get":{"operationId":"GetP","responses":{"200":{"description":"ok","content":{"application/json":{"schema":{"$ref":"#/components/schemas/Dto"}}}}}}}},
         "components":{"schemas":{"Dto":{"type":"object","properties":{
           "name":{"type":"string"},
           "age":{"type":"integer","default":"0;\n#error INJECTED-INTEGER-DEFAULT\n"},
           "ratio":{"type":"number","default":"0;\n#error INJECTED-NUMBER-DEFAULT\n"},
           "flag":{"type":"boolean","default":"true;\n#error INJECTED-BOOLEAN-DEFAULT\n"},
           "count":{"type":"integer","default":"42"},
           "enabled":{"type":"boolean","default":true}}}}}}
        """;

    private const string IntegerEnumSpec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/p":{"get":{"operationId":"GetP","responses":{"200":{"description":"ok","content":{"application/json":{"schema":{"$ref":"#/components/schemas/Dto"}}}}}}}},
         "components":{"schemas":{
           "Dto":{"type":"object","properties":{"name":{"type":"string"},"kind":{"$ref":"#/components/schemas/Kind"},"level":{"$ref":"#/components/schemas/Level"}}},
           "Kind":{"type":"integer","enum":[1,"0,\n#error INJECTED-ENUM\n"]},
           "Level":{"type":"integer","enum":[1,2,"3"]}}}}
        """;

    [TestMethod]
    public async Task Non_Literal_Property_Defaults_Are_Dropped()
    {
        string generatedCode = await GenerateCode(PropertyDefaultSpec);
        generatedCode.Should().NotContain("INJECTED");
    }

    [TestMethod]
    public async Task Literal_Property_Defaults_Are_Kept()
    {
        string generatedCode = await GenerateCode(PropertyDefaultSpec);
        generatedCode.Should().Contain("= 42;");
        generatedCode.Should().Contain("= true;");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Property_Default_Injection_Is_Inert_And_Compiles()
    {
        string generatedCode = await GenerateCode(PropertyDefaultSpec);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [TestMethod]
    public async Task Integer_Enum_With_Non_Integer_Entry_Is_Not_Emitted_As_Member_Value()
    {
        string generatedCode = await GenerateCode(IntegerEnumSpec);
        generatedCode.Should().NotMatchRegex(@"= 0,\s*#error");
    }

    [TestMethod]
    public async Task Integer_Enum_With_Integer_Strings_Keeps_Values()
    {
        string generatedCode = await GenerateCode(IntegerEnumSpec);
        generatedCode.Should().MatchRegex(@"_3 = 3,");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Integer_Enum_Injection_Is_Inert_And_Compiles()
    {
        string generatedCode = await GenerateCode(IntegerEnumSpec);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(string spec, Action<RefitGeneratorSettings>? configure = null)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);
        RefitGeneratorSettings settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        configure?.Invoke(settings);
        RefitGenerator generator = await RefitGenerator.CreateAsync(settings);
        return generator.Generate();
    }
}
