using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using System.Reflection;
using AwesomeAssertions;
using Spectre.Console;

namespace Refitter.Tests;

[TestClass]
public class SettingsDescriptionTests
{
    // Spectre.Console renders option descriptions as markup, so an unescaped [Attribute]
    // in a description makes --help fail with "Could not find color or style"
    [TestMethod]
    public void Option_Descriptions_Are_Valid_Markup()
    {
        var descriptions = typeof(Settings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.GetCustomAttribute<DescriptionAttribute>()?.Description)
            .Where(description => description is not null)
            .ToList();

        descriptions.Should().NotBeEmpty();
        foreach (var description in descriptions)
        {
            var parse = () => new Markup(description!);
            parse.Should().NotThrow(description);
        }
    }
}
