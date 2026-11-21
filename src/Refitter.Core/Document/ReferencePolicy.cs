namespace Refitter.Core;

/// <summary>
/// Describes which references the document resolvers are allowed to follow. The reference guard validates
/// a document before it is parsed; the policy makes the resolvers enforce the same rules, so a reference the
/// guard classified differently from the resolver still cannot be fetched.
/// </summary>
internal sealed class ReferencePolicy
{
    public static readonly ReferencePolicy Unrestricted = new(allowRemoteReferences: true);

    public ReferencePolicy(bool allowRemoteReferences)
    {
        AllowRemoteReferences = allowRemoteReferences;
    }

    public bool AllowRemoteReferences { get; }
}
