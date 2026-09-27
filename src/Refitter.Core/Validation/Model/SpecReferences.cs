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

    /// <summary>
    /// Follows references to the element they end at, the way Microsoft.OpenApi resolves them against the
    /// components the document was read with (<paramref name="components"/>).
    /// </summary>
    /// <remarks>
    /// A reference into another file resolves to the component with the same id in this document, because
    /// Microsoft.OpenApi never loads the other file. An id holding a path is treated as a URI.
    /// </remarks>
    /// <returns>The element, or <c>null</c> when a reference does not resolve.</returns>
    public static T? Resolve<T>(T? element, Func<SpecComponents, Dictionary<string, T?>?> components, SpecComponents? registered)
        where T : SpecReferenceable
    {
        var visited = new HashSet<T>();
        while (element?.Reference != null)
        {
            if (!visited.Add(element))
                return null;

            var id = element.Reference.Id;
            if (id.Contains('/'))
            {
                if (!id.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                    _ = new Uri(id);

                return null;
            }

            element = registered != null && components(registered) is { } candidates && candidates.TryGetValue(id, out var target)
                ? target
                : null;
        }

        return element;
    }
}
