using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

/// <summary>
/// The parser resolves references from the document set only, so a file is parsed only if the reader read and
/// checked it.
/// </summary>
[TestClass]
public class DocumentSetResolutionTests
{
    private const string RootJson =
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
        "\"components\":{\"schemas\":{\"Pet\":{\"$ref\":\"./other.json\"}}}}";

    private static string Schema(string property) =>
        "{\"type\":\"object\",\"properties\":{\"" + property + "\":{\"type\":\"string\"}}}";

    private static DocumentSet Set(string folder, Dictionary<string, string> files) =>
        new(Path.Combine(folder, "main.json"), RootJson, files, new Dictionary<string, string>());

    [TestMethod]
    public async Task Serves_Referenced_Files_From_The_Set_Not_From_Disk()
    {
        var folder = TestDirectory.CreateFolder();
        var other = Path.Combine(folder, "other.json");
        await File.WriteAllTextAsync(other, Schema("onDisk"));

        var document = await OpenApiDocumentParser.ParseAsync(
            Set(folder, new Dictionary<string, string> { [other] = Schema("inSet") }));

        document.Components.Schemas["Pet"].ActualSchema.Properties.Should().ContainKey("inSet")
            .And.NotContainKey("onDisk");
    }

    [TestMethod]
    public async Task Rejects_A_File_On_Disk_That_Is_Not_In_The_Set()
    {
        var folder = TestDirectory.CreateFolder();
        await File.WriteAllTextAsync(Path.Combine(folder, "other.json"), Schema("onDisk"));

        var act = () => OpenApiDocumentParser.ParseAsync(Set(folder, new Dictionary<string, string>()));

        var thrown = await act.Should().ThrowAsync<Exception>();
        ReferenceResolutionException.FindIn(thrown.Which).Should().NotBeNull()
            .And.Subject.As<ReferenceResolutionException>().Message.Should().Contain("other.json");
    }

    [TestMethod]
    public async Task Reports_A_Missing_File_As_Not_Found()
    {
        var folder = TestDirectory.CreateFolder();

        var act = () => OpenApiDocumentParser.ParseAsync(Set(folder, new Dictionary<string, string>()));

        var thrown = await act.Should().ThrowAsync<Exception>();
        ReferenceResolutionException.FindIn(thrown.Which).Should().BeNull();
        thrown.Which.ToString().Should().Contain("Could not find file");
    }

    [TestMethod]
    public async Task Rejects_A_Url_That_Is_Not_In_The_Set()
    {
        var folder = TestDirectory.CreateFolder();
        var set = new DocumentSet(
            Path.Combine(folder, "main.json"),
            RootJson.Replace("./other.json", "http://127.0.0.1:1/other.json"),
            new Dictionary<string, string>(),
            new Dictionary<string, string>());

        var act = () => OpenApiDocumentParser.ParseAsync(set);

        var thrown = await act.Should().ThrowAsync<Exception>();
        ReferenceResolutionException.FindIn(thrown.Which).Should().NotBeNull();
    }
}
