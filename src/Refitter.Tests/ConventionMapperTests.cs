using System.Reflection;
using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class ConventionMapperTests
{
    [TestMethod]
    public void Cli_Options_Named_Like_Settings_Have_The_Same_Type()
    {
        // ConventionMapper skips a same-named property with another type, so the option would silently be ignored
        var settingsProperties = typeof(RefitGeneratorSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanWrite)
            .ToDictionary(property => property.Name);

        var mismatches = typeof(Settings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead &&
                               settingsProperties.TryGetValue(property.Name, out var target) &&
                               target.PropertyType != property.PropertyType)
            .Select(property => $"{property.Name}: {property.PropertyType.Name} -> {settingsProperties[property.Name].PropertyType.Name}");

        mismatches.Should().BeEmpty();
    }
}
