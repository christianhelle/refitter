using System.Collections;

namespace Refitter.Core;

/// <summary>
/// The properties of a schema, keyed by their JSON names. Each property knows its name and parent schema.
/// Keeps insertion order like <see cref="Dictionary{TKey, TValue}"/> (a removed slot is reused by the next insert).
/// </summary>
internal sealed class ApiSchemaPropertyDictionary : IDictionary<string, ApiSchemaProperty>
{
    private readonly ApiSchema owner;
    private readonly Dictionary<string, ApiSchemaProperty> items = new();

    public ApiSchemaPropertyDictionary(ApiSchema owner, bool setNames = true)
    {
        this.owner = owner;
    }

    public ApiSchemaProperty this[string key]
    {
        get => items[key];
        set
        {
            items[key] = value;
            Wire();
        }
    }

    public ICollection<string> Keys => items.Keys;

    public ICollection<ApiSchemaProperty> Values => items.Values;

    public int Count => items.Count;

    public bool IsReadOnly => false;

    public void Add(string key, ApiSchemaProperty value)
    {
        items.Add(key, value);
        Wire();
    }

    public void Add(KeyValuePair<string, ApiSchemaProperty> item) => Add(item.Key, item.Value);

    public void Clear() => items.Clear();

    public bool Contains(KeyValuePair<string, ApiSchemaProperty> item) => items.Contains(item);

    public bool ContainsKey(string key) => items.ContainsKey(key);

    public void CopyTo(KeyValuePair<string, ApiSchemaProperty>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<string, ApiSchemaProperty>>)items).CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<string, ApiSchemaProperty>> GetEnumerator() => items.GetEnumerator();

    public bool Remove(string key) => items.Remove(key);

    public bool Remove(KeyValuePair<string, ApiSchemaProperty> item) =>
        ((ICollection<KeyValuePair<string, ApiSchemaProperty>>)items).Remove(item);

    public bool TryGetValue(string key, out ApiSchemaProperty value) => items.TryGetValue(key, out value!);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Wire()
    {
        foreach (var item in items)
        {
            item.Value.Name = item.Key;
            item.Value.Parent = owner;
        }
    }
}

/// <summary>The named schemas of a schema or document (definitions). Each schema knows its parent.</summary>
internal sealed class ApiSchemaDictionary : IDictionary<string, ApiSchema>
{
    private readonly object owner;
    private readonly Dictionary<string, ApiSchema> items = new();

    public ApiSchemaDictionary(object owner)
    {
        this.owner = owner;
    }

    public ApiSchema this[string key]
    {
        get => items[key];
        set
        {
            if (value == null!)
            {
                items.Remove(key);
                return;
            }

            items[key] = value;
            value.Parent = owner;
        }
    }

    public ICollection<string> Keys => items.Keys;

    public ICollection<ApiSchema> Values => items.Values;

    public int Count => items.Count;

    public bool IsReadOnly => false;

    public void Add(string key, ApiSchema value)
    {
        items.Add(key, value);
        value.Parent = owner;
    }

    public void Add(KeyValuePair<string, ApiSchema> item) => Add(item.Key, item.Value);

    public void Clear() => items.Clear();

    public bool Contains(KeyValuePair<string, ApiSchema> item) => items.Contains(item);

    public bool ContainsKey(string key) => items.ContainsKey(key);

    public void CopyTo(KeyValuePair<string, ApiSchema>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<string, ApiSchema>>)items).CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<string, ApiSchema>> GetEnumerator() => items.GetEnumerator();

    public bool Remove(string key) => items.Remove(key);

    public bool Remove(KeyValuePair<string, ApiSchema> item) =>
        ((ICollection<KeyValuePair<string, ApiSchema>>)items).Remove(item);

    public bool TryGetValue(string key, out ApiSchema value) => items.TryGetValue(key, out value!);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>A list of sub-schemas (allOf, anyOf, oneOf, tuple items). Each schema knows its parent.</summary>
internal sealed class ApiSchemaList : IList<ApiSchema>
{
    private readonly ApiSchema owner;
    private readonly List<ApiSchema> items = new();

    public ApiSchemaList(ApiSchema owner)
    {
        this.owner = owner;
    }

    public ApiSchema this[int index]
    {
        get => items[index];
        set
        {
            items[index] = value;
            value.Parent = owner;
        }
    }

    public int Count => items.Count;

    public bool IsReadOnly => false;

    public void Add(ApiSchema item)
    {
        items.Add(item);
        item.Parent = owner;
    }

    public void Clear() => items.Clear();

    public bool Contains(ApiSchema item) => items.Contains(item);

    public void CopyTo(ApiSchema[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);

    public IEnumerator<ApiSchema> GetEnumerator() => items.GetEnumerator();

    public int IndexOf(ApiSchema item) => items.IndexOf(item);

    public void Insert(int index, ApiSchema item)
    {
        items.Insert(index, item);
        item.Parent = owner;
    }

    public bool Remove(ApiSchema item) => items.Remove(item);

    public void RemoveAt(int index) => items.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
