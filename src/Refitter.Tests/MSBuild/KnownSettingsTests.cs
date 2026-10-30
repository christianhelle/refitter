using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Refitter.Core;
using Refitter.SourceGenerator.Telemetry;

namespace Refitter.Tests.MSBuild;

[TestClass]
public class KnownSettingsTests
{
    [TestMethod]
    public void Paths_Match_RefitGeneratorSettings()
    {
        var expected = new Dictionary<string, string[]?>();
        AddSettings(typeof(RefitGeneratorSettings), string.Empty, expected);

        KnownSettings.Paths.Keys.Should().BeEquivalentTo(expected.Keys);
        foreach (var setting in expected)
        {
            KnownSettings.Paths[setting.Key].Should().BeEquivalentTo(setting.Value, setting.Key);
        }
    }

    [TestMethod]
    public void NestedSettings_Match_RefitGeneratorSettings() =>
        KnownSettings.NestedSettings.Should().BeEquivalentTo(
            typeof(RefitGeneratorSettings).GetProperties()
                .Where(property => IsNestedSettings(property.PropertyType))
                .Select(GetJsonName));

    private static void AddSettings(Type type, string prefix, Dictionary<string, string[]?> settings)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite ||
                property.GetCustomAttribute<JsonIgnoreAttribute>() is { Condition: JsonIgnoreCondition.Always })
            {
                continue;
            }

            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (prefix.Length == 0 && IsNestedSettings(propertyType))
            {
                AddSettings(propertyType, GetJsonName(property) + ".", settings);
                continue;
            }

            settings[prefix + GetJsonName(property)] = propertyType.IsEnum ? Enum.GetNames(propertyType) : null;
        }
    }

    private static bool IsNestedSettings(Type type) =>
        type == typeof(NamingSettings) ||
        type == typeof(DependencyInjectionSettings) ||
        type == typeof(ApizrSettings) ||
        type == typeof(CodeGeneratorSettings);

    private static string GetJsonName(PropertyInfo property) =>
        property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ??
        JsonNamingPolicy.CamelCase.ConvertName(property.Name);
}
