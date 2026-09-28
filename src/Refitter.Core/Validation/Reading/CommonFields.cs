#nullable enable

using System.Globalization;
using System.Text.Json.Nodes;
using Refitter.Core.Validation.Model;

namespace Refitter.Core.Validation.Reading;

/// <summary>
/// What the Swagger 2.0 and OpenAPI 3 readers read the same way.
/// </summary>
internal static class CommonFields
{
    private static readonly FieldMap<SpecContact> ContactFields = new FieldMap<SpecContact>()
        .Field("name", (_, n, _) => n.GetScalarValue())
        .Field("email", (o, n, _) => o.Email = n.GetScalarValue())
        .Field("url", (_, n, _) => ReadUri(n))
        .Extensions();

    private static readonly FieldMap<SpecExternalDocs> ExternalDocsFields = new FieldMap<SpecExternalDocs>()
        .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
        .Field("url", (o, n, _) => o.Url = ReadUri(n))
        .Extensions();

    public static SpecContact LoadContact(JsonNode node, ParsingContext context)
    {
        var contact = new SpecContact();
        (node as JsonObject).ParseMap(contact, ContactFields, context);
        return contact;
    }

    public static SpecExternalDocs LoadExternalDocs(JsonNode node, ParsingContext context)
    {
        var externalDocs = new SpecExternalDocs();
        node.CheckMapNode(OpenApiNames.ExternalDocs, context).ParseMap(externalDocs, ExternalDocsFields, context);
        return externalDocs;
    }

    public static SpecSecurityRequirement LoadSecurityRequirement(JsonNode node, ParsingContext context)
    {
        var requirement = new SpecSecurityRequirement();
        foreach (var scheme in node.CheckMapNode("security", context))
        {
            requirement.Add(SpecReferences.Create(scheme.Key, null));
            scheme.Value.CreateSimpleList(OpenApiNames.StringType, item => item.GetScalarValue(), context);
        }

        return requirement;
    }

    /// <summary>
    /// Adds the operation fields both readers read alike.
    /// </summary>
    public static FieldMap<SpecOperation> OperationFields(this FieldMap<SpecOperation> fields) =>
        fields
            .Field("tags", (_, n, c) => n.CreateSimpleList("OpenApiTagReference", item => item.GetScalarValue(), c))
            .Field(OpenApiNames.Summary, (_, n, _) => n.GetScalarValue())
            .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
            .Field(OpenApiNames.ExternalDocs, (_, n, c) => LoadExternalDocs(n, c))
            .Field("operationId", (_, n, _) => n.GetScalarValue())
            .Field(OpenApiNames.Deprecated, (_, n, _) => ReadBool(n))
            .Field("security", (o, n, c) =>
            {
                if (n is JsonArray)
                    o.Security = n.CreateList("OpenApiSecurityRequirement", LoadSecurityRequirement, c);
            });

    /// <summary>
    /// Adds the schema fields both readers read alike.
    /// </summary>
    public static FieldMap<SpecSchema> SchemaFields(
        this FieldMap<SpecSchema> fields,
        Func<JsonNode, ParsingContext, SpecSchema> loadSchema,
        Action<JsonNode, ParsingContext> loadXml) =>
        fields
            .Field("title", (_, n, _) => n.GetScalarValue())
            .Field("multipleOf", (_, n, _) =>
            {
                var multipleOf = n.GetScalarValue();
                if (multipleOf != null)
                    ScalarChecks.CheckDecimal(multipleOf, NumberStyles.Float, CultureInfo.InvariantCulture);
            })
            .Field("maximum", (_, n, _) => n.GetScalarValue())
            .Field("minimum", (_, n, _) => n.GetScalarValue())
            .Field("maxLength", (_, n, _) => ReadInt(n))
            .Field("minLength", (_, n, _) => ReadInt(n))
            .Field("pattern", (_, n, _) => n.GetScalarValue())
            .Field("maxItems", (_, n, _) => ReadInt(n))
            .Field("minItems", (_, n, _) => ReadInt(n))
            .Field("uniqueItems", (_, n, _) => ReadBool(n))
            .Field("maxProperties", (_, n, _) => ReadInt(n))
            .Field("minProperties", (_, n, _) => ReadInt(n))
            .Field(OpenApiNames.Required, (o, n, c) => o.Required = new HashSet<string>(
                n.CreateSimpleList(OpenApiNames.StringType, item => item.GetScalarValue(), c).OfType<string>(),
                StringComparer.Ordinal))
            .Field("enum", (_, n, c) => n.CreateListOfAny(c))
            .Field("allOf", (o, n, c) => o.AllOf = n.CreateList(OpenApiNames.SchemaType, loadSchema, c))
            .Field("items", (o, n, c) => o.Items = loadSchema(n, c))
            .Field("properties", (o, n, c) => o.Properties = n.CreateMap(OpenApiNames.SchemaType, loadSchema, c))
            .Field("additionalProperties", (o, n, c) =>
            {
                if (n is JsonValue)
                    ReadBool(n);
                else
                    o.AdditionalProperties = loadSchema(n, c);
            })
            .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
            .Field("format", (_, n, _) => n.GetScalarValue())
            .Field("default", (_, _, _) => { })
            .Field("readOnly", (_, n, _) => ReadBool(n))
            .Field("xml", (_, n, c) => loadXml(n, c))
            .Field(OpenApiNames.ExternalDocs, (o, n, c) => o.ExternalDocs = LoadExternalDocs(n, c))
            .Field(OpenApiNames.Example, (_, _, _) => { });

    public static Uri ReadUri(JsonNode node) =>
        new(node.GetScalarValue(), UriKind.RelativeOrAbsolute);

    public static void ReadBool(JsonNode node)
    {
        var value = node.GetScalarValue();
        if (value != null)
            ScalarChecks.CheckBoolean(value);
    }

    public static void ReadInt(JsonNode node)
    {
        var value = node.GetScalarValue();
        if (value != null)
            ScalarChecks.CheckInt32(value, CultureInfo.InvariantCulture);
    }
}
