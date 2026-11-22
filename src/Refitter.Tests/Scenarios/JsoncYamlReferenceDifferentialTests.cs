using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Regression tests for GHSA-ch7x-rc9g-rhjc: a document that is valid JSON with comments can hide a
/// <c>$ref</c> inside a comment, where a JSON reader ignores it but a YAML reader sees a mapping entry.
/// The reference guard must check every reference the YAML loader can see.
/// </summary>
[TestClass]
public class JsoncYamlReferenceDifferentialTests
{
    private const string OutsideMarker = "OutsideJsoncRefMarker";
    private const string InTreeMarker = "InTreeJsoncRefMarker";

    private static string CreateDocument(string reference) =>
        $$"""
        {
          "openapi": "3.0.3",
          "info": { "title": "JSONC YAML differential", "version": "1.0.0" },
          "paths": {
            "/probe": {
              "get": {
                "operationId": "GetProbe",
                "responses": {
                  "200": {
                    "description": "ok",
                    "content": {
                      "application/json": {
                        "schema": {
                          /*: null,
                          "$ref": "{{reference}}" # */
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    private static string CreateSchema(string propertyName) =>
        "{\"type\":\"object\",\"properties\":{\"" + propertyName + "\":{\"type\":\"string\"}}}";

    private static string CreateLayout(string documentName, string documentContent)
    {
        var parent = Path.Combine(TestDirectory.Root, Guid.NewGuid().ToString());
        var root = Path.Combine(parent, "docs");
        Directory.CreateDirectory(root);
        File.WriteAllText(
            Path.Combine(parent, "outside.json"),
            CreateSchema(OutsideMarker));
        File.WriteAllText(
            Path.Combine(root, "safe.json"),
            CreateSchema(InTreeMarker));

        var path = Path.Combine(root, documentName);
        File.WriteAllText(path, documentContent);
        return path;
    }

    [TestMethod]
    public async Task Guard_Blocks_Out_Of_Tree_Reference_Hidden_In_Yaml_Comment()
    {
        var path = CreateLayout("openapi.yaml", CreateDocument("../outside.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*outside the input directory*");
    }

    [TestMethod]
    public async Task Guard_Blocks_Remote_Reference_Hidden_In_Yaml_Comment()
    {
        var path = CreateLayout("openapi.yaml", CreateDocument("http://127.0.0.1:1/schema.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Guard_Blocks_Hidden_Reference_In_Yaml_File_Referenced_From_Json_Root()
    {
        var path = CreateLayout(
            "openapi.json",
            CreateDocument("./child.yaml").Replace("/*: null,", "").Replace("# */", ""));
        File.WriteAllText(
            Path.Combine(Path.GetDirectoryName(path)!, "child.yaml"),
            """
            {
              /*: null,
              "$ref": "../outside.json" # */
            }
            """);

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*outside the input directory*");
    }

    [TestMethod]
    public async Task Guard_Ignores_Reference_In_Comment_Of_Json_Document()
    {
        var path = CreateLayout(
            "openapi.json",
            CreateDocument("../outside.json").Replace("/*: null,", "/*"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Generation_Rejects_Out_Of_Tree_Reference_Hidden_In_Yaml_Comment()
    {
        var path = CreateLayout("openapi.yaml", CreateDocument("../outside.json"));

        var act = async () =>
        {
            var generator = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = path });
            return generator.Generate();
        };

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Generation_Resolves_In_Tree_Reference_Hidden_In_Yaml_Comment()
    {
        var path = CreateLayout("openapi.yaml", CreateDocument("safe.json"));

        var generator = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = path });
        var code = generator.Generate();

        code.Should().Contain(InTreeMarker);
        code.Should().NotContain(OutsideMarker);
    }
}
