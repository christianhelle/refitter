using AwesomeAssertions;
using Refitter.Core.Validation;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

/// <summary>
/// Validating a document that reaches other files through external references.
/// </summary>
[TestClass]
public class OpenApiValidatorExternalReferenceTests
{
    private const string Header = "\"openapi\":\"3.0.1\",\"info\":{\"title\":\"t\",\"version\":\"1\"}";

    private static string Root(string refTarget) =>
        "{" + Header + ",\"paths\":{\"/pets\":{\"get\":{\"responses\":{\"200\":{\"description\":\"ok\"," +
        "\"content\":{\"application/json\":{\"schema\":{\"$ref\":\"" + refTarget + "\"}}}}}}}}}";

    private static string Components(string schemas, bool withVersion) =>
        "{" + (withVersion ? "\"openapi\":\"3.0.1\"," : "") + "\"components\":{\"schemas\":{" + schemas + "}}}";

    private static string Pet(string nested) =>
        "\"Pet\":{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}" + nested + "}}";

    private static async Task<string> Write(string folder, string name, string content)
    {
        var path = Path.Combine(folder, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content);
        return path;
    }

    [TestMethod]
    public async Task Validate_Has_No_Errors_For_External_Components_File()
    {
        var folder = TestDirectory.CreateFolder();
        await Write(folder, "components.json", Components(Pet(""), withVersion: true));
        var root = await Write(folder, "root.json", Root("./components.json#/components/schemas/Pet"));

        var result = await OpenApiValidator.Validate(root);

        result.Diagnostics.Errors.Should().BeEmpty();
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_Has_No_Errors_For_External_Components_File_Without_Version()
    {
        var folder = TestDirectory.CreateFolder();
        await Write(folder, "components.json", Components(Pet(""), withVersion: false));
        var root = await Write(folder, "root.json", Root("./components.json#/components/schemas/Pet"));

        var result = await OpenApiValidator.Validate(root);

        result.Diagnostics.Errors.Should().BeEmpty();
        result.IsValid.Should().BeTrue();
    }

    [TestMethod]
    public async Task Validate_Counts_Statistics_For_External_Components()
    {
        var folder = TestDirectory.CreateFolder();
        await Write(
            folder,
            "components.json",
            Components(Pet("") + ",\"Tag\":{\"type\":\"string\"}", withVersion: true));
        var root = await Write(folder, "root.json", Root("components.json#/components/schemas/Pet"));

        var result = await OpenApiValidator.Validate(root);

        result.Statistics.PathItemCount.Should().Be(1);
        result.Statistics.OperationCount.Should().Be(1);
        result.Statistics.ResponseCount.Should().Be(1);
        result.Statistics.SchemaCount.Should().Be(2);
    }

    [TestMethod]
    public async Task Validate_Does_Not_Count_Components_Of_A_Dot_Relative_Reference()
    {
        // Baseline of the OasReader based validation, which only merges references without a leading "./"
        var folder = TestDirectory.CreateFolder();
        await Write(folder, "components.json", Components(Pet("") + ",\"Tag\":{\"type\":\"string\"}", withVersion: true));
        var root = await Write(folder, "root.json", Root("./components.json#/components/schemas/Pet"));

        var result = await OpenApiValidator.Validate(root);

        result.Diagnostics.Errors.Should().BeEmpty();
        result.Statistics.SchemaCount.Should().Be(0);
    }

    [TestMethod]
    public async Task Validate_Resolves_Subfolder_Reference_Chain()
    {
        var folder = TestDirectory.CreateFolder();
        await Write(
            folder,
            Path.Combine("schemas", "pet.json"),
            Components(Pet(",\"owner\":{\"$ref\":\"./owner.json#/components/schemas/Owner\"}"), withVersion: true));
        await Write(
            folder,
            Path.Combine("schemas", "owner.json"),
            Components("\"Owner\":{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"integer\"}}}", withVersion: true));
        var root = await Write(folder, "root.json", Root("schemas/pet.json#/components/schemas/Pet"));

        var result = await OpenApiValidator.Validate(root);

        result.Diagnostics.Errors.Should().BeEmpty();
        result.Statistics.SchemaCount.Should().Be(2);
    }

    [TestMethod]
    public async Task Validate_Does_Not_Resolve_External_Header_Parameter()
    {
        var folder = TestDirectory.CreateFolder();
        await Write(
            folder,
            "params.json",
            "{" + Header + ",\"components\":{\"parameters\":{\"Bad\":{\"name\":\"X-Bad\\\"Header\",\"in\":\"header\"," +
            "\"schema\":{\"type\":\"string\"}}}}}");
        var root = await Write(
            folder,
            "root.json",
            "{" + Header + ",\"paths\":{\"/pets\":{\"get\":{\"parameters\":[{\"$ref\":\"./params.json#/components/parameters/Bad\"}]," +
            "\"responses\":{\"200\":{\"description\":\"ok\"}}}}}}");

        var result = await OpenApiValidator.Validate(root);

        // Baseline of the OasReader based validation: external parameters stay unresolved, so they are not checked
        result.Diagnostics.Errors.Should().BeEmpty();
    }

    [TestMethod]
    public async Task Validate_Does_Not_Resolve_External_ApiKey_Scheme()
    {
        var folder = TestDirectory.CreateFolder();
        await Write(
            folder,
            "security.json",
            "{" + Header + ",\"components\":{\"securitySchemes\":{\"Key\":{\"type\":\"apiKey\",\"in\":\"header\"," +
            "\"name\":\"X-Bad\\\"Key\"}}}}");
        var root = await Write(
            folder,
            "root.json",
            "{" + Header + ",\"paths\":{\"/pets\":{\"get\":{\"security\":[{\"Key\":[]}]," +
            "\"responses\":{\"200\":{\"description\":\"ok\"}}}}}," +
            "\"components\":{\"securitySchemes\":{\"Alias\":{\"$ref\":\"./security.json#/components/securitySchemes/Key\"}}}}");

        var result = await OpenApiValidator.Validate(root);

        // Baseline of the OasReader based validation: external security schemes stay unresolved, so they are not checked
        result.Diagnostics.Errors.Should().BeEmpty();
    }
}
