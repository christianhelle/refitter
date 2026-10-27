using AwesomeAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class StringCharExtensionsTests
{
    [Test]
    [Arguments("IPetApi", 'I', true)]
    [Arguments("PetApi", 'I', false)]
    [Arguments("", 'I', false)]
    public void StartsWith_Char_Checks_First_Character(string value, char character, bool expected)
    {
        StringCharExtensions.StartsWith(value, character).Should().Be(expected);
    }

    [Test]
    [Arguments("int?", '?', true)]
    [Arguments("int", '?', false)]
    [Arguments("", '?', false)]
    public void EndsWith_Char_Checks_Last_Character(string value, char character, bool expected)
    {
        StringCharExtensions.EndsWith(value, character).Should().Be(expected);
    }
}
