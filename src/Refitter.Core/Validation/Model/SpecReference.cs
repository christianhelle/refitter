namespace Refitter.Core.Validation.Model;

/// <summary>
/// A <c>$ref</c> to a component: its id and, for a reference into another file, the file.
/// </summary>
internal sealed record SpecReference(string Id, string? ExternalResource)
{
    /// <summary>
    /// The JSON pointer an OpenAPI 3.1 schema reference was written with, when it is not a component.
    /// </summary>
    public string? JsonPointer { get; init; }
}
