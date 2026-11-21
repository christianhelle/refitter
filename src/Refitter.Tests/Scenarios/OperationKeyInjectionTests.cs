using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Regression coverage for GHSA-2p8r-7mx6-gh86: the operation key under a path item was emitted
/// as a Refit attribute name, so a crafted key could inject arbitrary C# into the generated client.
/// </summary>
[TestClass]
public class OperationKeyInjectionTests
{
    private static string BuildSpec(string verb) =>
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{\"/p\":{"
        + System.Text.Json.JsonSerializer.Serialize(verb)
        + ":{\"operationId\":\"doIt\",\"responses\":{\"200\":{\"description\":\"ok\"}}}}}}";

    private static string InjectedVerb =>
        "get(\"/p\")] void Injected(); [System.Runtime.CompilerServices.ModuleInitializer] "
        + "[Get";

    [TestMethod]
    public async Task Rejects_Operation_Key_That_Is_Not_A_Http_Verb()
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(BuildSpec(InjectedVerb));

        Func<Task<string>> act = () => GenerateCode(swaggerFile);

        string? generated = null;
        try
        {
            generated = await act();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        generated.Should().NotContain("ModuleInitializer");
        Assert.Fail("Expected the unsupported operation key to be rejected.");
    }

    [TestMethod]
    public async Task Trace_Operation_Is_Still_Skipped()
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(BuildSpec("trace"));

        string code = await GenerateCode(swaggerFile);

        code.Should().NotContain("Trace(");
        code.Should().NotContain("[Get(");
    }

    [TestMethod]
    public async Task Path_Item_Metadata_Keys_Are_Not_Treated_As_Operations()
    {
        string spec = "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{\"/p\":{"
            + "\"summary\":\"s\",\"description\":\"d\",\"x-custom\":{\"a\":1},\"parameters\":[],\"servers\":[],"
            + "\"get\":{\"operationId\":\"getIt\",\"responses\":{\"200\":{\"description\":\"ok\"}}}}}}";
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);

        string code = await GenerateCode(swaggerFile);

        code.Should().Contain("[Get(\"/p\")]");
    }

    [TestMethod]
    [DataRow("get", "Get")]
    [DataRow("put", "Put")]
    [DataRow("post", "Post")]
    [DataRow("delete", "Delete")]
    [DataRow("options", "Options")]
    [DataRow("head", "Head")]
    [DataRow("patch", "Patch")]
    public async Task Supported_Verbs_Generate_Fixed_Attribute(string verb, string attribute)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(BuildSpec(verb));

        string code = await GenerateCode(swaggerFile);

        code.Should().Contain($"[{attribute}(\"/p\")]");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Supported_Verbs_Compile()
    {
        string spec = "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{"
            + string.Join(",", new[] { "get", "put", "post", "delete", "options", "head", "patch" }
                .Select(v => $"\"/{v}\":{{\"{v}\":{{\"operationId\":\"{v}It\",\"responses\":{{\"200\":{{\"description\":\"ok\"}}}}}}}}"))
            + "}}";
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);

        string code = await GenerateCode(swaggerFile);

        BuildHelper.BuildCSharp(code).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(string swaggerFile)
    {
        RefitGeneratorSettings settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        RefitGenerator sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
