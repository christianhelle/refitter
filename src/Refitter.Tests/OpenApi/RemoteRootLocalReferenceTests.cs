using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

[TestClass]
public class RemoteRootLocalReferenceTests
{
    private const string SecretSchema =
        """
        {
          "type": "object",
          "properties": { "secret": { "type": "string" } }
        }
        """;

    private static string CreateSpec(string reference) =>
        $$"""
        {
          "openapi": "3.0.1",
          "info": { "title": "Remote", "version": "1.0" },
          "paths": {
            "/pets": {
              "get": {
                "responses": {
                  "200": {
                    "description": "OK",
                    "content": {
                      "application/json": { "schema": { "$ref": "{{reference}}" } }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    private static string CreateSecretFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "refitter-remote-root-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "secret.json");
        File.WriteAllText(path, SecretSchema);
        return path;
    }

    private static IEnumerable<object[]> LocalReferences()
    {
        var secret = CreateSecretFile();
        var rooted = secret.Substring(Path.GetPathRoot(secret)!.Length - 1).Replace('\\', '/');
        yield return new object[] { rooted };
        yield return new object[] { "./secret.json" };
        yield return new object[] { "secret.json" };
    }

    [TestMethod]
    [DynamicData(nameof(LocalReferences))]
    public async Task Remote_Root_Does_Not_Resolve_Local_File_References(string reference)
    {
        await using var server = new LocalHttpServer(CreateSpec(reference));

        Func<Task> act = () => OpenApiDocumentFactory.CreateAsync(server.Url, allowRemoteReferences: true);

        await act.Should().ThrowAsync<Exception>();
    }
}
