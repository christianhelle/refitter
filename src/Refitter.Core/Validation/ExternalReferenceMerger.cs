#nullable enable

using Refitter.Core.Validation.Model;
using Refitter.Core.Validation.Reading;

namespace Refitter.Core.Validation;

/// <summary>
/// Copies the components a local document references in other files into it, as the OasReader multi-file reader
/// (MIT license) does before validation. Only files that are OpenAPI documents themselves, found by the end of
/// their path, are merged; a reference into any other file stays as it is.
/// </summary>
internal sealed class ExternalReferenceMerger
{
    private readonly Dictionary<string, SpecDocument> documentCache = new(StringComparer.Ordinal);
    private readonly List<FileInfo> files;

    private ExternalReferenceMerger(string openApiFile)
    {
        var directory = Path.GetDirectoryName(openApiFile);
        files = string.IsNullOrEmpty(directory)
            ? []
            : Directory.GetFiles(directory, "*" + Path.GetExtension(openApiFile), SearchOption.AllDirectories)
                .Select(file => new FileInfo(file))
                .Where(file => file.Exists)
                .ToList();
    }

    public static void Merge(SpecDocument document, string openApiFile, SpecComponents? registered)
    {
        if (!ContainsExternalReferences(document, registered))
            return;

        var merger = new ExternalReferenceMerger(openApiFile);
        int count;
        do
        {
            var resolved = new ComponentCache();
            SpecReferenceWalker.Walk(document, site => merger.ResolveExternal(site, resolved));
            resolved.UpdateDocument(document);

            var missing = new ComponentCache();
            SpecReferenceWalker.Walk(document, site => merger.FindMissing(document, site, missing));
            count = missing.Count;
            missing.UpdateDocument(document);
        }
        while (count > 0);

        document.Components ??= new SpecComponents();
        if (document.Components.Schemas != null)
        {
            document.Components.Schemas = document.Components.Schemas
                .OrderBy(schema => schema.Key)
                .ToDictionary(schema => schema.Key, schema => schema.Value, StringComparer.Ordinal);
        }
    }

    private void ResolveExternal(SpecReferenceSite site, ComponentCache cache)
    {
        var externalResource = site.Reference.ExternalResource;
        if (externalResource == null || !TryLoadDocument(externalResource, out var external))
            return;

        var id = site.Reference.Id.Split('/').Last();
        if (string.IsNullOrEmpty(id))
            return;

        var component = ResolveFromDocument(external, site.Type, id);
        if (component != null)
        {
            cache.Add(site.Type, id, component);
            site.Update(new SpecReference(id, null));
        }
    }

    private void FindMissing(SpecDocument document, SpecReferenceSite site, ComponentCache cache)
    {
        var id = site.Reference.Id.Split('/').Last();
        if (string.IsNullOrEmpty(id) || ExistsInDocument(document, site.Type, id))
            return;

        foreach (var external in documentCache.Values)
        {
            var component = ResolveFromDocument(external, site.Type, id);
            if (component != null)
                cache.Add(site.Type, id, component);
        }
    }

    private bool TryLoadDocument(string reference, out SpecDocument external)
    {
        if (documentCache.TryGetValue(reference, out external!))
            return true;

        var document = LoadDocument(reference);
        if (document == null)
            return false;

        documentCache[reference] = document;
        external = document;
        return true;
    }

    private SpecDocument? LoadDocument(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        if (reference.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return Download(reference);

        var suffix = reference.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var file = files.FirstOrDefault(f => f.FullName.EndsWith(suffix, StringComparison.CurrentCulture));
        if (file == null)
            return null;

        try
        {
            return SpecDocumentReader.Read(File.ReadAllBytes(file.FullName)).Document;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return null;
        }
    }

    private static SpecDocument? Download(string url)
    {
        try
        {
            using var client = new HttpClient();
            var content = client.GetByteArrayAsync(new Uri(url)).GetAwaiter().GetResult();
            return SpecDocumentReader.Read(content).Document;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return null;
        }
    }

    /// <summary>
    /// Whether a path, operation, request body or response refers to another file, directly or in a schema.
    /// </summary>
    private static bool ContainsExternalReferences(SpecDocument document, SpecComponents? registered)
    {
        return document.Paths.Values.Any(pathItem =>
        {
            var resolvedPathItem = SpecReferences.Resolve(pathItem, c => c.PathItems, registered);
            return (resolvedPathItem?.Parameters?.Any(IsExternalParameter) ?? false)
                   || (resolvedPathItem?.Operations?.Values.Any(IsExternalOperation) ?? false);
        });

        bool IsExternalParameter(SpecParameter parameter)
        {
            if (IsExternal(parameter))
                return true;

            var resolved = SpecReferences.Resolve(parameter, c => c.Parameters, registered);
            return IsExternal(resolved?.Schema)
                   || (resolved?.Content?.Any(content => IsExternal(content.Value!.Schema)) ?? false);
        }

        bool IsExternalOperation(SpecOperation operation)
        {
            if (operation.Parameters?.Any(IsExternalParameter) ?? false)
                return true;

            var requestBody = SpecReferences.Resolve(operation.RequestBody, c => c.RequestBodies, registered);
            if (requestBody?.Content?.Any(content => IsExternal(content.Value!.Schema)) == true)
                return true;

            return operation.Responses.Any(response =>
            {
                var resolved = SpecReferences.Resolve(response.Value, c => c.Responses, registered);
                return resolved?.Content?.Any(content => content.Value != null && IsExternal(content.Value.Schema)) == true
                       || resolved?.Headers?.Any(header => header.Value != null && IsExternal(SpecReferences.Resolve(header.Value, c => c.Headers, registered)?.Schema)) == true;
            });
        }

        static bool IsExternal(SpecReferenceable? element) => element?.Reference?.ExternalResource != null;
    }

    /// <remarks>Called after merging, which always leaves the document with components.</remarks>
    private static bool ExistsInDocument(SpecDocument document, SpecComponentType type, string id)
    {
        var components = document.Components!;

        return type switch
        {
            SpecComponentType.Schema => components.Schemas?.ContainsKey(id) ?? false,
            SpecComponentType.Response => components.Responses?.ContainsKey(id) ?? false,
            SpecComponentType.Parameter => components.Parameters?.ContainsKey(id) ?? false,
            SpecComponentType.Example => components.Examples?.ContainsKey(id) ?? false,
            SpecComponentType.RequestBody => components.RequestBodies?.ContainsKey(id) ?? false,
            SpecComponentType.Header => components.Headers?.ContainsKey(id) ?? false,
            SpecComponentType.SecurityScheme => components.SecuritySchemes?.ContainsKey(id) ?? false,
            SpecComponentType.Link => components.Links?.ContainsKey(id) ?? false,
            SpecComponentType.Callback => components.Callbacks?.ContainsKey(id) ?? false,
            _ => false,
        };
    }

    private static SpecReferenceable? ResolveFromDocument(SpecDocument document, SpecComponentType type, string id)
    {
        var components = document.Components;
        if (components == null)
            return null;

        return type switch
        {
            SpecComponentType.Schema => Find(components.Schemas, id),
            SpecComponentType.Response => Find(components.Responses, id),
            SpecComponentType.Parameter => Find(components.Parameters, id),
            SpecComponentType.Example => Find(components.Examples, id),
            SpecComponentType.RequestBody => Find(components.RequestBodies, id),
            SpecComponentType.Header => Find(components.Headers, id),
            SpecComponentType.SecurityScheme => Find(components.SecuritySchemes, id),
            SpecComponentType.Link => Find(components.Links, id),
            SpecComponentType.Callback => Find(components.Callbacks, id),
            _ => null,
        };

        static SpecReferenceable? Find<T>(Dictionary<string, T?>? map, string key)
            where T : SpecReferenceable =>
            map != null && map.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Components found in other files, by kind. The first one found for an id wins.
    /// </summary>
    private sealed class ComponentCache
    {
        private readonly Dictionary<SpecComponentType, Dictionary<string, SpecReferenceable>> data = new();

        public int Count => data.Sum(components => components.Value.Count);

        public void Add(SpecComponentType type, string id, SpecReferenceable component)
        {
            if (!data.TryGetValue(type, out var components))
            {
                components = new Dictionary<string, SpecReferenceable>(StringComparer.Ordinal);
                data[type] = components;
            }

            if (!components.ContainsKey(id))
                components[id] = component;
        }

        public void UpdateDocument(SpecDocument document)
        {
            var components = document.Components ??= new SpecComponents();
            foreach (var entry in data)
            {
                switch (entry.Key)
                {
                    case SpecComponentType.Schema:
                        Update(components.Schemas ??= new(StringComparer.Ordinal), entry.Value);
                        break;
                    case SpecComponentType.Response:
                        Update(components.Responses ??= new(StringComparer.Ordinal), entry.Value);
                        break;
                    case SpecComponentType.Parameter:
                        Update(components.Parameters ??= new(StringComparer.Ordinal), entry.Value);
                        break;
                    case SpecComponentType.Example:
                        Update(components.Examples ??= new(StringComparer.Ordinal), entry.Value);
                        break;
                    case SpecComponentType.RequestBody:
                        Update(components.RequestBodies ??= new(StringComparer.Ordinal), entry.Value);
                        break;
                    case SpecComponentType.Header:
                        Update(components.Headers ??= new(StringComparer.Ordinal), entry.Value);
                        break;
                    case SpecComponentType.SecurityScheme:
                        Update(components.SecuritySchemes ??= new(StringComparer.Ordinal), entry.Value);
                        break;
                    case SpecComponentType.Link:
                        Update(components.Links ??= new(StringComparer.Ordinal), entry.Value);
                        break;
                    case SpecComponentType.Callback:
                        Update(components.Callbacks ??= new(StringComparer.Ordinal), entry.Value);
                        break;
                }
            }
        }

        private static void Update<T>(Dictionary<string, T?> target, Dictionary<string, SpecReferenceable> source)
            where T : SpecReferenceable
        {
            foreach (var component in source)
            {
                target[component.Key] = (T)component.Value;
            }
        }
    }
}
