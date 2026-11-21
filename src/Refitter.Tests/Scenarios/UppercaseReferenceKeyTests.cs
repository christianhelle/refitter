using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Regression coverage for GHSA-mgqj-ph59-3r6r: the reference guard must treat the <c>$ref</c> key
/// case-insensitively, because the JSON reference resolvers bind it case-insensitively.
/// </summary>
[TestClass]
public class UppercaseReferenceKeyTests
{
    // Port 1 refuses connections, so a reference that slipped past the guard would surface as an
    // HTTP failure rather than a ReferenceResolutionException
    private const string RemoteTarget = "http://127.0.0.1:1/s.json";

    private static string JsonSpec(string key, string target) =>
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
        "\"components\":{\"schemas\":{\"Pet\":{\"" + key + "\":\"" + target + "\"}}}}";

    private static string YamlSpec(string key, string target) => $@"openapi: 3.0.0
info:
  title: t
  version: '1'
paths: {{}}
components:
  schemas:
    Pet:
      {key}: {target}
";

    [TestMethod]
    [DataRow("$REF")]
    [DataRow("$Ref")]
    [DataRow("\\u0024REF")]
    public async Task Blocks_Json_Remote_Reference_With_Altered_Key_Case(string key)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(JsonSpec(key, RemoteTarget));

        var act = () => GenerateCode(swaggerFile);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*127.0.0.1:1*");
    }

    [TestMethod]
    [DataRow("$REF")]
    [DataRow("$Ref")]
    public async Task Blocks_Yaml_Remote_Reference_With_Altered_Key_Case(string key)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(YamlSpec(key, RemoteTarget));

        var act = () => GenerateCode(swaggerFile);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*127.0.0.1:1*");
    }

    [TestMethod]
    public async Task Blocks_Uppercase_Key_Pointing_Outside_Input_Directory()
    {
        string root = CreateTempDirectory();
        string inner = Path.Combine(root, "inner");
        Directory.CreateDirectory(inner);
        await File.WriteAllTextAsync(Path.Combine(root, "outside.json"), "{\"type\":\"object\"}");
        string main = Path.Combine(inner, "main.json");
        await File.WriteAllTextAsync(main, JsonSpec("$REF", "../outside.json"));

        var act = () => GenerateCode(main);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*outside the input directory*");
    }

    [TestMethod]
    public async Task Allows_Uppercase_Key_Pointing_To_In_Tree_File()
    {
        string root = CreateTempDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(root, "pet.json"),
            "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}");
        string main = Path.Combine(root, "main.json");
        await File.WriteAllTextAsync(main, JsonSpec("$REF", "./pet.json"));

        string code = await GenerateCode(main);

        code.Should().Contain("class Pet").And.Contain("Name");
    }

    [TestMethod]
    public async Task Allows_Lowercase_Key_Pointing_To_In_Tree_File()
    {
        string root = CreateTempDirectory();
        await File.WriteAllTextAsync(
            Path.Combine(root, "pet.json"),
            "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}");
        string main = Path.Combine(root, "main.json");
        await File.WriteAllTextAsync(main, JsonSpec("$ref", "./pet.json"));

        string code = await GenerateCode(main);

        code.Should().Contain("class Pet").And.Contain("Name");
    }

    private static string CreateTempDirectory()
    {
        return TestDirectory.CreateFolder();
    }

    private static async Task<string> GenerateCode(string swaggerFile)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
