using System.Text;
using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// The nesting limit is measured with the YAML parser's own event stream, so every lexical rule of YAML
/// (comments, multiline quotes, block scalars, compact sequences, anchors, tags) applies. These tests
/// cover the inputs that fooled the earlier text scanner. They use depth 300: above the limit, but shallow
/// enough that a parser which does not reject it still cannot crash the test host.
/// </summary>
[TestClass]
public class DocumentNestingGuardTests
{
    private const int TooDeep = DocumentNestingGuard.MaxNestingDepth + 44;

    private static void ShouldBeRejected(string content, string source = "spec.yaml")
    {
        var act = () => DocumentNestingGuard.EnsureWithinLimit(content, source);

        act.Should().Throw<ReferenceResolutionException>()
            .Which.Message.Should()
            .Contain(DocumentNestingGuard.MaxNestingDepth.ToString()).And.Contain(source);
    }

    private static void ShouldBeAccepted(string content) =>
        ((Action)(() => DocumentNestingGuard.EnsureWithinLimit(content, "spec.yaml"))).Should().NotThrow();

    private static string Repeat(string text, int count)
    {
        var builder = new StringBuilder(text.Length * count);
        for (var i = 0; i < count; i++)
            builder.Append(text);
        return builder.ToString();
    }

    [TestMethod]
    public void Rejects_Deep_Flow_Sequences() =>
        ShouldBeRejected(Repeat("[", TooDeep) + Repeat("]", TooDeep));

    [TestMethod]
    public void Rejects_Deep_Flow_Mappings() =>
        ShouldBeRejected("x: " + Repeat("{a: ", TooDeep) + "1" + Repeat("}", TooDeep));

    [TestMethod]
    public void Rejects_Deep_Json() =>
        ShouldBeRejected("{\"x\":" + Repeat("[", TooDeep) + Repeat("]", TooDeep) + "}", "spec.json");

    [TestMethod]
    public void Rejects_Deep_Json_Objects() =>
        ShouldBeRejected(Repeat("{\"a\":", TooDeep) + "1" + Repeat("}", TooDeep), "spec.json");

    [TestMethod]
    public void Rejects_Deep_Inline_Compact_Sequences() =>
        ShouldBeRejected("x:\n" + Repeat("- ", TooDeep) + "x\n");

    [TestMethod]
    public void Rejects_Multiline_Quoted_Scalar_Closers_That_Hide_Openers()
    {
        // Each quoted scalar spans two lines and contains a ']' that is text, not structure
        ShouldBeRejected(Repeat("[ \"q\n]\" , ", TooDeep) + Repeat("]", TooDeep));
    }

    [TestMethod]
    public void Rejects_Comment_Closers_That_Hide_Openers()
    {
        // The ']' in each comment is text, not structure
        ShouldBeRejected(Repeat("[ # ]\n", TooDeep) + Repeat("]\n", TooDeep));
    }

    [TestMethod]
    public void Rejects_Indented_Compact_Sequences()
    {
        // Five indicators per line, each line indented deeper than the last
        var builder = new StringBuilder();
        for (var line = 0; line < TooDeep / 5 + 1; line++)
            builder.Append(' ', line * 10).Append("- - - - - \n");
        builder.Append(' ', (TooDeep / 5 + 1) * 10).Append("x\n");

        ShouldBeRejected(builder.ToString());
    }

    [TestMethod]
    public void Rejects_Block_Sequences_Nested_By_Indentation()
    {
        var builder = new StringBuilder();
        for (var level = 0; level < TooDeep; level++)
            builder.Append(' ', level * 2).Append("-\n");
        builder.Append(' ', TooDeep * 2).Append("x\n");

        ShouldBeRejected(builder.ToString());
    }

    [TestMethod]
    public void Adds_Inline_Block_Depth_To_Flow_Depth()
    {
        // 150 block levels and 150 flow levels are 300 levels together
        ShouldBeRejected(Repeat("- ", 150) + Repeat("[", 150) + Repeat("]", 150));
    }

    [TestMethod]
    [DataRow("&a ")]
    [DataRow("!t ")]
    [DataRow("&a !t ")]
    public void Rejects_Anchored_Or_Tagged_Quoted_Closers_That_Hide_Openers(string properties)
    {
        ShouldBeRejected(Repeat("[ " + properties + "\"]\" , ", TooDeep) + Repeat("]", TooDeep));
    }

    [TestMethod]
    public void Accepts_Brackets_Inside_A_Block_Scalar()
    {
        string content = "openapi: 3.0.0\ninfo:\n  title: t\n  version: '1'\n  description: |\n    " +
                         Repeat("[", TooDeep) + "\npaths: {}\n";

        ShouldBeAccepted(content);
    }

    [TestMethod]
    public void Accepts_Brackets_Inside_A_Folded_Scalar()
    {
        ShouldBeAccepted("description: >\n  " + Repeat("{[", TooDeep) + "\nother: 1\n");
    }

    [TestMethod]
    public void Accepts_Brackets_Inside_Quoted_Strings_And_Comments()
    {
        ShouldBeAccepted("a: \"" + Repeat("[", TooDeep) + "\"\nb: '" + Repeat("{", TooDeep) + "'\n" +
                         "c: 1 # " + Repeat("[", TooDeep) + "\n");
    }

    [TestMethod]
    public void Accepts_Brackets_Inside_Json_Strings() =>
        ShouldBeAccepted("{\"a\":\"" + Repeat("[", TooDeep) + "\"}");

    [TestMethod]
    public void Accepts_Document_At_The_Limit()
    {
        const int limit = DocumentNestingGuard.MaxNestingDepth;

        ShouldBeAccepted(Repeat("[", limit) + Repeat("]", limit));
        ShouldBeAccepted("{\"a\":" + Repeat("[", limit - 1) + Repeat("]", limit - 1) + "}");
    }

    [TestMethod]
    public void Rejects_Document_One_Beyond_The_Limit()
    {
        const int beyond = DocumentNestingGuard.MaxNestingDepth + 1;

        ShouldBeRejected(Repeat("[", beyond) + Repeat("]", beyond));
        ShouldBeRejected("{\"a\":" + Repeat("[", beyond - 1) + Repeat("]", beyond - 1) + "}", "spec.json");
    }

    [TestMethod]
    public void Rejects_Deep_Document_Without_Parsing_It_Fully()
    {
        // Stack safety: the event walk is iterative and stops at the limit, so even a document far deeper
        // than anything a recursive parser survives is rejected without exhausting the stack
        const int depth = 20_000;

        ShouldBeRejected(Repeat("[", depth) + Repeat("]", depth));
        ShouldBeRejected("x:\n" + Repeat("- ", depth) + "x\n");
        ShouldBeRejected("{\"x\":" + Repeat("[", depth) + Repeat("]", depth) + "}", "spec.json");
    }

    [TestMethod]
    public void Leaves_Syntax_Errors_To_The_Parser_That_Reports_Them()
    {
        ShouldBeAccepted("a: [unterminated");
        ShouldBeAccepted("a: b: c: d");
    }

    [TestMethod]
    public void Accepts_Json_With_Comments_Within_The_Limit() =>
        ShouldBeAccepted("// leading comment\n{\"a\": /* c */ [1, [2]]}");

    [TestMethod]
    public void Throws_Reference_Resolution_Exception_Naming_The_Source() =>
        ShouldBeRejected(Repeat("[", TooDeep) + Repeat("]", TooDeep), "https://example.com/spec.yaml");

    [TestMethod]
    public void All_Sample_Specifications_Are_Within_The_Limit()
    {
        string root = AppContext.BaseDirectory;
        while (root != null && !Directory.Exists(Path.Combine(root, "test", "OpenAPI")))
            root = Path.GetDirectoryName(root)!;
        root.Should().NotBeNull();

        var specs = Directory
            .EnumerateFiles(Path.Combine(root!, "test", "OpenAPI"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
            .ToList();
        specs.Should().NotBeEmpty();

        foreach (string spec in specs)
        {
            var act = () => DocumentNestingGuard.EnsureWithinLimit(File.ReadAllText(spec), spec);
            act.Should().NotThrow($"{spec} is a regular specification");
        }
    }
}
