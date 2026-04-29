using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.Regression;

/// <summary>
/// Regression tests for Issue #1051: XmlDocumentationGenerator.DecodeJsonEscapedText mishandles malformed \u sequences
/// Validates that malformed Unicode escape sequences (e.g., \uZZZZ) are handled correctly without consuming extra characters
/// </summary>
public class MalformedUnicodeEscapeTests
{
    [Test]
    public void DecodeJsonEscapedText_Handles_Valid_Unicode_Escape()
    {
        // Valid: \u0048 = 'H'
        string input = @"\u0048\u0065\u006C\u006C\u006F"; // "Hello"
        string result = XmlDocumentationGenerator.DecodeJsonEscapedText(input);

        result.Should().Be("Hello");
    }

    [Test]
    public void DecodeJsonEscapedText_Handles_Malformed_Unicode_Escape_NonHex()
    {
        // Malformed: \uZZZZ (Z is not valid hex)
        string input = @"\uZZZZ";
        string result = XmlDocumentationGenerator.DecodeJsonEscapedText(input);

        // Should preserve the malformed escape sequence as-is
        result.Should().Contain(@"\uZZZZ");
    }

    [Test]
    public void DecodeJsonEscapedText_Handles_Malformed_Unicode_Escape_Mixed()
    {
        // Malformed: \uZZ12 (first two chars are non-hex)
        string input = @"\uZZ12";
        string result = XmlDocumentationGenerator.DecodeJsonEscapedText(input);

        result.Should().Contain(@"\uZZ12");
    }

    [Test]
    public void DecodeJsonEscapedText_Does_Not_Misread_After_Malformed_Unicode()
    {
        // Malformed \uZZZZ followed by valid escape: \u0041 = 'A'
        // The bug was that after failing to parse \uZZZZ, it would reread those chars
        string input = @"\uZZZZ\u0041";
        string result = XmlDocumentationGenerator.DecodeJsonEscapedText(input);

        // Should contain the malformed sequence and the decoded 'A'
        result.Should().Contain(@"\uZZZZ");
        result.Should().Contain("A");
    }

    [Test]
    public void DecodeJsonEscapedText_Handles_Malformed_Unicode_With_Trailing_Backslash()
    {
        // Malformed \uZZZZ followed by another escape
        string input = @"\uZZZZ\n";
        string result = XmlDocumentationGenerator.DecodeJsonEscapedText(input);

        // Should preserve malformed sequence and decode the \n
        result.Should().Contain(@"\uZZZZ");
        result.Should().Contain("\n");
    }

    [Test]
    public void DecodeJsonEscapedText_Handles_Unicode_Too_Short()
    {
        // Too short: \u00 (only 2 hex digits instead of 4)
        string input = @"\u00";
        string result = XmlDocumentationGenerator.DecodeJsonEscapedText(input);

        // Should not crash and should handle gracefully
        result.Should().NotBeNullOrEmpty();
    }

    [Test]
    public void DecodeJsonEscapedText_Advances_Correctly_After_Failed_Parse()
    {
        // Multiple malformed escapes in sequence
        string input = @"\uZZZZ\uABCD";
        string result = XmlDocumentationGenerator.DecodeJsonEscapedText(input);

        // First should be preserved as malformed, second should still decode correctly.
        result.Should().Contain(@"\uZZZZ");
        result.Should().Contain("\uABCD");
    }

    [Test]
    public void DecodeJsonEscapedText_Handles_Mixed_Valid_And_Malformed()
    {
        // Valid escape, malformed escape, valid escape
        string input = @"\u0041\uXXXX\u0042"; // A, malformed, B
        string result = XmlDocumentationGenerator.DecodeJsonEscapedText(input);

        result.Should().Contain("A");
        result.Should().Contain("B");
        result.Should().Contain(@"\uXXXX");
    }

    [Test]
    public void DecodeJsonEscapedText_Handles_Standard_Escapes_After_Malformed_Unicode()
    {
        // Malformed unicode followed by standard escapes
        string input = @"\uZZZZ\n\t\r";
        string result = XmlDocumentationGenerator.DecodeJsonEscapedText(input);

        // Should decode standard escapes correctly
        result.Should().Contain("\n");
        result.Should().Contain("\t");
        result.Should().Contain("\r");
        result.Should().Contain(@"\uZZZZ");
    }

    [Test]
    public void DecodeJsonEscapedText_Handles_Quoted_String_After_Malformed_Unicode()
    {
        // Malformed unicode followed by escaped quote
        string input = @"\uZZZZ\" + "\"";
        string result = XmlDocumentationGenerator.DecodeJsonEscapedText(input);

        result.Should().Contain(@"\uZZZZ");
        result.Should().Contain("\"");
    }

    [Test]
    public void SanitizeResponseDescription_Handles_Malformed_Unicode()
    {
        // Malformed unicode should be preserved and XML chars escaped
        string input = @"Error: code is \uZZZZ & invalid";
        string result = XmlDocumentationGenerator.SanitizeResponseDescription(input);

        // Malformed escape preserved, & escaped to &amp;
        result.Should().Contain(@"\uZZZZ");
        result.Should().Contain("&amp;");
    }

    [Test]
    public void DecodeJsonEscapedText_Preserves_Valid_Xml_Control_Chars()
    {
        // Valid XML control chars should be preserved
        string input = "tab:\t newline:\n carriage-return:\r";
        string result = XmlDocumentationGenerator.DecodeJsonEscapedText(input);

        result.Should().Contain("\t");
        result.Should().Contain("\n");
        result.Should().Contain("\r");
    }

    [Test]
    public void DecodeJsonEscapedText_Strips_Invalid_Control_Chars()
    {
        // Invalid XML control chars (e.g., 0x01) should be stripped
        string input = "valid\x01invalid"; // 0x01 is not valid XML
        string result = XmlDocumentationGenerator.DecodeJsonEscapedText(input);

        result.Should().Contain("valid");
        result.Should().Contain("invalid");
        result.Should().NotContain("\x01");
    }

    [Test]
    public void DecodeJsonEscapedText_Handles_Complex_Real_World_Scenario()
    {
        // Real-world scenario: mixed valid escapes, malformed unicode, and standard text
        string input = @"Filter must be: name=""test"" & id<100 & status\uZZZZ\u0041";
        string result = XmlDocumentationGenerator.DecodeJsonEscapedText(input);

        result.Should().Contain("name=\"test\""); // Escaped quote decoded
        result.Should().Contain(@"\uZZZZ"); // Malformed preserved
        result.Should().Contain("A"); // \u0041 decoded
    }
}
