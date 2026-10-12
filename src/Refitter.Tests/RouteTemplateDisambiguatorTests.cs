using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

public class RouteTemplateDisambiguatorTests
{
    [Test]
    public void Returns_Path_And_Parameters_Unchanged_Without_Collisions()
    {
        string[] parameters = ["long id", "[Query] string name", "CancellationToken cancellationToken = default"];

        var (path, result) = RouteTemplateDisambiguator.Disambiguate("/items/{id}", parameters, ["id"]);

        path.Should().Be("/items/{id}");
        result.Should().Equal(parameters);
    }

    [Test]
    public void Renames_Placeholder_Colliding_With_Query_Alias_Differing_By_Case()
    {
        var (path, result) = RouteTemplateDisambiguator.Disambiguate(
            "/api/parent/{id}/child",
            ["int id", "[Query, AliasAs(\"ID\")] int? iD"],
            ["id"]);

        path.Should().Be("/api/parent/{id2}/child");
        result.Should().Equal("[AliasAs(\"id2\")] int id", "[Query, AliasAs(\"ID\")] int? iD");
    }

    [Test]
    public void Drops_Alias_When_Placeholder_Is_Renamed_To_Parameter_Name()
    {
        var (path, result) = RouteTemplateDisambiguator.Disambiguate(
            "/v1/{Id}/items/{id}",
            ["[AliasAs(\"Id\")] string idPath", "[AliasAs(\"id\")] string idPath2"],
            ["Id", "id"]);

        path.Should().Be("/v1/{idPath}/items/{id}");
        result.Should().Equal("string idPath", "[AliasAs(\"id\")] string idPath2");
    }

    [Test]
    public void Keeps_Other_Attributes_When_Dropping_Alias()
    {
        var (path, result) = RouteTemplateDisambiguator.Disambiguate(
            "/v1/{Id}/items/{id}",
            ["[Required, AliasAs(\"Id\")] string idPath", "[AliasAs(\"id\")] string idPath2"],
            ["Id", "id"]);

        path.Should().Be("/v1/{idPath}/items/{id}");
        result.Should().Equal("[Required] string idPath", "[AliasAs(\"id\")] string idPath2");
    }
}
