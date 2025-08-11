using FluentAssertions;
using Refitter.Core;
using Xunit;

namespace Refitter.Tests;

public class StringCasingExtensionTests
{
    [Theory]
    [InlineData("some-string", "SomeString")]
    public void CanConvertToPascalCase(string input, string expected)
        => input.ConvertKebabCaseToPascalCase().Should().Be(expected);

    [Theory]
    [InlineData("some-string", "someString")]
    public void CanConvertToCamelCase(string input, string expected)
        => input.ConvertKebabCaseToCamelCase().Should().Be(expected);

    [Theory]
    [InlineData("abcd", "Abcd")]
    public void CanCaptilalizeFirstLetter(string input, string expected)
        => input.CapitalizeFirstCharacter().Should().Be(expected);

    [Theory]
    [InlineData("", "")]
    public void CaptilalizeFirstLetterHandlesEmptyStrings(string input, string expected)
        => input.CapitalizeFirstCharacter().Should().Be(expected);

    [Theory]
    [InlineData("foo/bar", "fooBar")]
    public void CanConvertRouteToCamelCase(string input, string expected)
        => input.ConvertRouteToCamelCase().Should().Be(expected);

    [Theory]
    [InlineData("foo bar", "FooBar")]
    public void CanConvertSpacesToPascalCase(string input, string expected)
        => input.ConvertSpacesToPascalCase().Should().Be(expected);

    [Theory]
    [InlineData("foo:bar", "FooBar")]
    public void CanConvertColonsToPascalCase(string input, string expected)
        => input.ConvertColonsToPascalCase().Should().Be(expected);

    [Theory]
    [InlineData("foo_bar", "FooBar")]
    public void CanConvertSnakeCaseToPascalCase(string input, string expected)
        => input.ConvertSnakeCaseToPascalCase().Should().Be(expected);
}