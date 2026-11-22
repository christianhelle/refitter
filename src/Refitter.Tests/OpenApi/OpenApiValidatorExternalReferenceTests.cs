using AwesomeAssertions;
using Refitter.Core;
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
    public async Task Validate_Counts_Components_Of_A_Dot_Relative_Reference()
    {
        var folder = TestDirectory.CreateFolder();
        await Write(folder, "components.json", Components(Pet("") + ",\"Tag\":{\"type\":\"string\"}", withVersion: true));
        var root = await Write(folder, "root.json", Root("./components.json#/components/schemas/Pet"));

        var result = await OpenApiValidator.Validate(root);

        result.Diagnostics.Errors.Should().BeEmpty();
        result.Statistics.SchemaCount.Should().Be(2);
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
        result.Statistics.SchemaCount.Should().Be(4);
    }

    [TestMethod]
    public async Task Validate_Flags_Unsafe_Header_Parameter_From_External_File()
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

        result.Diagnostics.Errors.Should().Contain(e => e.Message.Contains("Header parameter name"));
    }

    [TestMethod]
    public async Task Validate_Flags_Unsafe_ApiKey_Header_Scheme_From_External_File()
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

        // Reported by the name it is defined with in its own file
        result.Diagnostics.Errors.Should().Contain(e => e.Message.Contains("Security scheme 'Key'"));
    }

    [TestMethod]
    public async Task Validate_Resolves_External_References_From_The_Set_Only()
    {
        // Neither file exists on disk: only the document set can have served the unsafe header
        var folder = Path.Combine(TestDirectory.Root, "validator-set-only");
        var paramsPath = Path.Combine(folder, "params.json");
        var set = new DocumentSet(
            Path.Combine(folder, "root.json"),
            "{" + Header + ",\"paths\":{\"/pets\":{\"get\":{\"parameters\":[{\"$ref\":\"" + paramsPath.Replace(Path.DirectorySeparatorChar, '/') +
            "#/components/parameters/Bad\"}],\"responses\":{\"200\":{\"description\":\"ok\"}}}}}}",
            new Dictionary<string, string>
            {
                [paramsPath] = "{" + Header + ",\"components\":{\"parameters\":{\"Bad\":{\"name\":\"X-Bad\\\"Header\"," +
                               "\"in\":\"header\",\"schema\":{\"type\":\"string\"}}}}}",
            },
            new Dictionary<string, string>());

        var result = await OpenApiValidator.ValidateAsync(set);

        result.Diagnostics.Errors.Should().Contain(e => e.Message.Contains("Header parameter name"));
    }

    private static string ParameterRoot(params string[] refs) =>
        "{" + Header + ",\"paths\":{\"/pets\":{\"get\":{\"parameters\":[" +
        string.Join(",", refs.Select(r => "{\"$ref\":\"" + r + "\"}")) +
        "],\"responses\":{\"200\":{\"description\":\"ok\"}}}}}}";

    private static string HeaderParameter(string headerName, bool withVersion) =>
        "{" + (withVersion ? Header + "," : "") + "\"components\":{\"parameters\":{\"P\":{\"name\":\"" + headerName +
        "\",\"in\":\"header\",\"schema\":{\"type\":\"string\"}}}}}";

    [TestMethod]
    public async Task Validate_Flags_Unsafe_Header_Parameter_From_External_File_Without_Version()
    {
        var folder = TestDirectory.CreateFolder();
        await Write(folder, "params.json", HeaderParameter("X-Bad\\\"Header", withVersion: false));
        var root = await Write(folder, "root.json", ParameterRoot("./params.json#/components/parameters/P"));

        var result = await OpenApiValidator.Validate(root);

        result.Diagnostics.Errors.Should().Contain(e => e.Message.Contains("Header parameter name"));
    }

    [TestMethod]
    public async Task Validate_Counts_Statistics_For_External_Components_File_Without_Version()
    {
        var folder = TestDirectory.CreateFolder();
        await Write(folder, "with.json", Components(Pet(""), withVersion: true));
        await Write(folder, "without.json", Components(Pet(""), withVersion: false));
        var with = await Write(folder, "with-root.json", Root("./with.json#/components/schemas/Pet"));
        var without = await Write(folder, "without-root.json", Root("./without.json#/components/schemas/Pet"));

        var expected = await OpenApiValidator.Validate(with);
        var result = await OpenApiValidator.Validate(without);

        result.Statistics.SchemaCount.Should().Be(expected.Statistics.SchemaCount).And.BeGreaterThan(0);
    }

    [TestMethod]
    public async Task Validate_Flags_Unsafe_Header_Parameter_Sharing_A_Name_With_A_Safe_One()
    {
        var folder = TestDirectory.CreateFolder();
        await Write(folder, "safe.json", HeaderParameter("X-Safe", withVersion: true));
        await Write(folder, "unsafe.json", HeaderParameter("X-Bad\\\"Header", withVersion: true));
        var root = await Write(
            folder,
            "root.json",
            ParameterRoot("./safe.json#/components/parameters/P", "./unsafe.json#/components/parameters/P"));

        var result = await OpenApiValidator.Validate(root);

        result.Diagnostics.Errors.Should().Contain(e => e.Message.Contains("X-Bad"));
    }

    [TestMethod]
    public async Task Validate_Counts_Components_Reached_Through_A_Reference_Inside_An_External_File()
    {
        var folder = TestDirectory.CreateFolder();
        string Owner(string properties) =>
            Components(
                Pet(",\"owner\":{\"$ref\":\"#/components/schemas/Owner\"}") +
                ",\"Owner\":{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"integer\"}" + properties + "}}",
                withVersion: true);
        await Write(folder, "small.json", Owner(""));
        await Write(folder, "large.json", Owner(",\"x\":{\"type\":\"string\"},\"y\":{\"type\":\"string\"}"));
        var small = await Write(folder, "small-root.json", Root("./small.json#/components/schemas/Pet"));
        var large = await Write(folder, "large-root.json", Root("./large.json#/components/schemas/Pet"));

        var smallResult = await OpenApiValidator.Validate(small);
        var largeResult = await OpenApiValidator.Validate(large);

        largeResult.Statistics.SchemaCount.Should().Be(smallResult.Statistics.SchemaCount + 2);
    }

    [TestMethod]
    public async Task Validate_Counts_A_Schema_File_Referenced_As_A_Whole()
    {
        var folder = TestDirectory.CreateFolder();
        await Write(folder, "pet.json", "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\"}}}");
        var root = await Write(folder, "root.json", Root("./pet.json"));

        var result = await OpenApiValidator.Validate(root);

        result.Diagnostics.Errors.Should().BeEmpty();
        result.Statistics.SchemaCount.Should().Be(2);
    }

    [TestMethod]
    public async Task Validate_Resolves_A_Relative_Reference_In_A_Remote_File_Against_Its_Url()
    {
        // Every path serves this document, so P reaches Q only when "./other.json" resolves against the server
        using var server = new LoopbackHttpServer(
            "{" + Header + ",\"components\":{\"parameters\":{" +
            "\"P\":{\"$ref\":\"./other.json#/components/parameters/Q\"}," +
            "\"Q\":{\"name\":\"X-Bad\\\"Header\",\"in\":\"header\",\"schema\":{\"type\":\"string\"}}}}}");
        var root = await Write(
            TestDirectory.CreateFolder(),
            "root.json",
            ParameterRoot(server.BaseUrl + "params.json#/components/parameters/P"));

        var result = await OpenApiValidator.Validate(root, allowRemoteReferences: true);

        result.Diagnostics.Errors.Should().Contain(e => e.Message.Contains("Header parameter name"));
    }

    [TestMethod]
    public async Task Validate_Terminates_On_Cyclic_External_References()
    {
        var folder = Path.Combine(TestDirectory.Root, "validator-cycle");
        var aPath = Path.Combine(folder, "a.json");
        var bPath = Path.Combine(folder, "b.json");
        string Cycle(string name, string other, string otherName) =>
            Components(
                "\"" + name + "\":{\"type\":\"object\",\"properties\":{\"next\":{\"$ref\":\"" +
                other.Replace(Path.DirectorySeparatorChar, '/') + "#/components/schemas/" + otherName + "\"}}}",
                withVersion: true);
        var set = new DocumentSet(
            Path.Combine(folder, "root.json"),
            Root(aPath.Replace(Path.DirectorySeparatorChar, '/') + "#/components/schemas/A"),
            new Dictionary<string, string> { [aPath] = Cycle("A", bPath, "B"), [bPath] = Cycle("B", aPath, "A") },
            new Dictionary<string, string>());

        var validation = OpenApiValidator.ValidateAsync(set);

        (await Task.WhenAny(validation, Task.Delay(TimeSpan.FromSeconds(30)))).Should().BeSameAs(validation);
        (await validation).Statistics.SchemaCount.Should().BeGreaterThan(0);
    }
}
