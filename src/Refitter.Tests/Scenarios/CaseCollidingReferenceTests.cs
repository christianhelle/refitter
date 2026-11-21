using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Regression coverage for GHSA-25j7-xpj3-7jf9: on a case-sensitive filesystem, two referenced files whose
/// names differ only by case are different files, so the reference guard must inspect both of them.
/// </summary>
[TestClass]
public class CaseCollidingReferenceTests
{
    private const string Benign = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}";

    // Port 1 refuses connections, so a reference that slipped past the guard would surface as an
    // HTTP failure rather than a ReferenceResolutionException
    private const string Malicious = "{\"type\":\"object\",\"properties\":{\"x\":{\"$ref\":\"http://127.0.0.1:1/x.json\"}}}";

    private static string RootSpec(params string[] files)
    {
        string schemas = string.Join(
            ",",
            files.Select((f, i) => "\"S" + i + "\":{\"$ref\":\"./" + f + "\"}"));
        return "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
               "\"components\":{\"schemas\":{" + schemas + "}}}";
    }

    [TestMethod]
    public async Task Blocks_Remote_Reference_In_File_Differing_Only_By_Case()
    {
        string root = CreateTempDirectory();
        SkipIfCaseInsensitive(root);
        await File.WriteAllTextAsync(Path.Combine(root, "Model.json"), Benign);
        await File.WriteAllTextAsync(Path.Combine(root, "model.json"), Malicious);
        string main = Path.Combine(root, "main.json");
        await File.WriteAllTextAsync(main, RootSpec("Model.json", "model.json"));

        var act = () => GenerateCode(main);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*127.0.0.1:1*");
    }

    [TestMethod]
    public async Task Blocks_Remote_Reference_In_Single_Lowercase_File()
    {
        string root = CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(root, "model.json"), Malicious);
        string main = Path.Combine(root, "main.json");
        await File.WriteAllTextAsync(main, RootSpec("model.json"));

        var act = () => GenerateCode(main);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*127.0.0.1:1*");
    }

    [TestMethod]
    public async Task Allows_Single_Benign_File()
    {
        string root = CreateTempDirectory();
        await File.WriteAllTextAsync(Path.Combine(root, "Model.json"), Benign);
        string main = Path.Combine(root, "main.json");
        await File.WriteAllTextAsync(main, RootSpec("Model.json"));

        string code = await GenerateCode(main);

        code.Should().NotBeNullOrWhiteSpace();
    }

    private static void SkipIfCaseInsensitive(string directory)
    {
        string probe = Path.Combine(directory, "probe.txt");
        File.WriteAllText(probe, "x");
        if (File.Exists(Path.Combine(directory, "PROBE.TXT")))
            Assert.Inconclusive("The filesystem is case-insensitive, so the files would collide on disk.");
    }

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "refitter-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task<string> GenerateCode(string swaggerFile)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
