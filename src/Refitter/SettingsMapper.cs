using Refitter.Core;

namespace Refitter;

internal static class SettingsMapper
{
    public static RefitGeneratorSettings Map(Settings settings)
    {
        var bundle = SettingsToBundleMapper.Map(settings);
        var result = bundle.ToLegacySettings();

        // Properties not covered by config slices
        result.OpenApiPath = settings.OpenApiPath!;
        result.ApizrSettings = settings.UseApizr ? new ApizrSettings() : null;
        result.CodeGeneratorSettings = new CodeGeneratorSettings
        {
            InlineJsonConverters = !settings.NoInlineJsonConverters,
            IntegerType = settings.IntegerType,
            JsonLibraryVersion = settings.JsonLibraryVersion ?? 8.0m,
        };

        return result;
    }
}
