using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

[TestClass]
public class OpenApiDocumentReaderEdgeCaseTests
{
    private const string NameSchema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}";

    private static string Spec(string schemas) =>
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
        "\"components\":{\"schemas\":{" + schemas + "}}}";

    private static async Task<string> WriteSpec(string folder, string schemas)
    {
        var path = Path.Combine(folder, "spec.json");
        await File.WriteAllTextAsync(path, Spec(schemas));
        return path;
    }

    [TestMethod]
    public void Rejects_Yaml_It_Cannot_Convert()
    {
        var act = () => OpenApiDocumentReader.ConvertYamlToJson("key: [unclosed", "broken.yaml");

        act.Should().Throw<ReferenceResolutionException>().WithMessage("*broken.yaml*");
    }

    [TestMethod]
    public async Task Rejects_A_Referenced_File_It_Cannot_Read()
    {
        var folder = TestDirectory.CreateFolder();
        var path = await WriteSpec(folder, "\"Pet\":{\"$ref\":\"./pet.json\"}");
        var pet = Path.Combine(folder, "pet.json");
        await File.WriteAllTextAsync(pet, NameSchema);

        // Held exclusively, so the reader's read fails. .NET enforces FileShare on Unix too
        using var exclusive = new FileStream(pet, FileMode.Open, FileAccess.Read, FileShare.None);

        var act = () => OpenApiDocumentReader.ReadAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*Failed to read OpenAPI document*pet.json*");
    }

    [TestMethod]
    public async Task Reads_A_Referenced_File_That_Is_Empty()
    {
        var folder = TestDirectory.CreateFolder();
        var path = await WriteSpec(folder, "\"Pet\":{\"$ref\":\"./empty.yaml\"}");
        await File.WriteAllTextAsync(Path.Combine(folder, "empty.yaml"), string.Empty);

        var set = await OpenApiDocumentReader.ReadAsync(path, allowRemoteReferences: false);

        set.TryGetFile(Path.Combine(folder, "empty.yaml"), out _).Should().BeTrue();
    }

    [TestMethod]
    public async Task Rejects_A_Non_Http_Reference_In_A_Remote_Document()
    {
        using var server = new LoopbackHttpServer(
            "{\"type\":\"object\",\"properties\":{\"x\":{\"$ref\":\"ftp://127.0.0.1/x.json\"}}}");
        var path = await WriteSpec(TestDirectory.CreateFolder(), "\"Pet\":{\"$ref\":\"" + server.BaseUrl + "pet.json\"}");

        var act = () => OpenApiDocumentReader.ReadAsync(path, allowRemoteReferences: true);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*ftp://127.0.0.1/x.json*");
    }

    [TestMethod]
    public async Task Resolves_A_Relative_Reference_In_A_Remote_Document_Against_Its_Url()
    {
        using var server = new LoopbackHttpServer(baseUrl =>
            "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}," +
            "\"self\":{\"$ref\":\"./pet.json\"}}}");
        var path = await WriteSpec(TestDirectory.CreateFolder(), "\"Pet\":{\"$ref\":\"" + server.BaseUrl + "pet.json\"}");

        var set = await OpenApiDocumentReader.ReadAsync(path, allowRemoteReferences: true);

        set.TryGetUrl(server.BaseUrl + "pet.json", out _).Should().BeTrue();
        server.RequestCount.Should().Be(1);
    }

    [TestMethod]
    public async Task Downloads_A_Remote_Discriminator_Mapping()
    {
        using var server = new LoopbackHttpServer(NameSchema);
        var path = await WriteSpec(
            TestDirectory.CreateFolder(),
            "\"Pet\":{\"type\":\"object\",\"discriminator\":{\"propertyName\":\"kind\"," +
            "\"mapping\":{\"dog\":\"" + server.BaseUrl + "dog.json\"}}}");

        var set = await OpenApiDocumentReader.ReadAsync(path, allowRemoteReferences: true);

        set.TryGetUrl(server.BaseUrl + "dog.json", out _).Should().BeTrue();
    }

    [TestMethod]
    public async Task Does_Not_Download_A_Property_Named_Mapping_Outside_A_Discriminator()
    {
        // Port 1 refuses connections, so a download would fail the read
        var path = await WriteSpec(
            TestDirectory.CreateFolder(),
            "\"Pet\":{\"type\":\"object\",\"properties\":{\"mapping\":{\"type\":\"object\"," +
            "\"x-source\":\"http://127.0.0.1:1/mapping.json\"}}}");

        var set = await OpenApiDocumentReader.ReadAsync(path, allowRemoteReferences: true);

        set.TryGetUrl("http://127.0.0.1:1/mapping.json", out _).Should().BeFalse();
    }

    [TestMethod]
    public async Task Rejects_A_Remote_Reference_The_Uri_Parser_Cannot_Read_When_Remote_References_Are_Disallowed()
    {
        var path = await WriteSpec(TestDirectory.CreateFolder(), "\"Pet\":{\"$ref\":\"http://exa mple.com/pet.json\"}");

        var act = () => OpenApiDocumentReader.ReadAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*exa mple.com*");
    }
}
