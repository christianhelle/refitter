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
        if (!string.IsNullOrEmpty(reference.JsonPointer))
            return reference.JsonPointer!;

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
        var visited = new HashSet<SpecReference>(ReferenceComparer.Instance);
        var current = reference;
        while (true)
        {
            if (!visited.Add(current))
                throw new InvalidOperationException("Circular reference detected while resolving reference: " + ReferenceV3(current));

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

        var visited = new Stack<SpecSchema>();
        return ResolveSubSchema(component, segments.Skip(3).ToArray(), visited);
    }

    private static bool IsSubComponent(string reference)
    {
        var parts = reference.Split('#');
        var fragment = parts.Length > 1 ? parts[1] : string.Empty;
        return fragment.StartsWith("/components/schemas/", StringComparison.OrdinalIgnoreCase)
               && fragment.Split(['/'], StringSplitOptions.RemoveEmptyEntries).Length > 3;
    }

    private static SpecSchema? ResolveSubSchema(SpecSchema schema, string[] path, Stack<SpecSchema> visited)
    {
        if (visited.Contains(schema))
        {
            throw new InvalidOperationException(schema.Reference != null
                ? "Circular reference detected while resolving schema: " + ReferenceV3(schema.Reference)
                : "Circular reference detected while resolving schema");
        }

        visited.Push(schema);
        if (path.Length == 0)
            return schema;

        var segment = path[0];
        path = path.Skip(1).ToArray();
        switch (segment)
        {
            case "properties":
                if (schema.Properties != null && schema.Properties.TryGetValue(path[0], out var property) && property != null)
                    return ResolveSubSchema(property, path.Skip(1).ToArray(), visited);

                break;
            case "items":
                return schema.Items is { Reference: null } items ? ResolveSubSchema(items, path, visited) : null;
            case "additionalProperties":
                return schema.AdditionalProperties is { Reference: null } additionalProperties
                    ? ResolveSubSchema(additionalProperties, path, visited)
                    : null;
            case "allOf":
            case "anyOf":
            case "oneOf":
                if (!int.TryParse(path[0], out var index))
                    return null;

                var schemas = segment switch
                {
                    "allOf" => schema.AllOf,
                    "anyOf" => schema.AnyOf,
                    _ => schema.OneOf,
                };
                if (schemas != null && index < schemas.Count)
                    return ResolveSubSchema(schemas[index], path.Skip(1).ToArray(), visited);

                break;
        }

        return null;
    }

    /// <summary>
    /// Compares references by identity, like the reference objects Microsoft.OpenApi tracks.
    /// </summary>
    private sealed class ReferenceComparer : IEqualityComparer<SpecReference>
    {
        public static readonly ReferenceComparer Instance = new();

        public bool Equals(SpecReference? x, SpecReference? y) => ReferenceEquals(x, y);

        public int GetHashCode(SpecReference obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
