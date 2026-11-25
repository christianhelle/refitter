using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class ConsoleOutputSanitizerTests
{
    private const string Payload =
        "Content type 'application/json\r\n##vso[task.uploadfile]/work/workspace-data-a.txt\r\n' contains illegal characters";

    [TestMethod]
    public void SanitizeLine_CrlfPayload_Yields_Single_Line_Without_Pipeline_Command()
    {
        var result = ConsoleOutputSanitizer.SanitizeLine(Payload);

        result.Should().NotContain("\r");
        result.Should().NotContain("\n");
        result.Contains("##vso[", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        result.Should().Contain(@"application/json\r\n## vso[task.uploadfile]");
    }

    [TestMethod]
    public void SanitizeLine_Uppercase_Vso_Command_Is_Neutralized()
    {
        var result = ConsoleOutputSanitizer.SanitizeLine("x##VSO[task.complete]y");

        result.Should().Be("x## VSO[task.complete]y");
    }

    [TestMethod]
    public void SanitizeLine_Escapes_Control_Characters_And_Unicode_Line_Separators()
    {
        var result = ConsoleOutputSanitizer.SanitizeLine("a\u001b[31mb\0c\u0085d\u2028e\u2029f");

        result.Should().Be(@"a\u001b[31mb\u0000c\u0085d\u2028e\u2029f");
    }

    [TestMethod]
    public void SanitizeLine_Keeps_Tab()
    {
        var text = "a\tb";

        ConsoleOutputSanitizer.SanitizeLine(text).Should().BeSameAs(text);
    }

    [TestMethod]
    public void SanitizeLine_Neutralizes_Hash_Group_Command()
    {
        var result = ConsoleOutputSanitizer.SanitizeLine("##[group]Build");

        result.Should().Be("## [group]Build");
    }

    [TestMethod]
    public void SanitizeLine_Returns_Plain_Text_Unchanged()
    {
        const string text = "Unsupported OpenAPI version: 1.0";

        ConsoleOutputSanitizer.SanitizeLine(text).Should().BeSameAs(text);
    }

    [TestMethod]
    public void NeutralizeCommands_Keeps_Newlines_And_Removes_Vso_Command()
    {
        var input = "line1\r\n##vso[task.setvariable variable=x]y\nline3";

        var result = ConsoleOutputSanitizer.NeutralizeCommands(input);

        result.Should().Be("line1\r\n## vso[task.setvariable variable=x]y\nline3");
    }

    [TestMethod]
    public void NeutralizeCommands_Prefixes_Leading_Double_Colon_At_Line_Start()
    {
        var input = "ok\n  ::error::x\n::warning::y";

        var result = ConsoleOutputSanitizer.NeutralizeCommands(input);

        result.Should().Be("ok\n  : :error::x\n: :warning::y");
    }

    [TestMethod]
    public void NeutralizeCommands_Keeps_Double_Colon_Mid_Line()
    {
        var input = "Type::Member";

        ConsoleOutputSanitizer.NeutralizeCommands(input).Should().Be(input);
    }

    [TestMethod]
    public void NeutralizeCommands_Preserves_Line_Count_For_Mixed_Endings()
    {
        var input = "a\r\nb\nc\r\n##[error]d\n";

        var result = ConsoleOutputSanitizer.NeutralizeCommands(input);

        result.Split('\n').Length.Should().Be(input.Split('\n').Length);
        result.Should().Be("a\r\nb\nc\r\n## [error]d\n");
    }

    [TestMethod]
    public void NeutralizeCommands_Returns_Plain_Text_Unchanged()
    {
        const string text = "first\r\nsecond\nthird";

        ConsoleOutputSanitizer.NeutralizeCommands(text).Should().BeSameAs(text);
    }
}
