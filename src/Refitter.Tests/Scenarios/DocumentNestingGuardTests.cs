using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Scenarios;

[TestClass]
public class DocumentNestingGuardTests
{
    [TestMethod]
    [DataRow("", 0)]
    [DataRow("{}", 1)]
    [DataRow("[]", 1)]
    [DataRow("{\"a\":[1,{\"b\":[]}]}", 4)]
    [DataRow("[[[]]] [[]]", 3)]
    [DataRow("{\"a\":{}, \"b\":{}}", 2)]
    [DataRow("a: [1, [2, [3]]]", 3)]
    public void Measures_Flow_Nesting(string content, int expected) =>
        DocumentNestingGuard.MeasureMaxDepth(content).Should().Be(expected);

    [TestMethod]
    [DataRow("{\"a\":\"[[[[[[\"}", 1)]
    [DataRow("{\"a\":\"}}}}{{{{\"}", 1)]
    [DataRow("a: '[[[[[['", 0)]
    [DataRow("a: [ '[[[[', \"{{{{\" ]", 1)]
    [DataRow("{'a': '[[[[' }", 1)]
    public void Ignores_Brackets_Inside_Quoted_Strings(string content, int expected) =>
        DocumentNestingGuard.MeasureMaxDepth(content).Should().Be(expected);

    [TestMethod]
    public void Handles_Escaped_Quotes_In_Double_Quoted_Strings()
    {
        // The escaped quote does not end the string, so the brackets after it are still text
        const string content = "{\"a\":\"x\\\"[[[[\"}";

        DocumentNestingGuard.MeasureMaxDepth(content).Should().Be(1);
    }

    [TestMethod]
    public void Handles_Escaped_Backslash_Before_Closing_Quote()
    {
        // The backslash is escaped, so the quote closes the string and the brackets are structure
        const string content = "{\"a\":\"x\\\\\",\"b\":[[[]]]}";

        DocumentNestingGuard.MeasureMaxDepth(content).Should().Be(4);
    }

    [TestMethod]
    public void Handles_Doubled_Quote_In_Single_Quoted_Strings()
    {
        DocumentNestingGuard.MeasureMaxDepth("a: ['it''s [[[[', [x]]").Should().Be(2);
    }

    [TestMethod]
    public void Apostrophe_In_A_Plain_Scalar_Does_Not_Hide_Following_Nesting()
    {
        // A plain scalar may contain quote characters. Treating one as a string opener would let a
        // document hide a deep flow collection from the scan
        DocumentNestingGuard.MeasureMaxDepth("[a - 'y, [[[[ 'z]").Should().BeGreaterThanOrEqualTo(5);
        DocumentNestingGuard.MeasureMaxDepth("[a:\"b, [[[[ \"]").Should().BeGreaterThanOrEqualTo(5);
    }

    [TestMethod]
    public void A_Quote_Does_Not_Span_Lines()
    {
        // An unterminated quote must not swallow the following lines
        DocumentNestingGuard.MeasureMaxDepth("a: 'oops\nb: [[[]]]\nc: 'x'").Should().Be(3);
    }

    [TestMethod]
    public void Measures_Inline_Block_Sequence_Indicators()
    {
        DocumentNestingGuard.MeasureMaxDepth("x:\n- - - - a\n").Should().Be(4);
    }

    [TestMethod]
    public void Does_Not_Count_Negative_Numbers_Or_Dashes_In_Text()
    {
        DocumentNestingGuard.MeasureMaxDepth("a:\n-1\n- text - more\n").Should().Be(1);
    }

    [TestMethod]
    public void Ignores_Indentation_Of_Pretty_Printed_Json()
    {
        string indent = new string(' ', 600);

        DocumentNestingGuard.MeasureMaxDepth("{\n" + indent + "\"a\": 1\n}").Should().Be(1);
    }

    [TestMethod]
    public void Unbalanced_Closers_Do_Not_Go_Negative()
    {
        DocumentNestingGuard.MeasureMaxDepth("]]]] [[").Should().Be(2);
    }

    [TestMethod]
    public void Throws_Reference_Resolution_Exception_Stating_The_Limit()
    {
        string content = new string('[', 11) + new string(']', 11);

        var act = () => DocumentNestingGuard.EnsureWithinLimit(content, "spec.json", maxDepth: 10);

        act.Should().Throw<ReferenceResolutionException>()
            .Which.Message.Should().Contain("10").And.Contain("spec.json");
    }

    [TestMethod]
    public void Accepts_Document_At_The_Limit()
    {
        string content = new string('[', 10) + new string(']', 10);

        var act = () => DocumentNestingGuard.EnsureWithinLimit(content, "spec.json", maxDepth: 10);

        act.Should().NotThrow();
    }
}
