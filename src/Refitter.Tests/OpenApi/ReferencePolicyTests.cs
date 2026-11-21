using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

/// <summary>
/// Covers the resolver-level enforcement of <see cref="ReferencePolicy"/> added for GHSA-5w8x-vqhx-3jrh, which
/// backs up the reference guard if the guard and the resolver ever classify a reference differently.
/// </summary>
[TestClass]
public class ReferencePolicyTests
{
    // Port 1 refuses connections, so a fetch that was not blocked would surface as an HTTP failure
    private const string Spec =
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
        "\"components\":{\"schemas\":{\"Pet\":{\"$ref\":\"http://127.0.0.1:1/s.json\"}}}}";

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Resolver_Blocks_Url_Reference_When_Remote_References_Are_Disallowed(bool isYaml)
    {
        var act = () => OpenApiDocumentParser.ParseAsync(
            Spec,
            Path.Combine(Path.GetTempPath(), "spec.json"),
            isYaml,
            new ReferencePolicy(allowRemoteReferences: false));

        // NJsonSchema wraps whatever the resolver throws
        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.InnerException.Should().BeOfType<ReferenceResolutionException>()
            .Which.Message.Should().Contain("127.0.0.1:1");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Resolver_Fetches_Url_Reference_When_Remote_References_Are_Allowed(bool isYaml)
    {
        using var server = new LoopbackHttpServer(
            "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}");
        var spec = Spec.Replace("http://127.0.0.1:1/", server.BaseUrl);

        var document = await OpenApiDocumentParser.ParseAsync(
            spec,
            Path.Combine(Path.GetTempPath(), "spec.json"),
            isYaml,
            new ReferencePolicy(allowRemoteReferences: true));

        server.RequestCount.Should().BeGreaterThan(0);
        document.Components.Schemas.Should().ContainKey("Pet");
        document.Components.Schemas["Pet"].ActualSchema.Properties.Should().ContainKey("name");
    }
}
