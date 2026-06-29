using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Refitter.Tests.TestUtilities;

internal sealed class LocalHttpServer : IAsyncDisposable
{
    private readonly TcpListener listener;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly string responseBody;
    private readonly string contentType;
    private readonly int statusCode;
    private readonly string statusText;
    private readonly Task acceptTask;

    /// <summary>
    /// Starts a loopback HTTP server with a fixed response.
    /// </summary>
    /// <param name="responseBody">The response body to return for the accepted request.</param>
    /// <param name="contentType">The value used for the response <c>Content-Type</c> header.</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <param name="statusText">The HTTP status text to return.</param>
    public LocalHttpServer(
        string responseBody,
        string contentType = "application/json",
        int statusCode = 200,
        string statusText = "OK")
    {
        this.responseBody = responseBody;
        this.contentType = contentType;
        this.statusCode = statusCode;
        this.statusText = statusText;

        listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        acceptTask = Task.Run(AcceptAsync);
    }

    public string Url => $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/openapi";

    /// <summary>
    /// Stops the server and waits for the accept loop to finish.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        cancellationTokenSource.Cancel();
        listener.Stop();
        try
        {
            await acceptTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }

        cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Accepts a request and writes the configured HTTP response.
    /// </summary>
    private async Task AcceptAsync()
    {
        using var client = await listener.AcceptTcpClientAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            if (line == null || line.Length == 0)
                break;
        }

        var bodyBytes = Encoding.UTF8.GetBytes(responseBody);
        var header =
            $"HTTP/1.1 {statusCode} {statusText}\r\n" +
            $"Content-Type: {contentType}\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Connection: close\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(header);

        await stream.WriteAsync(headerBytes, cancellationTokenSource.Token).ConfigureAwait(false);
        await stream.WriteAsync(bodyBytes, cancellationTokenSource.Token).ConfigureAwait(false);
        await stream.FlushAsync(cancellationTokenSource.Token).ConfigureAwait(false);
    }
}
