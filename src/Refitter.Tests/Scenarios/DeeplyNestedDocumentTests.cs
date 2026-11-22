using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// A deeply nested document makes the recursive YAML reader overflow the stack, which cannot be caught and
/// ends the process. The reference guard must reject such documents before any parser sees them. The depths
/// used here stay modest: the unfixed code parses them without trouble, so these tests fail without the
/// fix by not throwing, instead of crashing the test host.
/// </summary>
[TestClass]
public class DeeplyNestedDocumentTests
{
    private const int TooDeep = DocumentNestingGuard.MaxNestingDepth + 44;

    private static string JsonSpec(int depth) =>
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
        "\"x-deep\":" + new string('[', depth) + new string(']', depth) + "}";

    private static string YamlSpec(int depth) =>
        "openapi: 3.0.0\ninfo:\n  title: t\n  version: '1'\npaths: {}\nx-deep: " +
        new string('[', depth) + new string(']', depth) + "\n";

    private static string YamlBlockSequenceSpec(int depth) =>
        "openapi: 3.0.0\ninfo:\n  title: t\n  version: '1'\npaths: {}\nx-deep:\n" +
        string.Concat(Enumerable.Repeat("- ", depth)) + "x\n";

    private static string Write(string content, string extension)
    {
        string path = Path.Combine(Path.GetTempPath(), "refitter-" + Guid.NewGuid().ToString("N") + extension);
        File.WriteAllText(path, content);
        return path;
    }

    private static async Task<string> Generate(string path)
    {
        var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = path });
        return sut.Generate();
    }

    [TestMethod]
    [DataRow(".json")]
    [DataRow(".yaml")]
    public async Task Rejects_Document_Nested_Beyond_The_Limit(string extension)
    {
        string content = extension == ".json" ? JsonSpec(TooDeep) : YamlSpec(TooDeep);
        string path = Write(content, extension);

        var act = () => Generate(path);

        var exception = await act.Should().ThrowAsync<ReferenceResolutionException>();
        exception.Which.Message.Should().Contain(DocumentNestingGuard.MaxNestingDepth.ToString());
    }

    [TestMethod]
    public async Task Rejects_Deeply_Nested_Block_Sequence()
    {
        string path = Write(YamlBlockSequenceSpec(TooDeep), ".yaml");

        var act = () => Generate(path);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Rejects_Deeply_Nested_Referenced_File()
    {
        string root = Path.Combine(Path.GetTempPath(), "refitter-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "model.yaml"), "type: object\nx-deep: " +
            new string('[', TooDeep) + new string(']', TooDeep) + "\n");
        string main = Path.Combine(root, "main.yaml");
        await File.WriteAllTextAsync(main, "openapi: 3.0.0\ninfo:\n  title: t\n  version: '1'\npaths: {}\n" +
            "components:\n  schemas:\n    Pet:\n      $ref: './model.yaml'\n");

        var act = () => Generate(main);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public void Rejects_Deep_Yaml_When_Converting_To_Json()
    {
        var act = () => OpenApiDocumentReader.ConvertYamlToJson(YamlSpec(TooDeep));

        act.Should().Throw<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Rejects_Deep_Document_When_Parsing()
    {
        var act = () => OpenApiDocumentParser.ParseAsync(JsonSpec(TooDeep), Path.Combine(Path.GetTempPath(), "spec.json"), allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    [DataRow(".json")]
    [DataRow(".yaml")]
    public async Task Allows_Document_Nested_At_A_Normal_Depth(string extension)
    {
        string content = extension == ".json" ? JsonSpec(40) : YamlSpec(40);
        string path = Write(content, extension);

        string code = await Generate(path);

        code.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public void Allows_Document_Nested_Exactly_At_The_Limit()
    {
        var act = () => DocumentNestingGuard.EnsureWithinLimit(
            JsonSpec(DocumentNestingGuard.MaxNestingDepth - 1), "spec.json");

        act.Should().NotThrow();
    }

    [TestMethod]
    public void Extracts_References_From_Json_Nested_Beyond_The_Default_Reader_Depth()
    {
        // Newtonsoft's own limit of 64 would report invalid JSON and hand the document to the YAML reader
        var act = () => OpenApiDocumentReader.ExtractReferences(JsonSpec(150), "spec.json");

        act.Should().NotThrow();
    }

    [TestMethod]
    public void Rejects_Very_Deep_Json_Without_Falling_Back_To_The_Yaml_Reader()
    {
        // 20,000 levels overflow the YAML reader, so this passes only if the JSON path rejects it itself
        var act = () => OpenApiDocumentReader.ExtractReferences(JsonSpec(20_000), "spec.json");

        act.Should().Throw<ReferenceResolutionException>();
    }

    [TestMethod]
    public void Rejects_Very_Deep_Yaml_When_Extracting_References()
    {
        var act = () => OpenApiDocumentReader.ExtractReferences(YamlSpec(20_000), "spec.yaml");

        act.Should().Throw<ReferenceResolutionException>();
    }
}
