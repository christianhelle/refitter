using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Regression coverage for GHSA-r4f2-382w-frqc: a symbolic link inside the input directory must not let a
/// local <c>$ref</c> read a file outside it. All links below the input directory are rejected.
/// </summary>
[TestClass]
public class SymlinkReferenceTests
{
    private const string Schema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}";

    private static string Spec(string target) =>
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
        "\"components\":{\"schemas\":{\"Pet\":{\"$ref\":\"" + target + "\"}}}}";

    [TestMethod]
    public async Task Blocks_File_Link_To_Out_Of_Tree_File()
    {
        (string root, string outside) = CreateDirectories();
        await File.WriteAllTextAsync(Path.Combine(outside, "secret.json"), Schema);
        CreateFileLink(Path.Combine(root, "link.json"), Path.Combine(outside, "secret.json"));
        string main = await WriteMain(root, "./link.json");

        var act = () => GenerateCode(main);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*symbolic link*");
    }

    [TestMethod]
    public async Task Blocks_File_Link_To_In_Tree_File()
    {
        (string root, _) = CreateDirectories();
        await File.WriteAllTextAsync(Path.Combine(root, "real.json"), Schema);
        CreateFileLink(Path.Combine(root, "link.json"), Path.Combine(root, "real.json"));
        string main = await WriteMain(root, "./link.json");

        var act = () => GenerateCode(main);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*symbolic link*");
    }

    [TestMethod]
    public async Task Blocks_Reference_Through_Directory_Link()
    {
        (string root, string outside) = CreateDirectories();
        await File.WriteAllTextAsync(Path.Combine(outside, "x.json"), Schema);
        CreateDirectoryLink(Path.Combine(root, "sub"), outside);
        string main = await WriteMain(root, "./sub/x.json");

        var act = () => GenerateCode(main);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*symbolic link*");
    }

    [TestMethod]
    public async Task Allows_Regular_In_Tree_File()
    {
        (string root, _) = CreateDirectories();
        await File.WriteAllTextAsync(Path.Combine(root, "real.json"), Schema);
        string main = await WriteMain(root, "./real.json");

        string code = await GenerateCode(main);

        code.Should().NotBeNullOrWhiteSpace();
    }




    [TestMethod]
    public async Task Allows_Reference_Back_To_Input_Document_That_Is_A_Link()
    {
        (string root, string outside) = CreateDirectories();
        string real = Path.Combine(outside, "real.json");
        await File.WriteAllTextAsync(
            real,
            "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
            "\"components\":{\"schemas\":{\"Pet\":{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}," +
            "\"Owner\":{\"type\":\"object\",\"properties\":{\"pet\":{\"$ref\":\"./main.json#/components/schemas/Pet\"}}}}}}");
        string main = Path.Combine(root, "main.json");
        CreateFileLink(main, real);

        string code = await GenerateCode(main);

        code.Should().Contain("class Owner");
    }

    [TestMethod]
    public async Task Still_Blocks_Other_Links_When_Input_Document_Is_A_Link()
    {
        (string root, string outside) = CreateDirectories();
        string real = Path.Combine(outside, "real.json");
        await File.WriteAllTextAsync(real, Spec("./other.json"));
        await File.WriteAllTextAsync(Path.Combine(outside, "secret.json"), Schema);
        CreateFileLink(Path.Combine(root, "other.json"), Path.Combine(outside, "secret.json"));
        string main = Path.Combine(root, "main.json");
        CreateFileLink(main, real);

        var act = () => GenerateCode(main);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*symbolic link*");
    }

    private static (string Root, string Outside) CreateDirectories()
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), "refitter-" + Guid.NewGuid().ToString("N"));
        string root = Path.Combine(baseDirectory, "root");
        string outside = Path.Combine(baseDirectory, "outside");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(outside);
        return (root, outside);
    }

    private static async Task<string> WriteMain(string root, string target)
    {
        string main = Path.Combine(root, "main.json");
        await File.WriteAllTextAsync(main, Spec(target));
        return main;
    }

    private static void CreateFileLink(string link, string target)
    {
        try
        {
            File.CreateSymbolicLink(link, target);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            Assert.Inconclusive("Symbolic links cannot be created here: " + ex.Message);
        }
    }

    private static void CreateDirectoryLink(string link, string target)
    {
        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            Assert.Inconclusive("Symbolic links cannot be created here: " + ex.Message);
        }
    }

    private static async Task<string> GenerateCode(string swaggerFile)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
