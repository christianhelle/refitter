using System.Globalization;
using System.Text.Json.Nodes;

namespace Refitter.Core.Validation.Reading;

/// <summary>
/// Reads values out of JSON nodes the way Microsoft.OpenApi (MIT license) does, including the errors it reports.
/// The type names in the messages are the Microsoft.OpenApi types being read.
/// </summary>
internal static class JsonNodeExtensions
{
    /// <summary>
    /// Returns the value as a string: numbers keep their text and booleans become <c>True</c> or <c>False</c>.
    /// </summary>
    /// <exception cref="SpecificationException">Thrown when the node is an object or an array.</exception>
    public static string? GetScalarValue(this JsonNode? node)
    {
        if (node is not JsonValue value)
            throw new SpecificationException("Expected scalar value.");

        return Convert.ToString(value.GetValue<object>(), CultureInfo.InvariantCulture);
    }

    public static string? GetReferencePointer(this JsonObject jsonObject) =>
        jsonObject.TryGetPropertyValue("$ref", out var reference) ? reference?.GetScalarValue() : null;

    public static JsonObject CheckMapNode(this JsonNode? node, string nodeName, ParsingContext context) =>
        node as JsonObject ?? throw new SpecificationReaderException(nodeName + " must be a map/object", context);

    /// <summary>
    /// Reads the objects of a list. Items that are not objects are skipped.
    /// </summary>
    public static List<T> CreateList<T>(
        this JsonNode? node,
        string typeName,
        Func<JsonNode, ParsingContext, T?> map,
        ParsingContext context)
        where T : class
    {
        var list = node as JsonArray
                   ?? throw new SpecificationReaderException("Expected list while parsing " + typeName, context);

        return list.OfType<JsonObject>()
            .Select(item => map(item, context))
            .Where(item => item != null)
            .Select(item => item!)
            .ToList();
    }

    public static List<JsonNode> CreateListOfAny(this JsonNode? node, ParsingContext context)
    {
        var list = node as JsonArray
                   ?? throw new SpecificationReaderException("Cannot create a list from this type of node.", context);

        return list.OfType<JsonNode>().ToList();
    }

    public static List<T> CreateSimpleList<T>(
        this JsonNode? node,
        string typeName,
        Func<JsonNode, T> map,
        ParsingContext context)
    {
        var list = node as JsonArray
                   ?? throw new SpecificationReaderException("Expected list while parsing " + typeName, context);

        return list.OfType<JsonNode>()
            .Select(item => item is JsonValue
                ? map(item)
                : throw new SpecificationReaderException("Expected a value while parsing at " + context.GetLocation() + "."))
            .ToList();
    }

    /// <summary>
    /// Reads the objects of a map. Values that are not objects are read as <c>null</c>.
    /// </summary>
    public static Dictionary<string, T?> CreateMap<T>(
        this JsonNode? node,
        string typeName,
        Func<JsonNode, ParsingContext, T> map,
        ParsingContext context)
        where T : class
    {
        var jsonObject = node as JsonObject
                         ?? throw new SpecificationReaderException("Expected map while parsing " + typeName, context);

        var result = new Dictionary<string, T?>(StringComparer.Ordinal);
        foreach (var property in jsonObject)
        {
            try
            {
                context.StartObject(property.Key);
                result.Add(property.Key, property.Value is JsonObject value ? map(value, context) : null);
            }
            finally
            {
                context.EndObject();
            }
        }

        return result;
    }

    public static Dictionary<string, T> CreateSimpleMap<T>(
        this JsonNode? node,
        string typeName,
        Func<JsonNode, T> map,
        ParsingContext context)
    {
        var jsonObject = node as JsonObject
                         ?? throw new SpecificationReaderException("Expected map while parsing " + typeName, context);

        var result = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var property in jsonObject)
        {
            try
            {
                context.StartObject(property.Key);
                if (property.Value is not JsonValue value)
                    throw new SpecificationReaderException("Expected scalar while parsing " + typeName, context);

                result.Add(property.Key, map(value));
            }
            finally
            {
                context.EndObject();
            }
        }

        return result;
    }

    public static Dictionary<string, HashSet<T>> CreateArrayMap<T>(
        this JsonNode? node,
        string typeName,
        Func<JsonNode, T> map,
        ParsingContext context)
    {
        var jsonObject = node as JsonObject
                         ?? throw new SpecificationReaderException("Expected map while parsing " + typeName, context);

        var result = new Dictionary<string, HashSet<T>>(StringComparer.Ordinal);
        foreach (var property in jsonObject)
        {
            try
            {
                context.StartObject(property.Key);
                if (property.Value is not JsonArray values)
                    throw new SpecificationReaderException("Expected array while parsing " + typeName, context);

                result.Add(property.Key, new HashSet<T>(values.OfType<JsonNode>().Select(map)));
            }
            finally
            {
                context.EndObject();
            }
        }

        return result;
    }

    /// <summary>
    /// Reads the fields of an object, reporting each problem at the field it was found in.
    /// </summary>
    public static void ParseMap<T>(
        this JsonObject? jsonObject,
        T target,
        FieldMap<T> fieldMap,
        ParsingContext context,
        Action<T, string, JsonNode>? unrecognizedProperty = null)
    {
        if (jsonObject == null)
            return;

        foreach (var property in jsonObject)
        {
            ParseField(property.Key, property.Value ?? JsonNullSentinel.JsonNull, target, fieldMap, context, unrecognizedProperty);
        }
    }

    private static void ParseField<T>(
        string name,
        JsonNode value,
        T target,
        FieldMap<T> fieldMap,
        ParsingContext context,
        Action<T, string, JsonNode>? unrecognizedProperty)
    {
        if (fieldMap.Fixed.TryGetValue(name, out var readField))
        {
            Read(context, name, () => readField(target, value, context));
            return;
        }

        var pattern = fieldMap.Patterns.FirstOrDefault(p => p.Matches(name));
        if (pattern.Read != null)
        {
            Read(context, name, () => pattern.Read(target, name, value, context));
            return;
        }

        if (unrecognizedProperty != null)
        {
            unrecognizedProperty(target, name, value);
            return;
        }

        var issue = new ValidationIssue(string.Empty, name + " is not a valid property at " + context.GetLocation());
        if ("$schema".Equals(name, StringComparison.OrdinalIgnoreCase))
            context.Diagnostics.Warnings.Add(issue);
        else
            context.Diagnostics.Errors.Add(issue);
    }

    private static void Read(ParsingContext context, string name, Action read)
    {
        try
        {
            context.StartObject(name);
            read();
        }
        catch (SpecificationReaderException exception)
        {
            context.Diagnostics.Errors.Add(new ValidationIssue(exception.Pointer, exception.Message));
        }
        catch (SpecificationException exception)
        {
            exception.Pointer = context.GetLocation();
            context.Diagnostics.Errors.Add(new ValidationIssue(exception.Pointer, exception.Message));
        }
        finally
        {
            context.EndObject();
        }
    }

    /// <summary>
    /// Reads an enum value by its name in the specification, reporting a value that is not one of them.
    /// </summary>
    public static bool TryGetEnum<T>(
        this string? displayName,
        IReadOnlyDictionary<string, T> values,
        ParsingContext context,
        out T result)
        where T : struct
    {
        if (displayName != null && values.TryGetValue(displayName, out result))
            return true;

        context.Diagnostics.Errors.Add(new ValidationIssue(context.GetLocation(), "Enum value " + displayName + " is not recognized."));
        result = default;
        return false;
    }
}
