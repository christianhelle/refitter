#nullable enable

using System.Globalization;
using System.Text.Json.Nodes;
using Refitter.Core.Validation.Model;
using static Refitter.Core.Validation.Reading.CommonFields;

namespace Refitter.Core.Validation.Reading;

/// <summary>
/// Reads OpenAPI 3.0, 3.1 and 3.2 documents, reporting what the Microsoft.OpenApi (MIT license) deserializers of
/// those versions report.
/// </summary>
internal sealed class OpenApiV3Reader
{
    private static readonly IReadOnlyDictionary<string, SpecParameterLocation> ParameterLocations =
        new Dictionary<string, SpecParameterLocation>(StringComparer.OrdinalIgnoreCase)
        {
            [OpenApiNames.Query] = SpecParameterLocation.Query,
            ["header"] = SpecParameterLocation.Header,
            ["path"] = SpecParameterLocation.Path,
            ["cookie"] = SpecParameterLocation.Cookie,
            ["querystring"] = SpecParameterLocation.QueryString,
        };

    private static readonly IReadOnlyDictionary<string, int> ParameterStyles =
        new[] { "matrix", "label", "form", "simple", "spaceDelimited", "pipeDelimited", "deepObject", "cookie" }
            .Select((name, index) => (name, index))
            .ToDictionary(style => style.name, style => style.index, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, SpecSecuritySchemeType> SecuritySchemeTypes =
        new Dictionary<string, SpecSecuritySchemeType>(StringComparer.OrdinalIgnoreCase)
        {
            ["apiKey"] = SpecSecuritySchemeType.ApiKey,
            ["http"] = SpecSecuritySchemeType.Http,
            ["oauth2"] = SpecSecuritySchemeType.OAuth2,
            ["openIdConnect"] = SpecSecuritySchemeType.OpenIdConnect,
            ["mutualTLS"] = SpecSecuritySchemeType.MutualTls,
        };

    private static readonly IReadOnlyDictionary<string, int> XmlNodeTypes =
        new[] { "element", "attribute", "text", "cdata", "none" }
            .Select((name, index) => (name, index))
            .ToDictionary(nodeType => nodeType.name, nodeType => nodeType.index, StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> StandardHttpMethods = new(StringComparer.Ordinal)
    {
        "get", "put", "post", "delete", "options", "head", "patch", "trace", OpenApiNames.Query,
    };

    private readonly OpenApiSpecificationVersion version;
    private readonly FieldMap<SpecDocument> documentFields;
    private readonly FieldMap<SpecInfo> infoFields;
    private readonly FieldMap<SpecLicense> licenseFields;
    private readonly FieldMap<SpecServer> serverFields;
    private readonly FieldMap<SpecServerVariable> serverVariableFields;
    private readonly FieldMap<SpecComponents> componentsFields;
    private readonly FieldMap<SpecPaths> pathsFields;
    private readonly FieldMap<SpecPathItem> pathItemFields;
    private readonly FieldMap<SpecOperation> operationFields;
    private readonly FieldMap<SpecParameter> parameterFields;
    private readonly FieldMap<SpecRequestBody> requestBodyFields;
    private readonly FieldMap<SpecMediaType> mediaTypeFields;
    private readonly FieldMap<SpecEncoding> encodingFields;
    private readonly FieldMap<SpecResponses> responsesFields;
    private readonly FieldMap<SpecResponse> responseFields;
    private readonly FieldMap<SpecHeader> headerFields;
    private readonly FieldMap<SpecLink> linkFields;
    private readonly FieldMap<SpecCallback> callbackFields;
    private readonly FieldMap<SpecExample> exampleFields;
    private readonly FieldMap<SpecSchema> schemaFields;
    private readonly FieldMap<SpecDiscriminator> discriminatorFields;
    private readonly FieldMap<object> xmlFields;
    private readonly FieldMap<SpecSecurityScheme> securitySchemeFields;
    private readonly FieldMap<SpecOAuthFlows> oAuthFlowsFields;
    private readonly FieldMap<SpecOAuthFlow> oAuthFlowFields;
    private readonly FieldMap<SpecTag> tagFields;

    public OpenApiV3Reader(OpenApiSpecificationVersion version)
    {
        this.version = version;
        var is31 = version >= OpenApiSpecificationVersion.OpenApi3_1;
        var is32 = version >= OpenApiSpecificationVersion.OpenApi3_2;

        documentFields = CreateDocumentFields(is31, is32);
        infoFields = CreateInfoFields(is31);
        licenseFields = CreateLicenseFields(is31);
        serverFields = CreateServerFields(is32);
        serverVariableFields = CreateServerVariableFields();
        componentsFields = CreateComponentsFields(is31, is32);
        pathsFields = CreatePathsFields();
        pathItemFields = CreatePathItemFields(is32);
        operationFields = CreateOperationFields();
        parameterFields = CreateParameterFields();
        requestBodyFields = CreateRequestBodyFields();
        mediaTypeFields = CreateMediaTypeFields(is32);
        encodingFields = CreateEncodingFields(is32);
        responsesFields = CreateResponsesFields();
        responseFields = CreateResponseFields(is32);
        headerFields = CreateHeaderFields();
        linkFields = CreateLinkFields();
        callbackFields = CreateCallbackFields();
        exampleFields = CreateExampleFields(is32);
        schemaFields = CreateSchemaFields(is31);
        discriminatorFields = CreateDiscriminatorFields(is31, is32);
        xmlFields = CreateXmlFields(is32);
        securitySchemeFields = CreateSecuritySchemeFields(is32);
        oAuthFlowsFields = CreateOAuthFlowsFields(is32);
        oAuthFlowFields = CreateOAuthFlowFields(is32);
        tagFields = CreateTagFields(is32);
    }

    private FieldMap<SpecDocument> CreateDocumentFields(bool is31, bool is32)
    {
        var fields = new FieldMap<SpecDocument>()
            .Field("openapi", (_, _, _) => { })
            .Field("info", (o, n, c) => o.Info = LoadInfo(n, c))
            .Field("servers", (o, n, c) => o.Servers = n.CreateList("OpenApiServer", LoadServer, c))
            .Field("paths", (o, n, c) => o.Paths = LoadPaths(n, c))
            .Field("components", (o, n, c) => o.Components = LoadComponents(n, c))
            .Field("tags", (o, n, c) =>
            {
                var tags = DistinctTags(n.CreateList("OpenApiTag", LoadTag, c));
                if (tags.Count > 0)
                    o.Tags = tags;
            })
            .Field(OpenApiNames.ExternalDocs, (o, n, c) => o.ExternalDocs = LoadExternalDocs(n, c))
            .Field("security", (o, n, c) => o.Security = n.CreateList("OpenApiSecurityRequirement", LoadSecurityRequirement, c));
        if (is31)
        {
            fields
                .Field("jsonSchemaDialect", (_, n, _) => n.GetScalarValue())
                .Field("webhooks", (o, n, c) => o.Webhooks = n.CreateMap("IOpenApiPathItem", LoadPathItem, c));
        }

        if (is32)
        {
            fields.Field("$self", (_, n, _) => n.GetScalarValue()).Extensions();
        }
        else
        {
            fields.Pattern(FieldMap<SpecDocument>.IsExtension, (_, p, n, _) =>
            {
                if (!p.Equals("x-oai-$self", StringComparison.OrdinalIgnoreCase))
                    return;

                var self = n.GetScalarValue();
                if (self != null && !is31)
                    _ = new Uri(self, UriKind.Absolute);
            });
        }

        return fields;
    }

    private FieldMap<SpecInfo> CreateInfoFields(bool is31)
    {
        var fields = new FieldMap<SpecInfo>()
            .Field("title", (o, n, _) => o.Title = n.GetScalarValue())
            .Field("version", (o, n, _) => o.Version = n.GetScalarValue())
            .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
            .Field("termsOfService", (_, n, _) => ReadUri(n))
            .Field("contact", (o, n, c) => o.Contact = LoadContact(n, c))
            .Field("license", (o, n, c) => o.License = LoadLicense(n, c))
            .Extensions();
        if (is31)
            fields.Field(OpenApiNames.Summary, (_, n, _) => n.GetScalarValue());

        return fields;
    }

    private static FieldMap<SpecLicense> CreateLicenseFields(bool is31)
    {
        var fields = new FieldMap<SpecLicense>()
            .Field("name", (o, n, _) => o.Name = n.GetScalarValue())
            .Field("url", (_, n, _) => ReadUri(n))
            .Extensions();
        if (is31)
            fields.Field("identifier", (_, n, _) => n.GetScalarValue());

        return fields;
    }

    private FieldMap<SpecServer> CreateServerFields(bool is32)
    {
        var fields = new FieldMap<SpecServer>()
            .Field("url", (o, n, _) => o.Url = n.GetScalarValue())
            .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
            .Field("variables", (o, n, c) => o.Variables = n.CreateMap("OpenApiServerVariable", LoadServerVariable, c));
        if (is32)
            fields.Field("name", (_, n, _) => n.GetScalarValue()).Extensions();
        else
            fields.Pattern(FieldMap<SpecServer>.IsExtension, (_, p, n, _) => ReadIfNamed(p, "x-oai-name", n));

        return fields;
    }

    private static FieldMap<SpecServerVariable> CreateServerVariableFields()
    {
        var fields = new FieldMap<SpecServerVariable>()
            .Field("enum", (_, n, c) => n.CreateSimpleList(OpenApiNames.StringType, item => item.GetScalarValue(), c))
            .Field("default", (o, n, _) => o.Default = n.GetScalarValue())
            .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
            .Extensions();

        return fields;
    }

    private FieldMap<SpecComponents> CreateComponentsFields(bool is31, bool is32)
    {
        var fields = new FieldMap<SpecComponents>()
            .Field("schemas", (o, n, c) => o.Schemas = n.CreateMap(OpenApiNames.SchemaType, LoadSchema, c))
            .Field("responses", (o, n, c) => o.Responses = n.CreateMap("IOpenApiResponse", LoadResponse, c))
            .Field(OpenApiNames.Parameters, (o, n, c) => o.Parameters = n.CreateMap("IOpenApiParameter", LoadParameter, c))
            .Field(OpenApiNames.Examples, (o, n, c) => o.Examples = n.CreateMap(OpenApiNames.ExampleType, LoadExample, c))
            .Field("requestBodies", (o, n, c) => o.RequestBodies = n.CreateMap("IOpenApiRequestBody", LoadRequestBody, c))
            .Field("headers", (o, n, c) => o.Headers = n.CreateMap("IOpenApiHeader", LoadHeader, c))
            .Field("securitySchemes", (o, n, c) => o.SecuritySchemes = n.CreateMap("IOpenApiSecurityScheme", LoadSecurityScheme, c))
            .Field("links", (o, n, c) => o.Links = n.CreateMap("IOpenApiLink", LoadLink, c))
            .Field("callbacks", (o, n, c) => o.Callbacks = n.CreateMap("IOpenApiCallback", LoadCallback, c))
            .Extensions();
        if (is31)
            fields.Field("pathItems", (o, n, c) => o.PathItems = n.CreateMap("IOpenApiPathItem", LoadPathItem, c));
        if (is32)
            fields.Field("mediaTypes", (o, n, c) => o.MediaTypes = n.CreateMap(OpenApiNames.MediaTypeType, LoadMediaType, c));

        return fields;
    }

    private FieldMap<SpecPaths> CreatePathsFields()
    {
        var fields = new FieldMap<SpecPaths>()
            .Pattern(s => s.StartsWith("/", StringComparison.OrdinalIgnoreCase), (o, k, n, c) => o.Add(k, LoadPathItem(n, c)))
            .Extensions();

        return fields;
    }

    private FieldMap<SpecPathItem> CreatePathItemFields(bool is32)
    {
        var fields = new FieldMap<SpecPathItem>()
            .Field(OpenApiNames.Summary, (_, n, _) => n.GetScalarValue())
            .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
            .Field("get", (o, n, c) => o.AddOperation("get", LoadOperation(n, c)))
            .Field("put", (o, n, c) => o.AddOperation("put", LoadOperation(n, c)))
            .Field("post", (o, n, c) => o.AddOperation("post", LoadOperation(n, c)))
            .Field("delete", (o, n, c) => o.AddOperation("delete", LoadOperation(n, c)))
            .Field("options", (o, n, c) => o.AddOperation("options", LoadOperation(n, c)))
            .Field("head", (o, n, c) => o.AddOperation("head", LoadOperation(n, c)))
            .Field("patch", (o, n, c) => o.AddOperation("patch", LoadOperation(n, c)))
            .Field("trace", (o, n, c) => o.AddOperation("trace", LoadOperation(n, c)))
            .Field("servers", (_, n, c) => n.CreateList("OpenApiServer", LoadServer, c))
            .Field(OpenApiNames.Parameters, (o, n, c) => o.Parameters = n.CreateList("IOpenApiParameter", LoadParameter, c))
            .Extensions();
        if (is32)
        {
            fields
                .Field(OpenApiNames.Query, (o, n, c) => o.AddOperation(OpenApiNames.Query, LoadOperation(n, c)))
                .Field("additionalOperations", LoadAdditionalOperations);
        }

        return fields;
    }

    private FieldMap<SpecOperation> CreateOperationFields()
    {
        var fields = new FieldMap<SpecOperation>()
            .OperationFields()
            .Field(OpenApiNames.Parameters, (o, n, c) => o.Parameters = n.CreateList("IOpenApiParameter", LoadParameter, c))
            .Field("requestBody", (o, n, c) => o.RequestBody = LoadRequestBody(n, c))
            .Field("responses", (o, n, c) => o.Responses = LoadResponses(n, c))
            .Field("callbacks", (o, n, c) => o.Callbacks = n.CreateMap("IOpenApiCallback", LoadCallback, c))
            .Field("servers", (_, n, c) => n.CreateList("OpenApiServer", LoadServer, c))
            .Extensions();

        return fields;
    }

    private FieldMap<SpecParameter> CreateParameterFields()
    {
        var fields = new FieldMap<SpecParameter>()
            .Field("name", (o, n, _) => o.Name = n.GetScalarValue())
            .Field("in", (o, n, c) =>
            {
                if (n.GetScalarValue().TryGetEnum(ParameterLocations, c, out var location))
                    o.In = location;
            })
            .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
            .Field(OpenApiNames.Required, (o, n, _) =>
            {
                var required = n.GetScalarValue();
                if (required != null)
                    o.Required = bool.Parse(required);
            })
            .Field(OpenApiNames.Deprecated, (_, n, _) => ReadBool(n))
            .Field("allowEmptyValue", (_, n, _) => ReadBool(n))
            .Field("allowReserved", (_, n, _) => ReadBool(n))
            .Field("style", (_, n, c) => n.GetScalarValue().TryGetEnum(ParameterStyles, c, out int _))
            .Field("explode", (_, n, _) => ReadBool(n))
            .Field(OpenApiNames.Schema, (o, n, c) => o.Schema = LoadSchema(n, c))
            .Field(OpenApiNames.Content, (o, n, c) => o.Content = n.CreateMap(OpenApiNames.MediaTypeType, LoadMediaType, c))
            .Field(OpenApiNames.Examples, (o, n, c) => o.Examples = n.CreateMap(OpenApiNames.ExampleType, LoadExample, c))
            .Field(OpenApiNames.Example, (_, _, _) => { })
            .Extensions();

        return fields;
    }

    private FieldMap<SpecRequestBody> CreateRequestBodyFields()
    {
        var fields = new FieldMap<SpecRequestBody>()
            .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
            .Field(OpenApiNames.Content, (o, n, c) => o.Content = n.CreateMap(OpenApiNames.MediaTypeType, LoadMediaType, c))
            .Field(OpenApiNames.Required, (_, n, _) => ReadBool(n))
            .Extensions();

        return fields;
    }

    private FieldMap<SpecMediaType> CreateMediaTypeFields(bool is32)
    {
        var fields = new FieldMap<SpecMediaType>()
            .Field(OpenApiNames.Schema, (o, n, c) => o.Schema = LoadSchema(n, c))
            .Field(OpenApiNames.Examples, (o, n, c) => o.Examples = n.CreateMap(OpenApiNames.ExampleType, LoadExample, c))
            .Field(OpenApiNames.Example, (_, _, _) => { })
            .Field("encoding", (o, n, c) => o.Encoding = n.CreateMap(OpenApiNames.EncodingType, LoadEncoding, c));
        if (is32)
        {
            fields
                .Field("itemSchema", (_, n, c) => LoadSchema(n, c))
                .Field("itemEncoding", (_, n, c) => LoadEncoding(n, c))
                .Field("prefixEncoding", (_, n, c) => n.CreateList(OpenApiNames.EncodingType, LoadEncoding, c))
                .Extensions();
        }
        else
        {
            fields
                .Field("x-oai-itemEncoding", (_, n, c) => LoadEncoding(n, c))
                .Field("x-oai-prefixEncoding", (_, n, c) => n.CreateList(OpenApiNames.EncodingType, LoadEncoding, c))
                .Pattern(FieldMap<SpecMediaType>.IsExtension, (_, p, n, c) =>
                {
                    if (p.Equals("x-oai-itemSchema", StringComparison.OrdinalIgnoreCase))
                        LoadSchema(n, c);
                });
        }

        return fields;
    }

    private FieldMap<SpecEncoding> CreateEncodingFields(bool is32)
    {
        var fields = new FieldMap<SpecEncoding>()
            .Field("contentType", (_, n, _) => n.GetScalarValue())
            .Field("headers", (o, n, c) => o.Headers = n.CreateMap("IOpenApiHeader", LoadHeader, c))
            .Field("style", (_, n, c) => n.GetScalarValue().TryGetEnum(ParameterStyles, c, out int _))
            .Field("explode", (_, n, _) => ReadBool(n))
            .Field("allowReserved", (_, n, _) => ReadBool(n))
            .Extensions();
        if (is32)
        {
            fields
                .Field("encoding", (_, n, c) => n.CreateMap(OpenApiNames.EncodingType, LoadEncoding, c))
                .Field("itemEncoding", (_, n, c) => LoadEncoding(n, c))
                .Field("prefixEncoding", (_, n, c) => n.CreateList(OpenApiNames.EncodingType, LoadEncoding, c));
        }

        return fields;
    }

    private FieldMap<SpecResponses> CreateResponsesFields()
    {
        var fields = new FieldMap<SpecResponses>()
            .Pattern(s => !FieldMap<SpecResponses>.IsExtension(s), (o, p, n, c) => o.Add(p, LoadResponse(n, c)))
            .Extensions();

        return fields;
    }

    private FieldMap<SpecResponse> CreateResponseFields(bool is32)
    {
        var fields = new FieldMap<SpecResponse>()
            .Field(OpenApiNames.Description, (o, n, _) => o.Description = n.GetScalarValue())
            .Field("headers", (o, n, c) => o.Headers = n.CreateMap("IOpenApiHeader", LoadHeader, c))
            .Field(OpenApiNames.Content, (o, n, c) => o.Content = n.CreateMap(OpenApiNames.MediaTypeType, LoadMediaType, c))
            .Field("links", (o, n, c) => o.Links = n.CreateMap("IOpenApiLink", LoadLink, c));
        if (is32)
            fields.Field(OpenApiNames.Summary, (_, n, _) => n.GetScalarValue()).Extensions();
        else
            fields.Pattern(FieldMap<SpecResponse>.IsExtension, (_, p, n, _) => ReadIfNamed(p, "x-oai-summary", n));

        return fields;
    }

    private FieldMap<SpecHeader> CreateHeaderFields()
    {
        var fields = new FieldMap<SpecHeader>()
            .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
            .Field(OpenApiNames.Required, (_, n, _) => ReadBool(n))
            .Field(OpenApiNames.Deprecated, (_, n, _) => ReadBool(n))
            .Field("allowEmptyValue", (_, n, _) => ReadBool(n))
            .Field("allowReserved", (_, n, _) => ReadBool(n))
            .Field("style", (_, n, c) => n.GetScalarValue().TryGetEnum(ParameterStyles, c, out int _))
            .Field("explode", (_, n, _) => ReadBool(n))
            .Field(OpenApiNames.Schema, (o, n, c) => o.Schema = LoadSchema(n, c))
            .Field(OpenApiNames.Content, (o, n, c) => o.Content = n.CreateMap(OpenApiNames.MediaTypeType, LoadMediaType, c))
            .Field(OpenApiNames.Examples, (o, n, c) => o.Examples = n.CreateMap(OpenApiNames.ExampleType, LoadExample, c))
            .Field(OpenApiNames.Example, (o, n, _) => o.Example = n)
            .Extensions();

        return fields;
    }

    private FieldMap<SpecLink> CreateLinkFields()
    {
        var fields = new FieldMap<SpecLink>()
            .Field("operationRef", (_, n, _) => n.GetScalarValue())
            .Field("operationId", (_, n, _) => n.GetScalarValue())
            .Field(OpenApiNames.Parameters, (_, n, c) => n.CreateSimpleMap(
                "RuntimeExpressionAnyWrapper",
                item =>
                {
                    CheckRuntimeExpressionAnyWrapper(item);
                    return item;
                },
                c))
            .Field("requestBody", (_, n, _) => CheckRuntimeExpressionAnyWrapper(n))
            .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
            .Field("server", (o, n, c) => o.Server = LoadServer(n, c))
            .Extensions();

        return fields;
    }

    private FieldMap<SpecCallback> CreateCallbackFields()
    {
        var fields = new FieldMap<SpecCallback>()
            .Pattern(s => !FieldMap<SpecCallback>.IsExtension(s), (o, p, n, c) =>
            {
                RuntimeExpressions.Validate(p);
                (o.PathItems ??= new Dictionary<string, SpecPathItem?>(StringComparer.Ordinal))[p] = LoadPathItem(n, c);
            })
            .Extensions();

        return fields;
    }

    private static FieldMap<SpecExample> CreateExampleFields(bool is32)
    {
        var fields = new FieldMap<SpecExample>()
            .Field(OpenApiNames.Summary, (_, n, _) => n.GetScalarValue())
            .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
            .Field("value", (_, _, _) => { })
            .Field("externalValue", (_, n, _) => n.GetScalarValue());
        if (is32)
        {
            fields
                .Field("dataValue", (_, _, _) => { })
                .Field("serializedValue", (_, n, _) => n.GetScalarValue())
                .Extensions();
        }
        else
        {
            fields
                .Pattern(s => s.Equals("x-oai-dataValue", StringComparison.OrdinalIgnoreCase), (_, _, _, _) => { })
                .Pattern(s => s.Equals("x-oai-serializedValue", StringComparison.OrdinalIgnoreCase), (_, _, n, _) => n.GetScalarValue())
                .Extensions();
        }

        return fields;
    }

    private FieldMap<SpecDiscriminator> CreateDiscriminatorFields(bool is31, bool is32)
    {
        var fields = new FieldMap<SpecDiscriminator>()
            .Field("propertyName", (o, n, _) => o.PropertyName = n.GetScalarValue())
            .Field("mapping", (o, n, c) => o.Mapping = n.CreateSimpleMap("OpenApiSchemaReference", LoadMapping, c));
        if (is32)
        {
            fields.Field("defaultMapping", (_, n, _) => LoadMapping(n)).Extensions();
        }
        else if (is31)
        {
            fields.Pattern(FieldMap<SpecDiscriminator>.IsExtension, (_, p, n, _) =>
            {
                if (p.Equals("x-oas-default-mapping", StringComparison.OrdinalIgnoreCase))
                    LoadMapping(n);
            });
        }

        return fields;
    }

    private static FieldMap<object> CreateXmlFields(bool is32)
    {
        var fields = new FieldMap<object>()
            .Field("name", (_, n, _) => n.GetScalarValue())
            .Field("namespace", (_, n, _) =>
            {
                var xmlNamespace = n.GetScalarValue();
                if (xmlNamespace != null)
                    _ = new Uri(xmlNamespace, UriKind.Absolute);
            })
            .Field("prefix", (_, n, _) => n.GetScalarValue())
            .Extensions();
        if (is32)
        {
            fields.Field("nodeType", (_, n, c) => n.GetScalarValue().TryGetEnum(XmlNodeTypes, c, out int _));
        }
        else
        {
            fields
                .Field("attribute", (_, n, _) => ReadBool(n))
                .Field("wrapped", (_, n, _) => ReadBool(n));
        }

        return fields;
    }

    private FieldMap<SpecSecurityScheme> CreateSecuritySchemeFields(bool is32)
    {
        var fields = new FieldMap<SpecSecurityScheme>()
            .Field("type", (o, n, c) =>
            {
                if (n.GetScalarValue().TryGetEnum(SecuritySchemeTypes, c, out var type))
                    o.Type = type;
            })
            .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
            .Field("name", (o, n, _) => o.Name = n.GetScalarValue())
            .Field("in", (o, n, c) =>
            {
                if (n.GetScalarValue().TryGetEnum(ParameterLocations, c, out var location))
                    o.In = location;
            })
            .Field("scheme", (_, n, _) => n.GetScalarValue())
            .Field("bearerFormat", (_, n, _) => n.GetScalarValue())
            .Field("openIdConnectUrl", (_, n, _) => ReadUri(n))
            .Field("flows", (o, n, c) => o.Flows = LoadOAuthFlows(n, c));
        if (is32)
        {
            fields
                .Field("oauth2MetadataUrl", (_, n, _) => ReadUri(n))
                .Field(OpenApiNames.Deprecated, (_, n, _) => ReadBool(n))
                .Extensions();
        }
        else
        {
            fields.Pattern(FieldMap<SpecSecurityScheme>.IsExtension, (_, p, n, _) =>
            {
                if (p.Equals("x-oai-deprecated", StringComparison.OrdinalIgnoreCase))
                    ReadBool(n);
            });
        }

        return fields;
    }

    private FieldMap<SpecOAuthFlows> CreateOAuthFlowsFields(bool is32)
    {
        var fields = new FieldMap<SpecOAuthFlows>()
            .Field("implicit", (o, n, c) => o.Implicit = LoadOAuthFlow(n, c))
            .Field("password", (o, n, c) => o.Password = LoadOAuthFlow(n, c))
            .Field("clientCredentials", (o, n, c) => o.ClientCredentials = LoadOAuthFlow(n, c))
            .Field("authorizationCode", (o, n, c) => o.AuthorizationCode = LoadOAuthFlow(n, c));
        if (is32)
        {
            fields.Field("deviceAuthorization", (o, n, c) => o.DeviceAuthorization = LoadOAuthFlow(n, c)).Extensions();
        }
        else
        {
            fields
                .Pattern(
                    s => s.Equals("x-oai-deviceAuthorization", StringComparison.OrdinalIgnoreCase),
                    (o, _, n, c) => o.DeviceAuthorization = LoadOAuthFlow(n, c))
                .Extensions();
        }

        return fields;
    }

    private static FieldMap<SpecOAuthFlow> CreateOAuthFlowFields(bool is32)
    {
        var fields = new FieldMap<SpecOAuthFlow>()
            .Field("authorizationUrl", (o, n, _) => o.AuthorizationUrl = ReadUri(n))
            .Field("tokenUrl", (o, n, _) => o.TokenUrl = ReadUri(n))
            .Field("refreshUrl", (_, n, _) => ReadUri(n))
            .Field("scopes", (o, n, c) => o.Scopes = n.CreateSimpleMap(OpenApiNames.StringType, item => item.GetScalarValue(), c)
                .Where(scope => scope.Value != null)
                .ToDictionary(scope => scope.Key, scope => scope.Value!, StringComparer.Ordinal));
        if (is32)
        {
            fields.Field("deviceAuthorizationUrl", (_, n, _) => ReadUri(n)).Extensions();
        }
        else
        {
            fields.Pattern(FieldMap<SpecOAuthFlow>.IsExtension, (_, p, n, _) =>
            {
                if (p.Equals("x-oai-deviceAuthorizationUrl", StringComparison.OrdinalIgnoreCase))
                    ReadUri(n);
            });
        }

        return fields;
    }

    private FieldMap<SpecTag> CreateTagFields(bool is32)
    {
        var fields = new FieldMap<SpecTag>()
            .Field("name", (o, n, _) => o.Name = n.GetScalarValue())
            .Field(OpenApiNames.Description, (_, n, _) => n.GetScalarValue())
            .Field(OpenApiNames.ExternalDocs, (o, n, c) => o.ExternalDocs = LoadExternalDocs(n, c));
        if (is32)
        {
            fields
                .Field(OpenApiNames.Summary, (_, n, _) => n.GetScalarValue())
                .Field("parent", (_, n, _) => n.GetScalarValue())
                .Field("kind", (_, n, _) => n.GetScalarValue())
                .Extensions();
        }
        else
        {
            fields.Pattern(FieldMap<SpecTag>.IsExtension, (_, p, n, _) =>
            {
                if (p.Equals("x-oas-summary", StringComparison.OrdinalIgnoreCase)
                    || p.Equals("x-oas-parent", StringComparison.OrdinalIgnoreCase)
                    || p.Equals("x-oas-kind", StringComparison.OrdinalIgnoreCase))
                {
                    n.GetScalarValue();
                }
            });
        }

        return fields;
    }

    public SpecDocument LoadDocument(JsonNode jsonNode, ParsingContext context)
    {
        var document = new SpecDocument();
        jsonNode.CheckMapNode("OpenAPI", context).ParseMap(document, documentFields, context);
        return document;
    }

    private FieldMap<SpecSchema> CreateSchemaFields(bool is31)
    {
        var fields = new FieldMap<SpecSchema>()
            .SchemaFields(LoadSchema, LoadXml)
            .Field("oneOf", (o, n, c) => o.OneOf = n.CreateList(OpenApiNames.SchemaType, LoadSchema, c))
            .Field("anyOf", (o, n, c) => o.AnyOf = n.CreateList(OpenApiNames.SchemaType, LoadSchema, c))
            .Field("not", (o, n, c) => o.Not = LoadSchema(n, c))
            .Field("discriminator", (o, n, c) => o.Discriminator = LoadDiscriminator(n, c))
            .Field("writeOnly", (_, n, _) => ReadBool(n))
            .Field(OpenApiNames.Deprecated, (_, n, _) => ReadBool(n))
            .Extensions();

        if (!is31)
        {
            return fields
                .Field("exclusiveMaximum", (_, n, _) => ScalarChecks.CheckBoolean(n.GetScalarValue()!))
                .Field("exclusiveMinimum", (_, n, _) => ScalarChecks.CheckBoolean(n.GetScalarValue()!))
                .Field("type", (_, n, _) => n.GetScalarValue().ToJsonSchemaType())
                .Field("nullable", (_, n, _) => bool.TryParse(n.GetScalarValue(), out _))
                .Field("x-jsonschema-patternProperties", (_, n, c) => n.CreateMap(OpenApiNames.SchemaType, LoadSchema, c))
                .Field("x-jsonschema-unevaluatedProperties", (_, n, c) => ReadBoolOrSchema(n, c))
                .Field("x-jsonschema-$anchor", (_, n, _) => n.GetScalarValue())
                .Field("x-jsonschema-contentEncoding", (_, n, _) => n.GetScalarValue())
                .Field("x-jsonschema-contentMediaType", (_, n, _) => n.GetScalarValue())
                .Field("x-jsonschema-contentSchema", (_, n, c) => LoadSchema(n, c))
                .Field("x-jsonschema-contains", (_, n, c) => LoadSchema(n, c))
                .Field("x-jsonschema-maxContains", (_, n, _) => ReadUnsignedInt(n))
                .Field("x-jsonschema-minContains", (_, n, _) => ReadUnsignedInt(n))
                .Field("x-jsonschema-propertyNames", (_, n, c) => LoadSchema(n, c))
                .Field("x-jsonschema-dependentSchemas", (_, n, c) => n.CreateMap(OpenApiNames.SchemaType, LoadSchema, c))
                .Field("x-jsonschema-if", (_, n, c) => LoadSchema(n, c))
                .Field("x-jsonschema-then", (_, n, c) => LoadSchema(n, c))
                .Field("x-jsonschema-else", (_, n, c) => LoadSchema(n, c));
        }

        return fields
            .Field("$schema", (_, n, _) => n.GetScalarValue())
            .Field("$id", (_, n, _) => n.GetScalarValue())
            .Field("$comment", (_, n, _) => n.GetScalarValue())
            .Field("$vocabulary", (_, n, c) => n.CreateSimpleMap("Nullable`1", ReadNullableBool, c))
            .Field("$dynamicRef", (_, n, _) => n.GetScalarValue())
            .Field("$dynamicAnchor", (_, n, _) => n.GetScalarValue())
            .Field("$defs", (_, n, c) => n.CreateMap(OpenApiNames.SchemaType, LoadSchema, c))
            .Field("$anchor", (_, n, _) => n.GetScalarValue())
            .Field("exclusiveMaximum", (_, n, _) => n.GetScalarValue())
            .Field("exclusiveMinimum", (_, n, _) => n.GetScalarValue())
            .Field("contains", (_, n, c) => LoadSchema(n, c))
            .Field("maxContains", (_, n, _) => ReadUnsignedInt(n))
            .Field("minContains", (_, n, _) => ReadUnsignedInt(n))
            .Field("unevaluatedProperties", (_, n, c) => ReadBoolOrSchema(n, c))
            .Field("contentEncoding", (_, n, _) => n.GetScalarValue())
            .Field("contentMediaType", (_, n, _) => n.GetScalarValue())
            .Field("contentSchema", (_, n, c) => LoadSchema(n, c))
            .Field("type", (_, n, c) =>
            {
                if (n is JsonValue)
                {
                    n.GetScalarValue().ToJsonSchemaType();
                    return;
                }

                foreach (var type in n.CreateSimpleList(OpenApiNames.StringType, item => item.GetScalarValue(), c).Where(type => type != null))
                {
                    type!.ToJsonSchemaType();
                }
            })
            .Field("const", (_, n, _) => n.GetScalarValue())
            .Field("patternProperties", (_, n, c) => n.CreateMap(OpenApiNames.SchemaType, LoadSchema, c))
            .Field("propertyNames", (_, n, c) => LoadSchema(n, c))
            .Field("nullable", (_, n, _) =>
            {
                var nullable = n.GetScalarValue();
                if (nullable != null)
                    ScalarChecks.CheckBoolean(nullable);
            })
            .Field(OpenApiNames.Examples, (_, n, c) => n.CreateListOfAny(c))
            .Field("dependentRequired", (_, n, c) => n.CreateArrayMap(OpenApiNames.StringType, item => item.GetScalarValue(), c))
            .Field("dependentSchemas", (_, n, c) => n.CreateMap(OpenApiNames.SchemaType, LoadSchema, c))
            .Field("if", (_, n, c) => LoadSchema(n, c))
            .Field("then", (_, n, c) => LoadSchema(n, c))
            .Field("else", (_, n, c) => LoadSchema(n, c));
    }

    private SpecInfo LoadInfo(JsonNode node, ParsingContext context)
    {
        var info = new SpecInfo();
        node.CheckMapNode("Info", context).ParseMap(info, infoFields, context);
        return info;
    }

    private SpecLicense LoadLicense(JsonNode node, ParsingContext context)
    {
        var license = new SpecLicense();
        node.CheckMapNode("License", context).ParseMap(license, licenseFields, context);
        return license;
    }

    private SpecServer LoadServer(JsonNode node, ParsingContext context)
    {
        var server = new SpecServer();
        node.CheckMapNode("server", context).ParseMap(server, serverFields, context);
        return server;
    }

    private SpecServerVariable LoadServerVariable(JsonNode node, ParsingContext context)
    {
        var serverVariable = new SpecServerVariable();
        node.CheckMapNode("serverVariable", context).ParseMap(serverVariable, serverVariableFields, context);
        return serverVariable;
    }

    private SpecComponents LoadComponents(JsonNode node, ParsingContext context)
    {
        var components = new SpecComponents();
        node.CheckMapNode("components", context).ParseMap(components, componentsFields, context);
        return components;
    }

    private SpecPaths LoadPaths(JsonNode node, ParsingContext context)
    {
        var paths = new SpecPaths();
        node.CheckMapNode("Paths", context).ParseMap(paths, pathsFields, context);
        return paths;
    }

    private SpecPathItem LoadPathItem(JsonNode node, ParsingContext context)
    {
        var jsonObject = node.CheckMapNode("PathItem", context);
        var reference = jsonObject.GetReferencePointer();
        if (reference != null)
            return new SpecPathItem { Reference = GetReference(reference) };

        var pathItem = new SpecPathItem();
        jsonObject.ParseMap(pathItem, pathItemFields, context);
        return pathItem;
    }

    private void LoadAdditionalOperations(SpecPathItem pathItem, JsonNode node, ParsingContext context)
    {
        foreach (var operation in node.CheckMapNode("additionalOperations", context)
                     .Where(operation => !StandardHttpMethods.Contains(operation.Key))
                     .ToList())
        {
            pathItem.AddOperation(operation.Key.ToLowerInvariant(), LoadOperation(operation.Value ?? JsonNullSentinel.JsonNull, context));
        }
    }

    private SpecOperation LoadOperation(JsonNode node, ParsingContext context)
    {
        var operation = new SpecOperation();
        node.CheckMapNode("Operation", context).ParseMap(operation, operationFields, context);
        return operation;
    }

    private SpecParameter LoadParameter(JsonNode node, ParsingContext context)
    {
        var jsonObject = node.CheckMapNode("parameter", context);
        var reference = jsonObject.GetReferencePointer();
        if (reference != null)
            return new SpecParameter { Reference = GetReference(reference) };

        var parameter = new SpecParameter();
        jsonObject.ParseMap(parameter, parameterFields, context);
        return parameter;
    }

    private SpecRequestBody LoadRequestBody(JsonNode node, ParsingContext context)
    {
        var jsonObject = node.CheckMapNode("requestBody", context);
        var reference = jsonObject.GetReferencePointer();
        if (reference != null)
            return new SpecRequestBody { Reference = GetReference(reference) };

        var requestBody = new SpecRequestBody();
        jsonObject.ParseMap(requestBody, requestBodyFields, context);
        return requestBody;
    }

    private SpecMediaType LoadMediaType(JsonNode node, ParsingContext context)
    {
        var jsonObject = node.CheckMapNode(OpenApiNames.Content, context);
        if (version >= OpenApiSpecificationVersion.OpenApi3_2)
        {
            var reference = jsonObject.GetReferencePointer();
            if (reference != null)
                return new SpecMediaType { Reference = GetReference(reference) };
        }

        var mediaType = new SpecMediaType();
        jsonObject.ParseMap(mediaType, mediaTypeFields, context);
        return mediaType;
    }

#pragma warning disable S3241 // The encodings are read into media types through a method group, which Sonar does not follow
    private SpecEncoding LoadEncoding(JsonNode node, ParsingContext context)
    {
        var encoding = new SpecEncoding();
        node.CheckMapNode("encoding", context).ParseMap(encoding, encodingFields, context);
        return encoding;
    }
#pragma warning restore S3241

    private SpecResponses LoadResponses(JsonNode node, ParsingContext context)
    {
        var responses = new SpecResponses();
        node.CheckMapNode("Responses", context).ParseMap(responses, responsesFields, context);
        return responses;
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

    private SpecHeader LoadHeader(JsonNode node, ParsingContext context)
    {
        var jsonObject = node.CheckMapNode("header", context);
        var reference = jsonObject.GetReferencePointer();
        if (reference != null)
            return new SpecHeader { Reference = GetReference(reference) };

        var header = new SpecHeader();
        jsonObject.ParseMap(header, headerFields, context);
        return header;
    }

    private SpecLink LoadLink(JsonNode node, ParsingContext context)
    {
        var jsonObject = node.CheckMapNode("link", context);
        var reference = jsonObject.GetReferencePointer();
        if (reference != null)
            return new SpecLink { Reference = GetReference(reference) };

        var link = new SpecLink();
        jsonObject.ParseMap(link, linkFields, context);
        return link;
    }

    private SpecCallback LoadCallback(JsonNode node, ParsingContext context)
    {
        var jsonObject = node.CheckMapNode("callback", context);
        var reference = jsonObject.GetReferencePointer();
        if (reference != null)
            return new SpecCallback { Reference = GetReference(reference) };

        var callback = new SpecCallback();
        jsonObject.ParseMap(callback, callbackFields, context);
        return callback;
    }

    private SpecExample LoadExample(JsonNode node, ParsingContext context)
    {
        var jsonObject = node.CheckMapNode(OpenApiNames.Example, context);
        var reference = jsonObject.GetReferencePointer();
        if (reference != null)
            return new SpecExample { Reference = GetReference(reference) };

        var example = new SpecExample();
        jsonObject.ParseMap(example, exampleFields, context);
        return example;
    }

    private SpecSchema LoadSchema(JsonNode node, ParsingContext context)
    {
        if (version >= OpenApiSpecificationVersion.OpenApi3_1
            && node is JsonValue value
            && value.TryGetValue<bool>(out var allowed))
        {
            return allowed ? new SpecSchema() : new SpecSchema { Not = new SpecSchema() };
        }

        var jsonObject = node.CheckMapNode(OpenApiNames.Schema, context);
        var reference = jsonObject.GetReferencePointer();
        if (version >= OpenApiSpecificationVersion.OpenApi3_1 && jsonObject.TryGetPropertyValue("$id", out var id))
            id?.GetScalarValue();

        if (reference != null)
        {
            var schemaReference = GetReference(reference);
            if (version >= OpenApiSpecificationVersion.OpenApi3_1)
                schemaReference = schemaReference with { JsonPointer = GetJsonPointerPath(reference, context.GetLocation()) };

            return new SpecSchema { Reference = schemaReference };
        }

        var schema = new SpecSchema();
        if (version >= OpenApiSpecificationVersion.OpenApi3_1)
            jsonObject.ParseMap(schema, schemaFields, context, (_, _, _) => { });
        else
            jsonObject.ParseMap(schema, schemaFields, context);

        return schema;
    }

    private SpecDiscriminator LoadDiscriminator(JsonNode node, ParsingContext context)
    {
        var discriminator = new SpecDiscriminator();
        node.CheckMapNode("discriminator", context).ParseMap(discriminator, discriminatorFields, context);
        return discriminator;
    }

#pragma warning disable S3241 // The mappings are read into discriminators through a method group, which Sonar does not follow
    private SpecSchema LoadMapping(JsonNode node) =>
        new() { Reference = GetReference(node.GetScalarValue()) };
#pragma warning restore S3241

    private void LoadXml(JsonNode node, ParsingContext context) =>
        node.CheckMapNode("xml", context).ParseMap(new object(), xmlFields, context);

    private SpecSecurityScheme LoadSecurityScheme(JsonNode node, ParsingContext context)
    {
        var jsonObject = node.CheckMapNode("securityScheme", context);
        var reference = jsonObject.GetReferencePointer();
        if (reference != null)
            return new SpecSecurityScheme { Reference = GetReference(reference) };

        var securityScheme = new SpecSecurityScheme();
        jsonObject.ParseMap(securityScheme, securitySchemeFields, context);
        return securityScheme;
    }

    private SpecOAuthFlows LoadOAuthFlows(JsonNode node, ParsingContext context)
    {
        var flows = new SpecOAuthFlows();
        node.CheckMapNode("OAuthFlows", context).ParseMap(flows, oAuthFlowsFields, context);
        return flows;
    }

    private SpecOAuthFlow LoadOAuthFlow(JsonNode node, ParsingContext context)
    {
        var flow = new SpecOAuthFlow();
        node.CheckMapNode("OAuthFlow", context).ParseMap(flow, oAuthFlowFields, context);
        return flow;
    }

    private SpecTag LoadTag(JsonNode node, ParsingContext context)
    {
        var tag = new SpecTag();
        node.CheckMapNode("tag", context).ParseMap(tag, tagFields, context);
        return tag;
    }

    private SpecReference GetReference(string pointer)
    {
        var segments = pointer.Split('/');
        var isExternal = !segments[0].StartsWith("#", StringComparison.OrdinalIgnoreCase);
        if (version == OpenApiSpecificationVersion.OpenApi3_0)
        {
            return SpecReferences.Create(
                segments[segments.Length - 1],
                isExternal ? pointer.Split('#')[0].TrimEnd('#') : null);
        }

        var hasFragment = pointer.Contains('#');
        return SpecReferences.Create(
            hasFragment ? segments[segments.Length - 1] : pointer,
            isExternal && hasFragment ? pointer.Split('#')[0].TrimEnd('#') : null);
    }

    /// <summary>
    /// The pointer Microsoft.OpenApi keeps for an OpenAPI 3.1 schema reference: a pointer relative to the
    /// document is resolved against where the reference is, and a component pointer is kept as is.
    /// </summary>
    private static string? GetJsonPointerPath(string pointer, string location)
    {
        if (pointer.StartsWith("#/", StringComparison.OrdinalIgnoreCase)
            && !pointer.ToLowerInvariant().Contains("/components/schemas"))
        {
            return ResolveRelativePointer(location, pointer);
        }

        return pointer.Contains('#') || pointer.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? pointer : null;
    }

    private static string ResolveRelativePointer(string location, string relativeReference)
    {
        var locationSegments = location.TrimStart('#').Split(['/'], StringSplitOptions.RemoveEmptyEntries).ToList();
        var referenceSegments = relativeReference.TrimStart('#').Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i <= locationSegments.Count - referenceSegments.Length; i++)
        {
            if (referenceSegments.SequenceEqual(locationSegments.Skip(i).Take(referenceSegments.Length), StringComparer.Ordinal))
            {
                var prefix = locationSegments.Take(i + referenceSegments.Length).ToArray();
                if (prefix.Length > 0)
                    return "#/" + string.Join("/", prefix);
            }
        }

        if (location.StartsWith("#/components/schemas/", StringComparison.OrdinalIgnoreCase))
            return "#/" + string.Join("/", locationSegments.Take(3).Concat(referenceSegments));

        return "#/" + string.Join("/", locationSegments.Take(locationSegments.Count - referenceSegments.Length).Concat(referenceSegments));
    }

    /// <summary>
    /// Keeps the first tag of each name, as Microsoft.OpenApi keeps the tags in a set compared by name.
    /// </summary>
    internal static List<SpecTag> DistinctTags(IEnumerable<SpecTag> tags)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var hasUnnamed = false;
        var distinct = new List<SpecTag>();
        foreach (var tag in tags)
        {
            var isFirst = tag.Name == null ? !hasUnnamed : names.Add(tag.Name);
            hasUnnamed |= tag.Name == null;
            if (isFirst)
                distinct.Add(tag);
        }

        return distinct;
    }

    /// <summary>
    /// Checks a link parameter or request body, which is a runtime expression when it starts with <c>$</c>.
    /// </summary>
    private static void CheckRuntimeExpressionAnyWrapper(JsonNode node)
    {
        var value = node.GetScalarValue();
        if (value.StartsWith("$", StringComparison.OrdinalIgnoreCase))
            RuntimeExpressions.Validate(value);
    }

    private void ReadBoolOrSchema(JsonNode node, ParsingContext context)
    {
        if (node is JsonValue)
            ReadBool(node);
        else
            LoadSchema(node, context);
    }

    private static void ReadIfNamed(string name, string expected, JsonNode node)
    {
        if (name.Equals(expected, StringComparison.OrdinalIgnoreCase))
            node.GetScalarValue();
    }

    private static bool? ReadNullableBool(JsonNode node) =>
        bool.Parse(node.GetScalarValue());

    private static void ReadUnsignedInt(JsonNode node)
    {
        var value = node.GetScalarValue();
        if (value != null)
            ScalarChecks.CheckUInt32(value, CultureInfo.InvariantCulture);
    }
}
