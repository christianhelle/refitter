using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class RouteTemplateDisambiguatorTests
{
    [TestMethod]
    public void Returns_Path_And_Parameters_Unchanged_Without_Collisions()
    {
        MethodParameter[] parameters =
        [
            new("long", "id"),
            new("string", "name") { Attributes = ["Query"] },
            new("CancellationToken", "cancellationToken") { DefaultValue = "default" },
        ];

        var (path, result) = RouteTemplateDisambiguator.Disambiguate("/items/{id}", parameters, ["id"]);

        path.Should().Be("/items/{id}");
        result.Should().Equal(parameters);
    }

    [TestMethod]
    public void Renames_Placeholder_Colliding_With_Query_Alias_Differing_By_Case()
    {
        var (path, result) = RouteTemplateDisambiguator.Disambiguate(
            "/api/parent/{id}/child",
            [new("int", "id"), new("int?", "iD") { Attributes = ["Query"], Alias = "ID" }],
            ["id"]);

        path.Should().Be("/api/parent/{id2}/child");
        Render(result).Should().Equal("[AliasAs(\"id2\")] int id", "[Query, AliasAs(\"ID\")] int? iD");
    }

    [TestMethod]
    public void Drops_Alias_When_Placeholder_Is_Renamed_To_Parameter_Name()
    {
        var (path, result) = RouteTemplateDisambiguator.Disambiguate(
            "/v1/{Id}/items/{id}",
            [new("string", "idPath") { Alias = "Id" }, new("string", "idPath2") { Alias = "id" }],
            ["Id", "id"]);

        path.Should().Be("/v1/{idPath}/items/{id}");
        Render(result).Should().Equal("string idPath", "[AliasAs(\"id\")] string idPath2");
    }

    [TestMethod]
    public void Keeps_Other_Attributes_When_Dropping_Alias()
    {
        var (path, result) = RouteTemplateDisambiguator.Disambiguate(
            "/v1/{Id}/items/{id}",
            [new("string", "idPath") { Attributes = ["Required"], Alias = "Id" }, new("string", "idPath2") { Alias = "id" }],
            ["Id", "id"]);

        path.Should().Be("/v1/{idPath}/items/{id}");
        Render(result).Should().Equal("[Required] string idPath", "[AliasAs(\"id\")] string idPath2");
    }

    [TestMethod]
    public void Renames_Placeholder_Colliding_With_Deduplicated_Parameter()
    {
        var (path, result) = RouteTemplateDisambiguator.Disambiguate(
            "/upload/{id}",
            [
                new("string", "id"),
                new("string", "id2") { Alias = "id", IsAliasSeparate = true },
                new("StreamPart", "file"),
            ],
            ["id"]);

        path.Should().Be("/upload/{id3}");
        Render(result).Should().Equal("[AliasAs(\"id3\")] string id", "[AliasAs(\"id\")] string id2", "StreamPart file");
    }

    [TestMethod]
    public void Uses_Keyword_Parameter_Name_Without_Escape()
    {
        var (path, result) = RouteTemplateDisambiguator.Disambiguate(
            "/classes/{class}",
            [new("string", "@class"), new("string", "class2") { Attributes = ["Query"], Alias = "Class" }],
            ["class"]);

        path.Should().Be("/classes/{class3}");
        Render(result).Should().Equal("[AliasAs(\"class3\")] string @class", "[Query, AliasAs(\"Class\")] string class2");
    }

    private static IEnumerable<string> Render(IEnumerable<MethodParameter> parameters) =>
        parameters.Select(parameter => parameter.Render());
}
