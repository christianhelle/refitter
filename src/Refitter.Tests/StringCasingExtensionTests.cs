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
}