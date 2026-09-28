using System.Globalization;
using System.Text.Json.Nodes;
using Refitter.Core.Validation.Model;

namespace Refitter.Core.Validation.Reading;

/// <summary>
/// Reads Swagger 2.0 documents into the OpenAPI 3 shaped <see cref="SpecDocument"/>, reporting what the
/// Microsoft.OpenApi (MIT license) Swagger 2.0 deserializer reports. Like it, body and form parameters become
/// request bodies, and response schemas become content for each media type the operation produces.
/// </summary>
internal sealed class OpenApiV2Reader
{
    private const string BodyParameterKey = "bodyParameter";
    private const string FormParametersKey = "formParameters";
    private const string ParameterIsBodyOrFormDataKey = "parameterIsBodyOrFormData";
    private const string OperationConsumesKey = "operationConsumes";
    private const string OperationProducesKey = "operationProduces";
    private const string GlobalConsumesKey = "globalConsumes";
    private const string GlobalProducesKey = "globalProduces";
    private const string ResponseSchemaKey = "responseSchema";
    private const string ResponseProducesSetKey = "responseProducesSet";
    private const string ExamplesKey = "examples";
    private const string FlowKey = "flow";
    private const string FlowValueKey = "flowValue";

    private static readonly IReadOnlyDictionary<string, SpecParameterLocation> ParameterLocations =
        new Dictionary<string, SpecParameterLocation>(StringComparer.OrdinalIgnoreCase)
        {
            ["query"] = SpecParameterLocation.Query,
            ["header"] = SpecParameterLocation.Header,
            ["path"] = SpecParameterLocation.Path,
            ["cookie"] = SpecParameterLocation.Cookie,
            ["querystring"] = SpecParameterLocation.QueryString,
        };

    private readonly FieldMap<SpecDocument> documentFields;
    private readonly FieldMap<SpecInfo> infoFields;
    private readonly FieldMap<SpecContact> contactFields;
    private readonly FieldMap<SpecLicense> licenseFields;
    private readonly FieldMap<SpecPaths> pathsFields;
    private readonly FieldMap<SpecPathItem> pathItemFields;
    private readonly FieldMap<SpecOperation> operationFields;
    private readonly FieldMap<SpecParameter> parameterFields;
    private readonly FieldMap<SpecResponses> responsesFields;
    private readonly FieldMap<SpecResponse> responseFields;
    private readonly FieldMap<SpecHeader> headerFields;
    private readonly FieldMap<SpecSchema> schemaFields;
    private readonly FieldMap<object> xmlFields;
    private readonly FieldMap<SpecSecurityScheme> securitySchemeFields;
    private readonly FieldMap<SpecTag> tagFields;
    private readonly FieldMap<SpecExternalDocs> externalDocsFields;

    public OpenApiV2Reader()
    {
        documentFields = new FieldMap<SpecDocument>()
            .Field("swagger", (_, _, _) => { })
            .Field("info", (o, n, c) => o.Info = LoadInfo(n, c))
            .Field("host", (_, n, c) => c.SetTempStorage("host", n.GetScalarValue()))
            .Field("basePath", (_, n, c) => c.SetTempStorage("basePath", n.GetScalarValue()))
            .Field("schemes", (_, n, c) => c.SetTempStorage("schemes", n.CreateSimpleList("String", s => s.GetScalarValue(), c)))
            .Field("consumes", (_, n, c) => StoreMediaTypes(n, c, GlobalConsumesKey))
            .Field("produces", (_, n, c) => StoreMediaTypes(n, c, GlobalProducesKey))
            .Field("paths", (o, n, c) => o.Paths = LoadPaths(n, c))
            .Field("definitions", (o, n, c) =>
            {
                o.Components ??= new SpecComponents();
                o.Components.Schemas = n.CreateMap("IOpenApiSchema", LoadSchema, c);
            })
            .Field("parameters", (o, n, c) =>
            {
                o.Components ??= new SpecComponents();
                o.Components.Parameters = n.CreateMap("IOpenApiParameter", (p, context) => LoadParameter(p, loadRequestBody: false, context), c)
                    .Where(parameter => parameter.Value != null)
                    .ToDictionary(parameter => parameter.Key, parameter => parameter.Value, StringComparer.Ordinal);
                o.Components.RequestBodies = n.CreateMap(
                        "IOpenApiRequestBody",
                        (p, context) =>
                        {
                            var parameter = LoadParameter(p, loadRequestBody: true, context);
                            return parameter == null ? null : CreateRequestBody(context, parameter);
                        },
                        c)
                    .Where(requestBody => requestBody.Value != null)
                    .ToDictionary(requestBody => requestBody.Key, requestBody => requestBody.Value, StringComparer.Ordinal);
            })
            .Field("responses", (o, n, c) =>
            {
                o.Components ??= new SpecComponents();
                o.Components.Responses = n.CreateMap("IOpenApiResponse", LoadResponse, c);
            })
            .Field("securityDefinitions", (o, n, c) =>
            {
                o.Components ??= new SpecComponents();
                o.Components.SecuritySchemes = n.CreateMap("IOpenApiSecurityScheme", LoadSecurityScheme, c);
            })
            .Field("security", (o, n, c) => o.Security = n.CreateList("OpenApiSecurityRequirement", LoadSecurityRequirement, c))
            .Field("tags", (o, n, c) =>
            {
                var tags = OpenApiV3Reader.DistinctTags(n.CreateList("OpenApiTag", LoadTag, c));
                if (tags.Count > 0)
                    o.Tags = tags;
            })
            .Field("externalDocs", (o, n, c) => o.ExternalDocs = LoadExternalDocs(n, c))
            .Extensions();

        infoFields = new FieldMap<SpecInfo>()
            .Field("title", (o, n, _) => o.Title = n.GetScalarValue())
            .Field("description", (_, n, _) => n.GetScalarValue())
            .Field("termsOfService", (_, n, _) => ReadUri(n))
            .Field("contact", (o, n, c) => o.Contact = LoadContact(n, c))
            .Field("license", (o, n, c) => o.License = LoadLicense(n, c))
            .Field("version", (o, n, _) => o.Version = n.GetScalarValue())
            .Extensions();

        contactFields = new FieldMap<SpecContact>()
            .Field("name", (_, n, _) => n.GetScalarValue())
            .Field("url", (_, n, _) => ReadUri(n))
            .Field("email", (o, n, _) => o.Email = n.GetScalarValue())
            .Extensions();

        licenseFields = new FieldMap<SpecLicense>()
            .Field("name", (o, n, _) => o.Name = n.GetScalarValue())
            .Field("url", (_, n, _) => ReadUri(n))
            .Extensions();

        pathsFields = new FieldMap<SpecPaths>()
            .Pattern(s => s.StartsWith("/", StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.Add(k, LoadPathItem(n, c)))
            .Extensions();

        pathItemFields = new FieldMap<SpecPathItem>()
            .Field("get", (o, n, c) => o.AddOperation("get", LoadOperation(n, c)))
            .Field("put", (o, n, c) => o.AddOperation("put", LoadOperation(n, c)))
            .Field("post", (o, n, c) => o.AddOperation("post", LoadOperation(n, c)))
            .Field("delete", (o, n, c) => o.AddOperation("delete", LoadOperation(n, c)))
            .Field("options", (o, n, c) => o.AddOperation("options", LoadOperation(n, c)))
            .Field("head", (o, n, c) => o.AddOperation("head", LoadOperation(n, c)))
            .Field("patch", (o, n, c) => o.AddOperation("patch", LoadOperation(n, c)))
            .Field("parameters", LoadPathParameters)
            .Extensions();

        operationFields = new FieldMap<SpecOperation>()
            .Field("tags", (_, n, c) => n.CreateSimpleList("OpenApiTagReference", item => item.GetScalarValue(), c))
            .Field("summary", (_, n, _) => n.GetScalarValue())
            .Field("description", (_, n, _) => n.GetScalarValue())
            .Field("externalDocs", (_, n, c) => LoadExternalDocs(n, c))
            .Field("operationId", (_, n, _) => n.GetScalarValue())
            .Field("parameters", (o, n, c) => o.Parameters = n.CreateList("IOpenApiParameter", LoadOperationParameter, c))
            .Field("consumes", (_, n, c) => StoreMediaTypes(n, c, OperationConsumesKey))
            .Field("produces", (_, n, c) => StoreMediaTypes(n, c, OperationProducesKey))
            .Field("responses", (o, n, c) => o.Responses = LoadResponses(n, c))
            .Field("deprecated", (_, n, _) => ReadBool(n))
            .Field("security", (o, n, c) =>
            {
                if (n is JsonArray)
                    o.Security = n.CreateList("OpenApiSecurityRequirement", LoadSecurityRequirement, c);
            })
            .Extensions();

        responsesFields = new FieldMap<SpecResponses>()
            .Pattern(s => !FieldMap<SpecResponses>.IsExtension(s), (o, p, n, c) => o.Add(p, LoadResponse(n, c)))
            .Extensions();

        parameterFields = new FieldMap<SpecParameter>()
            .Field("name", (o, n, _) => o.Name = n.GetScalarValue())
            .Field("in", ProcessIn)
            .Field("description", (_, n, _) => n.GetScalarValue())
            .Field("required", (o, n, _) =>
            {
                var required = n.GetScalarValue();
                if (required != null)
                    o.Required = bool.Parse(required);
            })
            .Field("deprecated", (_, n, _) => ReadBool(n))
            .Field("allowEmptyValue", (_, n, _) => ReadBool(n))
            .Field("type", (o, n, _) =>
            {
                var type = n.GetScalarValue();
                if (type != null)
                {
                    GetOrCreateSchema(o);
                    type.ToJsonSchemaType();
                }
            })
            .Field("items", (o, n, c) => GetOrCreateSchema(o).Items = LoadSchema(n, c))
            .Field("collectionFormat", (_, n, _) =>
            {
                if (n.GetScalarValue() == "tsv")
                    throw new NotSupportedException();
            })
            .Field("format", (o, n, _) =>
            {
                GetOrCreateSchema(o);
                n.GetScalarValue();
            })
            .Field("minimum", (o, n, _) => ReadSchemaText(o, n))
            .Field("maximum", (o, n, _) => ReadSchemaText(o, n))
            .Field("maxLength", (o, n, _) => ReadSchemaInt(o, n))
            .Field("minLength", (o, n, _) => ReadSchemaInt(o, n))
            .Field("readOnly", (o, n, _) =>
            {
                var readOnly = n.GetScalarValue();
                if (readOnly != null)
                {
                    GetOrCreateSchema(o);
                    ScalarChecks.CheckBoolean(readOnly);
                }
            })
            .Field("default", (o, _, _) => GetOrCreateSchema(o))
            .Field("pattern", (o, n, _) =>
            {
                GetOrCreateSchema(o);
                n.GetScalarValue();
            })
            .Field("enum", (o, n, c) =>
            {
                GetOrCreateSchema(o);
                n.CreateListOfAny(c);
            })
            .Field("schema", (o, n, c) => o.Schema = LoadSchema(n, c))
            .Field("x-examples", (o, n, c) => c.SetTempStorage(ExamplesKey, LoadExamplesExtension(n, c), o))
            .Pattern(
                s => FieldMap<SpecParameter>.IsExtension(s) && !s.Equals("x-examples", StringComparison.OrdinalIgnoreCase),
                (_, _, _, _) => { });

        responseFields = new FieldMap<SpecResponse>()
            .Field("description", (o, n, _) => o.Description = n.GetScalarValue())
            .Field("headers", (o, n, c) => o.Headers = n.CreateMap("IOpenApiHeader", LoadHeader, c))
            .Field("examples", LoadExamples)
            .Field("x-examples", (o, n, c) => c.SetTempStorage(ExamplesKey, LoadExamplesExtension(n, c), o))
            .Field("schema", (o, n, c) => c.SetTempStorage(ResponseSchemaKey, LoadSchema(n, c), o))
            .Pattern(
                s => FieldMap<SpecResponse>.IsExtension(s) && !s.Equals("x-examples", StringComparison.OrdinalIgnoreCase),
                (_, _, _, _) => { });

        headerFields = new FieldMap<SpecHeader>()
            .Field("description", (_, n, _) => n.GetScalarValue())
            .Field("type", (o, n, _) =>
            {
                var type = n.GetScalarValue();
                if (type != null)
                {
                    GetOrCreateSchema(o);
                    type.ToJsonSchemaType();
                }
            })
            .Field("format", (o, n, _) =>
            {
                GetOrCreateSchema(o);
                n.GetScalarValue();
            })
            .Field("items", (o, n, c) => GetOrCreateSchema(o).Items = LoadSchema(n, c))
            .Field("collectionFormat", (_, n, _) =>
            {
                var style = n.GetScalarValue();
                if (style != null)
                    LoadHeaderStyle(style);
            })
            .Field("default", (o, _, _) => GetOrCreateSchema(o))
            .Field("maximum", (o, n, _) => ReadSchemaText(o, n))
            .Field("exclusiveMaximum", (o, n, _) =>
            {
                GetOrCreateSchema(o);
                ScalarChecks.CheckBoolean(n.GetScalarValue()!);
            })
            .Field("minimum", (o, n, _) => ReadSchemaText(o, n))
            .Field("exclusiveMinimum", (o, n, _) =>
            {
                GetOrCreateSchema(o);
                ScalarChecks.CheckBoolean(n.GetScalarValue()!);
            })
            .Field("maxLength", (o, n, _) => ReadSchemaInt(o, n))
            .Field("minLength", (o, n, _) => ReadSchemaInt(o, n))
            .Field("pattern", (o, n, _) =>
            {
                GetOrCreateSchema(o);
                n.GetScalarValue();
            })
            .Field("maxItems", (o, n, _) => ReadSchemaInt(o, n))
            .Field("minItems", (o, n, _) => ReadSchemaInt(o, n))
            .Field("uniqueItems", (o, n, _) =>
            {
                var uniqueItems = n.GetScalarValue();
                if (uniqueItems != null)
                {
                    GetOrCreateSchema(o);
                    ScalarChecks.CheckBoolean(uniqueItems);
                }
            })
            .Field("multipleOf", (o, n, _) =>
            {
                var multipleOf = n.GetScalarValue();
                if (multipleOf != null)
                {
                    GetOrCreateSchema(o);
                    ScalarChecks.CheckDecimal(multipleOf, CultureInfo.InvariantCulture);
                }
            })
            .Field("enum", (o, n, c) =>
            {
                GetOrCreateSchema(o);
                n.CreateListOfAny(c);
            })
            .Extensions();

        schemaFields = new FieldMap<SpecSchema>()
            .Field("title", (_, n, _) => n.GetScalarValue())
            .Field("multipleOf", (_, n, _) =>
            {
                var multipleOf = n.GetScalarValue();
                if (multipleOf != null)
                    ScalarChecks.CheckDecimal(multipleOf, NumberStyles.Float, CultureInfo.InvariantCulture);
            })
            .Field("maximum", (_, n, _) => n.GetScalarValue())
            .Field("exclusiveMaximum", (_, n, _) => ScalarChecks.CheckBoolean(n.GetScalarValue()!))
            .Field("minimum", (_, n, _) => n.GetScalarValue())
            .Field("exclusiveMinimum", (_, n, _) => ScalarChecks.CheckBoolean(n.GetScalarValue()!))
            .Field("maxLength", (_, n, _) => ReadInt(n))
            .Field("minLength", (_, n, _) => ReadInt(n))
            .Field("pattern", (_, n, _) => n.GetScalarValue())
            .Field("maxItems", (_, n, _) => ReadInt(n))
            .Field("minItems", (_, n, _) => ReadInt(n))
            .Field("uniqueItems", (_, n, _) => ReadBool(n))
            .Field("maxProperties", (_, n, _) => ReadInt(n))
            .Field("minProperties", (_, n, _) => ReadInt(n))
            .Field("required", (o, n, c) => o.Required = new HashSet<string>(
                n.CreateSimpleList("String", item => item.GetScalarValue(), c).OfType<string>(),
                StringComparer.Ordinal))
            .Field("enum", (_, n, c) => n.CreateListOfAny(c))
            .Field("type", (_, n, _) => n.GetScalarValue()?.ToJsonSchemaType())
            .Field("allOf", (o, n, c) => o.AllOf = n.CreateList("IOpenApiSchema", LoadSchema, c))
            .Field("items", (o, n, c) => o.Items = LoadSchema(n, c))
            .Field("properties", (o, n, c) => o.Properties = n.CreateMap("IOpenApiSchema", LoadSchema, c))
            .Field("additionalProperties", (o, n, c) =>
            {
                if (n is JsonValue)
                    ReadBool(n);
                else
                    o.AdditionalProperties = LoadSchema(n, c);
            })
            .Field("description", (_, n, _) => n.GetScalarValue())
            .Field("format", (_, n, _) => n.GetScalarValue())
            .Field("default", (_, _, _) => { })
            .Field("discriminator", (o, n, _) => o.Discriminator = new SpecDiscriminator { PropertyName = n.GetScalarValue() })
            .Field("readOnly", (_, n, _) => ReadBool(n))
            .Field("xml", (_, n, c) => LoadXml(n, c))
            .Field("externalDocs", (o, n, c) => o.ExternalDocs = LoadExternalDocs(n, c))
            .Field("example", (_, _, _) => { })
            .Field("x-jsonschema-patternProperties", (_, n, c) => n.CreateMap("IOpenApiSchema", LoadSchema, c))
            .Extensions();

        xmlFields = new FieldMap<object>()
            .Field("name", (_, n, _) => n.GetScalarValue())
            .Field("namespace", (_, n, _) =>
            {
                var xmlNamespace = n.GetScalarValue();
                if (Uri.IsWellFormedUriString(xmlNamespace, UriKind.Absolute) && xmlNamespace != null)
                    return;

                throw new SpecificationReaderException("Xml Namespace requires absolute URL. '" + n.GetScalarValue() + "' is not valid.");
            })
            .Field("prefix", (_, n, _) => n.GetScalarValue())
            .Field("attribute", (_, n, _) => ReadBool(n))
            .Field("wrapped", (_, n, _) => ReadBool(n))
            .Extensions();

        securitySchemeFields = new FieldMap<SpecSecurityScheme>()
            .Field("type", (o, n, c) =>
            {
                var type = n.GetScalarValue();
                switch (type)
                {
                    case "basic":
                        o.Type = SpecSecuritySchemeType.Http;
                        break;
                    case "apiKey":
                        o.Type = SpecSecuritySchemeType.ApiKey;
                        break;
                    case "oauth2":
                        o.Type = SpecSecuritySchemeType.OAuth2;
                        break;
                    default:
                        c.Diagnostics.Errors.Add(new ValidationIssue(c.GetLocation(), "Security scheme type " + type + " is not recognized."));
                        break;
                }
            })
            .Field("description", (_, n, _) => n.GetScalarValue())
            .Field("name", (o, n, _) => o.Name = n.GetScalarValue())
            .Field("in", (o, n, c) =>
            {
                if (n.GetScalarValue().TryGetEnum(ParameterLocations, c, out var location))
                    o.In = location;
            })
            .Field("flow", (o, n, c) => c.SetTempStorage(FlowValueKey, n.GetScalarValue(), o))
            .Field("authorizationUrl", (o, n, c) =>
            {
                var url = n.GetScalarValue();
                if (url != null)
                    c.GetFromTempStorage<SpecOAuthFlow>(FlowKey, o)!.AuthorizationUrl = new Uri(url, UriKind.RelativeOrAbsolute);
            })
            .Field("tokenUrl", (o, n, c) =>
            {
                var url = n.GetScalarValue();
                if (url != null)
                    c.GetFromTempStorage<SpecOAuthFlow>(FlowKey, o)!.TokenUrl = new Uri(url, UriKind.RelativeOrAbsolute);
            })
            .Field("scopes", (o, n, c) => c.GetFromTempStorage<SpecOAuthFlow>(FlowKey, o)!.Scopes = n
                .CreateSimpleMap("String", item => item.GetScalarValue(), c)
                .Where(scope => scope.Value != null)
                .ToDictionary(scope => scope.Key, scope => scope.Value!, StringComparer.Ordinal))
            .Extensions();

        tagFields = new FieldMap<SpecTag>()
            .Field("name", (o, n, _) => o.Name = n.GetScalarValue())
            .Field("description", (_, n, _) => n.GetScalarValue())
            .Field("externalDocs", (o, n, c) => o.ExternalDocs = LoadExternalDocs(n, c))
            .Extensions();

        externalDocsFields = new FieldMap<SpecExternalDocs>()
            .Field("description", (_, n, _) => n.GetScalarValue())
            .Field("url", (o, n, _) => o.Url = ReadUri(n))
            .Extensions();
    }

    public SpecDocument LoadDocument(JsonNode jsonNode, ParsingContext context)
    {
        var document = new SpecDocument();
        jsonNode.CheckMapNode("OpenAPI", context).ParseMap(document, documentFields, context);

        if (document.Paths != null)
        {
            ProcessResponsesMediaTypes(
                document.Paths.Values
                    .SelectMany(pathItem => pathItem?.Operations?.Values ?? Enumerable.Empty<SpecOperation>())
                    .SelectMany(operation => operation.Responses?.Values ?? Enumerable.Empty<SpecResponse?>()),
                context);
        }

        ProcessResponsesMediaTypes(document.Components?.Responses?.Values, context);
        document.Servers ??= [];
        MakeServers(document.Servers, context);
        FixRequestBodyReferences(document);
        return document;
    }

    private static void StoreMediaTypes(JsonNode node, ParsingContext context, string key)
    {
        var mediaTypes = node.CreateSimpleList("String", s => s.GetScalarValue(), context);
        if (mediaTypes.Count > 0)
            context.SetTempStorage(key, mediaTypes);
    }

    private SpecInfo LoadInfo(JsonNode node, ParsingContext context)
    {
        var info = new SpecInfo();
        node.CheckMapNode("Info", context).ParseMap(info, infoFields, context);
        return info;
    }

    private SpecContact LoadContact(JsonNode node, ParsingContext context)
    {
        var contact = new SpecContact();
        (node as JsonObject).ParseMap(contact, contactFields, context);
        return contact;
    }

    private SpecLicense LoadLicense(JsonNode node, ParsingContext context)
    {
        var license = new SpecLicense();
        node.CheckMapNode("OpenApiLicense", context).ParseMap(license, licenseFields, context);
        return license;
    }

    private SpecPaths LoadPaths(JsonNode node, ParsingContext context)
    {
        var paths = new SpecPaths();
        node.CheckMapNode("Paths", context).ParseMap(paths, pathsFields, context);
        return paths;
    }

    private SpecPathItem LoadPathItem(JsonNode node, ParsingContext context)
    {
        var pathItem = new SpecPathItem();
        node.CheckMapNode("PathItem", context).ParseMap(pathItem, pathItemFields, context);
        return pathItem;
    }

    private void LoadPathParameters(SpecPathItem pathItem, JsonNode node, ParsingContext context)
    {
        context.SetTempStorage(BodyParameterKey, null);
        context.SetTempStorage(FormParametersKey, null);
        pathItem.Parameters = node.CreateList("IOpenApiParameter", LoadOperationParameter, context);

        var bodyParameter = context.GetFromTempStorage<SpecParameter>(BodyParameterKey);
        if (bodyParameter != null && pathItem.Operations != null)
        {
            SetRequestBodyWhereMissing(pathItem, CreateRequestBody(context, bodyParameter));
            return;
        }

        var formParameters = context.GetFromTempStorage<List<SpecParameter>>(FormParametersKey);
        if (formParameters != null && pathItem.Operations != null)
            SetRequestBodyWhereMissing(pathItem, CreateFormBody(context, formParameters));
    }

    private static void SetRequestBodyWhereMissing(SpecPathItem pathItem, SpecRequestBody requestBody)
    {
        foreach (var operation in pathItem.Operations!.Where(operation => operation.Value.RequestBody == null))
        {
            if (operation.Key is "post" or "put" or "patch")
                operation.Value.RequestBody = requestBody;
        }
    }

    private SpecOperation LoadOperation(JsonNode node, ParsingContext context)
    {
        context.SetTempStorage(BodyParameterKey, null);
        context.SetTempStorage(FormParametersKey, null);
        context.SetTempStorage(OperationProducesKey, null);
        context.SetTempStorage(OperationConsumesKey, null);

        var jsonObject = node.CheckMapNode("Operation", context);
        var operation = new SpecOperation();
        jsonObject.ParseMap(operation, operationFields, context);

        var bodyParameter = context.GetFromTempStorage<SpecParameter>(BodyParameterKey);
        if (bodyParameter != null)
        {
            operation.RequestBody = CreateRequestBody(context, bodyParameter);
        }
        else
        {
            var formParameters = context.GetFromTempStorage<List<SpecParameter>>(FormParametersKey);
            if (formParameters != null)
                operation.RequestBody = CreateFormBody(context, formParameters);
        }

        var produces = context.GetFromTempStorage<List<string?>>(OperationProducesKey);
        if ((produces != null || jsonObject.ContainsKey("produces")) && operation.Responses != null)
        {
            foreach (var response in operation.Responses.Values)
            {
                if (response is { Reference: null })
                    ProcessProduces(response, context);
            }
        }

        context.SetTempStorage(OperationProducesKey, null);
        return operation;
    }

    private SpecResponses LoadResponses(JsonNode node, ParsingContext context)
    {
        var responses = new SpecResponses();
        node.CheckMapNode("Responses", context).ParseMap(responses, responsesFields, context);
        return responses;
    }

    private static SpecRequestBody CreateFormBody(ParsingContext context, List<SpecParameter> formParameters)
    {
        var mediaType = new SpecMediaType
        {
            Schema = new SpecSchema
            {
                // Microsoft.OpenApi copies each parameter schema, and fails on a parameter without one
                Properties = formParameters
                    .Where(parameter => parameter.Name != null)
                    .ToDictionary(
                        parameter => parameter.Name!,
                        parameter => (SpecSchema?)CopySchema(parameter.Schema ?? throw new NullReferenceException()),
                        StringComparer.Ordinal),
                Required = new HashSet<string>(
                    formParameters.Where(parameter => parameter.Required && parameter.Name != null).Select(parameter => parameter.Name!),
                    StringComparer.Ordinal),
            },
        };

        var consumes = context.GetFromTempStorage<List<string?>>(OperationConsumesKey)
                       ?? context.GetFromTempStorage<List<string?>>(GlobalConsumesKey)
                       ?? ["application/x-www-form-urlencoded"];
        return new SpecRequestBody
        {
            Content = consumes.ToDictionary(contentType => contentType!, _ => (SpecMediaType?)mediaType, StringComparer.Ordinal),
        };
    }

    private static SpecRequestBody CreateRequestBody(ParsingContext context, SpecParameter bodyParameter)
    {
        var consumes = context.GetFromTempStorage<List<string?>>(OperationConsumesKey)
                       ?? context.GetFromTempStorage<List<string?>>(GlobalConsumesKey)
                       ?? ["application/json"];
        return new SpecRequestBody
        {
            Content = consumes.ToDictionary(
                contentType => contentType!,
                _ => (SpecMediaType?)new SpecMediaType { Schema = bodyParameter.Schema },
                StringComparer.Ordinal),
        };
    }

    private static SpecSchema CopySchema(SpecSchema schema) =>
        new()
        {
            Reference = schema.Reference,
            Items = schema.Items,
            Not = schema.Not,
            AllOf = schema.AllOf,
            AnyOf = schema.AnyOf,
            OneOf = schema.OneOf,
            Properties = schema.Properties,
            AdditionalProperties = schema.AdditionalProperties,
            Discriminator = schema.Discriminator,
            ExternalDocs = schema.ExternalDocs,
            Required = schema.Required,
        };

    private static void ProcessIn(SpecParameter parameter, JsonNode node, ParsingContext context)
    {
        var location = node.GetScalarValue();
        switch (location)
        {
            case "body":
                context.SetTempStorage(ParameterIsBodyOrFormDataKey, true);
                context.SetTempStorage(BodyParameterKey, parameter);
                break;
            case "formData":
                context.SetTempStorage(ParameterIsBodyOrFormDataKey, true);
                var formParameters = context.GetFromTempStorage<List<SpecParameter>>(FormParametersKey);
                if (formParameters == null)
                {
                    formParameters = [];
                    context.SetTempStorage(FormParametersKey, formParameters);
                }

                formParameters.Add(parameter);
                break;
            case "query":
            case "header":
            case "path":
                parameter.In = ParameterLocations[location];
                break;
            default:
                parameter.In = null;
                break;
        }
    }

    private SpecParameter? LoadOperationParameter(JsonNode node, ParsingContext context) =>
        LoadParameter(node, loadRequestBody: false, context);

    private SpecParameter? LoadParameter(JsonNode node, bool loadRequestBody, ParsingContext context)
    {
        context.SetTempStorage(ParameterIsBodyOrFormDataKey, false);
        var jsonObject = node.CheckMapNode("parameter", context);
        var reference = jsonObject.GetReferencePointer();
        if (reference != null)
            return new SpecParameter { Reference = GetReference(reference) };

        var parameter = new SpecParameter();
        jsonObject.ParseMap(parameter, parameterFields, context);
        context.SetTempStorage(ExamplesKey, null);

        var isBodyOrFormData = context.GetFromTempStorage<object>(ParameterIsBodyOrFormDataKey) is true;
        if (isBodyOrFormData != loadRequestBody)
            return null;

        return parameter;
    }

    private static SpecSchema GetOrCreateSchema(SpecParameter parameter) =>
        parameter.Schema is { Reference: null } schema ? schema : parameter.Schema = new SpecSchema();

    private static SpecSchema GetOrCreateSchema(SpecHeader header) =>
        header.Schema is { Reference: null } schema ? schema : header.Schema = new SpecSchema();

    private static void ReadSchemaText(SpecParameter parameter, JsonNode node)
    {
        if (!string.IsNullOrEmpty(node.GetScalarValue()))
            GetOrCreateSchema(parameter);
    }

    private static void ReadSchemaText(SpecHeader header, JsonNode node)
    {
        if (!string.IsNullOrEmpty(node.GetScalarValue()))
            GetOrCreateSchema(header);
    }

    private static void ReadSchemaInt(SpecParameter parameter, JsonNode node)
    {
        var value = node.GetScalarValue();
        if (value != null)
        {
            GetOrCreateSchema(parameter);
            ScalarChecks.CheckInt32(value, CultureInfo.InvariantCulture);
        }
    }

    private static void ReadSchemaInt(SpecHeader header, JsonNode node)
    {
        var value = node.GetScalarValue();
        if (value != null)
        {
            GetOrCreateSchema(header);
            ScalarChecks.CheckInt32(value, CultureInfo.InvariantCulture);
        }
    }

    private static void LoadHeaderStyle(string style)
    {
        switch (style)
        {
            case "csv":
            case "ssv":
            case "pipes":
                return;
            case "tsv":
                throw new NotSupportedException();
            default:
                throw new SpecificationReaderException("Unrecognized header style: " + style);
        }
    }

    private SpecHeader LoadHeader(JsonNode node, ParsingContext context)
    {
        var header = new SpecHeader();
        node.CheckMapNode("header", context).ParseMap(header, headerFields, context);
        return header;
    }

    private SpecResponse LoadResponse(JsonNode node, ParsingContext context)
    {
        var jsonObject = node.CheckMapNode("response", context);
        var reference = jsonObject.GetReferencePointer();
        if (reference != null)
            return new SpecResponse { Reference = GetReference(reference) };

        var response = new SpecResponse();
        jsonObject.ParseMap(response, responseFields, context);
        return response;
    }

    private static void LoadExamples(SpecResponse response, JsonNode node, ParsingContext context)
    {
        foreach (var example in node.CheckMapNode("examples", context))
        {
            response.Content ??= new Dictionary<string, SpecMediaType?>(StringComparer.Ordinal);
            if (!response.Content.TryGetValue(example.Key, out var mediaType) || mediaType == null)
            {
                response.Content.Add(example.Key, new SpecMediaType
                {
                    Schema = context.GetFromTempStorage<SpecSchema>(ResponseSchemaKey, response),
                });
            }
        }
    }

    private static object LoadExamplesExtension(JsonNode node, ParsingContext context)
    {
        var examples = new Dictionary<string, SpecExample>(StringComparer.Ordinal);
        foreach (var example in node.CheckMapNode("x-examples", context))
        {
            foreach (var field in example.Value.CheckMapNode(example.Key, context))
            {
                switch (field.Key.ToLowerInvariant())
                {
                    case "summary":
                    case "description":
                    case "externalValue":
                        field.Value.GetScalarValue();
                        break;
                }
            }

            examples.Add(example.Key, new SpecExample());
        }

        return examples;
    }

    private static void ProcessResponsesMediaTypes(IEnumerable<SpecResponse?>? responses, ParsingContext context)
    {
        if (responses == null)
            return;

        foreach (var response in responses)
        {
            if (response is { Reference: null })
                ProcessProduces(response, context);
        }
    }

    private static void ProcessProduces(SpecResponse response, ParsingContext context)
    {
        if (response.Content == null)
            response.Content = new Dictionary<string, SpecMediaType?>(StringComparer.Ordinal);
        else if (context.GetFromTempStorage<object>(ResponseProducesSetKey, response) is true)
            return;

        var produces = context.GetFromTempStorage<List<string?>>(OperationProducesKey)
                       ?? context.GetFromTempStorage<List<string?>>(GlobalProducesKey)
                       ?? ["application/octet-stream"];
        var schema = context.GetFromTempStorage<SpecSchema>(ResponseSchemaKey, response);
        foreach (var contentType in produces)
        {
            if (response.Content.TryGetValue(contentType!, out var mediaType) && mediaType != null)
            {
                if (schema != null)
                    mediaType.Schema = schema;
            }
            else
            {
                response.Content.Add(contentType!, new SpecMediaType { Schema = schema });
            }
        }

        context.SetTempStorage(ResponseSchemaKey, null, response);
        context.SetTempStorage(ExamplesKey, null, response);
        context.SetTempStorage(ResponseProducesSetKey, true, response);
    }

    private SpecSchema LoadSchema(JsonNode node, ParsingContext context)
    {
        var jsonObject = node.CheckMapNode("schema", context);
        var reference = jsonObject.GetReferencePointer();
        if (reference != null)
            return new SpecSchema { Reference = GetReference(reference) };

        var schema = new SpecSchema();
        jsonObject.ParseMap(schema, schemaFields, context);
        return schema;
    }

    private void LoadXml(JsonNode node, ParsingContext context) =>
        node.CheckMapNode("xml", context).ParseMap(new object(), xmlFields, context);

    private SpecSecurityScheme LoadSecurityScheme(JsonNode node, ParsingContext context)
    {
        var securityScheme = new SpecSecurityScheme();
        var flow = new SpecOAuthFlow();
        context.SetTempStorage(FlowKey, flow, securityScheme);
        node.CheckMapNode("securityScheme", context).ParseMap(securityScheme, securitySchemeFields, context);
        securityScheme.Flows = context.GetFromTempStorage<string>(FlowValueKey, securityScheme) switch
        {
            "implicit" => new SpecOAuthFlows { Implicit = flow },
            "password" => new SpecOAuthFlows { Password = flow },
            "application" => new SpecOAuthFlows { ClientCredentials = flow },
            "accessCode" => new SpecOAuthFlows { AuthorizationCode = flow },
            _ => null,
        };
        return securityScheme;
    }

    private static SpecSecurityRequirement LoadSecurityRequirement(JsonNode node, ParsingContext context)
    {
        var requirement = new SpecSecurityRequirement();
        foreach (var scheme in node.CheckMapNode("security", context))
        {
            requirement.Add(SpecReferences.Create(scheme.Key, null));
            scheme.Value.CreateSimpleList("String", item => item.GetScalarValue(), context);
        }

        return requirement;
    }

    private SpecTag LoadTag(JsonNode node, ParsingContext context)
    {
        var tag = new SpecTag();
        node.CheckMapNode("tag", context).ParseMap(tag, tagFields, context);
        return tag;
    }

    private SpecExternalDocs LoadExternalDocs(JsonNode node, ParsingContext context)
    {
        var externalDocs = new SpecExternalDocs();
        node.CheckMapNode("externalDocs", context).ParseMap(externalDocs, externalDocsFields, context);
        return externalDocs;
    }

    /// <summary>
    /// Microsoft.OpenApi reads the file of a Swagger 2.0 reference from the first two path segments.
    /// </summary>
    private static SpecReference GetReference(string pointer)
    {
        var segments = pointer.Split('/');
        var id = segments[segments.Length - 1];
        var externalResource = !segments[0].StartsWith("#", StringComparison.OrdinalIgnoreCase)
            ? segments[0] + "/" + segments[1].TrimEnd('#')
            : null;
        return SpecReferences.Create(id, externalResource);
    }

    /// <summary>
    /// Adds the servers Microsoft.OpenApi derives from host, basePath and schemes, reporting an invalid host.
    /// </summary>
    private static void MakeServers(List<SpecServer> servers, ParsingContext context)
    {
        var host = context.GetFromTempStorage<string>("host");
        var basePath = context.GetFromTempStorage<string>("basePath");
        var schemes = context.GetFromTempStorage<List<string?>>("schemes");
        var baseUrl = context.BaseUrl;

        if (string.IsNullOrEmpty(basePath) && !string.IsNullOrEmpty(host))
            basePath = "/";

        if (string.IsNullOrEmpty(host) && string.IsNullOrEmpty(basePath) && (schemes == null || schemes.Count == 0) && baseUrl == null)
            return;

        if (!string.IsNullOrEmpty(host) && !IsHostValid(host!))
        {
            context.Diagnostics.Errors.Add(new ValidationIssue(context.GetLocation(), "Invalid host"));
            return;
        }

        if (baseUrl != null)
        {
            host ??= baseUrl.GetComponents(UriComponents.Host | UriComponents.Port, UriFormat.SafeUnescaped);
            basePath ??= baseUrl.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped);
            schemes ??= [baseUrl.GetComponents(UriComponents.Scheme, UriFormat.SafeUnescaped)];
        }
        else if (string.IsNullOrEmpty(host) && string.IsNullOrEmpty(basePath))
        {
            return;
        }

        if (schemes != null && schemes.Count > 0)
        {
            foreach (var scheme in schemes)
            {
                servers.Add(new SpecServer { Url = BuildUrl(scheme, host, basePath) });
            }
        }
        else
        {
            servers.Add(new SpecServer { Url = BuildUrl(null, host, basePath) });
        }
    }

    private static string BuildUrl(string? scheme, string? host, string? basePath)
    {
        if (string.IsNullOrEmpty(scheme) && !string.IsNullOrEmpty(host))
            host = "//" + host;

        int? port = null;
        if (host != null && host.Contains(':'))
        {
            var parts = host.Split(':');
            host = parts[0];
            port = int.Parse(parts[parts.Length - 1], CultureInfo.InvariantCulture);
        }

        var builder = new UriBuilder { Scheme = scheme, Host = host, Path = basePath };
        if (port.HasValue)
            builder.Port = port.Value;

        return builder.ToString();
    }

    private static bool IsHostValid(string host) =>
        !host.Contains(Uri.SchemeDelimiter)
        && Uri.CheckHostName(host.Split(':').First()) != UriHostNameType.Unknown;

    /// <summary>
    /// Replaces a reference to a body parameter, which became a request body component, with a reference to
    /// that request body. Microsoft.OpenApi only resolves references after this, so none of them resolve here.
    /// </summary>
    private static void FixRequestBodyReferences(SpecDocument document)
    {
        var requestBodies = document.Components?.RequestBodies;
        if (requestBodies == null || requestBodies.Count == 0 || document.Paths == null)
            return;

        foreach (var operation in document.Paths.Values
                     .SelectMany(pathItem => pathItem?.Operations?.Values ?? Enumerable.Empty<SpecOperation>()))
        {
            var bodyReference = operation.Parameters?.FirstOrDefault(parameter =>
                parameter.Reference != null && requestBodies.ContainsKey(parameter.Reference.Id));
            if (bodyReference == null)
                continue;

            operation.Parameters!.Remove(bodyReference);
            operation.RequestBody = new SpecRequestBody { Reference = SpecReferences.Create(bodyReference.Reference!.Id, null) };
        }
    }

    private static Uri? ReadUri(JsonNode node)
    {
        var value = node.GetScalarValue();
        return value != null ? new Uri(value, UriKind.RelativeOrAbsolute) : null;
    }

    private static void ReadBool(JsonNode node)
    {
        var value = node.GetScalarValue();
        if (value != null)
            ScalarChecks.CheckBoolean(value);
    }

    private static void ReadInt(JsonNode node)
    {
        var value = node.GetScalarValue();
        if (value != null)
            ScalarChecks.CheckInt32(value, CultureInfo.InvariantCulture);
    }
}
