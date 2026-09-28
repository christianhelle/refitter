using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>Names operations and the clients (interfaces) they are grouped into. See THIRD-PARTY-NOTICES.md.</summary>
internal interface IApiOperationNameGenerator
{
    bool SupportsMultipleClients { get; }

    string GetClientName(ApiDocument document, string path, string httpMethod, ApiOperation operation);

    string GetOperationName(ApiDocument document, string path, string httpMethod, ApiOperation operation);
}

/// <summary>Client and operation names from operation IDs like <c>client_operation</c>.</summary>
internal class MultipleClientsFromOperationIdApiOperationNameGenerator : IApiOperationNameGenerator
{
    public bool SupportsMultipleClients => true;

    public virtual string GetClientName(ApiDocument document, string path, string httpMethod, ApiOperation operation) =>
        GetClientName(operation);

    public virtual string GetOperationName(ApiDocument document, string path, string httpMethod, ApiOperation operation)
    {
        var clientName = GetClientName(operation);
        var operationName = GetOperationName(operation);

        var hasOperationWithSameName = false;
        foreach (var pathItem in document.Paths)
        {
            foreach (var other in pathItem.Value.ActualPathItem)
            {
                if (other.Value != operation &&
                    GetOperationName(other.Value) == operationName &&
                    GetClientName(other.Value) == clientName)
                {
                    hasOperationWithSameName = true;
                    break;
                }
            }
        }

        if (hasOperationWithSameName &&
            operationName.StartsWith("get", StringComparison.InvariantCultureIgnoreCase) &&
            operation.ActualResponses.TryGetValue("200", out var response) &&
            response.Schema?.ActualSchema.Type.HasFlag(ApiObjectType.Array) == true)
        {
            return "GetAll" + operationName.Substring(3);
        }

        return operationName;
    }

    private static string GetClientName(ApiOperation operation)
    {
        var operationId = operation.OperationId ?? string.Empty;
        var index = operationId.IndexOf('_');
        return index <= 0 ? string.Empty : operationId.Substring(0, index);
    }

    private static string GetOperationName(ApiOperation operation)
    {
        var operationId = operation.OperationId ?? string.Empty;
        var index = operationId.IndexOf('_');
        return index == -1 || index >= operationId.Length - 1 ? operationId : operationId.Substring(index + 1);
    }
}

/// <summary>Client names from the first tag, operation names from the operation ID.</summary>
internal class MultipleClientsFromFirstTagAndOperationIdApiOperationNameGenerator : IApiOperationNameGenerator
{
    public bool SupportsMultipleClients => true;

    public virtual string GetClientName(ApiDocument document, string path, string httpMethod, ApiOperation operation) =>
        ConversionUtilities.ConvertToUpperCamelCase(operation.Tags.FirstOrDefault(), firstCharacterMustBeAlpha: false);

    public virtual string GetOperationName(ApiDocument document, string path, string httpMethod, ApiOperation operation) =>
        operation.OperationId!;
}

/// <summary>Client names from the first tag, operation names like <see cref="MultipleClientsFromOperationIdApiOperationNameGenerator"/>.</summary>
internal sealed class MultipleClientsFromFirstTagAndOperationNameApiOperationNameGenerator : MultipleClientsFromOperationIdApiOperationNameGenerator
{
    public override string GetClientName(ApiDocument document, string path, string httpMethod, ApiOperation operation) =>
        ConversionUtilities.ConvertToUpperCamelCase(operation.Tags.FirstOrDefault(), firstCharacterMustBeAlpha: false);
}

/// <summary>Client names from the first tag, operation names from the last path segment.</summary>
internal sealed class MultipleClientsFromFirstTagAndPathSegmentsApiOperationNameGenerator : IApiOperationNameGenerator
{
    public bool SupportsMultipleClients => true;

    public string GetClientName(ApiDocument document, string path, string httpMethod, ApiOperation operation) =>
        ConversionUtilities.ConvertToUpperCamelCase(operation.Tags.FirstOrDefault(), firstCharacterMustBeAlpha: false);

    public string GetOperationName(ApiDocument document, string path, string httpMethod, ApiOperation operation)
    {
        var operationName = ConvertPathToName(path);
        var clientName = GetClientName(document, path, httpMethod, operation);
        var hasNameConflict = document.Paths
            .SelectMany(pair => pair.Value.Select(p => (Path: pair.Key.Trim('/'), HttpMethod: p.Key, Operation: p.Value)))
            .Count(op => GetClientName(document, op.Path, op.HttpMethod, op.Operation) == clientName &&
                         ConvertPathToName(op.Path) == operationName) > 1;

        return hasNameConflict
            ? operationName + ConversionUtilities.ConvertToUpperCamelCase(httpMethod, firstCharacterMustBeAlpha: false)
            : operationName;
    }

    internal static string ConvertPathToName(string path) =>
        path.Split('/').Where(p => !p.Contains('{') && !string.IsNullOrWhiteSpace(p)).Reverse().FirstOrDefault() ?? "Index";
}

/// <summary>Client names from the second to last path segment, operation names from the last one.</summary>
internal sealed class MultipleClientsFromPathSegmentsApiOperationNameGenerator : IApiOperationNameGenerator
{
    public bool SupportsMultipleClients => true;

    public string GetClientName(ApiDocument document, string path, string httpMethod, ApiOperation operation) =>
        path.Split('/').Where(p => !p.Contains('{') && !string.IsNullOrWhiteSpace(p)).Reverse().Skip(1).FirstOrDefault()
        ?? string.Empty;

    public string GetOperationName(ApiDocument document, string path, string httpMethod, ApiOperation operation)
    {
        var operationName = ConvertPathToName(path);
        var clientName = GetClientName(document, path, httpMethod, operation);
        var hasNameConflict = document.Paths
            .SelectMany(pair => pair.Value.ActualPathItem.Select(p => (Path: pair.Key.Trim('/'), HttpMethod: p.Key, Operation: p.Value)))
            .Count(op => GetClientName(document, op.Path, op.HttpMethod, op.Operation) == clientName &&
                         ConvertPathToName(op.Path) == operationName) > 1;

        return hasNameConflict ? operationName + CapitalizeFirst(httpMethod) : operationName;
    }

    internal static string ConvertPathToName(string path) =>
        path.Split('/').Where(p => !p.Contains('{') && !string.IsNullOrWhiteSpace(p)).Reverse().FirstOrDefault() ?? "Index";

    internal static string CapitalizeFirst(string name) =>
        string.IsNullOrEmpty(name) ? string.Empty : char.ToUpperInvariant(name[0]) + name.Substring(1);
}

/// <summary>A single client, operation names from the operation ID.</summary>
internal sealed class SingleClientFromOperationIdApiOperationNameGenerator : IApiOperationNameGenerator
{
    public bool SupportsMultipleClients => true;

    public string GetClientName(ApiDocument document, string path, string httpMethod, ApiOperation operation) => string.Empty;

    public string GetOperationName(ApiDocument document, string path, string httpMethod, ApiOperation operation) =>
        operation.OperationId!;
}

/// <summary>A single client, operation names from all path segments.</summary>
internal sealed class SingleClientFromPathSegmentsApiOperationNameGenerator : IApiOperationNameGenerator
{
    private static readonly Regex PathParameterRegex = new(@"\{.*?\}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    public bool SupportsMultipleClients => true;

    public string GetClientName(ApiDocument document, string path, string httpMethod, ApiOperation operation) => string.Empty;

    public string GetOperationName(ApiDocument document, string path, string httpMethod, ApiOperation operation)
    {
        var operationName = ConvertPathToName(path);
        var clientName = GetClientName(document, path, httpMethod, operation);
        var hasNameConflict = document.Paths
            .SelectMany(pair => pair.Value.ActualPathItem.Select(p => (Path: pair.Key.Trim('/'), HttpMethod: p.Key, Operation: p.Value)))
            .Count(op => GetClientName(document, op.Path, op.HttpMethod, op.Operation) == clientName &&
                         ConvertPathToName(op.Path) == operationName) > 1;

        return hasNameConflict
            ? operationName + MultipleClientsFromPathSegmentsApiOperationNameGenerator.CapitalizeFirst(httpMethod)
            : operationName;
    }

    public static string ConvertPathToName(string path)
    {
        var name = PathParameterRegex.Replace(path, string.Empty)
            .Split('/', '-', '_')
            .Where(part => !part.Contains('{') && !string.IsNullOrWhiteSpace(part))
            .Aggregate(string.Empty, (current, part) => current + MultipleClientsFromPathSegmentsApiOperationNameGenerator.CapitalizeFirst(part));

        return string.IsNullOrEmpty(name) ? "Index" : name;
    }
}

/// <summary>
/// The operation names Refitter uses for methods: from the configured strategy, made valid C# identifiers.
/// By default, operation IDs are used unless they are not unique.
/// </summary>
internal sealed class RefitterOperationNameGenerator : IApiOperationNameGenerator
{
    private readonly IApiOperationNameGenerator defaultGenerator;

    public RefitterOperationNameGenerator(ApiDocument document, IPartitioningConfiguration partitioning)
    {
        switch (partitioning.OperationNameGenerator)
        {
            case OperationNameGeneratorTypes.MultipleClientsFromOperationId:
                defaultGenerator = new MultipleClientsFromOperationIdApiOperationNameGenerator();
                break;
            case OperationNameGeneratorTypes.MultipleClientsFromPathSegments:
                defaultGenerator = new MultipleClientsFromPathSegmentsApiOperationNameGenerator();
                break;
            case OperationNameGeneratorTypes.MultipleClientsFromFirstTagAndOperationId:
                defaultGenerator = new MultipleClientsFromFirstTagAndOperationIdApiOperationNameGenerator();
                break;
            case OperationNameGeneratorTypes.MultipleClientsFromFirstTagAndOperationName:
                defaultGenerator = new MultipleClientsFromFirstTagAndOperationNameApiOperationNameGenerator();
                break;
            case OperationNameGeneratorTypes.MultipleClientsFromFirstTagAndPathSegments:
                defaultGenerator = new MultipleClientsFromFirstTagAndPathSegmentsApiOperationNameGenerator();
                break;
            case OperationNameGeneratorTypes.SingleClientFromOperationId:
                defaultGenerator = new SingleClientFromOperationIdApiOperationNameGenerator();
                break;
            case OperationNameGeneratorTypes.SingleClientFromPathSegments:
                defaultGenerator = new SingleClientFromPathSegmentsApiOperationNameGenerator();
                break;
            default:
                defaultGenerator = new MultipleClientsFromOperationIdApiOperationNameGenerator();
                if (HasDuplicateOperationNames(document))
                    defaultGenerator = new MultipleClientsFromFirstTagAndPathSegmentsApiOperationNameGenerator();
                break;
        }
    }

    public bool SupportsMultipleClients => defaultGenerator.SupportsMultipleClients;

    public string GetClientName(ApiDocument document, string path, string httpMethod, ApiOperation operation) =>
        defaultGenerator.GetClientName(document, path, httpMethod, operation);

    public string GetOperationName(ApiDocument document, string path, string httpMethod, ApiOperation operation) =>
        defaultGenerator
            .GetOperationName(document, path, httpMethod, operation)
            .ConvertKebabCaseToPascalCase()
            .ConvertSnakeCaseToPascalCase()
            .ConvertRouteToCamelCase()
            .ConvertSpacesToPascalCase()
            .ConvertColonsToPascalCase()
            .Sanitize()
            .CapitalizeFirstCharacter();

    private bool HasDuplicateOperationNames(ApiDocument document)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in document.Paths)
        {
            foreach (var operation in path.Value)
            {
                if (!seen.Add(GetOperationName(document, path.Key, operation.Key, operation.Value)))
                    return true;
            }
        }

        return false;
    }
}
