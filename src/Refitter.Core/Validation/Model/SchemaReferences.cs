#nullable enable

namespace Refitter.Core.Validation.Model;

/// <summary>
/// Resolves local schema references the way Microsoft.OpenApi (MIT license) does for its reference rule: by the
/// component name in the reference, then along a sub-schema path such as <c>/properties/name</c>.
/// </summary>
internal static class SchemaReferences
{
    private const string ComponentSchemasFragment = "#/components/schemas/";

    /// <summary>
    /// The reference as Microsoft.OpenApi writes it in OpenAPI 3.
    /// </summary>
    public static string ReferenceV3(SpecReference reference)
    {
        if (reference.JsonPointer is { Length: > 0 } jsonPointer)
            return jsonPointer;

        var id = reference.Id;
        if (id.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || id.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || id.Contains("#/components"))
        {
            return id;
        }

        return ComponentSchemasFragment + id;
    }

    /// <summary>
    /// Follows the reference, and the references it ends at, to a schema.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the references form a cycle.</exception>
    public static SpecSchema? ResolveRecursive(SpecReference reference, SpecComponents? registered)
    {
        // References are tracked by identity, like the reference objects Microsoft.OpenApi tracks
        var visited = new List<SpecReference>();
        var current = reference;
        while (true)
        {
            if (visited.Exists(seen => ReferenceEquals(seen, current)))
                throw new InvalidOperationException("Circular reference detected while resolving reference: " + ReferenceV3(current));

            visited.Add(current);

            var target = Resolve(current, registered);
            if (target?.Reference == null)
                return target;

            current = target.Reference;
        }
    }

    private static SpecSchema? Resolve(SpecReference reference, SpecComponents? registered)
    {
        var id = reference.Id;
        if (id.Contains('/'))
        {
            // Microsoft.OpenApi reads an id holding a path as a URI, which only resolves within this document
            if (!id.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                _ = new Uri(id);

            return null;
        }

        var referenceV3 = ReferenceV3(reference);
        var location = IsSubComponent(referenceV3) ? referenceV3 : ComponentSchemasFragment + id;
        var fragment = location.Substring(location.IndexOf('#') + 1);
        var segments = fragment.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if (registered?.Schemas == null
            || !registered.Schemas.TryGetValue(segments[2], out var component)
            || component == null)
        {
            return null;
        }

        return ResolveSubSchema(component, segments.Skip(3).ToArray());
    }

    private static bool IsSubComponent(string reference)
    {
        var parts = reference.Split('#');
        var fragment = parts.Length > 1 ? parts[1] : string.Empty;
        return fragment.StartsWith("/components/schemas/", StringComparison.OrdinalIgnoreCase)
               && fragment.Split(['/'], StringSplitOptions.RemoveEmptyEntries).Length > 3;
    }

    /// <summary>
    /// Follows a path such as <c>properties/name</c> down from a component. The reader only builds trees, so
    /// unlike Microsoft.OpenApi this needs no check for revisiting a schema.
    /// </summary>
    private static SpecSchema? ResolveSubSchema(SpecSchema schema, string[] path)
    {
        if (path.Length == 0)
            return schema;

        var segment = path[0];
        path = path.Skip(1).ToArray();
        switch (segment)
        {
            case "properties":
                if (schema.Properties != null && schema.Properties.TryGetValue(path[0], out var property) && property != null)
                    return ResolveSubSchema(property, path.Skip(1).ToArray());

                break;
            case "items":
                return schema.Items is { Reference: null } items ? ResolveSubSchema(items, path) : null;
            case "additionalProperties":
                return schema.AdditionalProperties is { Reference: null } additionalProperties
                    ? ResolveSubSchema(additionalProperties, path)
                    : null;
            case "allOf":
                return ResolveListItem(schema.AllOf, path);
            case "anyOf":
                return ResolveListItem(schema.AnyOf, path);
            case "oneOf":
                return ResolveListItem(schema.OneOf, path);
        }

        return null;
    }

    /// <summary>
    /// Follows a path that starts with an index into <c>allOf</c>, <c>anyOf</c> or <c>oneOf</c>.
    /// </summary>
    private static SpecSchema? ResolveListItem(List<SpecSchema>? schemas, string[] path)
    {
        if (!int.TryParse(path[0], out var index) || schemas == null || index >= schemas.Count)
            return null;

        return ResolveSubSchema(schemas[index], path.Skip(1).ToArray());
    }
}
