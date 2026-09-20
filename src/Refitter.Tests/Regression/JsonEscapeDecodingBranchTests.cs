using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Regression;

public class JsonEscapeDecodingBranchTests
{
    [Test]
    [Arguments(null)]
    [Arguments("")]
    public void DecodeJsonEscapedText_Returns_Input_For_Null_Or_Empty(string? input)
    {
        var result = XmlDocumentationGenerator.DecodeJsonEscapedText(input!);

        result.Should().Be(input);
    }

    [Test]
    public void DecodeJsonEscapedText_Decodes_Backslash_Escape()
    {
        var result = XmlDocumentationGenerator.DecodeJsonEscapedText(@"a\\b");

        result.Should().Be(@"a\b");
    }

    [Test]
    public void DecodeJsonEscapedText_Decodes_Forward_Slash_Escape()
    {
        var result = XmlDocumentationGenerator.DecodeJsonEscapedText(@"a\/b");

        result.Should().Be("a/b");
    }

    [Test]
    public void DecodeJsonEscapedText_Decodes_Backspace_Escape()
    {
        var result = XmlDocumentationGenerator.DecodeJsonEscapedText(@"a\bb");

        result.Should().Be("ab");
    }

    [Test]
    public void DecodeJsonEscapedText_Decodes_Form_Feed_Escape()
    {
        var result = XmlDocumentationGenerator.DecodeJsonEscapedText(@"a\fb");

        result.Should().Be("ab");
    }

    [Test]
    public void DecodeJsonEscapedText_Preserves_Unknown_Escape_Sequence()
    {
        var result = XmlDocumentationGenerator.DecodeJsonEscapedText(@"a\qb");

        result.Should().Be(@"a\qb");
    }
}
