using System.Text;
using Microsoft.OpenApi;

namespace Refitter.Core.Validation;

/// <summary>
/// Serves the referenced files of a <see cref="DocumentSet"/> to Microsoft.OpenApi, so that validation reads
/// the files the reader read and checked, and never touches the disk or the network itself.
/// </summary>
internal sealed class DocumentSetStreamLoader(DocumentSet set) : IStreamLoader
{
    // Microsoft.OpenApi loads a file again for every document that reaches it, and follows a reference cycle
    // until the stack overflows. A file that is requested this often is a cycle, so it is served empty
    private const int MaxServesPerDocument = 16;

    private const string EmptyDocument = "{}";

    private readonly Dictionary<string, int> served = new(StringComparer.Ordinal);

    public Task<Stream> LoadAsync(Uri baseUrl, Uri uri, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var json = Find(baseUrl, uri) ?? EmptyDocument;
        return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(json)));
    }

    private string? Find(Uri baseUrl, Uri uri)
    {
        var target = uri.IsAbsoluteUri ? uri : new Uri(baseUrl, uri);

        string key;
        string? json;
        if (target.Scheme is "http" or "https")
        {
            // The reader keys a remote document by its URL as written, without the fragment
            key = (uri.IsAbsoluteUri ? uri.OriginalString : target.ToString()).Split('#')[0];
            set.TryGetUrl(key, out json);
        }
        else if (target.IsFile)
        {
            key = Path.GetFullPath(target.LocalPath);
            if (!set.TryGetFile(key, out json) && File.Exists(key))
            {
                // A file that exists but was not read is a path the reader resolved differently, and is never
                // read unchecked
                throw new ReferenceResolutionException(
                    $"Local '$ref' to \"{key}\" was not read with the OpenAPI document and is rejected.");
            }
        }
        else
        {
            return null;
        }

        if (json is null)
            return null;

        served.TryGetValue(key, out var count);
        served[key] = count + 1;
        return count < MaxServesPerDocument ? json : null;
    }
}
