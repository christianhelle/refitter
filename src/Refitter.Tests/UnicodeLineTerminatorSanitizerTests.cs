using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class UnicodeLineTerminatorSanitizerTests
{
    private const char T = (char)0x2028;

    [TestMethod]
    [DataRow(0x0085)]
    [DataRow(0x2028)]
    [DataRow(0x2029)]
    public void Sanitize_Replaces_Unicode_Line_Terminator_In_Line_Comment(int terminator)
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

    [TestMethod]
    [DataRow("x = @\"a<T>b\";")]
    [DataRow("x = \"a<T>b\";")]
    [DataRow("x = \"http://a<T>\";")]
    [DataRow("x = @\"a\"\" // <T>\";")]
    [DataRow("x = \"a\\\" // <T>\";")]
    [DataRow("x = $@\"a\"\" // <T>\";")]
    [DataRow("x = @$\"a\"\" // <T>\";")]
    [DataRow("x = \"\"\"a \"\" // <T>\"\"\";")]
    [DataRow("/* a<T>b */")]
    public void Sanitize_Keeps_Unicode_Line_Terminator_Outside_Line_Comments(string template)
    {
        string code = template.Replace("<T>", T.ToString());
        UnicodeLineTerminatorSanitizer.Sanitize(code).Should().Be(code);
    }

    [TestMethod]
    [DataRow("x = \"a\"; // c<T>d", "x = \"a\"; // c d")]
    [DataRow("x = @\"a\"\"b\"; // c<T>d", "x = @\"a\"\"b\"; // c d")]
    [DataRow("c = '\"'; // a<T>b", "c = '\"'; // a b")]
    [DataRow("c = '\\''; // a<T>b", "c = '\\''; // a b")]
    [DataRow("x = \"\"\"a<T>\"\"\"; // c<T>d", "x = \"\"\"a<T>\"\"\"; // c d")]
    [DataRow("x = \"\"; // c<T>d", "x = \"\"; // c d")]
    public void Sanitize_Replaces_Terminator_In_Comment_After_Literal(string template, string expected)
    {
        string code = template.Replace("<T>", T.ToString());
        UnicodeLineTerminatorSanitizer.Sanitize(code).Should().Be(expected.Replace("<T>", T.ToString()));
    }
}
