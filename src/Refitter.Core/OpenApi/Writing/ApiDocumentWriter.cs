using System.Text;
using System.Text.Json;

namespace Refitter.Core;

/// <summary>
/// Writes an <see cref="ApiDocument"/> as JSON in the specification it was read from.
/// </summary>
/// <remarks>
/// Copies of a document (e.g. before filtering or trimming it) are made by writing and reading it again, which is
/// how they have always been made. That normalizes the document: operation IDs are generated, references point
/// to where their target was first found, and schemas shared by several names become separate schemas.
/// See THIRD-PARTY-NOTICES.md.
/// </remarks>
internal sealed class ApiDocumentWriter
{
    private readonly ApiDocument document;
    private readonly bool isSwagger2;
    private readonly Dictionary<object, string> referencePaths;

    private ApiDocumentWriter(ApiDocument document)
    {
        this.document = document;
        isSwagger2 = document.SchemaType == ApiSchemaType.Swagger2;
        referencePaths = ApiJsonPathFinder.FindReferencePaths(document);
    }

    public static string Write(ApiDocument document)
    {
        document.GenerateOperationIds();
        return new ApiDocumentWriter(document).WriteDocument();
    }

    /// <summary>Copies a document by writing and reading it.</summary>
    public static ApiDocument Clone(ApiDocument document) =>
        ApiDocumentLoader.Load(Write(document), documentPath: null, isYaml: false);

    private string WriteDocument()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            if (isSwagger2)
            {
                if (document.Swagger != null)
                    writer.WriteString("swagger", document.Swagger);
            }
            else if (document.OpenApi != null)
            {
                writer.WriteString("openapi", document.OpenApi);
            }

            if (document.Info != null)
            {
                writer.WritePropertyName("info");
                writer.WriteStartObject();
                WriteString(writer, "title", document.Info.Title);
                WriteString(writer, OpenApiKeywords.Description, document.Info.Description);
                WriteString(writer, "version", document.Info.Version);
                writer.WriteEndObject();
            }

            if (isSwagger2)
            {
                WriteStringList(writer, "consumes", document.Consumes);
                WriteStringList(writer, "produces", document.Produces);
            }

            writer.WritePropertyName("paths");
            writer.WriteStartObject();
            foreach (var path in document.Paths)
            {
                writer.WritePropertyName(path.Key);
                WritePathItem(writer, path.Value);
            }

            writer.WriteEndObject();

            if (isSwagger2)
            {
                WriteSchemaDictionary(writer, "definitions", document.Definitions);
                WriteDictionary(writer, OpenApiKeywords.Parameters, document.Parameters, WriteParameter);
                WriteDictionary(writer, "responses", document.Responses, WriteResponse);
                WriteDictionary(writer, "securityDefinitions", document.SecurityDefinitions, WriteSecurityScheme);
            }
            else
            {
                writer.WritePropertyName("components");
                writer.WriteStartObject();
                WriteSchemaDictionary(writer, "schemas", document.Components.Schemas);
                WriteDictionary(writer, "requestBodies", document.Components.RequestBodies, WriteRequestBody);
                WriteDictionary(writer, "responses", document.Components.Responses, WriteResponse);
                WriteDictionary(writer, OpenApiKeywords.Parameters, document.Components.Parameters, WriteParameter);
                WriteDictionary(writer, "headers", document.Components.Headers, WriteParameter);
                WriteDictionary(writer, "securitySchemes", document.Components.SecuritySchemes, WriteSecurityScheme);
                writer.WriteEndObject();
            }

            WriteSecurityRequirements(writer, "security", document.Security);

            writer.WritePropertyName("tags");
            writer.WriteStartArray();
            foreach (var tag in document.Tags)
            {
                writer.WriteStartObject();
                WriteString(writer, "name", tag.Name);
                WriteString(writer, OpenApiKeywords.Description, tag.Description);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            WriteExtensionData(writer, document.ExtensionData);
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private void WritePathItem(Utf8JsonWriter writer, ApiPathItem pathItem)
    {
        writer.WriteStartObject();
        WriteString(writer, "summary", pathItem.Summary);
        WriteString(writer, OpenApiKeywords.Description, pathItem.Description);
        WriteExtensionData(writer, pathItem.ExtensionData);
        if (pathItem.Parameters.Count > 0)
        {
            writer.WritePropertyName(OpenApiKeywords.Parameters);
            writer.WriteStartArray();
            foreach (var parameter in pathItem.Parameters)
                WriteParameter(writer, parameter);
            writer.WriteEndArray();
        }

        foreach (var operation in pathItem)
        {
            writer.WritePropertyName(operation.Key.ToLowerInvariant());
            WriteOperation(writer, operation.Value);
        }

        writer.WriteEndObject();
    }

    private void WriteOperation(Utf8JsonWriter writer, ApiOperation operation)
    {
        writer.WriteStartObject();
        WriteStringList(writer, "tags", operation.Tags);
        WriteString(writer, "summary", operation.Summary);
        WriteString(writer, OpenApiKeywords.Description, operation.Description);
        WriteString(writer, "operationId", operation.OperationId);
        if (isSwagger2)
        {
            if (operation.Consumes != null)
                WriteStringList(writer, "consumes", operation.Consumes);
            if (operation.Produces != null)
                WriteStringList(writer, "produces", operation.Produces);
        }

        var parameters = isSwagger2
            ? operation.Parameters.ToList()
            : operation.Parameters.Where(p => p.Kind != ApiParameterKind.Body).ToList();
        writer.WritePropertyName(OpenApiKeywords.Parameters);
        writer.WriteStartArray();
        foreach (var parameter in parameters)
            WriteParameter(writer, parameter);
        writer.WriteEndArray();

        if (!isSwagger2 && operation.RequestBody != null)
        {
            writer.WritePropertyName("requestBody");
            WriteRequestBody(writer, operation.RequestBody);
        }

        writer.WritePropertyName("responses");
        writer.WriteStartObject();
        foreach (var response in operation.Responses)
        {
            writer.WritePropertyName(response.Key);
            WriteResponse(writer, response.Value);
        }

        writer.WriteEndObject();

        if (operation.IsDeprecated)
            writer.WriteBoolean("deprecated", true);

        if (operation.Security != null)
            WriteSecurityRequirements(writer, "security", operation.Security);

        WriteExtensionData(writer, operation.ExtensionData);
        writer.WriteEndObject();
    }

    private void WriteRequestBody(Utf8JsonWriter writer, ApiRequestBody requestBody)
    {
        writer.WriteStartObject();
        if (requestBody.Reference != null)
        {
            writer.WriteString("$ref", GetReferencePath(requestBody.Reference.ActualRequestBody));
        }

        WriteString(writer, "x-name", requestBody.Name);
        WriteString(writer, OpenApiKeywords.Description, requestBody.Description);
        if (requestBody.Content.Count > 0)
        {
            writer.WritePropertyName("content");
            writer.WriteStartObject();
            foreach (var mediaType in requestBody.Content)
            {
                writer.WritePropertyName(mediaType.Key);
                WriteMediaType(writer, mediaType.Value);
            }

            writer.WriteEndObject();
        }

        if (requestBody.IsRequired)
            writer.WriteBoolean("required", true);

        if (requestBody.Position.HasValue)
            writer.WriteNumber("x-position", requestBody.Position.Value);

        writer.WriteEndObject();
    }

    private void WriteMediaType(Utf8JsonWriter writer, ApiMediaType mediaType)
    {
        writer.WriteStartObject();
        if (mediaType.Schema != null)
        {
            writer.WritePropertyName("schema");
            WriteSchema(writer, mediaType.Schema);
        }

        if (mediaType.Example != null)
        {
            writer.WritePropertyName("example");
            WriteRawValue(writer, mediaType.Example);
        }

        writer.WriteEndObject();
    }

    private void WriteResponse(Utf8JsonWriter writer, ApiResponse response)
    {
        writer.WriteStartObject();
        if (response.Reference != null)
        {
            writer.WriteString("$ref", GetReferencePath(response.Reference.ActualResponse));
        }

        writer.WriteString(OpenApiKeywords.Description, response.Description ?? string.Empty);
        if (response.Headers.Count > 0)
        {
            writer.WritePropertyName("headers");
            writer.WriteStartObject();
            foreach (var header in response.Headers)
            {
                writer.WritePropertyName(header.Key);
                WriteParameter(writer, header.Value);
            }

            writer.WriteEndObject();
        }

        if (isSwagger2)
        {
            if (response.IsNullableRaw.HasValue)
                writer.WriteBoolean("x-nullable", response.IsNullableRaw.Value);

            if (response.Schema != null)
            {
                writer.WritePropertyName("schema");
                WriteSchema(writer, response.Schema);
            }

            if (response.Examples != null)
            {
                writer.WritePropertyName("examples");
                WriteRawValue(writer, response.Examples);
            }
        }
        else if (response.Content.Count > 0)
        {
            writer.WritePropertyName("content");
            writer.WriteStartObject();
            foreach (var mediaType in response.Content)
            {
                writer.WritePropertyName(mediaType.Key);
                WriteMediaType(writer, mediaType.Value);
            }

            writer.WriteEndObject();
        }

        WriteExtensionData(writer, response.ExtensionData);
        writer.WriteEndObject();
    }

    private void WriteSecurityScheme(Utf8JsonWriter writer, ApiSecurityScheme securityScheme)
    {
        writer.WriteStartObject();
        var type = securityScheme.Type;
        if (isSwagger2)
        {
            type = type switch
            {
                ApiSecuritySchemeType.Http => ApiSecuritySchemeType.Basic,
                ApiSecuritySchemeType.OpenIdConnect => ApiSecuritySchemeType.OAuth2,
                _ => type,
            };
        }

        writer.WriteString("type", type switch
        {
            ApiSecuritySchemeType.Basic => "basic",
            ApiSecuritySchemeType.ApiKey => "apiKey",
            ApiSecuritySchemeType.OAuth2 => "oauth2",
            ApiSecuritySchemeType.Http => "http",
            ApiSecuritySchemeType.OpenIdConnect => "openIdConnect",
            _ => OpenApiKeywords.Undefined,
        });
        WriteString(writer, OpenApiKeywords.Description, securityScheme.Description);
        WriteString(writer, "name", securityScheme.Name);
        if (securityScheme.In != ApiSecurityApiKeyLocation.Undefined)
            writer.WriteString("in", securityScheme.In.ToString().ToLowerInvariant());

        if (!isSwagger2)
        {
            WriteString(writer, "scheme", securityScheme.Scheme);
            WriteString(writer, "bearerFormat", securityScheme.BearerFormat);
            WriteString(writer, "openIdConnectUrl", securityScheme.OpenIdConnectUrl);
        }

        writer.WriteEndObject();
    }

    private void WriteParameter(Utf8JsonWriter writer, ApiParameter parameter) => WriteSchema(writer, parameter);

    private void WriteSchemaDictionary(Utf8JsonWriter writer, string name, IDictionary<string, ApiSchema> schemas)
    {
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        foreach (var schema in schemas)
        {
            writer.WritePropertyName(schema.Key);
            WriteSchema(writer, schema.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteDictionary<T>(
        Utf8JsonWriter writer,
        string name,
        IDictionary<string, T> items,
        Action<Utf8JsonWriter, T> write)
    {
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        foreach (var item in items)
        {
            writer.WritePropertyName(item.Key);
            write(writer, item.Value);
        }

        writer.WriteEndObject();
    }

    private void WriteSchema(Utf8JsonWriter writer, ApiSchema schema)
    {
        writer.WriteStartObject();
        var parameter = schema as ApiParameter;

        WriteString(writer, "$schema", schema.SchemaVersion);
        WriteString(writer, "id", schema.Id);
        if (!(isSwagger2 && parameter != null))
            WriteString(writer, "title", schema.Title);

        WriteType(writer, schema.Type);
        WriteDiscriminator(writer, schema);

        if (parameter != null)
            WriteParameterKeywords(writer, parameter);

        WriteValueKeywords(writer, schema);
        WriteSubschemas(writer, schema, parameter);
        WriteEnumeration(writer, schema);

        WriteSchemaList(writer, "allOf", schema.AllOf);
        WriteSchemaList(writer, "anyOf", schema.AnyOf);
        WriteSchemaList(writer, "oneOf", schema.OneOf);

        if (schema.Reference != null)
            writer.WriteString("$ref", GetSchemaReferencePath(schema.Reference));

        if (schema is ApiSchemaProperty property)
            WritePropertyKeywords(writer, property);

        WriteExtensionData(writer, schema.ExtensionData);
        writer.WriteEndObject();
    }

    private void WriteParameterKeywords(Utf8JsonWriter writer, ApiParameter parameter)
    {
        if (!string.IsNullOrEmpty(parameter.Name))
            writer.WriteString("name", parameter.Name);
        WriteString(writer, "x-originalName", parameter.OriginalName);
        if (parameter.Kind != ApiParameterKind.Undefined)
            writer.WriteString("in", ParameterKindName(parameter.Kind));
        if (parameter.Style != ApiParameterStyle.Undefined)
            writer.WriteString("style", ParameterStyleName(parameter.Style));
        if (parameter.Explode.HasValue)
            writer.WriteBoolean("explode", parameter.Explode.Value);
        WriteTrue(writer, "required", parameter.IsRequired);
        WriteTrue(writer, "allowEmptyValue", parameter.AllowEmptyValue);
        if (parameter.CollectionFormat != ApiParameterCollectionFormat.Undefined)
            writer.WriteString("collectionFormat", parameter.CollectionFormat.ToString().ToLowerInvariant());
        WriteOptionalSchema(writer, "schema", parameter.Schema);
        WriteOptionalSchema(writer, "x-schema", parameter.CustomSchema);

        if (!isSwagger2 && parameter.Position.HasValue)
            writer.WriteNumber("x-position", parameter.Position.Value);
    }

    private void WriteValueKeywords(Utf8JsonWriter writer, ApiSchema schema)
    {
        WriteString(writer, OpenApiKeywords.Description, schema.Description);
        WriteString(writer, "format", schema.Format);
        WriteOptionalRawValue(writer, "default", schema.Default);
        WriteDecimal(writer, "multipleOf", schema.MultipleOf);
        WriteDecimal(writer, "maximum", schema.Maximum);
        WriteDecimal(writer, "minimum", schema.Minimum);
        WriteOptionalNumber(writer, "maxLength", schema.MaxLength);
        WriteOptionalNumber(writer, "minLength", schema.MinLength);
        WriteString(writer, "pattern", schema.Pattern);
        WriteNonZero(writer, "maxItems", schema.MaxItems);
        WriteNonZero(writer, "minItems", schema.MinItems);
        WriteTrue(writer, "uniqueItems", schema.UniqueItems);
        WriteNonZero(writer, "maxProperties", schema.MaxProperties);
        WriteNonZero(writer, "minProperties", schema.MinProperties);
        WriteTrue(writer, isSwagger2 ? "x-deprecated" : "deprecated", schema.IsDeprecated);
        WriteString(writer, "x-deprecatedMessage", schema.DeprecatedMessage);
        WriteTrue(writer, "x-abstract", schema.IsAbstract);
        if (schema.IsNullableRaw.HasValue)
            writer.WriteBoolean(isSwagger2 ? "x-nullable" : "nullable", schema.IsNullableRaw.Value);
        WriteOptionalRawValue(writer, "example", schema.Example);
        WriteTrue(writer, "x-enumFlags", schema.IsFlagEnumerable);
    }

    private void WriteSubschemas(Utf8JsonWriter writer, ApiSchema schema, ApiParameter? parameter)
    {
        WriteOptionalSchema(writer, "x-dictionaryKey", schema.DictionaryKey);
        WriteOptionalSchema(writer, "not", schema.Not);

        WriteExclusiveBound(writer, "exclusiveMaximum", schema.ExclusiveMaximum, schema.IsExclusiveMaximum);
        WriteExclusiveBound(writer, "exclusiveMinimum", schema.ExclusiveMinimum, schema.IsExclusiveMinimum);

        if (schema.AdditionalItemsSchema != null)
            WriteOptionalSchema(writer, "additionalItems", schema.AdditionalItemsSchema);
        else if (!schema.AllowAdditionalItems)
            writer.WriteBoolean("additionalItems", false);

        WriteAdditionalProperties(writer, schema, parameter);
        WriteItems(writer, schema);

        if (parameter == null && schema.RequiredProperties.Count > 0)
            WriteStringList(writer, "required", schema.RequiredProperties);

        if (schema.Properties.Count > 0)
            WriteDictionary(writer, "properties", schema.Properties, WriteSchema);

        if (schema.PatternProperties.Count > 0)
            WriteDictionary(writer, "patternProperties", schema.PatternProperties, WriteSchema);

        if (schema.Definitions.Count > 0)
            WriteSchemaDictionary(writer, "definitions", schema.Definitions);
    }

    private void WriteAdditionalProperties(Utf8JsonWriter writer, ApiSchema schema, ApiParameter? parameter)
    {
        if (schema.AdditionalPropertiesSchema != null)
        {
            WriteOptionalSchema(writer, "additionalProperties", schema.AdditionalPropertiesSchema);
        }
        else if (isSwagger2)
        {
            // Swagger 2.0 schemas only allow additional properties when they say so, written as an empty schema object
            if (schema.AllowAdditionalProperties &&
                (schema.Type.IsObject() || schema.Type == ApiObjectTypes.None) &&
                !schema.HasReference &&
                schema.AllOf.Count == 0 &&
                parameter == null)
            {
                writer.WritePropertyName("additionalProperties");
                writer.WriteStartObject();
                writer.WriteEndObject();
            }
        }
        else if (!schema.AllowAdditionalProperties)
        {
            writer.WriteBoolean("additionalProperties", false);
        }
    }

    private void WriteItems(Utf8JsonWriter writer, ApiSchema schema)
    {
        if (schema.Item != null)
        {
            WriteOptionalSchema(writer, "items", schema.Item);
        }
        else if (schema.Items.Count > 0)
        {
            writer.WritePropertyName("items");
            writer.WriteStartArray();
            foreach (var item in schema.Items)
                WriteSchema(writer, item);
            writer.WriteEndArray();
        }
    }

    private static void WriteEnumeration(Utf8JsonWriter writer, ApiSchema schema)
    {
        if (schema.EnumerationNames.Count > 0)
            WriteStringList(writer, "x-enumNames", schema.EnumerationNames);

        if (schema.EnumerationDescriptions.Count > 0)
        {
            writer.WritePropertyName("x-enum-descriptions");
            writer.WriteStartArray();
            foreach (var description in schema.EnumerationDescriptions)
            {
                if (description == null)
                    writer.WriteNullValue();
                else
                    writer.WriteStringValue(description);
            }

            writer.WriteEndArray();
        }

        if (schema.Enumeration.Count > 0)
        {
            writer.WritePropertyName("enum");
            writer.WriteStartArray();
            foreach (var value in schema.Enumeration)
                WriteRawValue(writer, value);
            writer.WriteEndArray();
        }
    }

    private void WritePropertyKeywords(Utf8JsonWriter writer, ApiSchemaProperty property)
    {
        WriteTrue(writer, "readOnly", property.IsReadOnly);
        WriteTrue(writer, isSwagger2 ? "x-writeOnly" : "writeOnly", property.IsWriteOnly);
    }

    private void WriteOptionalSchema(Utf8JsonWriter writer, string name, ApiSchema? schema)
    {
        if (schema == null)
            return;

        writer.WritePropertyName(name);
        WriteSchema(writer, schema);
    }

    private static void WriteOptionalRawValue(Utf8JsonWriter writer, string name, object? value)
    {
        if (value == null)
            return;

        writer.WritePropertyName(name);
        WriteRawValue(writer, value);
    }

    private static void WriteOptionalNumber(Utf8JsonWriter writer, string name, int? value)
    {
        if (value.HasValue)
            writer.WriteNumber(name, value.Value);
    }

    private static void WriteNonZero(Utf8JsonWriter writer, string name, int value)
    {
        if (value != 0)
            writer.WriteNumber(name, value);
    }

    private static void WriteTrue(Utf8JsonWriter writer, string name, bool value)
    {
        if (value)
            writer.WriteBoolean(name, true);
    }

    private void WriteDiscriminator(Utf8JsonWriter writer, ApiSchema schema)
    {
        if (schema.DiscriminatorObject == null)
            return;

        if (isSwagger2)
        {
            WriteString(writer, "discriminator", schema.DiscriminatorObject.PropertyName);
            return;
        }

        writer.WritePropertyName("discriminator");
        writer.WriteStartObject();
        WriteString(writer, "propertyName", schema.DiscriminatorObject.PropertyName);
        if (schema.DiscriminatorObject.Mapping.Count > 0)
        {
            writer.WritePropertyName("mapping");
            writer.WriteStartObject();
            foreach (var mapping in schema.DiscriminatorObject.Mapping)
            {
                var path = mapping.Value.Reference != null
                    ? GetSchemaReferencePath(mapping.Value.Reference)
                    : mapping.Value.ReferencePath;
                if (path == null)
                    writer.WriteNull(mapping.Key);
                else
                    writer.WriteString(mapping.Key, path);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private void WriteSchemaList(Utf8JsonWriter writer, string name, IList<ApiSchema> schemas)
    {
        if (schemas.Count == 0)
            return;

        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var schema in schemas)
            WriteSchema(writer, schema);
        writer.WriteEndArray();
    }

    private static void WriteType(Utf8JsonWriter writer, ApiObjectTypes type)
    {
        var types = ApiObjectTypeExtensions.AllTypes.Where(t => type.HasFlag(t)).ToList();
        if (types.Count == 1)
        {
            writer.WriteString("type", types[0].ToJsonName());
        }
        else if (types.Count > 1)
        {
            writer.WritePropertyName("type");
            writer.WriteStartArray();
            foreach (var t in types)
                writer.WriteStringValue(t.ToJsonName());
            writer.WriteEndArray();
        }
    }

    private static void WriteExclusiveBound(Utf8JsonWriter writer, string name, decimal? bound, bool isExclusive)
    {
        if (bound.HasValue)
            WriteDecimal(writer, name, bound);
        else if (isExclusive)
            writer.WriteBoolean(name, true);
    }

    private static void WriteDecimal(Utf8JsonWriter writer, string name, decimal? value)
    {
        if (!value.HasValue)
            return;

        // Decimals are written with at least one decimal place, so they are read with it
        var text = value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        writer.WritePropertyName(name);
        writer.WriteRawValue(text.IndexOf('.') >= 0 ? text : text + ".0", skipInputValidation: true);
    }

    private static void WriteSecurityRequirements(Utf8JsonWriter writer, string name, List<ApiSecurityRequirement> requirements)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var requirement in requirements)
        {
            writer.WriteStartObject();
            foreach (var scheme in requirement)
            {
                writer.WritePropertyName(scheme.Key);
                writer.WriteStartArray();
                foreach (var scope in scheme.Value)
                    writer.WriteStringValue(scope);
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value != null)
            writer.WriteString(name, value);
    }

    private static void WriteStringList(Utf8JsonWriter writer, string name, IEnumerable<string> values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var value in values)
            writer.WriteStringValue(value);
        writer.WriteEndArray();
    }

    private static void WriteExtensionData(Utf8JsonWriter writer, Dictionary<string, object?>? extensionData)
    {
        if (extensionData == null)
            return;

        foreach (var item in extensionData)
        {
            writer.WritePropertyName(item.Key);
            WriteRawValue(writer, item.Value);
        }
    }

    private static void WriteRawValue(Utf8JsonWriter writer, object? value) =>
        writer.WriteRawValue(RawJson.ToIndentedString(value), skipInputValidation: true);

    // A reference points to where its actual schema was first found. For a parameter that is the schema of the
    // referenced parameter, which is why referenced parameters are lost in copies.
    private string GetSchemaReferencePath(ApiSchema reference) => GetReferencePath(reference.ActualSchema);

    private string GetReferencePath(object target) =>
        referencePaths.TryGetValue(target, out var path)
            ? path
            : throw new InvalidOperationException(
                "Could not find the JSON path of a referenced schema: " + target.GetType().FullName +
                ". Manually referenced schemas must be added to the 'Definitions' of a parent schema.");

    private static string ParameterKindName(ApiParameterKind kind)
    {
        switch (kind)
        {
            case ApiParameterKind.Body:
                return "body";
            case ApiParameterKind.Query:
                return "query";
            case ApiParameterKind.Path:
                return "path";
            case ApiParameterKind.Header:
                return "header";
            case ApiParameterKind.FormData:
                return "formData";
            case ApiParameterKind.ModelBinding:
                return "modelbinding";
            case ApiParameterKind.Cookie:
                return "cookie";
            default:
                return OpenApiKeywords.Undefined;
        }
    }

    private static string ParameterStyleName(ApiParameterStyle style)
    {
        switch (style)
        {
            case ApiParameterStyle.Simple:
                return "simple";
            case ApiParameterStyle.Label:
                return "label";
            case ApiParameterStyle.Matrix:
                return "matrix";
            case ApiParameterStyle.Form:
                return "form";
            case ApiParameterStyle.SpaceDelimited:
                return "spaceDelimited";
            case ApiParameterStyle.PipeDelimited:
                return "pipeDelimited";
            case ApiParameterStyle.DeepObject:
                return "deepObject";
            default:
                return OpenApiKeywords.Undefined;
        }
    }
}
