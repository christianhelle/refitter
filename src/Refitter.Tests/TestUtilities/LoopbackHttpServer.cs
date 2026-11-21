using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Refitter.Tests.TestUtilities;

/// <summary>
/// A minimal loopback HTTP server that answers every request with the same body and counts the requests it received
/// </summary>
public sealed class LoopbackHttpServer : IDisposable
{
    private readonly HttpListener listener = new();
    private readonly byte[] body;
    private readonly string contentType;
    private int requestCount;

    public LoopbackHttpServer(string responseBody, string contentType = "application/json")
    {
        body = Encoding.UTF8.GetBytes(responseBody);
        this.contentType = contentType;

        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        BaseUrl = $"http://127.0.0.1:{port}/";
        listener.Prefixes.Add(BaseUrl);
        listener.Start();
        _ = Task.Run(ServeAsync);
    }

    /// <summary>
    /// The base address of the server, ending in a slash
    /// </summary>
    public string BaseUrl { get; }

    /// <summary>
    /// The number of requests received so far
    /// </summary>
    public int RequestCount => Volatile.Read(ref requestCount);

    private async Task ServeAsync()
    {
        while (listener.IsListening)
        {
            try
            {
                var context = await listener.GetContextAsync().ConfigureAwait(false);
                Interlocked.Increment(ref requestCount);
                context.Response.StatusCode = 200;
                context.Response.ContentType = contentType;
                context.Response.ContentLength64 = body.Length;
                await context.Response.OutputStream.WriteAsync(body).ConfigureAwait(false);
                context.Response.Close();
            }
            catch (Exception)
            {
                // The listener was stopped or the client went away
                return;
            }
        }
    }

    public void Dispose() => listener.Close();
}
