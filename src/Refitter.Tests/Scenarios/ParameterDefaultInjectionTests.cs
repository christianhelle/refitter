using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

// Regression tests for GHSA-h924-jfhp-9vjr. Numeric and boolean query parameter defaults are written into
// the generated method signature or dynamic querystring type. Each payload is a JSON string that ends the
// expression and puts an #error directive on its own line, so the generated code only compiles if it is not emitted.
[TestClass]
public class ParameterDefaultInjectionTests
{
    private const string ParameterDefaultSpec = """
        {"openapi":"3.0.0","info":{"title":"inject","version":"1.0.0"},
         "paths":{"/p":{"get":{"operationId":"GetP",
           "parameters":[
             {"name":"a","in":"query","schema":{"type":"string"}},
             {"name":"n","in":"query","required":false,"schema":{"type":"integer","default":"0);\n#error INJECTED-PARAMETER-DEFAULT\n"}},
             {"name":"b","in":"query","required":false,"schema":{"type":"boolean","default":"true);\n#error INJECTED-PARAMETER-BOOLEAN\n"}}],
           "responses":{"200":{"description":"ok","content":{"application/json":{"schema":{"type":"string"}}}}}}}}}
        """;

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Non_Literal_Parameter_Defaults_Are_Not_Emitted(bool useDynamicQuerystringParameters)
    {
        string generatedCode = await GenerateCode(
            ParameterDefaultSpec,
            s =>
            {
                s.OptionalParameters = true;
                s.UseDynamicQuerystringParameters = useDynamicQuerystringParameters;
            });
        generatedCode.Should().NotContain("INJECTED");
    }

    [TestMethod]
    [TestCategory("Integration")]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Parameter_Default_Injection_Is_Inert_And_Compiles(bool useDynamicQuerystringParameters)
    {
        string generatedCode = await GenerateCode(
            ParameterDefaultSpec,
            s =>
            {
                s.OptionalParameters = true;
                s.UseDynamicQuerystringParameters = useDynamicQuerystringParameters;
            });
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
