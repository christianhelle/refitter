namespace Refitter.Core;

/// <summary>
/// The C# view of an operation: its parameters with their C# names and types, and its responses.
/// Creating it resolves (and so names) the types of the parameters. The members are also the ones the client
/// templates have always used, see <see cref="ContractGenerator"/>. See THIRD-PARTY-NOTICES.md.
/// </summary>
internal sealed class OperationModel
{
    private static readonly HashSet<string> ReservedKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum",
        "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
        "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
        "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
    };

    private readonly ApiOperation operation;
    private readonly ContractGenerator generator;
    private readonly ContractGeneratorSettings settings;

    public OperationModel(ApiOperation operation, ContractGenerator generator)
    {
        this.operation = operation;
        this.generator = generator;
        settings = generator.Settings;

        var responses = operation
            .GetActualResponses((_, _) => true)
            .Select(response => new OperationResponseModel(
                this,
                operation,
                response.Key,
                response.Value,
                response.Value == operation.GetSuccessResponse().Value,
                generator))
            .ToList();
        responses.Sort(StatusCodeComparer.Instance);
        var defaultResponse = responses.Find(r => r.StatusCode == "default");
        if (defaultResponse != null)
            responses.Remove(defaultResponse);

        Responses = responses;
        DefaultResponse = defaultResponse;

        var parameters = GetActualParameters();
        Parameters = parameters
            .Select(parameter =>
            {
                var variableName = GetParameterVariableName(parameter, operation.Parameters);
                var variableIdentifier = settings.ParameterNameGenerator.Generate(parameter, operation.Parameters);
                return new OperationParameterModel(
                    parameter.Name,
                    variableName,
                    variableIdentifier,
                    ResolveParameterType(parameter),
                    parameter,
                    parameters,
                    generator);
            })
            .ToList();
    }

    public ApiOperation Operation => operation;

    public string? Path { get; set; }

    public string? HttpMethod { get; set; }

    public string? OperationName { get; set; }

    public string? ControllerName { get; set; }

    public string? ActualOperationName =>
        OperationName != null ? ConversionUtilities.ConvertToUpperCamelCase(OperationName, firstCharacterMustBeAlpha: true) : null;

    public string MethodAccessModifier => "public";

    public string HttpMethodUpper => ConversionUtilities.ConvertToUpperCamelCase(HttpMethod, firstCharacterMustBeAlpha: false);

    public bool IsGetOrDeleteOrHead => HttpMethod is "get" or "delete" or "head";

    public IList<OperationParameterModel> Parameters { get; }

    public List<OperationResponseModel> Responses { get; }

    public OperationResponseModel? DefaultResponse { get; }

    public bool HasDefaultResponse => DefaultResponse != null;

    public bool HasSuccessResponse => Responses.Any(r => r.IsSuccess);

    public OperationResponseModel? SuccessResponse => Responses.FirstOrDefault(r => r.IsSuccess);

    public OperationParameterModel? ContentParameter
    {
        get
        {
            OperationParameterModel? contentParameter = null;
            foreach (var parameter in Parameters)
            {
                if (parameter.Kind == ApiParameterKind.Body)
                {
                    if (contentParameter != null)
                        throw new InvalidOperationException("Multiple body parameters found in operation '" + operation.OperationId + "'.");

                    contentParameter = parameter;
                }
            }

            return contentParameter;
        }
    }

    public bool HasContent => ContentParameter != null;

    public IEnumerable<OperationParameterModel> PathParameters => Parameters.Where(p => p.Kind == ApiParameterKind.Path);

    public IEnumerable<OperationParameterModel> QueryParameters =>
        Parameters.Where(p => p.Kind is ApiParameterKind.Query or ApiParameterKind.ModelBinding);

    public bool HasQueryParameters => QueryParameters.Any();

    public IEnumerable<OperationParameterModel> HeaderParameters => Parameters.Where(p => p.Kind == ApiParameterKind.Header);

    public bool HasAcceptHeaderParameterParameter => HeaderParameters.Any(p => p.Name.Equals("accept", StringComparison.OrdinalIgnoreCase));

    public bool HasFormParameters => Parameters.Any(p => p.Kind == ApiParameterKind.FormData);

    public IEnumerable<OperationParameterModel> FormParameters => Parameters.Where(p => p.Kind == ApiParameterKind.FormData);

    public bool ConsumesOnlyFormUrlEncoded => ConsumesFormUrlEncoded && !ConsumesJson;

    public bool ConsumesFormUrlEncoded =>
        operation.ActualConsumes.Contains("application/x-www-form-urlencoded") ||
        (operation.ActualRequestBody?.Content.ContainsKey("application/x-www-form-urlencoded") ?? false);

    public bool ConsumesJson =>
        operation.ActualConsumes.Contains("application/json") ||
        (operation.ActualRequestBody?.Content.ContainsKey("application/json") ?? false);

    public string? Summary => ConversionUtilities.TrimWhiteSpaces(operation.Summary);

    public bool HasSummary => !string.IsNullOrEmpty(Summary);

    public string? Description => ConversionUtilities.TrimWhiteSpaces(operation.Description);

    public bool HasDescription => !string.IsNullOrEmpty(Description);

    public bool IsDeprecated => operation.IsDeprecated;

    public bool HasXmlBodyParameter => Parameters.Any(p => p.IsXmlBodyParameter);

    public bool HasBinaryBodyParameter => Parameters.Any(p => p.IsBinaryBodyParameter);

    public bool HasPlainTextBodyParameter => Consumes == "text/plain";

    public bool WrapResponse => false;

    public IEnumerable<ApiSecurityRequirement> Security => operation.ActualSecurity;

    public string Consumes
    {
        get
        {
            var consumes = operation.ActualConsumes;
            if (consumes.Contains("application/json"))
                return "application/json";

            var contentType = consumes.FirstOrDefault() ?? operation.ActualRequestBody?.Content.Keys.FirstOrDefault();
            return contentType?.Replace("\"", "\\\"") ?? "application/json";
        }
    }

    public string Produces
    {
        get
        {
            var produces = operation.ActualProduces;
            if (produces.Contains("application/json"))
                return "application/json";

            var contentType = produces.FirstOrDefault() ?? SuccessResponse?.Produces;
            return contentType?.Replace("\"", "\\\"") ?? "application/json";
        }
    }

    public bool HasResultType
    {
        get
        {
            var successResponse = operation.GetSuccessResponse().Value;
            return successResponse != null && !successResponse.IsEmpty(operation);
        }
    }

    public string UnwrappedResultType
    {
        get
        {
            var successResponse = operation.GetSuccessResponse();
            if (successResponse.Value == null || successResponse.Value.IsEmpty(operation))
                return "void";

            if (successResponse.Value.IsBinary(operation))
                return ContractGenerator.BinaryResponseTypeName;

            var isNullable = successResponse.Value.IsNullable(settings.SchemaType);
            var typeNameHint = successResponse.Value.Schema?.HasTypeNameTitle != true ? "Response" : null;
            return generator.GetTypeName(successResponse.Value.Schema, isNullable, typeNameHint);
        }
    }

    public string SyncResultType => UnwrappedResultType;

    public string ResultType => SyncResultType == "void"
        ? "System.Threading.Tasks.Task"
        : "System.Threading.Tasks.Task<" + SyncResultType + ">";

    public bool HasResult => UnwrappedResultType != "void";

    public string? UnwrappedResultDefaultValue =>
        HasResult
            ? "default(" + UnwrappedResultType + ")" + (settings.GenerateNullableReferenceTypes ? "!" : string.Empty)
            : null;

    public string? ResultDescription
    {
        get
        {
            var successResponse = operation.GetSuccessResponse();
            return successResponse.Value != null
                ? ConversionUtilities.TrimWhiteSpaces(successResponse.Value.Description)
                : null;
        }
    }

    public bool HasResultDescription => !string.IsNullOrEmpty(ResultDescription);

    public IEnumerable<ExceptionDescriptionModel> ExceptionDescriptions =>
        Responses
            .Where(r => !r.IsSuccess)
            .SelectMany(r => r.InheritsExceptionSchema
                ? [new ExceptionDescriptionModel(r.Type, r.ExceptionDescription)]
                : Array.Empty<ExceptionDescriptionModel>());

    private string GetParameterVariableName(ApiParameter parameter, IEnumerable<ApiParameter> allParameters)
    {
        var variableName = settings.ParameterNameGenerator.Generate(parameter, allParameters);
        return ReservedKeywords.Contains(variableName) ? "@" + variableName : variableName;
    }

    private string ResolveParameterType(ApiParameter parameter)
    {
        var actualSchema = parameter.ActualSchema;
        if (parameter.IsBinaryBodyParameter)
            return parameter.HasBinaryBodyWithMultipleMimeTypes ? "FileParameter" : "System.IO.Stream";

        if (actualSchema.Type == ApiObjectType.Array && actualSchema.Item?.IsBinary == true)
            return "System.Collections.Generic.IEnumerable<FileParameter>";

        if (actualSchema.IsBinary)
        {
            return parameter.CollectionFormat == ApiParameterCollectionFormat.Multi && !actualSchema.Type.IsArray()
                ? "System.Collections.Generic.IEnumerable<FileParameter>"
                : "FileParameter";
        }

        return ResolveBaseParameterType(parameter)
            .Replace(settings.ArrayType + "<", settings.ParameterArrayType + "<")
            .Replace(settings.DictionaryType + "<", settings.ParameterDictionaryType + "<");
    }

    private string ResolveBaseParameterType(ApiParameter parameter)
    {
        var schema = parameter.ActualSchema;
        if (parameter.IsXmlBodyParameter)
            return "string";

        if (parameter.CollectionFormat == ApiParameterCollectionFormat.Multi && (schema.Type & ApiObjectType.Array) == 0)
        {
            schema = new ApiSchema { Type = ApiObjectType.Array, Item = schema };
        }

        var typeNameHint = !schema.HasTypeNameTitle
            ? ConversionUtilities.ConvertToUpperCamelCase(parameter.Name, firstCharacterMustBeAlpha: true)
            : null;
        var isNullable = !parameter.IsRequired || parameter.IsNullable(settings.SchemaType);
        return generator.Resolver.Resolve(schema, isNullable, typeNameHint);
    }

    private IList<ApiParameter> GetActualParameters()
    {
        var parameters = operation.GetActualParameters().ToList();
        ApiMediaType? multipartContent = null;
        var formProperties = operation.ActualRequestBody?.Content.TryGetValue("multipart/form-data", out multipartContent) == true
            ? multipartContent?.Schema?.ActualSchema?.ActualProperties
            : null;

        if (formProperties is { Count: > 0 })
        {
            var count = parameters.Count;
            parameters = parameters
                .Where(p => !p.IsBinaryBodyParameter)
                .Concat(formProperties.Select((p, i) => new ApiParameter
                {
                    Name = p.Key,
                    Kind = ApiParameterKind.FormData,
                    Schema = p.Value,
                    Description = p.Value.Description,
                    CollectionFormat = (p.Value.Type & ApiObjectType.Array) != ApiObjectType.None && p.Value.Item != null
                        ? ApiParameterCollectionFormat.Multi
                        : ApiParameterCollectionFormat.Undefined,
                    Position = count + 100 + i,
                }))
                .ToList();
        }

        return parameters;
    }

    private sealed class StatusCodeComparer : IComparer<OperationResponseModel>
    {
        public static readonly StatusCodeComparer Instance = new();

        public int Compare(OperationResponseModel? x, OperationResponseModel? y)
        {
            if (int.TryParse(x!.StatusCode, out var xCode) && int.TryParse(y!.StatusCode, out var yCode))
                return xCode.CompareTo(yCode);

            return StringComparer.OrdinalIgnoreCase.Compare(x.StatusCode, y!.StatusCode);
        }
    }
}

/// <summary>An exception a client method documents.</summary>
internal sealed class ExceptionDescriptionModel(string type, string description)
{
    public string Type { get; } = "ApiException{" + type + "}";

    public string Description { get; } = !string.IsNullOrEmpty(description) ? description : "A server side error occurred.";
}

/// <summary>The C# view of an operation parameter.</summary>
internal sealed class OperationParameterModel
{
    private readonly ApiParameter parameter;
    private readonly IList<ApiParameter> allParameters;
    private readonly ContractGenerator generator;
    private readonly ContractGeneratorSettings settings;
    private readonly List<ParameterPropertyModel> properties;

    public OperationParameterModel(
        string name,
        string variableName,
        string variableIdentifier,
        string typeName,
        ApiParameter parameter,
        IList<ApiParameter> allParameters,
        ContractGenerator generator)
    {
        this.parameter = parameter;
        this.allParameters = allParameters;
        this.generator = generator;
        settings = generator.Settings;
        Type = typeName;
        Name = name;
        VariableName = variableName;
        VariableIdentifier = variableIdentifier;
        properties = parameter.ActualSchema.ActualProperties
            .Select(p => new ParameterPropertyModel(p.Key, p.Value, settings.PropertyNameGenerator.Generate(p.Value)))
            .ToList();
    }

    public ApiParameter Parameter => parameter;

    public string Type { get; }

    public string Name { get; }

    public string VariableName { get; }

    public string VariableIdentifier { get; }

    public ApiParameterKind Kind => parameter.Kind;

    public ApiParameterStyle Style => parameter.Style;

    public bool? Explode => parameter.Explode;

    public bool IsDeepObject => parameter.Style == ApiParameterStyle.DeepObject;

    public bool IsForm => parameter.Style == ApiParameterStyle.Form;

    public IEnumerable<ParameterPropertyModel> PropertyNames => properties.Where(p => !p.IsCollection);

    public IEnumerable<ParameterPropertyModel> CollectionPropertyNames => properties.Where(p => p.IsCollection);

    public string? Description => ConversionUtilities.TrimWhiteSpaces(parameter.Description);

    public bool HasDescription => !string.IsNullOrEmpty(Description);

    public ApiSchema Schema => parameter.ActualSchema;

    public bool IsRequired => parameter.IsRequired;

    public bool IsNullable => parameter.IsNullable(settings.SchemaType);

    public bool IsOptional => !parameter.IsRequired;

    public bool IsLast => allParameters.LastOrDefault() == parameter;

    public bool IsXmlBodyParameter => parameter.IsXmlBodyParameter;

    public bool IsBinaryBodyParameter => parameter.IsBinaryBodyParameter;

    public bool HasBinaryBodyWithMultipleMimeTypes => parameter.HasBinaryBodyWithMultipleMimeTypes;

    public bool IsDate => Schema.Format == "date" && generator.GetTypeName(Schema, IsNullable, null) != "string";

    public bool IsDateTime => Schema.Format == "date-time" && generator.GetTypeName(Schema, IsNullable, null) != "string";

    public bool IsDateOrDateTime => IsDate || IsDateTime;

    public bool IsArray => Schema.Type.HasFlag(ApiObjectType.Array) || parameter.CollectionFormat == ApiParameterCollectionFormat.Multi;

    public bool IsExplodedArray =>
        IsArray &&
        (settings.SchemaType == ApiSchemaType.Swagger2
            ? parameter.CollectionFormat == ApiParameterCollectionFormat.Multi
            : Explode ?? Kind is ApiParameterKind.Query or ApiParameterKind.Cookie);

    public bool IsStringArray => IsArray && Schema.Item?.ActualSchema.Type.HasFlag(ApiObjectType.String) == true;

    public bool IsFile => Schema.IsBinary || (IsArray && Schema.Item?.IsBinary == true);

    public bool IsDictionary => Schema.IsDictionary;

    public bool IsDateTimeArray =>
        IsArray &&
        Schema.Item?.ActualSchema.Format == "date-time" &&
        generator.GetTypeName(Schema.Item.ActualSchema, IsNullable, null) != "string";

    public bool IsDateArray =>
        IsArray &&
        Schema.Item?.ActualSchema.Format == "date" &&
        generator.GetTypeName(Schema.Item.ActualSchema, IsNullable, null) != "string";

    public bool IsObject => Schema.ActualSchema.Type == ApiObjectType.Object;

    public bool IsQuery => Kind == ApiParameterKind.Query;

    public bool IsHeader => Kind == ApiParameterKind.Header;

    public bool IsSystemNullable => Type.EndsWith("?", StringComparison.Ordinal);

    public bool HasAdditionalProperties =>
        IsObject && Schema.AllowAdditionalProperties && !IsDictionary && Type != "object";
}

/// <summary>A property of an object parameter (e.g. a deepObject query parameter).</summary>
internal sealed class ParameterPropertyModel(string key, ApiSchemaProperty property, string name)
{
    public string Key { get; } = key;

    public string Name { get; } = name;

    public bool IsCollection => property.Type == ApiObjectType.Array;
}

/// <summary>The C# view of an operation response.</summary>
internal sealed class OperationResponseModel
{
    private readonly OperationModel operationModel;
    private readonly ApiOperation operation;
    private readonly ApiResponse response;
    private readonly ContractGenerator generator;

    public OperationResponseModel(
        OperationModel operationModel,
        ApiOperation operation,
        string statusCode,
        ApiResponse response,
        bool isPrimarySuccessResponse,
        ContractGenerator generator)
    {
        this.operationModel = operationModel;
        this.operation = operation;
        this.response = response;
        this.generator = generator;
        StatusCode = statusCode;
        IsPrimarySuccessResponse = isPrimarySuccessResponse;
        ActualResponseSchema = response.Schema?.ActualSchema;
    }

    public string StatusCode { get; }

    public ApiSchema? ActualResponseSchema { get; }

    public bool IsPrimarySuccessResponse { get; }

    public bool HasType => ActualResponseSchema != null;

    public bool IsNullable => response.IsNullable(generator.Settings.SchemaType);

    public string Type =>
        response.IsBinary(operation)
            ? ContractGenerator.BinaryResponseTypeName
            : generator.GetTypeName(ActualResponseSchema, IsNullable, "Response");

    public bool IsDate =>
        ActualResponseSchema != null &&
        ActualResponseSchema.Format is "date" or "date-time" &&
        generator.GetTypeName(ActualResponseSchema, IsNullable, "Response") != "string";

    public bool IsPlainText => !response.Content.ContainsKey("application/json") && response.Content.ContainsKey("text/plain");

    public bool IsFile => IsSuccess && response.IsBinary(operation);

    public bool CheckChunkedStatusCode => IsFile && StatusCode is "200" or "204";

    public bool InheritsExceptionSchema => ActualResponseSchema?.InheritsSchema(generator.Resolver.ExceptionSchema) ?? false;

    public bool IsSuccess
    {
        get
        {
            if (IsPrimarySuccessResponse)
                return true;

            var primarySuccessResponse = operationModel.Responses.FirstOrDefault(r => r.IsPrimarySuccessResponse);
            return HttpUtilities.IsSuccessStatusCode(StatusCode) &&
                   (primarySuccessResponse == null || primarySuccessResponse.Type == Type);
        }
    }

    public string ExceptionDescription =>
        string.IsNullOrEmpty(response.Description)
            ? "A server side error occurred."
            : ConversionUtilities.ConvertToStringLiteral(response.Description!);

    public string? Produces
    {
        get
        {
            if (response.Content.ContainsKey("*/*"))
                return "*/*";

            if (response.Content.ContainsKey("application/json"))
                return "application/json";

            return response.Content.FirstOrDefault().Key;
        }
    }
}
