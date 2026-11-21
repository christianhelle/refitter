namespace Refitter.Core;

/// <summary>
/// Describes which references the document resolvers are allowed to follow. The reference guard validates
/// a document before it is parsed; the policy makes the resolvers enforce the same rules, so a reference the
/// guard classified differently from the resolver still cannot be fetched.
/// </summary>
internal sealed class ReferencePolicy
{
    public static readonly ReferencePolicy Unrestricted = new(allowRemoteReferences: true);

    public ReferencePolicy(bool allowRemoteReferences, string? rootDirectory = null, string? rootDocumentPath = null)
    {
        AllowRemoteReferences = allowRemoteReferences;
        RootDirectory = rootDirectory;
        RootDocumentPath = rootDocumentPath;
    }

    public bool AllowRemoteReferences { get; }

    /// <summary>
    /// The directory every referenced file must stay within, or <c>null</c> when file access is unconfined.
    /// </summary>
    public string? RootDirectory { get; }

    /// <summary>
    /// The path of the input document itself, as passed to the reference guard, or <c>null</c> when unknown.
    /// It is the one path that may be a symbolic link, because the user chose it as the input.
    /// </summary>
    public string? RootDocumentPath { get; }
}
