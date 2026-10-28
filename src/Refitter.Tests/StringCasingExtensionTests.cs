using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;


[TestClass]
public class StringCasingExtensionTests
{
    [TestMethod]
    [DataRow("some-string", "SomeString")]
    public void CanConvertToPascalCase(string input, string expected)
        => input.ConvertKebabCaseToPascalCase().Should().Be(expected);

    [TestMethod]
    [DataRow("abcd", "Abcd")]
    public void CanCaptilalizeFirstLetter(string input, string expected)
        => input.CapitalizeFirstCharacter().Should().Be(expected);

    [TestMethod]
    [DataRow("", "")]
    public void CaptilalizeFirstLetterHandlesEmptyStrings(string input, string expected)
        => input.CapitalizeFirstCharacter().Should().Be(expected);

    [TestMethod]
    [DataRow("foo/bar", "fooBar")]
    public void CanConvertRouteToCamelCase(string input, string expected)
        => input.ConvertRouteToCamelCase().Should().Be(expected);

    [TestMethod]
    [DataRow("foo bar", "FooBar")]
    public void CanConvertSpacesToPascalCase(string input, string expected)
        => input.ConvertSpacesToPascalCase().Should().Be(expected);

    [TestMethod]
    [DataRow("foo:bar", "FooBar")]
    public void CanConvertColonsToPascalCase(string input, string expected)
        => input.ConvertColonsToPascalCase().Should().Be(expected);

    [TestMethod]
    [DataRow("foo_bar", "FooBar")]
    public void CanConvertSnakeCaseToPascalCase(string input, string expected)
        => input.ConvertSnakeCaseToPascalCase().Should().Be(expected);
}
