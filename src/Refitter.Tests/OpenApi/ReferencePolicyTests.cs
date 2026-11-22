using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

/// <summary>
/// The parser reads remote references with the document only when they are allowed (GHSA-5w8x-vqhx-3jrh), and
/// resolves them from what was read.
/// </summary>
[TestClass]
public class ReferencePolicyTests
{
    // Port 1 refuses connections, so a fetch that was not blocked would surface as an HTTP failure
    private const string Spec =
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
        "\"components\":{\"schemas\":{\"Pet\":{\"$ref\":\"http://127.0.0.1:1/s.json\"}}}}";

    [TestMethod]
    [DataRow("spec.json")]
    [DataRow("spec.yaml")]
    public async Task Resolver_Blocks_Url_Reference_When_Remote_References_Are_Disallowed(string fileName)
    {
        var act = () => OpenApiDocumentParser.ParseAsync(
            Spec,
            Path.Combine(Path.GetTempPath(), fileName),
            allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*127.0.0.1:1*");
    }

    [TestMethod]
    [DataRow("spec.json")]
    [DataRow("spec.yaml")]
    public async Task Resolver_Fetches_Url_Reference_When_Remote_References_Are_Allowed(string fileName)
    {
        using var server = new LoopbackHttpServer(
            "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}");
        var spec = Spec.Replace("http://127.0.0.1:1/", server.BaseUrl);

        var document = await OpenApiDocumentParser.ParseAsync(
            spec,
            Path.Combine(Path.GetTempPath(), fileName),
            allowRemoteReferences: true);

        server.RequestCount.Should().BeGreaterThan(0);
        document.Components.Schemas.Should().ContainKey("Pet");
        document.Components.Schemas["Pet"].ActualSchema.Properties.Should().ContainKey("name");
    }
}
