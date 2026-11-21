using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Regression coverage for GHSA-wp69-6hgq-9cc3: a <c>$ref</c> inside a referenced file must be validated
/// against the same location the resolver reads, and the resolver must never read a file outside the input
/// document's directory.
/// </summary>
[TestClass]
public class NestedYamlReferenceBaseTests
{
    private const string OutsideMarker = "OutsideMarkerProperty";
    private const string DecoyMarker = "DecoyMarkerProperty";

    // Three directory levels separate the child file from the input root, so the same relative reference
    // names an in-tree decoy when read relative to the child, and a file outside the root when read
    // relative to the root
    private const string NestedReference = "../../../outside/secret";

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Never_Reads_Outside_File_For_Nested_Reference(bool yaml)
    {
        string ext = yaml ? ".yaml" : ".json";
        (string main, _) = await CreateLayout(yaml, NestedReference + ext);

        string? code = null;
        Exception? failure = null;
        try
        {
            code = await GenerateCode(main);
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        // Either the reference is rejected, or it resolves to the in-tree decoy; the outside file is never used
        if (failure == null)
        {
            code.Should().NotContain(OutsideMarker);
            code.Should().Contain(DecoyMarker);
        }
    }

    private static async Task<(string Main, string Root)> CreateLayout(bool yaml, string nestedReference)
    {
        string ext = yaml ? ".yaml" : ".json";
        string baseDirectory = Path.Combine(
            Path.GetTempPath(), "refitter-" + Guid.NewGuid().ToString("N"), "x", "y", "z");
        string root = Path.Combine(baseDirectory, "root");
        string childDirectory = Path.Combine(root, "sub", "deep", "deeper");
        Directory.CreateDirectory(childDirectory);

        // The file the resolver would read when it resolves relative to the root
        string outside = Path.GetFullPath(Path.Combine(root, "..", "..", "..", "outside"));
        Directory.CreateDirectory(outside);
        await File.WriteAllTextAsync(Path.Combine(outside, "secret" + ext), ObjectSchema(OutsideMarker, yaml));

        // The decoy the guard validates when it resolves relative to the child file
        string decoyDirectory = Path.Combine(root, "outside");
        Directory.CreateDirectory(decoyDirectory);
        await File.WriteAllTextAsync(Path.Combine(decoyDirectory, "secret" + ext), ObjectSchema(DecoyMarker, yaml));

        string components = yaml
            ? "components:\n  schemas:\n    Pet:\n      type: object\n      properties:\n        owner:\n          $ref: '" +
              nestedReference + "'\n"
            : "{\"components\":{\"schemas\":{\"Pet\":{\"type\":\"object\",\"properties\":{\"owner\":{\"$ref\":\"" +
              nestedReference + "\"}}}}}}";
        await File.WriteAllTextAsync(Path.Combine(childDirectory, "components" + ext), components);

        string target = "./sub/deep/deeper/components" + ext + "#/components/schemas/Pet";
        string spec = yaml
            ? "openapi: 3.0.0\ninfo:\n  title: t\n  version: '1'\npaths:\n  /pets:\n    get:\n      operationId: getPets\n" +
              "      responses:\n        '200':\n          description: ok\n          content:\n" +
              "            application/json:\n              schema:\n                $ref: '" + target + "'\n"
            : "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{\"/pets\":{\"get\":" +
              "{\"operationId\":\"getPets\",\"responses\":{\"200\":{\"description\":\"ok\",\"content\":" +
              "{\"application/json\":{\"schema\":{\"$ref\":\"" + target + "\"}}}}}}}}}";
        string main = Path.Combine(root, "main" + ext);
        await File.WriteAllTextAsync(main, spec);
        return (main, root);
    }

    private static string ObjectSchema(string property, bool yaml) =>
        yaml
            ? "type: object\nproperties:\n  " + property + ":\n    type: string\n"
            : "{\"type\":\"object\",\"properties\":{\"" + property + "\":{\"type\":\"string\"}}}";

    private static async Task<string> GenerateCode(string swaggerFile)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
