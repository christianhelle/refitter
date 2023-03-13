namespace Refitter.Core;

public static class StringCasingExtensions
{
    public static string ConvertKebabCaseToPascalCase(string str)
    {
        var parts = str.Split('-');
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = CapitalizeFirstCharacter(parts[i]);
        }

        return string.Join(string.Empty, parts);
    }

    public static string CapitalizeFirstCharacter(string str)
    {
        return str.Substring(0, 1).ToUpperInvariant() +
               str.Substring(1, str.Length - 1);
    }
}