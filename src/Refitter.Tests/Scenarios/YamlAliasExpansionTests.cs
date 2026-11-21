using System.Diagnostics;
using System.Text;
using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// YAML aliases share one node, but serializing the parsed document as JSON writes the node out again for
/// every alias. A few hundred bytes of nested aliases ("billion laughs") therefore expand to gigabytes. The
/// guard must measure the expanded size, which is cheap because the nodes are shared, and reject such
/// documents before anything serializes them. The tests use a small budget so no test ever expands a bomb.
/// </summary>
[TestClass]
public class YamlAliasExpansionTests
{
    private const long SmallBudget = 1_000;
    private const int SmallAmplification = 20;

    // Nine references to the previous level, so the expanded size is about 9^levels nodes
    private static string Bomb(int levels)
    {
        var yaml = new StringBuilder("lol0: &a0 \"lol\"\n");
        for (var level = 1; level <= levels; level++)
        {
            var previous = "*a" + (level - 1);
            yaml.Append($"lol{level}: &a{level} [{string.Join(",", Enumerable.Repeat(previous, 9))}]\n");
        }

        return yaml.ToString();
    }

    private static string Spec(string extra) =>
        "openapi: 3.0.0\ninfo:\n  title: t\n  version: '1'\npaths: {}\n" + extra;

    [TestMethod]
    public void Rejects_Alias_Bomb_Above_The_Budget()
    {
        var act = () => ReferenceGuard.ExtractReferences(Bomb(5), "bomb.yaml", SmallBudget, SmallAmplification);

        act.Should().Throw<ReferenceResolutionException>()
            .Which.Message.Should().Contain("bomb.yaml").And.Contain(SmallBudget.ToString());
    }

    [TestMethod]
    public void Rejects_Alias_Bomb_When_Converting_To_Json()
    {
        var act = () => OpenApiDocumentParser.ConvertYamlToJson(Bomb(5), SmallBudget, SmallAmplification);

        act.Should().Throw<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Rejects_The_Default_Size_Alias_Bomb_Before_It_Is_Expanded()
    {
        // Seven levels expand to several million nodes and gigabytes of JSON if anything serializes them
        string path = Path.Combine(Path.GetTempPath(), "refitter-" + Guid.NewGuid().ToString("N") + ".yaml");
        await File.WriteAllTextAsync(path, Spec(Bomb(7)));

        var act = async () =>
        {
            var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = path });
            sut.Generate();
        };

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public void Allows_Aliases_Used_Normally()
    {
        const string yaml = @"openapi: 3.0.0
info:
  title: t
  version: '1'
paths: {}
components:
  schemas:
    Base: &base
      type: object
      properties:
        id: {type: string}
    A: *base
    B: *base
    C: *base
";

        var references = ReferenceGuard.ExtractReferences(yaml, "spec.yaml", SmallBudget, SmallAmplification);
        var json = OpenApiDocumentParser.ConvertYamlToJson(yaml, SmallBudget, SmallAmplification);

        references.Should().BeEmpty();
        json.Should().Contain("\"C\"");
    }

    [TestMethod]
    public async Task Generates_Code_For_A_Document_That_Uses_Aliases()
    {
        const string yaml = @"openapi: 3.0.0
info:
  title: t
  version: '1'
paths:
  /pets:
    get:
      operationId: GetPets
      responses: &ok
        '200':
          description: ok
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/A'
  /other:
    get:
      operationId: GetOther
      responses: *ok
components:
  schemas:
    Base: &base
      type: object
      properties:
        id: {type: string}
    A: *base
";
        string path = Path.Combine(Path.GetTempPath(), "refitter-" + Guid.NewGuid().ToString("N") + ".yaml");
        await File.WriteAllTextAsync(path, yaml);

        var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = path });

        sut.Generate().Should().Contain("GetOther");
    }

    [TestMethod]
    public void Computes_The_Expanded_Size_Of_Shared_Nodes()
    {
        var shared = new List<object> { "x", "y" };
        var root = new List<object> { shared, shared, shared };

        // The root, three references to the shared list, and two scalars in each of them
        YamlAliasExpansionGuard.MeasureExpandedNodes(root).Should().Be(1 + 3 * (1 + 2));
    }

    [TestMethod]
    public void Measures_A_Very_Deep_Alias_Chain_In_Linear_Time()
    {
        // Each level refers to the previous one twice, so the expanded size is 2^100000 while the document
        // has 100000 distinct nodes. Without memoization this would never finish
        const int levels = 100_000;
        var yaml = new StringBuilder("l0: &a0 [x]\n");
        for (var level = 1; level <= levels; level++)
            yaml.Append($"l{level}: &a{level} [*a{level - 1}, *a{level - 1}]\n");

        var stopwatch = Stopwatch.StartNew();
        var act = () => ReferenceGuard.ExtractReferences(yaml.ToString(), "chain.yaml");
        act.Should().Throw<ReferenceResolutionException>();
        stopwatch.Stop();

        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30));
    }

    [TestMethod]
    public void Large_Documents_Without_Aliases_Are_Not_Rejected_By_Size_Alone()
    {
        var yaml = new StringBuilder();
        for (var i = 0; i < 5_000; i++)
            yaml.Append($"k{i}: [a, b, c, d]\n");

        // 5000 sequences of 4 scalars is far above the budget, but nothing is amplified
        var act = () => ReferenceGuard.ExtractReferences(yaml.ToString(), "big.yaml", SmallBudget, SmallAmplification);

        act.Should().NotThrow();
    }

    [TestMethod]
    public void Rejects_An_Alias_That_Refers_To_Its_Own_Ancestor()
    {
        var parent = new List<object>();
        parent.Add(parent);

        var act = () => YamlAliasExpansionGuard.MeasureExpandedNodes(parent);

        act.Should().Throw<ReferenceResolutionException>();
    }
}
