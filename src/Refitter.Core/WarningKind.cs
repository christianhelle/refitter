namespace Refitter.Core;

/// <summary>
/// Identifies a configuration <see cref="Warning"/>, so hosts can react to it without matching its text.
/// </summary>
public enum WarningKind
{
    /// <summary>A warning without a specific kind.</summary>
    Other,

    /// <summary><c>codeGeneratorSettings.dateFormat</c> is ignored because <c>useIsoDateFormat</c> is enabled.</summary>
    DateFormatOverride,

    /// <summary>A deprecated setting is used.</summary>
    DeprecatedSetting,
}
