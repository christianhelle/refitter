namespace Refitter.Core;

internal static class StringCasingExtensions
{
    public static string ConvertKebabCaseToPascalCase(this string str)
    {
        var parts = str.Split('-');

        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].CapitalizeFirstCharacter().Replace(".", "_");
        }

        return string.Join(string.Empty, parts);
    }

    public static string ConvertKebabCaseToCamelCase(this string str)
    {
        var parts = str.Split('-');
        for (var i = 1; i < parts.Length; i++)
        {
            parts[i] = parts[i].CapitalizeFirstCharacter();
        }

        return string.Join(string.Empty, parts);
    }

    public static string ConvertRouteToCamelCase(this string str)
    {
        var parts = str.Split('/');
        for (var i = 1; i < parts.Length; i++)
        {
            parts[i] = parts[i].CapitalizeFirstCharacter();
        }

        return string.Join(string.Empty, parts);
    }

    public static string CapitalizeFirstCharacter(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return str.Substring(0, 1).ToUpperInvariant() +
               str.Substring(1, str.Length - 1);
    }

    public static string ConvertSpacesToPascalCase(this string str)
    {
        var parts = str.Split(' ');
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].CapitalizeFirstCharacter();
        }

        return string.Join(string.Empty, parts);
    }

    public static string ConvertColonsToPascalCase(this string str)
    {
        var parts = str.Split(':');
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].CapitalizeFirstCharacter();
        }

        return string.Join(string.Empty, parts);
    }
}