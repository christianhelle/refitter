using System.Diagnostics.CodeAnalysis;
using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace Refitter.Core;

internal class CustomCSharpPropertyNameGenerator : IPropertyNameGenerator
{
    private static readonly char[] ReservedFirstPassChars = ['"', '\'', '@', '?', '!', '$', '[', ']', '(', ')', '.', '=', '+'];
    private static readonly char[] ReservedSecondPassChars = ['*', ':', '-', '#', '&', '%'];

    public string Generate(JsonSchemaProperty property) =>
        string.IsNullOrWhiteSpace(property.Name)
            ? "_"
            : ReplaceNameContainingReservedCharacters(property);

    /// <summary>
    /// This code is taken directly from NJsonSchema.CodeGeneration.CSharp.CSharpPropertyNameGenerator
    /// which since v14.0.0 is no longer extensible.
    /// See https://github.com/RicoSuter/NJsonSchema/blob/3585d60e949e43284601e0bea16c33de4c6c21f5/src/NJsonSchema.CodeGeneration.CSharp/CSharpPropertyNameGenerator.cs#L12"
    /// </summary>
    [ExcludeFromCodeCoverage]
    private static string ReplaceNameContainingReservedCharacters(JsonSchemaProperty property)
    {
        var name = property.Name;

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

        name = ConversionUtilities.ConvertToUpperCamelCase(name, true);

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

        return name;
    }
}
