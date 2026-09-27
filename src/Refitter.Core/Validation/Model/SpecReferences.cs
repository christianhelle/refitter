namespace Refitter.Core.Validation.Model;

internal static class SpecReferences
{
    /// <summary>
    /// Creates a reference, failing on an empty id like the Microsoft.OpenApi (MIT license) reference types do.
    /// </summary>
    public static SpecReference Create(string id, string? externalResource)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentNullException("referenceId", "Value cannot be null or empty: referenceId");

        return new SpecReference(id, externalResource);
    }
}
