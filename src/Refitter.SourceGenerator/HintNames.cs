namespace Refitter.SourceGenerator;

/// <summary>
/// Names the sources the generator adds for a <c>.refitter</c> file. The name starts with the settings file name
/// and is unique among the sources of the compilation.
/// </summary>
internal static class HintNames
{
    public static string Create(string settingsFilePath, string plannedFilePath, HashSet<string> usedHintNames)
    {
        var settingsName = GetFileNameWithoutExtension(settingsFilePath);
        var plannedName = GetFileNameWithoutExtension(plannedFilePath);
        if (plannedName.EndsWith(".g", StringComparison.OrdinalIgnoreCase))
        {
            plannedName = plannedName.Substring(0, plannedName.Length - 2);
        }

        var baseName = SanitizeHintName(
            string.Equals(settingsName, plannedName, StringComparison.OrdinalIgnoreCase)
                ? settingsName
                : settingsName + "." + plannedName);

        return MakeUnique(baseName + ".g.cs", usedHintNames);
    }

    public static string MakeUnique(string hintName, HashSet<string> usedHintNames)
    {
        const string extension = ".g.cs";
        var baseName = hintName.Substring(0, hintName.Length - extension.Length);
        var uniqueName = hintName;
        var counter = 2;
        while (!usedHintNames.Add(uniqueName))
        {
            uniqueName = $"{baseName}{counter}{extension}";
            counter++;
        }

        return uniqueName;
    }

    private static string SanitizeHintName(string name)
    {
        var chars = name.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];
            if (!char.IsLetterOrDigit(c) && c != '.' && c != '_' && c != '-')
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }

    private static string GetFileNameWithoutExtension(string path)
    {
        var lastSep = path.LastIndexOfAny(new[] { '/', '\\' });
        var fileName = lastSep >= 0 ? path.Substring(lastSep + 1) : path;
        var lastDot = fileName.LastIndexOf('.');
        return lastDot >= 0 ? fileName.Substring(0, lastDot) : fileName;
    }
}
