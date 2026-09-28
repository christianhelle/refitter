#nullable enable

using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>Generates the C# property names of contract types.</summary>
internal interface IContractPropertyNameGenerator
{
    string Generate(ApiSchemaProperty property);
}

/// <summary>Generates the C# member names of enums.</summary>
internal interface IContractEnumNameGenerator
{
    string Generate(int index, string? name, object? value, ApiSchema schema);
}

/// <summary>Generates the C# variable names of operation parameters.</summary>
internal interface IOperationParameterNameGenerator
{
    string Generate(ApiParameter parameter, IEnumerable<ApiParameter> allParameters);
}

/// <summary>
/// PascalCase property names without the characters C# does not allow. See THIRD-PARTY-NOTICES.md.
/// </summary>
internal sealed class ContractPropertyNameGenerator : IContractPropertyNameGenerator
{
    private static readonly char[] ReservedFirstPassChars = ['"', '\'', '@', '?', '!', '$', '[', ']', '(', ')', '.', '=', '+'];
    private static readonly char[] ReservedSecondPassChars = ['*', ':', '-', '#', '&', '%'];

    public string Generate(ApiSchemaProperty property) =>
        string.IsNullOrWhiteSpace(property.Name) ? "_" : Generate(property.Name);

    internal static string Generate(string name)
    {
        if (name.IndexOfAny(ReservedFirstPassChars) != -1)
        {
            name = name
                .Replace("\"", string.Empty)
                .Replace("'", string.Empty)
                .Replace("@", string.Empty)
                .Replace("?", string.Empty)
                .Replace("!", string.Empty)
                .Replace("$", string.Empty)
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Replace("(", "_")
                .Replace(")", string.Empty)
                .Replace(".", "-")
                .Replace("=", "-")
                .Replace("+", "plus");
        }

        name = ConversionUtilities
            .ConvertToUpperCamelCase(name, true)
            .ConvertSnakeCaseToPascalCase();

        if (name.IndexOfAny(ReservedSecondPassChars) != -1)
        {
            name = name
                .Replace("*", "Star")
                .Replace(":", "_")
                .Replace("-", "_")
                .Replace("#", "_")
                .Replace("&", "And")
                .Replace("%", "Percent");
        }

        return IdentifierUtils.ToCompilableIdentifier(name);
    }
}

/// <summary>Property names that keep the original JSON name (made a valid identifier).</summary>
internal sealed class PreserveOriginalContractPropertyNameGenerator : IContractPropertyNameGenerator
{
    public string Generate(ApiSchemaProperty property) => IdentifierUtils.ToCompilableIdentifier(property.Name);
}

/// <summary>Property names from a user-provided <see cref="IPropertyNameProvider"/>.</summary>
internal sealed class ProviderContractPropertyNameGenerator(IPropertyNameProvider provider) : IContractPropertyNameGenerator
{
    internal IPropertyNameProvider Provider => provider;

    public string Generate(ApiSchemaProperty property) =>
        provider.GetPropertyName(new PropertyNameContext(property.Name, property.IsRequired));
}

/// <summary>
/// Makes the generated members of a contract type unique (#1268): properties that normalize to the same name
/// (<c>user_name</c>, <c>userName</c>) get a numeric suffix, as do properties that would clash with the
/// enclosing type name (CS0542) or with the generated <c>AdditionalProperties</c> dictionary.
/// The wire names are kept by the generated <c>JsonPropertyName</c> attributes.
/// </summary>
internal sealed class UniqueContractPropertyNameGenerator(
    IContractPropertyNameGenerator inner,
    Func<ApiSchema, string?> getTypeName) : IContractPropertyNameGenerator
{
    private const string AdditionalPropertiesName = "AdditionalProperties";

    private readonly Dictionary<ApiSchema, Dictionary<ApiSchemaProperty, string>> namesBySchema = new();

    internal IContractPropertyNameGenerator Inner => inner;

    public string Generate(ApiSchemaProperty property)
    {
        if (property.Parent is not ApiSchema parent)
            return inner.Generate(property);

        if (!namesBySchema.TryGetValue(parent, out var names))
        {
            names = GenerateUniqueNames(parent);
            namesBySchema[parent] = names;
        }

        return names[property];
    }

    private Dictionary<ApiSchemaProperty, string> GenerateUniqueNames(ApiSchema schema)
    {
        var usedNames = new HashSet<string>(StringComparer.Ordinal);

        var typeName = getTypeName(schema);
        if (typeName != null)
            usedNames.Add(typeName);

        // Mirrors when the class template generates the AdditionalProperties dictionary
        if (schema.ActualTypeSchema.AllowAdditionalProperties || schema.ActualTypeSchema.AdditionalPropertiesSchema != null)
            usedNames.Add(AdditionalPropertiesName);

        var names = new Dictionary<ApiSchemaProperty, string>();
        foreach (var property in schema.Properties.Values)
        {
            var candidate = inner.Generate(property);

            // The class template does not emit inheritance discriminators as members
            if (property.IsInheritanceDiscriminator)
            {
                names[property] = candidate;
                continue;
            }

            var uniqueName = candidate;
            var suffix = 2;
            while (!usedNames.Add(uniqueName))
            {
                uniqueName = candidate + suffix;
                suffix++;
            }

            names[property] = uniqueName;
        }

        return names;
    }
}

/// <summary>Enum member names derived from the enum values. See THIRD-PARTY-NOTICES.md.</summary>
internal class DefaultContractEnumNameGenerator : IContractEnumNameGenerator
{
    private static readonly Regex InvalidNameCharactersPattern = new(
        @"[^\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(250));

    public virtual string Generate(int index, string? name, object? value, ApiSchema schema)
    {
        if (string.IsNullOrEmpty(name))
            return "Empty";

        switch (name)
        {
            case "=":
                name = "Eq";
                break;
            case "!=":
                name = "Ne";
                break;
            case ">":
                name = "Gt";
                break;
            case "<":
                name = "Lt";
                break;
            case ">=":
                name = "Ge";
                break;
            case "<=":
                name = "Le";
                break;
            case "~=":
                name = "Approx";
                break;
        }

        if (name!.StartsWith("-", StringComparison.Ordinal))
            name = "Minus" + name.Substring(1);

        if (name.StartsWith("+", StringComparison.Ordinal))
            name = "Plus" + name.Substring(1);

        if (name.StartsWith("_-", StringComparison.Ordinal))
            name = "__" + name.Substring(2);

        var cleaned = name.Replace(':', '-').Replace("\"", string.Empty);
        var converted = ConversionUtilities.ConvertToUpperCamelCase(cleaned, firstCharacterMustBeAlpha: true);
        return InvalidNameCharactersPattern.Replace(converted, "_");
    }
}

/// <summary>
/// Adds a numeric suffix when two values of the same enum map to the same member name, e.g. <c>a</c> and
/// <c>A</c> (#1267). The original values are kept in the generated <c>EnumMember</c> attributes.
/// </summary>
internal sealed class UniqueContractEnumNameGenerator : IContractEnumNameGenerator
{
    private readonly DefaultContractEnumNameGenerator defaultGenerator = new();
    private readonly Dictionary<ApiSchema, Dictionary<int, string>> namesBySchema = new();

    public string Generate(int index, string? name, object? value, ApiSchema schema)
    {
        if (!namesBySchema.TryGetValue(schema, out var names))
        {
            names = GenerateUniqueNames(schema);
            namesBySchema[schema] = names;
        }

        return names[index];
    }

    private Dictionary<int, string> GenerateUniqueNames(ApiSchema schema)
    {
        var names = new Dictionary<int, string>();
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < schema.Enumeration.Count; i++)
        {
            // Null values do not become enum members
            var value = schema.Enumeration[i];
            if (value is null)
                continue;

            var candidate = defaultGenerator.Generate(i, GetOriginalName(schema, i, value), value, schema);
            var uniqueName = candidate;
            var suffix = 2;
            while (!usedNames.Add(uniqueName))
            {
                uniqueName = candidate + suffix;
                suffix++;
            }

            names[i] = uniqueName;
        }

        return names;
    }

    // The names the enum template model passes to the enum name generator
    private static string GetOriginalName(ApiSchema schema, int index, object value)
    {
        if (schema.EnumerationNames.Count > index)
            return schema.EnumerationNames[index];

        if (schema.Type.IsInteger())
            return "_" + value;

        return value.ToString()!;
    }
}

/// <summary>Parameter variable names in lowerCamelCase. See THIRD-PARTY-NOTICES.md.</summary>
internal sealed class DefaultOperationParameterNameGenerator : IOperationParameterNameGenerator
{
    private static readonly char[] ParameterNameCleanupChars = ['-', '.', ':', '$', '@', '[', ']'];

    public string Generate(ApiParameter parameter, IEnumerable<ApiParameter> allParameters)
    {
        var variableName = GetVariableName(parameter);
        return allParameters.Count(p => GetVariableName(p) == variableName) > 1
            ? variableName + parameter.Kind
            : variableName;
    }

    private static string GetVariableName(ApiParameter parameter)
    {
        var name = !string.IsNullOrEmpty(parameter.OriginalName) ? parameter.OriginalName : parameter.Name;
        if (string.IsNullOrEmpty(name))
            return "unnamed";

        if (name!.IndexOfAny(ParameterNameCleanupChars) != -1)
        {
            name = name
                .Replace("-", "_")
                .Replace(".", "_")
                .Replace(":", "_")
                .Replace("$", string.Empty)
                .Replace("@", string.Empty)
                .Replace("[", string.Empty)
                .Replace("]", string.Empty);
        }

        return ConversionUtilities.ConvertToLowerCamelCase(name, firstCharacterMustBeAlpha: true);
    }
}

/// <summary>Parameter variable names from a user-provided <see cref="IParameterNameProvider"/>.</summary>
internal sealed class ProviderOperationParameterNameGenerator(IParameterNameProvider provider) : IOperationParameterNameGenerator
{
    internal IParameterNameProvider Provider => provider;

    public string Generate(ApiParameter parameter, IEnumerable<ApiParameter> allParameters) =>
        provider.GetParameterName(
            new ParameterNameContext(
                parameter.Name,
                parameter.Kind switch
                {
                    ApiParameterKind.Path => ParameterSource.Path,
                    ApiParameterKind.Query => ParameterSource.Query,
                    ApiParameterKind.Header => ParameterSource.Header,
                    ApiParameterKind.Cookie => ParameterSource.Cookie,
                    ApiParameterKind.Body => ParameterSource.Body,
                    ApiParameterKind.FormData => ParameterSource.Form,
                    _ => ParameterSource.Other,
                },
                parameter.IsRequired,
                allParameters.Select(p => p.Name).ToList()));
}
