#nullable enable

using System.Globalization;
using System.Text.Json;

namespace Refitter.Core;

/// <summary>
/// Reads the objects of an OpenAPI document from JSON. The keywords each object understands, and how values
/// are converted, depend on the specification the document is written in (<see cref="ApiSchemaType"/>).
/// References are recorded as <c>ReferencePath</c> and resolved afterwards by <see cref="ApiReferenceResolver"/>.
/// </summary>
internal sealed class ApiJsonReader
{
    private readonly ApiSchemaType schemaType;

    public ApiJsonReader(ApiSchemaType schemaType)
    {
        this.schemaType = schemaType;
    }

    public ApiSchemaType SchemaType => schemaType;

    public ApiDocument ReadDocument(JsonElement element)
    {
        EnsureObject(element, "document");
        var document = new ApiDocument { SchemaType = schemaType, Info = null };
        var isSwagger2 = schemaType == ApiSchemaType.Swagger2;

        foreach (var property in element.EnumerateObject())
        {
            var value = property.Value;
            switch (property.Name)
            {
                case "swagger" when isSwagger2:
                    document.Swagger = ReadString(value);
                    break;
                case "openapi" when !isSwagger2:
                    document.OpenApi = ReadString(value);
                    break;
                case "info":
                    document.Info = value.ValueKind == JsonValueKind.Null ? null : ReadInfo(value);
                    break;
                case "paths":
                    foreach (var path in NonNullPropertiesOf(value))
                        document.AddPath(path.Name, ReadPathItem(path.Value));
                    break;
                case "components" when !isSwagger2:
                    ReadComponents(value, document.Components);
                    break;
                case "definitions" when isSwagger2:
                    ReadSchemaDictionary(value, document.Components.Schemas);
                    break;
                case OpenApiKeywords.Parameters when isSwagger2:
                    ReadDictionary(value, document.Components.Parameters, ReadParameter);
                    break;
                case "responses" when isSwagger2:
                    ReadDictionary(value, document.Components.Responses, ReadComponentResponse(document));
                    break;
                case "securityDefinitions" when isSwagger2:
                    ReadDictionary(value, document.Components.SecuritySchemes, ReadSecurityScheme);
                    break;
                case "security":
                    document.Security = ReadSecurityRequirements(value) ?? new();
                    break;
                case "tags":
                    document.Tags = ReadTags(value);
                    break;
                case "consumes" when isSwagger2:
                    document.Consumes = ReadStringList(value) ?? new();
                    break;
                case "produces" when isSwagger2:
                    document.Produces = ReadStringList(value) ?? new();
                    break;
                case "x-generator":
                case "servers" when !isSwagger2:
                case "host" when isSwagger2:
                case "basePath" when isSwagger2:
                case "schemes" when isSwagger2:
                case "externalDocs":
                    break;
                default:
                    // Includes the keywords of the other specification, which references can still point into
                    AddExtensionData(document.ExtensionData ??= new(), property);
                    break;
            }
        }

        return document;
    }

    public ApiSchema ReadSchema(JsonElement element) => ReadSchema(element, new ApiSchema(schemaType));

    public ApiSchemaProperty ReadSchemaProperty(JsonElement element) =>
        (ApiSchemaProperty)ReadSchema(element, new ApiSchemaProperty(schemaType));

    public ApiParameter ReadParameter(JsonElement element) =>
        (ApiParameter)ReadSchema(element, new ApiParameter(schemaType));

    private static ApiInfo ReadInfo(JsonElement element)
    {
        EnsureObject(element, "info");
        var info = new ApiInfo();
        var hasTitle = false;
        var hasVersion = false;
        foreach (var property in element.EnumerateObject())
        {
            switch (property.Name)
            {
                case "title":
                    info.Title = ReadString(property.Value);
                    hasTitle = true;
                    break;
                case OpenApiKeywords.Description:
                    info.Description = ReadString(property.Value);
                    break;
                case "version":
                    info.Version = ReadString(property.Value);
                    hasVersion = true;
                    break;
            }
        }

        if (!hasTitle || info.Title == null)
            throw new ApiDocumentReadException("Required property 'title' not found in JSON.");

        if (!hasVersion || info.Version == null)
            throw new ApiDocumentReadException("Required property 'version' not found in JSON.");

        return info;
    }

    private void ReadComponents(JsonElement element, ApiComponents components)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in element.EnumerateObject())
        {
            switch (property.Name)
            {
                case "schemas":
                    ReadSchemaDictionary(property.Value, components.Schemas);
                    break;
                case "requestBodies":
                    ReadDictionary(property.Value, components.RequestBodies, ReadComponentRequestBody(components.Document));
                    break;
                case "responses":
                    ReadDictionary(property.Value, components.Responses, ReadComponentResponse(components.Document));
                    break;
                case OpenApiKeywords.Parameters:
                    ReadDictionary(property.Value, components.Parameters, ReadParameter);
                    break;
                case "headers":
                    ReadDictionary(property.Value, components.Headers, ReadParameter);
                    break;
                case "securitySchemes":
                    ReadDictionary(property.Value, components.SecuritySchemes, ReadSecurityScheme);
                    break;
            }
        }
    }

    private Func<JsonElement, ApiRequestBody> ReadComponentRequestBody(ApiDocument document) =>
        element =>
        {
            var requestBody = ReadRequestBody(element);
            requestBody.Parent = document;
            return requestBody;
        };

    private Func<JsonElement, ApiResponse> ReadComponentResponse(ApiDocument document) =>
        element =>
        {
            var response = ReadResponse(element);
            response.Parent = document;
            return response;
        };

    private void ReadSchemaDictionary(JsonElement element, ApiSchemaDictionary target)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Null)
            {
                target.Remove(property.Name);
                continue;
            }

            target[property.Name] = ReadSchema(property.Value);
        }
    }

    private static void ReadDictionary<T>(JsonElement element, Dictionary<string, T> target, Func<JsonElement, T> read)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Null)
                continue;

            target[property.Name] = read(property.Value);
        }
    }

    public ApiPathItem ReadPathItem(JsonElement element)
    {
        EnsureObject(element, "path item");
        var pathItem = new ApiPathItem();
        foreach (var property in element.EnumerateObject())
        {
            switch (property.Name)
            {
                case "summary":
                    pathItem.Summary = ReadString(property.Value);
                    continue;
                case OpenApiKeywords.Description:
                    pathItem.Description = ReadString(property.Value);
                    continue;
                case OpenApiKeywords.Parameters:
                    pathItem.Parameters = ReadList(property.Value, ReadParameter);
                    continue;
                case "servers":
                    continue;
            }

            if (property.Name.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
            {
                (pathItem.ExtensionData ??= new())[property.Name] = RawJson.FromElement(property.Value);
            }
            else if (property.Name.Contains("$ref"))
            {
                pathItem.ReferencePath = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : property.Value.GetRawText();
            }
            else
            {
                pathItem.Add(property.Name, ReadOperation(property.Value));
            }
        }

        return pathItem;
    }

    private ApiOperation ReadOperation(JsonElement element)
    {
        EnsureObject(element, "operation");
        var isSwagger2 = schemaType == ApiSchemaType.Swagger2;
        var operation = new ApiOperation();
        var hasResponses = false;

        foreach (var property in element.EnumerateObject())
        {
            var value = property.Value;
            switch (property.Name)
            {
                case "tags":
                    operation.Tags = ReadStringList(value) ?? new();
                    break;
                case "summary":
                    operation.Summary = ReadString(value);
                    break;
                case OpenApiKeywords.Description:
                    operation.Description = ReadString(value);
                    break;
                case "operationId":
                    operation.OperationId = ReadString(value);
                    break;
                case "consumes" when isSwagger2:
                    operation.Consumes = ReadStringList(value);
                    break;
                case "produces" when isSwagger2:
                    operation.Produces = ReadStringList(value);
                    break;
                case OpenApiKeywords.Parameters:
                    foreach (var item in NonNullItemsOf(value))
                        operation.Parameters.Add(ReadParameter(item));
                    break;
                case "requestBody" when !isSwagger2:
                    operation.RequestBody = value.ValueKind == JsonValueKind.Null ? null : ReadRequestBody(value);
                    break;
                case "responses":
                    ReadOperationResponses(value, operation);
                    hasResponses = true;
                    break;
                case "deprecated":
                    operation.IsDeprecated = ReadBoolean(value);
                    break;
                case "security":
                    operation.Security = ReadSecurityRequirements(value);
                    break;
                case "externalDocs":
                case "schemes" when isSwagger2:
                case "callbacks" when !isSwagger2:
                case "servers" when !isSwagger2:
                    break;
                default:
                    AddExtensionData(operation.ExtensionData ??= new(), property);
                    break;
            }
        }

        if (!hasResponses)
            throw new ApiDocumentReadException("Required property 'responses' not found in JSON.");

        return operation;
    }

    private void ReadOperationResponses(JsonElement value, ApiOperation operation)
    {
        if (value.ValueKind == JsonValueKind.Null)
            throw new ApiDocumentReadException("Required property 'responses' expects a value but got null.");

        foreach (var response in NonNullPropertiesOf(value))
            operation.AddResponse(response.Name, ReadResponse(response.Value));
    }

    public ApiRequestBody ReadRequestBody(JsonElement element)
    {
        EnsureObject(element, "request body");
        var requestBody = new ApiRequestBody();
        foreach (var property in element.EnumerateObject())
        {
            var value = property.Value;
            switch (property.Name)
            {
                case "$ref":
                    requestBody.ReferencePath = ReadString(value);
                    break;
                case "x-name":
                    requestBody.Name = ReadString(value);
                    break;
                case OpenApiKeywords.Description:
                    requestBody.Description = ReadString(value);
                    break;
                case "content":
                    foreach (var mediaType in PropertiesOf(value))
                        requestBody.Content[mediaType.Name] = ReadMediaType(mediaType.Value);
                    break;
                case "required":
                    requestBody.IsRequired = ReadBoolean(value);
                    break;
                case "x-position":
                    requestBody.Position = ReadNullableInt(value);
                    break;
            }
        }

        return requestBody;
    }

    private ApiMediaType ReadMediaType(JsonElement element)
    {
        var mediaType = new ApiMediaType();
        if (element.ValueKind != JsonValueKind.Object)
            return mediaType;

        foreach (var property in element.EnumerateObject())
        {
            switch (property.Name)
            {
                case "schema":
                    mediaType.Schema = property.Value.ValueKind == JsonValueKind.Null ? null : ReadSchema(property.Value);
                    break;
                case "example":
                    mediaType.Example = RawJson.FromElement(property.Value);
                    break;
            }
        }

        return mediaType;
    }

    public ApiResponse ReadResponse(JsonElement element)
    {
        EnsureObject(element, "response");
        var isSwagger2 = schemaType == ApiSchemaType.Swagger2;
        var response = new ApiResponse();
        foreach (var property in element.EnumerateObject())
        {
            var value = property.Value;
            switch (property.Name)
            {
                case "$ref":
                    response.ReferencePath = ReadString(value);
                    break;
                case OpenApiKeywords.Description:
                    response.Description = ReadString(value);
                    break;
                case "headers":
                    ReadDictionary(value, response.Headers, ReadParameter);
                    break;
                case "x-nullable" when isSwagger2:
                    response.IsNullableRaw = ReadNullableBoolean(value);
                    break;
                case "content" when !isSwagger2:
                    foreach (var mediaType in PropertiesOf(value))
                        response.Content[mediaType.Name] = ReadMediaType(mediaType.Value);
                    break;
                case "schema" when isSwagger2:
                    response.Schema = value.ValueKind == JsonValueKind.Null ? null : ReadSchema(value);
                    break;
                case "examples" when isSwagger2:
                    response.Examples = RawJson.FromElement(value);
                    break;
                case "x-expectedSchemas":
                case "links" when !isSwagger2:
                    break;
                default:
                    AddExtensionData(response.ExtensionData ??= new(), property);
                    break;
            }
        }

        return response;
    }

    private ApiSecurityScheme ReadSecurityScheme(JsonElement element)
    {
        EnsureObject(element, "security scheme");
        var isSwagger2 = schemaType == ApiSchemaType.Swagger2;
        var securityScheme = new ApiSecurityScheme();
        var hasType = false;
        foreach (var property in element.EnumerateObject())
        {
            var value = property.Value;
            switch (property.Name)
            {
                case "type":
                    securityScheme.Type = ReadEnum(
                        value,
                        ApiSecuritySchemeType.Undefined,
                        (OpenApiKeywords.Undefined, ApiSecuritySchemeType.Undefined),
                        ("basic", ApiSecuritySchemeType.Basic),
                        ("apiKey", ApiSecuritySchemeType.ApiKey),
                        ("oauth2", ApiSecuritySchemeType.OAuth2),
                        ("http", ApiSecuritySchemeType.Http),
                        ("openIdConnect", ApiSecuritySchemeType.OpenIdConnect));
                    hasType = true;
                    break;
                case OpenApiKeywords.Description:
                    securityScheme.Description = ReadString(value);
                    break;
                case "name":
                    securityScheme.Name = ReadString(value);
                    break;
                case "in":
                    securityScheme.In = ReadEnum(
                        value,
                        ApiSecurityApiKeyLocation.Undefined,
                        ("query", ApiSecurityApiKeyLocation.Query),
                        ("header", ApiSecurityApiKeyLocation.Header),
                        ("cookie", ApiSecurityApiKeyLocation.Cookie));
                    break;
                case "scheme" when !isSwagger2:
                    securityScheme.Scheme = ReadString(value);
                    break;
                case "bearerFormat" when !isSwagger2:
                    securityScheme.BearerFormat = ReadString(value);
                    break;
                case "openIdConnectUrl" when !isSwagger2:
                    securityScheme.OpenIdConnectUrl = ReadString(value);
                    break;
            }
        }

        if (!hasType)
            throw new ApiDocumentReadException("Required property 'type' not found in JSON.");

        return securityScheme;
    }

    private static List<ApiSecurityRequirement>? ReadSecurityRequirements(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
            return null;

        var requirements = new List<ApiSecurityRequirement>();
        if (element.ValueKind != JsonValueKind.Array)
            return requirements;

        foreach (var item in element.EnumerateArray())
        {
            var requirement = new ApiSecurityRequirement();
            if (item.ValueKind == JsonValueKind.Object)
            {
                foreach (var scheme in item.EnumerateObject())
                {
                    requirement[scheme.Name] = ReadStringList(scheme.Value) ?? new List<string>();
                }
            }

            requirements.Add(requirement);
        }

        return requirements;
    }

    private static List<ApiTag> ReadTags(JsonElement element)
    {
        var tags = new List<ApiTag>();
        if (element.ValueKind != JsonValueKind.Array)
            return tags;

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var tag = new ApiTag();
            foreach (var property in item.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "name":
                        tag.Name = ReadString(property.Value);
                        break;
                    case OpenApiKeywords.Description:
                        tag.Description = ReadString(property.Value);
                        break;
                }
            }

            tags.Add(tag);
        }

        return tags;
    }

    private ApiSchema ReadSchema(JsonElement element, ApiSchema schema)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new ApiDocumentReadException($"Unexpected {element.ValueKind} where a schema object was expected.");

        var property = schema as ApiSchemaProperty;
        var parameter = schema as ApiParameter;

        foreach (var member in element.EnumerateObject())
        {
            var value = member.Value;
            var name = member.Name;

            var isRead =
                (parameter != null && ReadParameterKeyword(parameter, name, value)) ||
                (property != null && ReadPropertyKeyword(property, name, value)) ||
                ReadValueKeyword(schema, name, value) ||
                ReadSubschemaKeyword(schema, name, value);

            // Includes nullable, deprecated and example (and their x- forms), which are read from the extension data
            if (!isRead)
                AddExtensionData(schema.ExtensionData ??= new(), member);
        }

        return schema;
    }

    private bool ReadPropertyKeyword(ApiSchemaProperty property, string name, JsonElement value)
    {
        if (schemaType == ApiSchemaType.JsonSchema ? name == "readonly" : name == "readOnly")
        {
            property.IsReadOnly = ReadBoolean(value);
            return true;
        }

        if (schemaType == ApiSchemaType.OpenApi3 ? name == "writeOnly" : name == "x-writeOnly")
        {
            property.IsWriteOnly = ReadBoolean(value);
            return true;
        }

        return false;
    }

    private bool ReadValueKeyword(ApiSchema schema, string name, JsonElement value)
    {
        var isOpenApi3 = schemaType == ApiSchemaType.OpenApi3;
        var isJsonSchema = schemaType == ApiSchemaType.JsonSchema;
        switch (name)
        {
            case "$ref":
                schema.ReferencePath = ReadString(value);
                return true;
            case "$schema":
                schema.SchemaVersion = ReadString(value);
                return true;
            case "id":
                schema.Id = ReadString(value);
                return true;
            case "title":
                schema.Title = ReadString(value);
                return true;
            case OpenApiKeywords.Description:
                schema.Description = ReadString(value);
                return true;
            case "format":
                schema.Format = ReadString(value);
                return true;
            case "default":
                schema.Default = RawJson.FromElement(value);
                return true;
            case "multipleOf":
                schema.MultipleOf = ReadNullableDecimal(value, clamp: false);
                return true;
            case "maximum":
                schema.Maximum = ReadNullableDecimal(value, clamp: true);
                return true;
            case "minimum":
                schema.Minimum = ReadNullableDecimal(value, clamp: true);
                return true;
            case "exclusiveMaximum":
                ReadExclusiveBound(value, b => schema.IsExclusiveMaximum = b, d => schema.ExclusiveMaximum = d);
                return true;
            case "exclusiveMinimum":
                ReadExclusiveBound(value, b => schema.IsExclusiveMinimum = b, d => schema.ExclusiveMinimum = d);
                return true;
            case "maxLength":
                schema.MaxLength = ReadNullableInt(value);
                return true;
            case "minLength":
                schema.MinLength = ReadNullableInt(value);
                return true;
            case "pattern":
                schema.Pattern = ReadString(value);
                return true;
            case "maxItems":
                schema.MaxItems = ReadInt(value);
                return true;
            case "minItems":
                schema.MinItems = ReadInt(value);
                return true;
            case "uniqueItems":
                schema.UniqueItems = ReadBoolean(value);
                return true;
            case "maxProperties":
                schema.MaxProperties = ReadInt(value);
                return true;
            case "minProperties":
                schema.MinProperties = ReadInt(value);
                return true;
            case "deprecated" when isOpenApi3:
            case "x-deprecated" when !isOpenApi3:
                schema.IsDeprecated = ReadBoolean(value);
                return true;
            case "x-deprecatedMessage":
                schema.DeprecatedMessage = ReadString(value);
                return true;
            case "x-abstract":
                schema.IsAbstract = ReadBoolean(value);
                return true;
            case "nullable" when isOpenApi3:
            case "x-nullable" when !isOpenApi3:
                schema.IsNullableRaw = ReadNullableBoolean(value);
                return true;
            case "example" when !isJsonSchema:
            case "x-example" when isJsonSchema:
                schema.Example = RawJson.FromElement(value);
                return true;
            case "x-enumFlags":
                schema.IsFlagEnumerable = ReadBoolean(value);
                return true;
            case "type":
                schema.Type = ReadType(value);
                return true;
            case "required":
                schema.RequiredProperties = ReadRequired(value);
                return true;
            default:
                return false;
        }
    }

    private bool ReadSubschemaKeyword(ApiSchema schema, string name, JsonElement value)
    {
        switch (name)
        {
            case "x-dictionaryKey":
                schema.DictionaryKey = ReadNullableSchema(value);
                return true;
            case "xml":
                return true;
            case "not":
                schema.Not = ReadNullableSchema(value);
                return true;
            case "discriminator":
                ReadDiscriminator(schema, value);
                return true;
            case "additionalItems":
                ReadAdditional(value, b => schema.AllowAdditionalItems = b, s => schema.AdditionalItemsSchema = s);
                return true;
            case "additionalProperties":
                ReadAdditional(value, b => schema.AllowAdditionalProperties = b, s => schema.AdditionalPropertiesSchema = s);
                return true;
            case "items":
                ReadItems(schema, value);
                return true;
            case "properties":
                ReadProperties(value, schema.Properties);
                return true;
            case "patternProperties":
                ReadProperties(value, schema.PatternProperties);
                return true;
            case "definitions":
                schema.Definitions.Clear();
                ReadSchemaDictionary(value, schema.Definitions);
                return true;
            case "x-enumNames":
                schema.EnumerationNames = ReadStringList(value) ?? new();
                return true;
            case "x-enum-names":
            case "x-enum-varnames":
                // Only honored when x-enumNames is absent, which the model never detects (kept for parity)
                return true;
            case "x-enumDescriptions":
            case "x-enum-descriptions":
                var descriptions = ReadPossibleStringArray(value);
                if (descriptions != null)
                    schema.EnumerationDescriptions = descriptions;
                return true;
            case "enum":
                schema.Enumeration = value.ValueKind == JsonValueKind.Array
                    ? value.EnumerateArray().Select(RawJson.FromElement).ToList()
                    : new List<object?>();
                return true;
            case "allOf":
                ReadSchemaList(value, schema.AllOf);
                return true;
            case "anyOf":
                ReadSchemaList(value, schema.AnyOf);
                return true;
            case "oneOf":
                ReadSchemaList(value, schema.OneOf);
                return true;
            default:
                return false;
        }
    }

    private void ReadItems(ApiSchema schema, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            schema.Item = null;
            schema.Items.Clear();
            foreach (var item in value.EnumerateArray())
                schema.Items.Add(ReadSchema(item));
        }
        else if (value.ValueKind != JsonValueKind.Null)
        {
            schema.Item = ReadSchema(value);
        }
    }

    private void ReadProperties(JsonElement value, ApiSchemaPropertyDictionary target)
    {
        target.Clear();
        foreach (var item in PropertiesOf(value))
            target[item.Name] = ReadSchemaProperty(item.Value);
    }

    private ApiSchema? ReadNullableSchema(JsonElement value) =>
        value.ValueKind == JsonValueKind.Null ? null : ReadSchema(value);

    /// <summary>The properties of an object, or none when the value is not an object.</summary>
    private static IEnumerable<JsonProperty> PropertiesOf(JsonElement value) =>
        value.ValueKind == JsonValueKind.Object ? value.EnumerateObject() : Enumerable.Empty<JsonProperty>();

    /// <summary>The properties of an object whose values are not null.</summary>
    private static IEnumerable<JsonProperty> NonNullPropertiesOf(JsonElement value) =>
        PropertiesOf(value).Where(property => property.Value.ValueKind != JsonValueKind.Null);

    /// <summary>The items of an array that are not null, or none when the value is not an array.</summary>
    private static IEnumerable<JsonElement> NonNullItemsOf(JsonElement value) =>
        value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Where(item => item.ValueKind != JsonValueKind.Null)
            : Enumerable.Empty<JsonElement>();

    private bool ReadParameterKeyword(ApiParameter parameter, string name, JsonElement value)
    {
        var isSwagger2 = schemaType == ApiSchemaType.Swagger2;
        switch (name)
        {
            case "name":
                parameter.Name = ReadString(value) ?? string.Empty;
                return true;
            case "x-originalName":
                parameter.OriginalName = ReadString(value);
                return true;
            case "in":
                parameter.Kind = ReadEnum(
                    value,
                    ApiParameterKind.Undefined,
                    (OpenApiKeywords.Undefined, ApiParameterKind.Undefined),
                    ("body", ApiParameterKind.Body),
                    ("query", ApiParameterKind.Query),
                    ("path", ApiParameterKind.Path),
                    ("header", ApiParameterKind.Header),
                    ("formData", ApiParameterKind.FormData),
                    ("modelbinding", ApiParameterKind.ModelBinding),
                    ("cookie", ApiParameterKind.Cookie));
                return true;
            case "style":
                parameter.Style = ReadEnum(
                    value,
                    ApiParameterStyle.Undefined,
                    (OpenApiKeywords.Undefined, ApiParameterStyle.Undefined),
                    ("simple", ApiParameterStyle.Simple),
                    ("label", ApiParameterStyle.Label),
                    ("matrix", ApiParameterStyle.Matrix),
                    ("form", ApiParameterStyle.Form),
                    ("spaceDelimited", ApiParameterStyle.SpaceDelimited),
                    ("pipeDelimited", ApiParameterStyle.PipeDelimited),
                    ("deepObject", ApiParameterStyle.DeepObject));
                return true;
            case "explode":
                parameter.Explode = ReadNullableBoolean(value);
                return true;
            case "required":
                parameter.IsRequired = ReadBoolean(value);
                return true;
            case "allowEmptyValue":
                parameter.AllowEmptyValue = ReadBoolean(value);
                return true;
            case OpenApiKeywords.Description:
                parameter.Description = ReadString(value);
                return true;
            case "collectionFormat":
                parameter.CollectionFormat = ReadEnum(
                    value,
                    ApiParameterCollectionFormat.Undefined,
                    (OpenApiKeywords.Undefined, ApiParameterCollectionFormat.Undefined),
                    ("csv", ApiParameterCollectionFormat.Csv),
                    ("ssv", ApiParameterCollectionFormat.Ssv),
                    ("tsv", ApiParameterCollectionFormat.Tsv),
                    ("pipes", ApiParameterCollectionFormat.Pipes),
                    ("multi", ApiParameterCollectionFormat.Multi));
                return true;
            case "examples" when isSwagger2:
                AddExtensionData(parameter.ExtensionData ??= new(), name, value);
                return true;
            case "examples":
                return true;
            case "schema":
                parameter.Schema = value.ValueKind == JsonValueKind.Null ? null : ReadSchema(value);
                return true;
            case "x-schema":
                parameter.CustomSchema = value.ValueKind == JsonValueKind.Null ? null : ReadSchema(value);
                return true;
            case "x-position":
                if (isSwagger2)
                    AddExtensionData(parameter.ExtensionData ??= new(), name, value);
                else
                    parameter.Position = ReadNullableInt(value);
                return true;
            case "title" when isSwagger2:
                AddExtensionData(parameter.ExtensionData ??= new(), name, value);
                return true;
            default:
                return false;
        }
    }

    private void ReadDiscriminator(ApiSchema schema, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                schema.Discriminator = value.GetString();
                break;
            case JsonValueKind.Object:
                var discriminator = new ApiDiscriminator();
                foreach (var property in value.EnumerateObject())
                {
                    switch (property.Name)
                    {
                        case "propertyName":
                            discriminator.PropertyName = ReadString(property.Value);
                            break;
                        case "mapping":
                            discriminator.Mapping.Clear();
                            if (property.Value.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var mapping in property.Value.EnumerateObject())
                                {
                                    discriminator.Mapping[mapping.Name] = new ApiSchema(schemaType)
                                    {
                                        ReferencePath = ReadString(mapping.Value),
                                    };
                                }
                            }

                            break;
                    }
                }

                schema.DiscriminatorObject = discriminator;
                break;
        }
    }

    private void ReadAdditional(JsonElement value, Action<bool> setAllowed, Action<ApiSchema> setSchema)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.True:
                setAllowed(true);
                break;
            case JsonValueKind.False:
                setAllowed(false);
                break;
            case JsonValueKind.String when value.GetString() is "true" or "false":
                setAllowed(value.GetString() == "true");
                break;
            case JsonValueKind.Null:
                break;
            default:
                setSchema(ReadSchema(value));
                break;
        }
    }

    private static void ReadExclusiveBound(JsonElement value, Action<bool> setFlag, Action<decimal> setBound)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.True:
                setFlag(true);
                break;
            case JsonValueKind.False:
                setFlag(false);
                break;
            case JsonValueKind.String when value.GetString() is "true" or "false":
                setFlag(value.GetString() == "true");
                break;
            case JsonValueKind.Null:
                break;
            case JsonValueKind.Number when value.GetRawText().IndexOfAny(['.', 'e', 'E']) >= 0 &&
                                           TryConvertDoubleToDecimal(value.GetDouble(), out var fractionalBound):
                // Bounds that can be a boolean are read as a double first, which drops trailing zeros
                setBound(fractionalBound);
                break;
            default:
                var bound = ReadNullableDecimal(value, clamp: true);
                if (bound.HasValue)
                    setBound(bound.Value);
                break;
        }
    }

    private static bool TryConvertDoubleToDecimal(double value, out decimal result)
    {
        try
        {
            result = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (OverflowException)
        {
            result = 0;
            return false;
        }
    }

    private void ReadSchemaList(JsonElement value, ApiSchemaList target)
    {
        target.Clear();
        if (value.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in value.EnumerateArray())
        {
            target.Add(ReadSchema(item));
        }
    }

    private static ApiObjectTypes ReadType(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.Array => value.EnumerateArray()
                .Aggregate(ApiObjectTypes.None, (type, item) => type | ApiObjectTypeExtensions.Parse(ToScalarString(item))),
            JsonValueKind.String => ApiObjectTypeExtensions.Parse(value.GetString()),
            _ => ApiObjectTypes.None,
        };

    private static List<string> ReadRequired(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
                return new List<string>();
            case JsonValueKind.Array:
                return value.EnumerateArray()
                    .Select(item => item.ValueKind == JsonValueKind.String
                        ? item.GetString()!
                        : throw new ApiDocumentReadException("Unexpected value in the required property names."))
                    .ToList();
            default:
                throw new ApiDocumentReadException($"Error converting value {value.GetRawText()} to the required property names.");
        }
    }

    private static List<string?>? ReadPossibleStringArray(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
            return null;

        var list = new List<string?>();
        foreach (var item in value.EnumerateArray())
        {
            switch (item.ValueKind)
            {
                case JsonValueKind.String:
                    list.Add(item.GetString());
                    break;
                case JsonValueKind.Null:
                    list.Add(null);
                    break;
                default:
                    return null;
            }
        }

        return list;
    }

    private static List<T> ReadList<T>(JsonElement value, Func<JsonElement, T> read)
    {
        var list = new List<T>();
        if (value.ValueKind != JsonValueKind.Array)
            return list;

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Null)
                list.Add(read(item));
        }

        return list;
    }

    private static void AddExtensionData(Dictionary<string, object?> extensionData, JsonProperty property) =>
        AddExtensionData(extensionData, property.Name, property.Value);

    private static void AddExtensionData(Dictionary<string, object?> extensionData, string name, JsonElement value) =>
        extensionData[name] = RawJson.FromElement(value);

    private static void EnsureObject(JsonElement element, string description)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new ApiDocumentReadException($"Unexpected {element.ValueKind} where a {description} object was expected.");
    }

    internal static string? ReadString(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number => value.GetRawText(),
            _ => throw new ApiDocumentReadException($"Unexpected {value.ValueKind} where a string was expected."),
        };

    private static string? ToScalarString(JsonElement value) =>
        value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();

    private static List<string>? ReadStringList(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null)
            return null;

        if (value.ValueKind != JsonValueKind.Array)
            throw new ApiDocumentReadException($"Unexpected {value.ValueKind} where a list of strings was expected.");

        return value.EnumerateArray().Select(item => ReadString(item)!).ToList();
    }

    internal static bool ReadBoolean(JsonElement value) => ReadNullableBoolean(value) ?? false;

    private static bool? ReadNullableBoolean(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            JsonValueKind.Number => value.GetDouble() != 0,
            _ => throw new ApiDocumentReadException($"Could not convert {value.GetRawText()} to a boolean."),
        };

    private static int ReadInt(JsonElement value) => ReadNullableInt(value) ?? 0;

    private static int? ReadNullableInt(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Number when value.TryGetInt32(out var number):
                return number;
            case JsonValueKind.Number:
                return Convert.ToInt32(value.GetDouble(), CultureInfo.InvariantCulture);
            case JsonValueKind.String:
                var text = value.GetString();
                if (string.IsNullOrEmpty(text))
                    return null;
                return int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
            default:
                throw new ApiDocumentReadException($"Could not convert {value.GetRawText()} to an integer.");
        }
    }

    private static decimal? ReadNullableDecimal(JsonElement value, bool clamp)
    {
        string? text;
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
                return null;
            case JsonValueKind.Number:
                text = value.GetRawText();
                break;
            case JsonValueKind.String:
                text = value.GetString();
                if (string.IsNullOrEmpty(text))
                    return null;
                break;
            default:
                throw new ApiDocumentReadException($"Could not convert {value.GetRawText()} to a number.");
        }

        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            return result;

        // Bounds outside the decimal range (e.g. double.MaxValue) are clamped (#1273)
        if (clamp && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return number < 0 ? decimal.MinValue : decimal.MaxValue;

        throw new ApiDocumentReadException($"Could not convert {text} to a decimal.");
    }

    private static TEnum ReadEnum<TEnum>(JsonElement value, TEnum defaultValue, params (string Name, TEnum Value)[] values)
    {
        if (value.ValueKind == JsonValueKind.Null)
            return defaultValue;

        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText();
        foreach (var (name, enumValue) in values)
        {
            if (string.Equals(name, text, StringComparison.Ordinal))
                return enumValue;
        }

        foreach (var (name, enumValue) in values)
        {
            if (string.Equals(name, text, StringComparison.OrdinalIgnoreCase))
                return enumValue;
        }

        throw new ApiDocumentReadException($"Error converting value \"{text}\" to type '{typeof(TEnum).Name}'.");
    }
}

/// <summary>An OpenAPI document could not be read.</summary>
internal sealed class ApiDocumentReadException(string message) : InvalidOperationException(message);
