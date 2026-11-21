using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Regression coverage for GHSA-5w8x-vqhx-3jrh: a remote <c>$ref</c> that mixes forward and back slashes in
/// its scheme separator is still fetched over the network, so the reference guard must classify references
/// by URI scheme rather than by a literal <c>http://</c> prefix.
/// </summary>
[TestClass]
public class MixedSeparatorReferenceTests
{
    // Port 1 refuses connections, so a reference that slipped past the guard would surface as an
    // HTTP failure rather than a ReferenceResolutionException
    private static string JsonSpec(string target) =>
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
        "\"components\":{\"schemas\":{\"Pet\":{\"$ref\":\"" + target.Replace("\\", "\\\\") + "\"}}}}";

    [TestMethod]
    [DataRow("http:/\\127.0.0.1:1/s.json")]
    [DataRow("http:\\\\127.0.0.1:1/s.json")]
    [DataRow("HTTP:/\\127.0.0.1:1/s.json")]
    [DataRow("ftp://127.0.0.1:1/s.json")]
    [DataRow("http://127.0.0.1:1/s.json")]
    public async Task Blocks_Remote_Reference_By_Default(string reference)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(JsonSpec(reference));

        var act = () => GenerateCode(swaggerFile, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Fetches_Standard_Reference_When_Remote_Allowed()
    {
        using var server = new LoopbackHttpServer(
            "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}");
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(JsonSpec(server.BaseUrl + "s.json"));

        string code = await GenerateCode(swaggerFile, allowRemoteReferences: true);

        server.RequestCount.Should().BeGreaterThan(0);
        code.Should().Contain("class Pet");
    }

    [TestMethod]
    [DataRow("http:/\\")]
    [DataRow("HTTP:/\\")]
    [DataRow("https:\\\\")]
    public async Task Blocks_Mixed_Separator_Reference_When_Remote_Allowed(string schemeSeparator)
    {
        using var server = new LoopbackHttpServer(
            "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}");
        var reference = schemeSeparator + server.BaseUrl.Substring("http://".Length) + "s.json";
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(JsonSpec(reference));

        var act = () => GenerateCode(swaggerFile, allowRemoteReferences: true);

        // Downstream resolvers read a non-standard spelling as a local file name rather than fetch it, so
        // it is neither a usable remote reference nor a confined local one
        await act.Should().ThrowAsync<ReferenceResolutionException>();
        server.RequestCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Blocks_Mixed_Separator_Traversal_When_Remote_Allowed()
    {
        string parent = TestDirectory.CreateFolder();
        string root = Path.Combine(parent, "input");
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(
            Path.Combine(parent, "outside.json"),
            "{\"type\":\"object\",\"properties\":{\"OutsideMarker\":{\"type\":\"string\"}}}");
        string main = Path.Combine(root, "main.json");
        await File.WriteAllTextAsync(main, JsonSpec("http:/\\host/../../outside.json"));

        var act = () => GenerateCode(main, allowRemoteReferences: true);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Allows_Local_Relative_Reference()
    {
        string root = Path.Combine(Path.GetTempPath(), "refitter-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(
            Path.Combine(root, "child.json"),
            "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}");
        string main = Path.Combine(root, "main.json");
        await File.WriteAllTextAsync(main, JsonSpec("./child.json"));

        string code = await GenerateCode(main, allowRemoteReferences: false);

        code.Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<string> GenerateCode(string swaggerFile, bool allowRemoteReferences)
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            AllowRemoteReferences = allowRemoteReferences
        };
        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
