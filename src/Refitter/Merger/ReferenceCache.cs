using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace Refitter.Merger;

internal class ReferenceCache
{
    private readonly Dictionary<ReferenceType, Dictionary<string, IOpenApiReferenceable>> data = new();

    public int Count => data.Sum(x => x.Value.Count);

    public void Add(IOpenApiReferenceable referenceable)
    {
        var type = referenceable.Reference.Type ?? ReferenceType.Schema;
        if (!data.ContainsKey(type))
        {
            data[type] = new Dictionary<string, IOpenApiReferenceable>();
        }

        if (data[type].TryGetValue(referenceable.Reference.Id, out _))
        {
            return;
        }

        data[type][referenceable.Reference.Id] = referenceable;
    }

    public void UpdateDocument(OpenApiDocument document)
    {
        foreach (var kvp in data)
        {
            switch (kvp.Key)
            {
                case ReferenceType.Schema:
                    Update(document.Components.Schemas, kvp.Value);
                    break;
                case ReferenceType.Response:
                    Update(document.Components.Responses, kvp.Value);
                    break;
                case ReferenceType.Parameter:
                    Update(document.Components.Parameters, kvp.Value);
                    break;
                case ReferenceType.Example:
                    Update(document.Components.Examples, kvp.Value);
                    break;
                case ReferenceType.RequestBody:
                    Update(document.Components.RequestBodies, kvp.Value);
                    break;
                case ReferenceType.Header:
                    Update(document.Components.Headers, kvp.Value);
                    break;
                case ReferenceType.SecurityScheme:
                    Update(document.Components.SecuritySchemes, kvp.Value);
                    break;
                case ReferenceType.Link:
                    Update(document.Components.Links, kvp.Value);
                    break;
                case ReferenceType.Callback:
                    Update(document.Components.Callbacks, kvp.Value);
                    break;
                case ReferenceType.Tag:
                    Update(document.Tags, kvp.Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kvp.Key));
            }
        }
    }

    private static void Update<T>(IDictionary<string, T> collection, Dictionary<string, IOpenApiReferenceable> data)
        where T : IOpenApiReferenceable
    {
        foreach (var kvp in data)
        {
            collection[kvp.Key] = (T)kvp.Value;
        }
    }

    private static void Update<T>(ICollection<T> collection, Dictionary<string, IOpenApiReferenceable> data)
        where T : IOpenApiReferenceable
    {
        foreach (var kvp in data)
        {
            collection.Add((T)kvp.Value);
        }
    }
}