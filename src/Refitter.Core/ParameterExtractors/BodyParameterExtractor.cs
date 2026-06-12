using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using NSwag.CodeGeneration.Models;
using Refitter.Core;
using System.Text;

namespace Refitter.Core;

internal sealed class BodyParameterExtractor : IParameterTypeExtractor
{
    public bool CanExtract(OpenApiParameterKind kind) => kind == OpenApiParameterKind.Body;

    public bool CanExtract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings)
    {
        return operationModel.Parameters.Any(p => p.Kind == OpenApiParameterKind.Body);
    }

    public IEnumerable<string> Extract(
        CSharpOperationModel operationModel,
        OpenApiOperation operation,
        RefitGeneratorSettings settings,
        string? dynamicQuerystringParameterType = null)
    {
        var parameters = new List<string>();

        // Non-binary body parameters
        var bodyParams = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && !p.IsBinaryBodyParameter)
            .Select(p =>
            {
                var variableName = GetVariableName(p);
                return $"{JoinAttributes(GetBodyAttribute(p, settings), GetAliasAsAttribute(p.Name, variableName))}{GetParameterType(p, settings)} {variableName}";
            })
            .ToList();
        parameters.AddRange(bodyParams);

        // Binary body parameters
        var binaryParams = operationModel.Parameters
            .Where(p => p.Kind == OpenApiParameterKind.Body && p.IsBinaryBodyParameter)
            .Select(p =>
            {
                var variableName = GetVariableName(p);
                var aliasAsAttribute = GetAliasAsAttribute(p.Name, variableName);
                var generatedAliasAsAttribute = string.IsNullOrWhiteSpace(aliasAsAttribute)
                    ? string.Empty
                    : $"[{aliasAsAttribute}]";

                return $"{generatedAliasAsAttribute}StreamPart {variableName}";
            })
            .ToList();
        parameters.AddRange(binaryParams);

        return parameters;
    }

    private static string GetVariableName(ParameterModelBase parameterModel)
    {
        return IdentifierUtils.ToCompilableIdentifier(parameterModel.VariableName);
    }

    private static string GetAliasAsAttribute(string originalName, string variableName)
    {
        return string.Equals(originalName, variableName, StringComparison.Ordinal)
            ? string.Empty
            : $"AliasAs(\"{EscapeString(originalName)}\")";
    }

    private static string EscapeString(string value)
    {
        var sb = new StringBuilder(value.Length + 10);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '\f': sb.Append("\\f"); break;
                case '\v': sb.Append("\\v"); break;
                case '\b': sb.Append("\\b"); break;
                case '\0': sb.Append("\\0"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    private static string JoinAttributes(params string[] attributes)
    {
        var filteredAttributes = attributes
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .ToList();

        if (filteredAttributes.Count == 0)
            return string.Empty;

        return "[" + string.Join(", ", filteredAttributes) + "] ";
    }

    private static string GetBodyAttribute(CSharpParameterModel parameter, RefitGeneratorSettings settings)
    {
        var anyType = settings.CodeGeneratorSettings?.AnyType ?? "object";
        var parameterType = WellKnownNamespaces.TrimImportedNamespaces(FindSupportedType(parameter.Type));

        if (parameterType.Equals(anyType, StringComparison.OrdinalIgnoreCase) ||
            parameterType.Contains("JsonElement", StringComparison.OrdinalIgnoreCase))
        {
            return "Body(BodySerializationMethod.Serialized)";
        }

        return "Body";
    }

    private static string GetParameterType(
        ParameterModelBase parameterModel,
        RefitGeneratorSettings settings)
    {
        var type = WellKnownNamespaces
            .TrimImportedNamespaces(
                FindSupportedType(
                    parameterModel.Type));

        if (settings.OptionalParameters &&
            !type.EndsWith("?") &&
            (parameterModel.IsNullable || parameterModel.IsOptional || !parameterModel.IsRequired))
            type += "?";

        return type;
    }

    private static string FindSupportedType(string typeName)
    {
        if (typeName is "FileResponse" or "FileParameter")
            return "StreamPart";

        if (typeName.Contains("FileParameter") || typeName.Contains("FileResponse"))
        {
            return typeName
                .Replace("FileParameter", "StreamPart")
                .Replace("FileResponse", "StreamPart");
        }

        return typeName;
    }
}
