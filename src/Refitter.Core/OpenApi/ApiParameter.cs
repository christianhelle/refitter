#nullable enable

using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>
/// An operation parameter. Swagger 2.0 parameters describe their type inline, so a parameter is also a schema;
/// OpenAPI 3 parameters describe it in <see cref="Schema"/>.
/// </summary>
internal sealed class ApiParameter : ApiSchema
{
    private static readonly Regex AppJsonRegex = new(@"application\/(\S+?)?\+?json;?(\S+)?", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private string name = string.Empty;
    private ApiParameterKind kind;
    private ApiParameterStyle style;
    private bool? explode;
    private bool isRequired;
    private ApiSchema? schema;
    private int? position;

    public ApiParameter()
    {
    }

    public ApiParameter(ApiSchemaType schemaType)
        : base(schemaType)
    {
    }

    public ApiOperation? ParentOperation => Parent as ApiOperation;

    public string Name
    {
        get => name;
        set
        {
            name = value;
            ParentOperation?.UpdateRequestBody(this);
        }
    }

    public string? OriginalName { get; set; }

    public ApiParameterKind Kind
    {
        get => kind;
        set
        {
            kind = value;
            ParentOperation?.UpdateRequestBody(this);
        }
    }

    public ApiParameterStyle Style
    {
        get => style;
        set
        {
            style = value;
            ParentOperation?.UpdateRequestBody(this);
        }
    }

    public bool? Explode
    {
        get => explode;
        set
        {
            explode = value;
            ParentOperation?.UpdateRequestBody(this);
        }
    }

    public bool IsRequired
    {
        get => isRequired;
        set
        {
            isRequired = value;
            ParentOperation?.UpdateRequestBody(this);
        }
    }

    public bool AllowEmptyValue { get; set; }

    public override string? Description
    {
        get => base.Description;
        set
        {
            base.Description = value;
            ParentOperation?.UpdateRequestBody(this);
        }
    }

    public ApiParameterCollectionFormat CollectionFormat { get; set; }

    public ApiSchema? Schema
    {
        get => schema;
        set
        {
            schema = value;
            ParentOperation?.UpdateRequestBody(this);
        }
    }

    public ApiSchema? CustomSchema { get; set; }

    public int? Position
    {
        get => position;
        set
        {
            position = value;
            ParentOperation?.UpdateRequestBody(this);
        }
    }

    public ApiParameter ActualParameter => Reference as ApiParameter ?? this;

    public override ApiSchema ActualSchema =>
        Reference is ApiParameter referencedParameter
            ? referencedParameter.ActualSchema
            : Schema?.ActualSchema ?? CustomSchema?.ActualSchema ?? base.ActualSchema;

    public bool IsXmlBodyParameter
    {
        get
        {
            if (Kind != ApiParameterKind.Body)
                return false;

            var operation = Parent as ApiOperation;
            var consumes = operation?.ActualConsumes;
            if (consumes is { Count: > 0 })
            {
                return consumes.Any(p => p.Contains("application/xml")) &&
                       !consumes.Any(p => AppJsonRegex.IsMatch(p));
            }

            var contentTypes = operation?.ActualRequestBody?.Content.Keys;
            if (contentTypes is { Count: > 0 } && contentTypes.Any(p => p.Contains("application/xml")))
            {
                return !contentTypes.Any(p => AppJsonRegex.IsMatch(p));
            }

            return false;
        }
    }

    public bool IsBinaryBodyParameter
    {
        get
        {
            if (Kind != ApiParameterKind.Body || IsXmlBodyParameter)
                return false;

            var operation = Parent as ApiOperation;
            if (operation != null && operation.ActualConsumes.Count > 0)
            {
                var consumes = operation.ActualConsumes;
                return (Schema == null || Schema.IsBinary || consumes.Contains("multipart/form-data")) &&
                       !consumes.Any(p => p.Contains("*/*")) &&
                       !consumes.Any(p => AppJsonRegex.IsMatch(p));
            }

            var content = operation?.ActualRequestBody?.Content;
            if ((content?.ContainsKey("multipart/form-data") == true ||
                 content?.Any(p => p.Value.Schema?.IsBinary ?? true) == true) &&
                !content.Any(p => p.Key.Contains("*/*") && !(p.Value.Schema?.IsBinary ?? false)))
            {
                return !content.Any(p => AppJsonRegex.IsMatch(p.Key) && !(p.Value.Schema?.IsBinary ?? false));
            }

            return false;
        }
    }

    public bool HasBinaryBodyWithMultipleMimeTypes
    {
        get
        {
            if (!IsBinaryBodyParameter)
                return false;

            var operation = Parent as ApiOperation;
            if (operation != null && operation.ActualConsumes.Count > 0)
            {
                var consumes = operation.ActualConsumes;
                return consumes.Count > 1 || consumes.Any(p => p.Contains('*'));
            }

            var content = operation?.ActualRequestBody?.Content;
            if (content is { Count: > 0 })
            {
                return content.Count > 1 || content.Any(p => p.Key.Contains('*'));
            }

            return false;
        }
    }

    public override bool IsNullable(ApiSchemaType schemaType)
    {
        switch (schemaType)
        {
            case ApiSchemaType.Swagger2:
                return IsNullableRaw ?? !IsRequired;
            case ApiSchemaType.OpenApi3:
                if (IsNullableRaw.HasValue)
                    return IsNullableRaw.Value;
                if (Schema != null)
                    return Schema.IsNullable(schemaType);
                if (CustomSchema != null)
                    return CustomSchema.IsNullable(schemaType);
                break;
        }

        return base.IsNullable(schemaType);
    }
}

/// <summary>An OpenAPI request body.</summary>
internal sealed class ApiRequestBody
{
    private ApiRequestBody? reference;
    private string? name;
    private string? description;
    private bool isRequired;
    private int? position;

    public ApiRequestBody()
    {
        Content = new ApiRequestBodyContent(this);
    }

    public object? Parent { get; internal set; }

    public ApiOperation? ParentOperation => Parent as ApiOperation;

    public string? ReferencePath { get; set; }

    public ApiRequestBody? Reference
    {
        get => reference;
        set
        {
            if (reference != value)
            {
                reference = value;
                ReferencePath = null;
            }

            ParentOperation?.UpdateBodyParameter();
        }
    }

    public ApiRequestBody ActualRequestBody => Reference ?? this;

    public string? Name
    {
        get => name;
        set
        {
            name = value;
            ParentOperation?.UpdateBodyParameter();
        }
    }

    public string? Description
    {
        get => description;
        set
        {
            description = value;
            ParentOperation?.UpdateBodyParameter();
        }
    }

    public ApiRequestBodyContent Content { get; }

    public bool IsRequired
    {
        get => isRequired;
        set
        {
            isRequired = value;
            ParentOperation?.UpdateBodyParameter();
        }
    }

    public int? Position
    {
        get => position;
        set
        {
            position = value;
            ParentOperation?.UpdateBodyParameter();
        }
    }

    public string ActualName => !NullCheck.IsNullOrEmpty(Name) ? Name : "body";
}

/// <summary>The media types of a request body, which keeps the operation's body parameter in sync.</summary>
internal sealed class ApiRequestBodyContent : IEnumerable<KeyValuePair<string, ApiMediaType>>
{
    private readonly ApiRequestBody owner;
    private readonly Dictionary<string, ApiMediaType> items = new();

    public ApiRequestBodyContent(ApiRequestBody owner)
    {
        this.owner = owner;
    }

    public int Count => items.Count;

    public ICollection<string> Keys => items.Keys;

    public ICollection<ApiMediaType> Values => items.Values;

    public ApiMediaType this[string key]
    {
        get => items[key];
        set
        {
            items[key] = value;
            value.Parent = owner;
            owner.ParentOperation?.UpdateBodyParameter();
        }
    }

    public void Add(string key, ApiMediaType value)
    {
        items.Add(key, value);
        value.Parent = owner;
        owner.ParentOperation?.UpdateBodyParameter();
    }

    public void Clear() => items.Clear();

    public bool ContainsKey(string key) => items.ContainsKey(key);

    public bool TryGetValue(string key, out ApiMediaType value) => items.TryGetValue(key, out value!);

    public IEnumerator<KeyValuePair<string, ApiMediaType>> GetEnumerator() => items.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>A media type of a request body or response.</summary>
internal sealed class ApiMediaType
{
    private ApiSchema? schema;
    private object? example;

    public ApiRequestBody? Parent { get; internal set; }

    public ApiSchema? Schema
    {
        get => schema;
        set
        {
            schema = value;
            Parent?.ParentOperation?.UpdateBodyParameter();
        }
    }

    public object? Example
    {
        get => example;
        set
        {
            example = value;
            Parent?.ParentOperation?.UpdateBodyParameter();
        }
    }
}

/// <summary>A response of an operation.</summary>
internal sealed class ApiResponse
{
    private static readonly Regex AppJsonRegex = new(@"application\/(\S+?)?\+?json;?(\S+)?", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private ApiResponse? reference;

    public object? Parent { get; internal set; }

    public string? ReferencePath { get; set; }

    public ApiResponse? Reference
    {
        get => reference;
        set
        {
            if (reference != value)
            {
                reference = value;
                ReferencePath = null;
            }
        }
    }

    public ApiResponse ActualResponse => Reference ?? this;

    public string? Description { get; set; } = string.Empty;

    public Dictionary<string, ApiParameter> Headers { get; } = new();

    public bool? IsNullableRaw { get; set; }

    public Dictionary<string, ApiMediaType> Content { get; } = new();

    public Dictionary<string, object?>? ExtensionData { get; set; }

    /// <summary>The schema of the first media type that has one (the Swagger 2.0 response schema).</summary>
    public ApiSchema? Schema
    {
        get => Content.FirstOrDefault(c => c.Value.Schema != null).Value?.Schema;
        set => UpdateContent(value, Examples);
    }

    public object? Examples
    {
        get => Content.FirstOrDefault(c => c.Value.Example != null).Value?.Example;
        set => UpdateContent(Schema, value);
    }

    public bool IsNullable(ApiSchemaType schemaType) => IsNullable(schemaType, fallbackValue: false);

    public bool IsNullable(ApiSchemaType schemaType, bool fallbackValue)
    {
        if (schemaType == ApiSchemaType.Swagger2)
            return IsNullableRaw ?? fallbackValue;

        return ActualResponse.Schema?.IsNullable(schemaType) ?? false;
    }

    public bool IsBinary(ApiOperation operation)
    {
        // The first response of the operation that is this one decides
        var isResponseOfOperation = operation.Responses.Any(response =>
            response.Value.ActualResponse == this && response.Key != "204");

        return isResponseOfOperation && IsBinaryResponse();
    }

    private bool IsBinaryResponse()
    {
        if (ActualResponse.Content.Count > 0 && HasBinaryContent())
            return true;

        var produces = (ActualResponse.Parent as ApiOperation)?.ActualProduces;
        if (produces == null || produces.Count <= 0)
            return false;

        if (Schema?.ActualSchema.IsBinary == true)
            return true;

        if (Schema != null && !Schema.ActualSchema.IsAnyType && !Schema.ActualSchema.IsBinary)
            return false;

        return ProducesBinary(produces);
    }

    private bool HasBinaryContent()
    {
        if (ActualResponse.Content.All(c => c.Value.Schema?.ActualSchema.IsBinary ?? false))
            return true;

        return ActualResponse.Content.All(c =>
            {
                var schema = c.Value.Schema?.ActualSchema;
                return schema == null || schema.IsAnyType || schema.IsBinary;
            }) &&
            ProducesBinary(ActualResponse.Content.Keys);
    }

    public bool IsEmpty(ApiOperation operation) =>
        ActualResponse.Content.Count == 0 &&
        ActualResponse.Schema?.ActualSchema == null &&
        !IsBinary(operation);

    private static bool CannotProduceBinary(string contentType) =>
        contentType.Contains("application/json") ||
        contentType.Contains("text/plain") ||
        AppJsonRegex.IsMatch(contentType);

    private static bool ProducesBinary(IEnumerable<string> contentTypes) => !contentTypes.Any(CannotProduceBinary);

    private void UpdateContent(ApiSchema? schema, object? example)
    {
        Content.Clear();
        if (schema != null || example != null)
        {
            var key = schema?.IsBinary == true ? "application/octet-stream" : "application/json";
            Content[key] = new ApiMediaType { Schema = schema, Example = example };
        }
    }
}
