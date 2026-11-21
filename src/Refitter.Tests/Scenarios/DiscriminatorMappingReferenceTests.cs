using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Regression coverage for GHSA-rqpj-f34p-mr22: <c>discriminator.mapping</c> values are references that the
/// schema loader resolves, so the reference guard must validate them like any <c>$ref</c>.
/// </summary>
[TestClass]
public class DiscriminatorMappingReferenceTests
{
    // Port 1 refuses connections, so a reference that slipped past the guard would surface as an
    // HTTP failure rather than a ReferenceResolutionException
    private const string Remote = "http://127.0.0.1:1/x.json";

    private static string JsonSpec(string mappingTarget) =>
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
        "\"components\":{\"schemas\":{" +
        "\"Pet\":{\"type\":\"object\",\"required\":[\"kind\"],\"properties\":{\"kind\":{\"type\":\"string\"}}," +
        "\"discriminator\":{\"propertyName\":\"kind\",\"mapping\":{\"dog\":\"" + mappingTarget + "\"}}}," +
        "\"Dog\":{\"allOf\":[{\"$ref\":\"#/components/schemas/Pet\"},{\"type\":\"object\"," +
        "\"properties\":{\"bark\":{\"type\":\"string\"}}}]}}}}";

    private static string YamlSpec(string mappingTarget) => $@"openapi: 3.0.0
info:
  title: t
  version: '1'
paths: {{}}
components:
  schemas:
    Pet:
      type: object
      required: [kind]
      properties:
        kind:
          type: string
      discriminator:
        propertyName: kind
        mapping:
          dog: '{mappingTarget}'
    Dog:
      allOf:
        - $ref: '#/components/schemas/Pet'
        - type: object
          properties:
            bark:
              type: string
";

    [TestMethod]
    [DataRow("../outside.json", false)]
    [DataRow("../outside.json", true)]
    [DataRow(Remote, false)]
    [DataRow(Remote, true)]
    public async Task Blocks_Discriminator_Mapping_Target(string target, bool yaml)
    {
        string main = await CreateInput(yaml, target);

        var act = () => GenerateCode(main);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    [DataRow("#/components/schemas/Dog", false)]
    [DataRow("#/components/schemas/Dog", true)]
    [DataRow("./dog.json", false)]
    [DataRow("./dog.json", true)]
    public async Task Allows_Legitimate_Discriminator_Mapping_Target(string target, bool yaml)
    {
        string main = await CreateInput(yaml, target);

        string code = await GenerateCode(main);

        code.Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<string> CreateInput(bool yaml, string target)
    {
        string root = Path.Combine(Path.GetTempPath(), "refitter-" + Guid.NewGuid().ToString("N"));
        string inner = Path.Combine(root, "inner");
        Directory.CreateDirectory(inner);
        await File.WriteAllTextAsync(Path.Combine(root, "outside.json"), "{\"type\":\"object\"}");
        await File.WriteAllTextAsync(Path.Combine(inner, "dog.json"), "{\"type\":\"object\"}");
        string main = Path.Combine(inner, yaml ? "main.yaml" : "main.json");
        await File.WriteAllTextAsync(main, yaml ? YamlSpec(target) : JsonSpec(target));
        return main;
    }

    private static async Task<string> GenerateCode(string swaggerFile)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
