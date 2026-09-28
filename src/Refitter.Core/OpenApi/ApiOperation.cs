#nullable enable

using System.Collections;

namespace Refitter.Core;

/// <summary>The operations of a path, keyed by HTTP method.</summary>
internal sealed class ApiPathItem : IEnumerable<KeyValuePair<string, ApiOperation>>
{
    private readonly Dictionary<string, ApiOperation> operations = new();

    public ApiDocument? Parent { get; internal set; }

    public string? Summary { get; set; }

    public string? Description { get; set; }

    public List<ApiParameter> Parameters { get; set; } = new();

    public Dictionary<string, object?>? ExtensionData { get; set; }

    public string? ReferencePath { get; set; }

    public ApiPathItem? Reference { get; set; }

    public ApiPathItem ActualPathItem => Reference ?? this;

    public int Count => operations.Count;

    public ICollection<string> Keys => operations.Keys;

    public ICollection<ApiOperation> Values => operations.Values;

    public ApiOperation this[string method]
    {
        get => operations[method];
        set
        {
            operations[method] = value;
            value.Parent = this;
        }
    }

    public void Add(string method, ApiOperation operation)
    {
        operations.Add(method, operation);
        operation.Parent = this;
    }

    public bool Remove(string method) => operations.Remove(method);

    public bool TryGetValue(string method, out ApiOperation operation) => operations.TryGetValue(method, out operation!);

    public IEnumerator<KeyValuePair<string, ApiOperation>> GetEnumerator() => operations.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal enum ApiParameterKind
{
    Undefined,
    Body,
    Query,
    Path,
    Header,
    FormData,
    ModelBinding,
    Cookie,
}

internal enum ApiParameterStyle
{
    Undefined,
    Simple,
    Label,
    Matrix,
    Form,
    SpaceDelimited,
    PipeDelimited,
    DeepObject,
}

internal enum ApiParameterCollectionFormat
{
    Undefined,
    Csv,
    Ssv,
    Tsv,
    Pipes,
    Multi,
}

/// <summary>
/// An operation. Like in the model Refitter has always used, an OpenAPI request body is also represented as a
/// body parameter (and a Swagger body parameter as a request body), and the two are kept in sync.
/// </summary>
internal sealed class ApiOperation
{
    private ApiRequestBody? requestBody;
    private bool disableRequestBodyUpdate;
    private bool disableBodyParameterUpdate;

    public ApiOperation()
    {
        Parameters = new ApiParameterList(this);
    }

    public ApiPathItem? Parent { get; internal set; }

    public List<string> Tags { get; set; } = new();

    public string? Summary { get; set; }

    public string? Description { get; set; }

    public string? OperationId { get; set; }

    public List<string>? Consumes { get; set; }

    public List<string>? Produces { get; set; }

    public ApiParameterList Parameters { get; }

    public ApiRequestBody? RequestBody
    {
        get => requestBody;
        set
        {
            requestBody = value;
            if (value != null)
            {
                value.Parent = this;
                UpdateBodyParameter();
            }
        }
    }

    public Dictionary<string, ApiResponse> Responses { get; } = new();

    public bool IsDeprecated { get; set; }

    public List<ApiSecurityRequirement>? Security { get; set; }

    public Dictionary<string, object?>? ExtensionData { get; set; }

    public List<string> ActualConsumes => Consumes ?? Parent!.Parent!.Consumes;

    public List<string> ActualProduces => Produces ?? Parent!.Parent!.Produces;

    public ApiRequestBody? ActualRequestBody => RequestBody?.ActualRequestBody;

    public List<ApiSecurityRequirement> ActualSecurity => Security ?? Parent!.Parent!.Security;

    public IReadOnlyList<ApiParameter> ActualParameters => GetActualParameters().ToList();

    public IReadOnlyDictionary<string, ApiResponse> ActualResponses =>
        Responses.ToDictionary(r => r.Key, r => r.Value.ActualResponse);

    public void AddResponse(string statusCode, ApiResponse response)
    {
        Responses[statusCode] = response;
        response.Parent = this;
    }

    /// <summary>The parameters of the operation followed by those of its path that it does not override.</summary>
    public IEnumerable<ApiParameter> GetActualParameters()
    {
        var parameters = Parameters.Select(p => p.ActualParameter);
        if (Parent?.Parameters == null)
            return parameters;

        return parameters
            .Concat(Parent.Parameters.Select(p => p.ActualParameter))
            .GroupBy(p => (p.Name, p.Kind))
            .Select(g => g.First());
    }

    public IEnumerable<KeyValuePair<string, ApiResponse>> GetActualResponses(Func<string, ApiResponse, bool> predicate)
    {
        foreach (var response in Responses)
        {
            if (predicate(response.Key, response.Value.ActualResponse))
                yield return new KeyValuePair<string, ApiResponse>(response.Key, response.Value.ActualResponse);
        }
    }

    public bool HasActualResponse(Func<string, ApiResponse, bool> predicate) => GetActualResponse(predicate) != null;

    public ApiResponse? GetActualResponse(Func<string, ApiResponse, bool> predicate)
    {
        foreach (var response in Responses)
        {
            if (predicate(response.Key, response.Value.ActualResponse))
                return response.Value.ActualResponse;
        }

        return null;
    }

    public KeyValuePair<string?, ApiResponse?> GetSuccessResponse()
    {
        var result = new KeyValuePair<string?, ApiResponse?>(null, null);
        ApiResponse? defaultResponse = null;
        foreach (var response in Responses)
        {
            var code = response.Key;
            var actualResponse = response.Value.ActualResponse;
            if (code == "200")
                return new KeyValuePair<string?, ApiResponse?>(code, actualResponse);

            if (result.Key == null && HttpUtilities.IsSuccessStatusCode(code))
            {
                result = new KeyValuePair<string?, ApiResponse?>(code, actualResponse);
            }
            else if (code == "default")
            {
                defaultResponse = actualResponse;
            }
        }

        return result.Key != null
            ? result
            : new KeyValuePair<string?, ApiResponse?>("default", defaultResponse);
    }

    internal void OnParametersAdded(IReadOnlyList<ApiParameter> addedParameters)
    {
        if (disableRequestBodyUpdate)
            return;

        var bodyParameter = addedParameters.SingleOrDefault(p => p.Kind == ApiParameterKind.Body);
        if (bodyParameter != null)
        {
            UpdateRequestBody(bodyParameter);
        }
        else if (Parameters.All(p => p.Kind != ApiParameterKind.Body))
        {
            RequestBody = null;
        }
    }

    internal void UpdateRequestBody(ApiParameter parameter)
    {
        if (disableRequestBodyUpdate)
            return;

        try
        {
            disableBodyParameterUpdate = true;
            if (parameter.Kind == ApiParameterKind.Body)
            {
                RequestBody ??= new ApiRequestBody();

                RequestBody.Name = parameter.Name;
                RequestBody.Position = parameter.Position;
                RequestBody.Description = parameter.Description;
                RequestBody.IsRequired = parameter.IsRequired;
                RequestBody.Content.Clear();
                RequestBody.Content.Add(
                    parameter.Schema?.IsBinary == true ? "application/octet-stream" : "application/json",
                    new ApiMediaType { Schema = parameter.Schema, Example = parameter.Example });
            }
        }
        finally
        {
            disableBodyParameterUpdate = false;
        }
    }

    internal void UpdateBodyParameter()
    {
        if (disableBodyParameterUpdate)
            return;

        try
        {
            disableRequestBodyUpdate = true;
            var bodyParameter = Parameters.SingleOrDefault(p => p.Kind == ApiParameterKind.Body);
            if (bodyParameter != null)
            {
                if (RequestBody == null)
                {
                    Parameters.RemoveAt(Parameters.IndexOf(bodyParameter));
                }
                else
                {
                    UpdateBodyParameter(bodyParameter);
                }
            }
            else if (RequestBody != null)
            {
                var parameter = new ApiParameter();
                UpdateBodyParameter(parameter);
                Parameters.Add(parameter);
            }
        }
        finally
        {
            disableRequestBodyUpdate = false;
        }
    }

    private void UpdateBodyParameter(ApiParameter parameter)
    {
        var actualRequestBody = ActualRequestBody!;
        parameter.Kind = ApiParameterKind.Body;
        parameter.Name = actualRequestBody.ActualName;
        parameter.Position = actualRequestBody.Position;
        parameter.Description = actualRequestBody.Description;
        parameter.IsRequired = actualRequestBody.IsRequired;
        parameter.Example = actualRequestBody.Content.FirstOrDefault().Value?.Example;
        parameter.Schema = actualRequestBody.Content.FirstOrDefault().Value?.Schema;
    }
}

/// <summary>The parameters of an operation, which keeps the operation's request body in sync.</summary>
internal sealed class ApiParameterList : IList<ApiParameter>
{
    private readonly ApiOperation owner;
    private readonly List<ApiParameter> items = new();

    public ApiParameterList(ApiOperation owner)
    {
        this.owner = owner;
    }

    public ApiParameter this[int index]
    {
        get => items[index];
        set
        {
            items[index] = value;
            value.Parent = owner;
            owner.OnParametersAdded([value]);
        }
    }

    public int Count => items.Count;

    public bool IsReadOnly => false;

    public void Add(ApiParameter item)
    {
        items.Add(item);
        item.Parent = owner;
        owner.OnParametersAdded([item]);
    }

    public void Clear() => items.Clear();

    public bool Contains(ApiParameter item) => items.Contains(item);

    public void CopyTo(ApiParameter[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);

    public IEnumerator<ApiParameter> GetEnumerator() => items.GetEnumerator();

    public int IndexOf(ApiParameter item) => items.IndexOf(item);

    public void Insert(int index, ApiParameter item)
    {
        items.Insert(index, item);
        item.Parent = owner;
        owner.OnParametersAdded([item]);
    }

    public bool Remove(ApiParameter item) => items.Remove(item);

    public void RemoveAt(int index) => items.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
