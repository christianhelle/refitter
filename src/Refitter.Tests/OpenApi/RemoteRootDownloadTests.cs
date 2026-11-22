using System.Net;
using System.Net.Sockets;
using System.Text;
using AwesomeAssertions;
using Refitter.Core;
using Refitter.Core.Validation;
using Refitter.Tests.Resources;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

[TestClass]
public class RemoteRootDownloadTests
{
    private static string PetstoreSpec =>
        EmbeddedResources.GetSwaggerPetstore(SampleOpenSpecifications.SwaggerPetstoreJsonV3);

    [TestMethod]
    public async Task Factory_Rejects_A_Root_Document_That_Is_Not_Found()
    {
        await using var server = new LocalHttpServer(PetstoreSpec, statusCode: 404, statusText: "Not Found");

        var act = () => OpenApiDocumentFactory.CreateAsync(server.Url);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to download OpenAPI document*404*");
    }

    [TestMethod]
    public async Task Validator_Rejects_A_Root_Document_That_Is_Not_Found()
    {
        await using var server = new LocalHttpServer(PetstoreSpec, statusCode: 404, statusText: "Not Found");

        var act = () => OpenApiValidator.Validate(server.Url);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to download OpenAPI document*404*");
    }

    [TestMethod]
    public async Task Factory_Rejects_A_Root_Document_Larger_Than_The_Limit()
    {
        using var server = new OversizedResponseServer();

        var act = () => OpenApiDocumentFactory.CreateAsync(server.Url);

        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Message.Should().Contain(RemoteDocumentDownloader.MaxResponseBytes.ToString());
    }

    [TestMethod]
    public async Task Validator_Rejects_A_Root_Document_Larger_Than_The_Limit()
    {
        using var server = new OversizedResponseServer();

        var act = () => OpenApiValidator.Validate(server.Url);

        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
        thrown.Which.Message.Should().Contain(RemoteDocumentDownloader.MaxResponseBytes.ToString());
    }

    /// <summary>
    /// Announces a body larger than the download limit and never sends it
    /// </summary>
    private sealed class OversizedResponseServer : IDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);

        public OversizedResponseServer()
        {
            listener.Start();
            _ = Task.Run(ServeAsync);
        }

        public string Url => $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/openapi.json";

        public void Dispose() => listener.Stop();

        private async Task ServeAsync()
        {
            try
            {
                using var client = await listener.AcceptTcpClientAsync();
                var stream = client.GetStream();
                _ = await stream.ReadAsync(new byte[4096]);
                var headers = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n" +
                    $"Content-Length: {RemoteDocumentDownloader.MaxResponseBytes + 1}\r\n\r\n");
                await stream.WriteAsync(headers);
                await stream.FlushAsync();
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
