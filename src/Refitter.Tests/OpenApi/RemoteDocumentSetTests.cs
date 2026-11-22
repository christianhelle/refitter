using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

/// <summary>
/// The reader downloads the remote documents the schema resolver will ask for, under the keys it asks for them by.
/// </summary>
[TestClass]
public class RemoteDocumentSetTests
{
    private static string Spec(string schemas) =>
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
        "\"components\":{\"schemas\":{" + schemas + "}}}";

    private const string NameSchema = "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}";

    [TestMethod]
    [DataRow("\"Pet\":{\"type\":\"object\",\"x-example\":{\"$ref\":\"http://127.0.0.1:1/sample.json\"}}")]
    [DataRow("\"Pet\":{\"type\":\"object\",\"example\":{\"$ref\":\"http://127.0.0.1:1/sample.json\"}}")]
    [DataRow("\"Pet\":{\"type\":\"object\",\"default\":{\"$ref\":\"http://127.0.0.1:1/sample.json\"}}")]
    public async Task Does_Not_Download_A_Reference_Inside_A_Literal_Value(string schemas)
    {
        // Port 1 refuses connections, so a download would fail the read
        var path = Path.Combine(TestDirectory.CreateFolder(), "spec.json");
        await File.WriteAllTextAsync(path, Spec(schemas));

        var document = await OpenApiDocumentFactory.CreateAsync(path, allowRemoteReferences: true);

        document.Components.Schemas.Should().ContainKey("Pet");
    }

    [TestMethod]
    public async Task Still_Rejects_A_Reference_Inside_A_Literal_Value_When_Remote_References_Are_Disallowed()
    {
        var path = Path.Combine(TestDirectory.CreateFolder(), "spec.json");
        await File.WriteAllTextAsync(
            path,
            Spec("\"Pet\":{\"type\":\"object\",\"x-example\":{\"$ref\":\"http://127.0.0.1:1/sample.json\"}}"));

        var act = () => OpenApiDocumentFactory.CreateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Keeps_An_Absolute_Nested_Reference_As_Written()
    {
        // Uri normalizes "/x/../name.json" to "/name.json", but the schema resolver asks for the text as written
        using var names = new LoopbackHttpServer(NameSchema);
        using var pets = new LoopbackHttpServer(
            "{\"type\":\"object\",\"properties\":{\"owner\":{\"$ref\":\"" + names.BaseUrl + "x/../name.json\"}}}");
        var path = Path.Combine(TestDirectory.CreateFolder(), "spec.json");
        await File.WriteAllTextAsync(path, Spec("\"Pet\":{\"$ref\":\"" + pets.BaseUrl + "pet.json\"}"));

        var document = await OpenApiDocumentFactory.CreateAsync(path, allowRemoteReferences: true);

        document.Components.Schemas["Pet"].ActualSchema.Properties["owner"].ActualSchema.Properties
            .Should().ContainKey("name");
    }

    [TestMethod]
    [DataRow("openapi.json")]
    [DataRow("openapi.json#selection")]
    public async Task Reuses_The_Remote_Root_For_A_Reference_Back_To_It(string rootPath)
    {
        using var server = new LoopbackHttpServer(baseUrl =>
            Spec("\"Pet\":" + NameSchema + ",\"Alias\":{\"$ref\":\"" + baseUrl + "openapi.json#/components/schemas/Pet\"}"));

        var document = await OpenApiDocumentFactory.CreateAsync(server.BaseUrl + rootPath, allowRemoteReferences: true);

        document.Components.Schemas["Alias"].ActualSchema.Properties.Should().ContainKey("name");
        server.RequestCount.Should().Be(1);
    }
}
