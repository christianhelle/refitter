using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests;

public class NativeGeneratorEdgeCaseTests
{
    private static ApiDocument Load(string json) => ApiDocumentLoader.Load(json, null, isYaml: false);

    [Test]
    public void Leaves_Documents_Without_Path_Item_Components_Alone()
    {
        Load("""{ "openapi": "3.1.0", "info": { "title": "T", "version": "1" }, "x-pathItems": 1 }""").Paths.Should().BeEmpty();
        Load("""{ "openapi": "3.1.0", "info": { "title": "pathItems", "version": "1" }, "paths": {} }""").Paths.Should().BeEmpty();
    }

    [Test]
    public void Keeps_Path_Item_References_That_Cannot_Be_Inlined()
    {
        var act = () => Load("""
            {
              "openapi": "3.1.0",
              "info": { "title": "T", "version": "1" },
              "paths": {
                "/missing": { "$ref": "#/components/pathItems/Missing" },
                "/cycle": { "$ref": "#/components/pathItems/Cycle" },
                "/broken": { "$ref": "#/components/pathItems/Broken" }
              },
              "components": {
                "pathItems": {
                  "Cycle": { "$ref": "#/components/pathItems/Cycle" },
                  "Broken": { "$ref": "#/components/pathItems/Missing" }
                }
              }
            }
            """);

        // The references stay, and cannot be resolved as path items
        act.Should().Throw<Exception>();
    }

    [Test]
    public void Keeps_Null_Siblings_Of_Inlined_Path_Items()
    {
        var document = Load("""
            {
              "openapi": "3.1.0",
              "info": { "title": "T", "version": "1" },
              "paths": { "/a": { "$ref": "#/components/pathItems/A", "description": null } },
              "components": { "pathItems": { "A": { "description": "D", "get": { "responses": {} } } } }
            }
            """);

        document.Paths["/a"].Description.Should().BeNull();
        document.Paths["/a"].Keys.Should().Equal("get");
    }

    [Test]
    public async Task Downloads_Referenced_Documents()
    {
        await using var server = new LocalHttpServer("""{ "type": "string", "format": "remote" }""");

        var document = Load($$"""
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": {},
              "components": { "schemas": { "Remote": { "allOf": [{ "$ref": "{{server.Url}}" }] } } }
            }
            """);

        document.Definitions["Remote"].AllOf[0].ActualSchema.Format.Should().Be("remote");
    }

    [Test]
    public void Renders_Custom_Templates_With_The_Refitter_Filters()
    {
        var folder = TestDirectory.CreateFolder();
        File.WriteAllText(
            Path.Combine(folder, "Enum.liquid"),
            """
            // {{ Name | uppercamelcase: firstCharacterMustBeAlpha: true }} {{ Name | literal }}
            """);
        var generator = CreateGenerator(folder);

        var code = generator.GenerateFile(new MultipleClientsFromOperationIdApiOperationNameGenerator());

        code.Should().Contain("// Status \"Status\"");
    }

    [Test]
    public void Reports_Template_Errors()
    {
        var folder = TestDirectory.CreateFolder();
        File.WriteAllText(Path.Combine(folder, "Enum.liquid"), "{% template Missing %}");
        var generator = CreateGenerator(folder);

        var act = () => generator.GenerateFile(new MultipleClientsFromOperationIdApiOperationNameGenerator());

        act.Should().Throw<InvalidOperationException>().WithMessage("*Error while rendering Liquid template CSharp/Enum*");
    }

    [Test]
    public void Generates_Clients_Without_Multiple_Client_Support()
    {
        var generator = CreateGenerator(null);

        var code = generator.GenerateFile(new SingleClientFromOperationIdApiOperationNameGenerator());

        code.Should().Contain("enum Status");
    }

    [Test]
    public void Renames_Properties_Named_Like_Their_Class()
    {
        var document = Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": {},
              "components": {
                "schemas": {
                  "Pet": { "type": "object", "properties": { "pet": { "type": "string" }, "pet1": { "type": "string" } } }
                }
              }
            }
            """);
        var generator = new ContractGeneratorFactory(new RefitGeneratorSettings(), document).Create();

        var code = generator.GenerateFile(new MultipleClientsFromOperationIdApiOperationNameGenerator());

        code.Should().Contain("public string Pet2 { get; set; }");
    }

    private static ContractGenerator CreateGenerator(string? templateDirectory)
    {
        var document = Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "T", "version": "1" },
              "paths": { "/a": { "get": { "operationId": "GetAsync", "responses": { "200": { "description": "ok" } } } } },
              "components": { "schemas": { "Status": { "type": "string", "enum": ["a"] } } }
            }
            """);
        var settings = new RefitGeneratorSettings { CustomTemplateDirectory = templateDirectory };
        return new ContractGeneratorFactory(settings, document).Create();
    }
}
