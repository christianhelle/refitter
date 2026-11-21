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
    [DataRow("http:/\\127.0.0.1:1/s.json")]
    [DataRow("HTTP:/\\127.0.0.1:1/s.json")]
    public async Task Does_Not_Block_Mixed_Separator_Reference_When_Remote_Allowed(string reference)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(JsonSpec(reference));

        var act = () => GenerateCode(swaggerFile, allowRemoteReferences: true);

        // The guard lets it through, so the failure is the refused connection, not a guard rejection
        var thrown = await act.Should().ThrowAsync<Exception>();
        thrown.Which.Should().NotBeOfType<ReferenceResolutionException>();
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
