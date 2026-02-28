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

        if (char.IsUpper(str[0]))
            return str;

        return char.ToUpperInvariant(str[0]) + str.Substring(1);
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

    public static string ConvertSnakeCaseToPascalCase(this string str)
    {
        var parts = str.Split('_');
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].CapitalizeFirstCharacter();
        }

        return string.Join(string.Empty, parts);
    }
}
