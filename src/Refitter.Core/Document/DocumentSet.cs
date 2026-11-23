namespace Refitter.Core;

/// <summary>
/// An OpenAPI document together with every referenced file it reaches, each read once by
/// <see cref="OpenApiDocumentReader"/>, converted to JSON, normalized and checked against the reference rules.
/// The reference resolvers serve files from here only, so the file that is parsed is the file that was checked.
/// </summary>
internal sealed class DocumentSet
{
    // Ordinal even on Windows: the reader and the resolvers derive a path from the same reference text, and
    // individual NTFS directories can be case-sensitive
    private readonly IReadOnlyDictionary<string, string> files;
    private readonly IReadOnlyDictionary<string, string> urls;

    internal DocumentSet(
        string? rootDocumentPath,
        string rootJson,
        IReadOnlyDictionary<string, string> files,
        IReadOnlyDictionary<string, string> urls)
    {
        RootDocumentPath = rootDocumentPath;
        RootJson = rootJson;
        this.files = files;
        this.urls = urls;
    }

    /// <summary>
    /// The full path of a local OpenAPI document, which relative references resolve against, or <c>null</c> for
    /// a remote one.
    /// </summary>
    public string? RootDocumentPath { get; }

    /// <summary>
    /// The OpenAPI document as normalized JSON.
    /// </summary>
    public string RootJson { get; }

    /// <summary>
    /// Gets a referenced local file by its full path.
    /// </summary>
    public bool TryGetFile(string fullPath, out string json) => files.TryGetValue(fullPath, out json!);

    /// <summary>
    /// Gets a referenced remote document by its URL without the fragment.
    /// </summary>
    public bool TryGetUrl(string url, out string json) => urls.TryGetValue(url, out json!);
}
