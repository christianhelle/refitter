using FluentAssertions;
using Refitter.Core;
using Xunit;

namespace Refitter.Tests;

public class IdentifierUtilsTests
{
    [Fact]
    public void Counted_Returns_Original_Name_When_Not_In_Set()
    {
        var knownIdentifiers = new HashSet<string>();
        var result = IdentifierUtils.Counted(knownIdentifiers, "TestName");
        result.Should().Be("TestName");
    }

    [Fact]
    public void Counted_Returns_Name_With_Suffix_When_Not_In_Set()
    {
        var knownIdentifiers = new HashSet<string>();
        var result = IdentifierUtils.Counted(knownIdentifiers, "TestName", "Async");
        result.Should().Be("TestNameAsync");
    }

    [Fact]
    public void Counted_Returns_Name_With_Counter_When_Already_In_Set()
    {
        var knownIdentifiers = new HashSet<string> { "TestName" };
        var result = IdentifierUtils.Counted(knownIdentifiers, "TestName");
        result.Should().Be("TestName2");
    }

    [Fact]
    public void Counted_Returns_Name_With_Counter_And_Suffix_When_Already_In_Set()
    {
        var knownIdentifiers = new HashSet<string> { "TestNameAsync" };
        var result = IdentifierUtils.Counted(knownIdentifiers, "TestName", "Async");
        result.Should().Be("TestName2Async");
    }

    [Fact]
    public void Counted_Increments_Counter_When_Multiple_Conflicts()
    {
        var knownIdentifiers = new HashSet<string> { "TestName", "TestName2" };
        var result = IdentifierUtils.Counted(knownIdentifiers, "TestName");
        result.Should().Be("TestName3");
    }

    [Fact]
    public void Counted_With_Parent_Returns_Name_When_Not_In_Set()
    {
        var knownIdentifiers = new HashSet<string>();
        var result = IdentifierUtils.Counted(knownIdentifiers, "ChildName", "", "ParentName");
        result.Should().Be("ChildName");
    }

    [Fact]
    public void Counted_With_Parent_Returns_Name_With_Counter_When_Conflict()
    {
        var knownIdentifiers = new HashSet<string> { "ParentName.ChildName" };
        var result = IdentifierUtils.Counted(knownIdentifiers, "ChildName", "", "ParentName");
        result.Should().Be("ChildName2");
    }

    [Fact]
    public void Counted_With_Parent_And_Suffix_Returns_Correct_Name()
    {
        var knownIdentifiers = new HashSet<string> { "ParentName.ChildNameAsync" };
        var result = IdentifierUtils.Counted(knownIdentifiers, "ChildName", "Async", "ParentName");
        result.Should().Be("ChildName2Async");
    }

    [Fact]
    public void Sanitize_Removes_Spaces()
    {
        var result = "Test Name".Sanitize();
        result.Should().Be("TestName");
    }

    [Fact]
    public void Sanitize_Removes_Dashes()
    {
        var result = "Test-Name".Sanitize();
        result.Should().Be("TestName");
    }

    [Fact]
    public void Sanitize_Removes_Dots()
    {
        var result = "Test.Name".Sanitize();
        result.Should().Be("TestName");
    }

    [Fact]
    public void Sanitize_Removes_Special_Characters()
    {
        var result = "Test!@#$%^&*()Name".Sanitize();
        result.Should().Be("TestName");
    }

    [Fact]
    public void Sanitize_Removes_Quotes()
    {
        var result = "Test\"Name'".Sanitize();
        result.Should().Be("TestName");
    }

    [Fact]
    public void Sanitize_Removes_Newlines_And_Tabs()
    {
        var result = "Test\nName\t".Sanitize();
        result.Should().Be("TestName");
    }

    [Fact]
    public void Sanitize_Removes_Brackets_And_Braces()
    {
        var result = "Test[Name]{Value}(Param)".Sanitize();
        result.Should().Be("TestNameValueParam");
    }

    [Fact]
    public void Sanitize_Removes_Pipes_And_Slashes()
    {
        var result = "Test|Name/Value\\Item".Sanitize();
        result.Should().Be("TestNameValueItem");
    }

    [Fact]
    public void Sanitize_Removes_Colons_Semicolons_And_Commas()
    {
        var result = "Test:Name;Value,Item".Sanitize();
        result.Should().Be("TestNameValueItem");
    }

    [Fact]
    public void Sanitize_Adds_Underscore_Prefix_When_Starting_With_Number()
    {
        var result = "123Test".Sanitize();
        result.Should().Be("_123Test");
    }

    [Fact]
    public void Sanitize_Preserves_Underscore_Prefix()
    {
        var result = "_TestName".Sanitize();
        result.Should().Be("_TestName");
    }

    [Fact]
    public void Sanitize_Preserves_Valid_Identifier_Characters()
    {
        var result = "Test_Name123".Sanitize();
        result.Should().Be("Test_Name123");
    }

    [Fact]
    public void Sanitize_Handles_Multiple_Consecutive_Illegal_Characters()
    {
        var result = "Test   ---   Name".Sanitize();
        result.Should().Be("TestName");
    }

    [Fact]
    public void Sanitize_Adds_Underscore_When_Starting_With_Lowercase()
    {
        var result = "aTestName".Sanitize();
        result.Should().Be("aTestName");
    }

    [Fact]
    public void Sanitize_Removes_Special_Characters_Including_At_Symbol()
    {
        var result = "@TestName".Sanitize();
        result.Should().Be("_TestName");
    }

    [Fact]
    public void Sanitize_Trims_Trailing_Dashes()
    {
        var result = "TestName---".Sanitize();
        result.Should().Be("TestName");
    }

    [Fact]
    public void Sanitize_Complex_String_With_Multiple_Issues()
    {
        var result = "123-Test Name!@#Value".Sanitize();
        result.Should().Be("_123TestNameValue");
    }
}
