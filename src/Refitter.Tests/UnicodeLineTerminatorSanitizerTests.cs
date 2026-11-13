using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class UnicodeLineTerminatorSanitizerTests
{
    [TestMethod]
    [DataRow(0x0085)]
    [DataRow(0x2028)]
    [DataRow(0x2029)]
    public void Sanitize_Replaces_Unicode_Line_Terminator_With_Space(int terminator)
    {
        string code = "/// a" + (char)terminator + "b";
        UnicodeLineTerminatorSanitizer.Sanitize(code).Should().Be("/// a b");
    }

    [TestMethod]
    public void Sanitize_Keeps_CR_And_LF()
    {
        string code = "/// a\r\n/// b\n";
        UnicodeLineTerminatorSanitizer.Sanitize(code).Should().BeSameAs(code);
    }
}
