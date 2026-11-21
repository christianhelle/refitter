using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// A remote reference is downloaded and parsed by the reference resolver, which is a second way into the
/// recursive YAML reader. The resolver downloads the content itself so it can check the nesting first.
/// </summary>
[TestClass]
public class RemoteReferenceNestingTests
{
    private const int TooDeep = DocumentNestingGuard.MaxNestingDepth + 44;

    private static string SpecReferencing(string url) =>
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
        "\"components\":{\"schemas\":{\"Pet\":{\"$ref\":\"" + url + "\"}}}}";

    private static string DeepSchema(int depth) =>
        "{\"type\":\"object\",\"x-deep\":" + new string('[', depth) + new string(']', depth) + "}";

    private static Task<NSwag.OpenApiDocument> Parse(string spec, bool isYaml) =>
        OpenApiDocumentParser.ParseAsync(
            spec,
            Path.Combine(Path.GetTempPath(), "spec.json"),
            isYaml,
            new ReferencePolicy(allowRemoteReferences: true));

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Rejects_Deeply_Nested_Remote_Reference(bool isYaml)
    {
        using var server = new LoopbackHttpServer(DeepSchema(TooDeep));

        var act = () => Parse(SpecReferencing(server.BaseUrl + "s.json"), isYaml);

        var thrown = await act.Should().ThrowAsync<Exception>();
        ReferenceResolutionException.FindIn(thrown.Which).Should().NotBeNull()
            .And.Subject.As<ReferenceResolutionException>().Message
            .Should().Contain(DocumentNestingGuard.MaxNestingDepth.ToString());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Rejects_Deeply_Nested_Remote_Yaml_Reference(bool isYaml)
    {
        string body = "type: object\nx-deep: " + new string('[', TooDeep) + new string(']', TooDeep) + "\n";
        using var server = new LoopbackHttpServer(body, "application/yaml");

        var act = () => Parse(SpecReferencing(server.BaseUrl + "s.yaml"), isYaml);

        var thrown = await act.Should().ThrowAsync<Exception>();
        ReferenceResolutionException.FindIn(thrown.Which).Should().NotBeNull();
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Allows_Remote_Reference_Nested_At_A_Normal_Depth(bool isYaml)
    {
        using var server = new LoopbackHttpServer(DeepSchema(40));

        var document = await Parse(SpecReferencing(server.BaseUrl + "s.json"), isYaml);

        document.Components.Schemas.Should().ContainKey("Pet");
        server.RequestCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Downloads_A_Remote_Reference_Only_Once()
    {
        using var server = new LoopbackHttpServer("{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}");
        string url = server.BaseUrl + "s.json";
        string spec = SpecReferencing(url).Replace(
            "\"Pet\":{\"$ref\":\"" + url + "\"}",
            "\"Pet\":{\"$ref\":\"" + url + "#/properties/name\"},\"Owner\":{\"$ref\":\"" + url + "\"}");

        var document = await Parse(spec, isYaml: false);

        document.Components.Schemas.Should().ContainKey("Owner");
        server.RequestCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Rejects_A_Remote_Response_Larger_Than_The_Limit()
    {
        using var server = new LoopbackHttpServer(new string('a', 1000));

        var act = () => RemoteDocumentDownloader.DownloadAsync(server.BaseUrl, maxBytes: 999);

        var thrown = await act.Should().ThrowAsync<ReferenceResolutionException>();
        thrown.Which.Message.Should().Contain("999");
    }

    [TestMethod]
    public async Task Accepts_A_Remote_Response_At_The_Limit()
    {
        using var server = new LoopbackHttpServer(new string('a', 1000));

        string content = await RemoteDocumentDownloader.DownloadAsync(server.BaseUrl, maxBytes: 1000);

        content.Should().HaveLength(1000);
    }

    [TestMethod]
    public async Task Times_Out_When_The_Response_Body_Stalls()
    {
        // Sends complete headers promising a body, then never sends it
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            int port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            var serve = Task.Run(async () =>
            {
                using var client = await listener.AcceptTcpClientAsync();
                var stream = client.GetStream();
                var request = new byte[4096];
                _ = await stream.ReadAsync(request);
                byte[] headers = System.Text.Encoding.ASCII.GetBytes(
                    "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: 100\r\n\r\n{");
                await stream.WriteAsync(headers);
                await stream.FlushAsync();
                await Task.Delay(TimeSpan.FromSeconds(30));
            });

            var download = RemoteDocumentDownloader.DownloadAsync(
                $"http://127.0.0.1:{port}/stall.json",
                timeout: TimeSpan.FromSeconds(1));
            var finished = await Task.WhenAny(download, Task.Delay(TimeSpan.FromSeconds(15)));

            finished.Should().BeSameAs(download, "a stalled response body must not block the download indefinitely");
            var act = () => download;
            var thrown = await act.Should().ThrowAsync<ReferenceResolutionException>();
            thrown.Which.Message.Should().Contain("timed out");
        }
        finally
        {
            listener.Stop();
        }
    }
}
