#nullable enable

namespace Refitter.Core;

/// <summary>An OpenAPI (3.x) or Swagger (2.0) document.</summary>
internal sealed class ApiDocument
{
    public ApiDocument()
    {
        Components = new ApiComponents(this);
    }

    public ApiSchemaType SchemaType { get; set; } = ApiSchemaType.Swagger2;

    public string? DocumentPath { get; set; }

    public string? Swagger { get; set; } = "2.0";

    public string? OpenApi { get; set; } = "3.0.0";

    public ApiInfo? Info { get; set; } = new();

    public Dictionary<string, ApiPathItem> Paths { get; } = new();

    public ApiComponents Components { get; }

    public List<ApiSecurityRequirement> Security { get; set; } = new();

    public List<ApiTag> Tags { get; set; } = new();

    public List<string> Consumes { get; set; } = new();

    public List<string> Produces { get; set; } = new();

    public Dictionary<string, object?>? ExtensionData { get; set; }

    /// <summary>The schemas of the document (Swagger definitions, OpenAPI component schemas).</summary>
    public ApiSchemaDictionary Definitions => Components.Schemas;

    public Dictionary<string, ApiParameter> Parameters => Components.Parameters;

    public Dictionary<string, ApiResponse> Responses => Components.Responses;

    public Dictionary<string, ApiSecurityScheme> SecurityDefinitions => Components.SecuritySchemes;

    public void AddPath(string path, ApiPathItem pathItem)
    {
        Paths[path] = pathItem;
        pathItem.ActualPathItem.Parent = this;
    }

    public IEnumerable<ApiOperationDescription> GetOperations()
    {
        foreach (var path in Paths)
        {
            foreach (var operation in path.Value.ActualPathItem)
            {
                yield return new ApiOperationDescription(path.Key, operation.Key, operation.Value);
            }
        }
    }

    /// <summary>Generates missing or non-unique operation IDs.</summary>
    public void GenerateOperationIds() =>
        GenerateOperationIds(GetOperations().ToList(), new HashSet<string>(), new HashSet<string>());

    private static void GenerateOperationIds(
        List<ApiOperationDescription> operations,
        HashSet<string> operationIds,
        HashSet<string> duplicatedOperationIds)
    {
        operationIds.Clear();
        duplicatedOperationIds.Clear();
        foreach (var operation in operations)
        {
            if (string.IsNullOrEmpty(operation.Operation.OperationId))
            {
                operation.Operation.OperationId = GetOperationNameFromPath(operation);
            }

            if (!operationIds.Add(operation.Operation.OperationId!))
            {
                duplicatedOperationIds.Add(operation.Operation.OperationId!);
            }
        }

        if (duplicatedOperationIds.Count == 0)
            return;

        operations = operations.Where(o => duplicatedOperationIds.Contains(o.Operation.OperationId!)).ToList();

        foreach (var group in operations.GroupBy(o => o.Operation.OperationId))
        {
            if (group.Count() <= 1)
                continue;

            var arrayResponses = group.Where(o => o.Operation.HasActualResponse(IsSuccessArrayResponse));
            if (arrayResponses.Count() == group.Count())
                continue;

            foreach (var operation in group)
            {
                if (operation.Operation.HasActualResponse(IsSuccessArrayResponse))
                {
                    operation.Operation.OperationId += "All";
                }
            }
        }

        foreach (var group in operations.GroupBy(o => o.Operation.OperationId))
        {
            if (group.Count() <= 1)
                continue;

            if (group.Select(o => o.Method.ToUpperInvariant()).Distinct().Count() == 1)
                continue;

            foreach (var operation in group)
            {
                operation.Operation.OperationId += operation.Method.ToUpperInvariant();
            }
        }

        foreach (var group in operations.GroupBy(o => o.Operation.OperationId))
        {
            var list = group.ToList();
            if (group.Count() <= 1)
                continue;

            var counter = 2;
            foreach (var operation in list.Skip(1))
            {
                operation.Operation.OperationId += counter++;
            }

            GenerateOperationIds(operations, operationIds, duplicatedOperationIds);
            break;
        }
    }

    private static bool IsSuccessArrayResponse(string code, ApiResponse response) =>
        HttpUtilities.IsSuccessStatusCode(code) &&
        response.Schema?.ActualSchema.Type == ApiObjectTypes.Array;

    private static string GetOperationNameFromPath(ApiOperationDescription operation)
    {
        var segments = operation.Path.Trim('/').Split('/');
        var lastSegment = segments.LastOrDefault(s => !s.Contains('{'));
        return !string.IsNullOrEmpty(lastSegment) ? lastSegment! : "Anonymous";
    }
}

/// <summary>An operation together with its path and HTTP method.</summary>
internal sealed record ApiOperationDescription(string Path, string Method, ApiOperation Operation);

/// <summary>The reusable components of a document.</summary>
internal sealed class ApiComponents
{
    public ApiComponents(ApiDocument document)
    {
        Document = document;
        Schemas = new ApiSchemaDictionary(this);
    }

    public ApiDocument Document { get; }

    public ApiSchemaDictionary Schemas { get; }

    public Dictionary<string, ApiParameter> Parameters { get; } = new();

    public Dictionary<string, ApiResponse> Responses { get; } = new();

    public Dictionary<string, ApiRequestBody> RequestBodies { get; } = new();

    public Dictionary<string, ApiParameter> Headers { get; } = new();

    public Dictionary<string, ApiSecurityScheme> SecuritySchemes { get; } = new();
}

internal sealed class ApiInfo
{
    public string? Title { get; set; } = "Swagger specification";

    public string? Description { get; set; }

    public string? Version { get; set; } = "1.0.0";
}

internal sealed class ApiTag
{
    public string? Name { get; set; }

    public string? Description { get; set; }
}

/// <summary>A security requirement: security scheme names and their scopes.</summary>
internal sealed class ApiSecurityRequirement : Dictionary<string, IEnumerable<string>>
{
}

internal enum ApiSecuritySchemeType
{
    Undefined,
    Basic,
    ApiKey,
    OAuth2,
    Http,
    OpenIdConnect,
}

internal enum ApiSecurityApiKeyLocation
{
    Undefined,
    Query,
    Header,
    Cookie,
}

internal sealed class ApiSecurityScheme
{
    private ApiSecuritySchemeType type;

    public ApiSecuritySchemeType Type
    {
        get => type;
        set
        {
            if (value == ApiSecuritySchemeType.Basic)
            {
                type = ApiSecuritySchemeType.Http;
                Scheme = "basic";
            }
            else
            {
                type = value;
            }
        }
    }

    public string? Description { get; set; }

    public string? Name { get; set; }

    public ApiSecurityApiKeyLocation In { get; set; }

    public string? Scheme { get; set; }

    public string? BearerFormat { get; set; }

    public string? OpenIdConnectUrl { get; set; }
}

internal static class HttpUtilities
{
    public static bool IsSuccessStatusCode(string statusCode) =>
        statusCode.Length == 3 && statusCode[0] == '2';
}
