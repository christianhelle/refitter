using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Refitter.Core;

internal static class AsyncHelper
{
    internal static async Task<string> ReadAsStringWithCancellationAsync(
        this HttpContent content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
#pragma warning disable S8949
        string result = await content.ReadAsStringAsync().ConfigureAwait(false);
#pragma warning restore S8949
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    internal static async Task<string> ReadToEndWithCancellationAsync(
        this StreamReader reader,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
#pragma warning disable S8949
        string result = await reader.ReadToEndAsync().ConfigureAwait(false);
#pragma warning restore S8949
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
}
