using AwesomeAssertions;
using Refitter.Core;
using Refitter.Core.Validation;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

[TestClass]
public class DocumentSetStreamLoaderTests
{
    private static readonly string Folder = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "no-such-refitter-folder"));
    private static readonly string RootPath = Path.Combine(Folder, "root.json");

    private static DocumentSet Set(
        Dictionary<string, string>? files = null,
        Dictionary<string, string>? urls = null) =>
        new(RootPath, "{}", files ?? [], urls ?? []);

    private static async Task<string> ReadAsync(DocumentSetStreamLoader loader, Uri baseUrl, Uri uri)
    {
        using var stream = await loader.LoadAsync(baseUrl, uri, CancellationToken.None);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    [TestMethod]
    public async Task Serves_Local_File_By_Absolute_Uri()
    {
        var file = Path.Combine(Folder, "sub dir", "a.json");
        var loader = new DocumentSetStreamLoader(Set(new() { [file] = "{\"a\":1}" }));

        var content = await ReadAsync(loader, new Uri(RootPath), new Uri(file));

        content.Should().Be("{\"a\":1}");
    }

    [TestMethod]
    public async Task Resolves_Relative_Uri_Against_Base_Url()
    {
        var file = Path.Combine(Folder, "schemas", "a.json");
        var loader = new DocumentSetStreamLoader(Set(new() { [file] = "{\"a\":1}" }));

        var content = await ReadAsync(loader, new Uri(RootPath), new Uri("schemas/a.json", UriKind.Relative));

        content.Should().Be("{\"a\":1}");
    }

    [TestMethod]
    public async Task Serves_Remote_Document_By_Url_As_Written()
    {
        var loader = new DocumentSetStreamLoader(
            Set(urls: new() { ["https://example.com/Specs/a.json"] = "{\"remote\":1}" }));

        var content = await ReadAsync(
            loader,
            new Uri(RootPath),
            new Uri("https://example.com/Specs/a.json#/components/schemas/A"));

        content.Should().Be("{\"remote\":1}");
    }

    [TestMethod]
    public async Task Serves_Relative_Remote_Reference_By_Resolved_Url()
    {
        var loader = new DocumentSetStreamLoader(
            Set(urls: new() { ["https://example.com/specs/b.json"] = "{\"remote\":2}" }));

        var content = await ReadAsync(
            loader,
            new Uri("https://example.com/specs/root.json"),
            new Uri("b.json#/components/schemas/B", UriKind.Relative));

        content.Should().Be("{\"remote\":2}");
    }

    [TestMethod]
    public async Task Rejects_Existing_File_Not_In_Set()
    {
        var folder = TestDirectory.CreateFolder();
        var file = Path.Combine(folder, "other.json");
        await File.WriteAllTextAsync(file, "{\"onDisk\":1}");
        var loader = new DocumentSetStreamLoader(Set());

        var act = () => ReadAsync(loader, new Uri(Path.Combine(folder, "root.json")), new Uri(file));

        var thrown = await act.Should().ThrowAsync<ReferenceResolutionException>();
        thrown.Which.Message.Should().Contain("other.json");
    }

    [TestMethod]
    public async Task Returns_Empty_Document_For_Missing_File()
    {
        var loader = new DocumentSetStreamLoader(Set());

        var content = await ReadAsync(loader, new Uri(RootPath), new Uri(Path.Combine(Folder, "missing.json")));

        content.Should().Be("{}");
    }

    [TestMethod]
    public async Task Returns_Empty_Document_For_Remote_Url_Not_In_Set()
    {
        var loader = new DocumentSetStreamLoader(Set());

        var content = await ReadAsync(loader, new Uri(RootPath), new Uri("https://example.com/unread.json"));

        content.Should().Be("{}");
    }

    [TestMethod]
    public async Task Stops_Serving_A_Document_That_Is_Requested_Repeatedly()
    {
        var file = Path.Combine(Folder, "a.json");
        var loader = new DocumentSetStreamLoader(Set(new() { [file] = "{\"a\":1}" }));

        var contents = new List<string>();
        for (var i = 0; i < 40; i++)
            contents.Add(await ReadAsync(loader, new Uri(RootPath), new Uri(file)));

        contents.Take(3).Should().AllBe("{\"a\":1}");
        contents.Last().Should().Be("{}");
    }
}
