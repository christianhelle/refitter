using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class StringCharExtensionsTests
{
    [TestMethod]
    [DataRow("IPetApi", 'I', true)]
    [DataRow("PetApi", 'I', false)]
    [DataRow("PetIApi", 'I', false)]
    [DataRow("", 'I', false)]
    public void StartsWith_Char_Checks_First_Character(string value, char character, bool expected)
    {
        StringCharExtensions.StartsWith(value, character).Should().Be(expected);
    }

    [TestMethod]
    [DataRow("int?", '?', true)]
    [DataRow("int", '?', false)]
    [DataRow("int?Value", '?', false)]
    [DataRow("", '?', false)]
    public void EndsWith_Char_Checks_Last_Character(string value, char character, bool expected)
    {
        StringCharExtensions.EndsWith(value, character).Should().Be(expected);
    }
}
