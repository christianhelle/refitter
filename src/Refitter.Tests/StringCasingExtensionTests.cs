using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class StringCasingExtensionTests
{
    [Test]
    [Arguments("some-string", "SomeString")]
    public void CanConvertToPascalCase(string input, string expected)
        => input.ConvertKebabCaseToPascalCase().Should().Be(expected);

    [Test]
    [Arguments("some-string", "someString")]
    public void CanConvertToCamelCase(string input, string expected)
        => input.ConvertKebabCaseToCamelCase().Should().Be(expected);

    [Test]
    [Arguments("abcd", "Abcd")]
    public void CanCaptilalizeFirstLetter(string input, string expected)
        => input.CapitalizeFirstCharacter().Should().Be(expected);

    [Test]
    [Arguments("", "")]
    public void CaptilalizeFirstLetterHandlesEmptyStrings(string input, string expected)
        => input.CapitalizeFirstCharacter().Should().Be(expected);

    [Test]
    [Arguments("foo/bar", "fooBar")]
    public void CanConvertRouteToCamelCase(string input, string expected)
        => input.ConvertRouteToCamelCase().Should().Be(expected);

    [Test]
    [Arguments("foo bar", "FooBar")]
    public void CanConvertSpacesToPascalCase(string input, string expected)
        => input.ConvertSpacesToPascalCase().Should().Be(expected);

    [Test]
    [Arguments("foo:bar", "FooBar")]
    public void CanConvertColonsToPascalCase(string input, string expected)
        => input.ConvertColonsToPascalCase().Should().Be(expected);

    [Test]
    [Arguments("foo_bar", "FooBar")]
    public void CanConvertSnakeCaseToPascalCase(string input, string expected)
        => input.ConvertSnakeCaseToPascalCase().Should().Be(expected);
}
