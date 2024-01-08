using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace Refitter.Core;

internal class CustomCSharpPropertyNameGenerator : IPropertyNameGenerator
{
    private static readonly char[] ReservedFirstPassChars = ['"', '\'', '@', '?', '!', '$', '[', ']', '(', ')', '.', '=', '+'];
    private static readonly char[] ReservedSecondPassChars = ['*', ':', '-', '#', '&'];
    
    public string Generate(JsonSchemaProperty property)
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
                .Replace("&", "And");
        }

        return string.IsNullOrWhiteSpace(property.Name)
            ? "_"
            : name;
    }
}