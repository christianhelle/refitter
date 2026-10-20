using System.ComponentModel;
using System.Reflection;
using AwesomeAssertions;
using Spectre.Console;
using TUnit.Core;

namespace Refitter.Tests;

public class SettingsDescriptionTests
{
    // Spectre.Console renders option descriptions as markup, so an unescaped [Attribute]
    // in a description makes --help fail with "Could not find color or style"
    [Test]
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
