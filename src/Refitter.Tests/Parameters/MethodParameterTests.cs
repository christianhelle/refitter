using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Parameters;

[TestClass]
public class MethodParameterTests
{
    [TestMethod]
    public void Render_Without_Attributes()
    {
        new MethodParameter("long", "petId")
            .Render()
            .Should()
            .Be("long petId");
    }

    [TestMethod]
    public void Render_Appends_Alias_To_Attributes()
    {
        new MethodParameter("string", "pageSize") { Attributes = ["Query"], Alias = "page-size" }
            .Render()
            .Should()
            .Be("[Query, AliasAs(\"page-size\")] string pageSize");
    }

    [TestMethod]
    public void Render_Separate_Alias_Before_Attributes()
    {
        new MethodParameter("string", "id2") { Attributes = ["Query"], Alias = "id", IsAliasSeparate = true }
            .Render()
            .Should()
            .Be("[AliasAs(\"id\")] [Query] string id2");
    }

    [TestMethod]
    public void Render_Escapes_Alias()
    {
        new MethodParameter("string", "a") { Alias = "a\"b" }
            .Render()
            .Should()
            .Be("[AliasAs(\"a\\\"b\")] string a");
    }

    [TestMethod]
    public void Render_Default_Value()
    {
        new MethodParameter("CancellationToken", "cancellationToken") { DefaultValue = "default" }
            .Render()
            .Should()
            .Be("CancellationToken cancellationToken = default");
    }

    [TestMethod]
    public void Render_Joins_Parameters()
    {
        MethodParameter.Render([new("long", "id"), new("string?", "name") { DefaultValue = "default" }])
            .Should()
            .Be("long id, string? name = default");
    }

    [TestMethod]
    public void Identifier_Drops_Keyword_Escape()
    {
        var parameter = new MethodParameter("string", "@class");

        parameter.Identifier.Should().Be("class");
        parameter.BoundName.Should().Be("class");
    }

    [TestMethod]
    public void BoundName_Is_The_Alias()
    {
        new MethodParameter("string", "pageSize") { Alias = "page-size" }
            .BoundName
            .Should()
            .Be("page-size");
    }

    [TestMethod]
    [DataRow("Query", true)]
    [DataRow("Query(CollectionFormat.Multi)", true)]
    [DataRow("Header(\"X-Key\")", false)]
    [DataRow("Body", false)]
    [DataRow("Body(BodySerializationMethod.Serialized)", false)]
    public void IsBoundByName_Depends_On_Attributes(string attribute, bool expected)
    {
        new MethodParameter("string", "value") { Attributes = [attribute] }
            .IsBoundByName
            .Should()
            .Be(expected);
    }

    [TestMethod]
    public void IsBoundByName_Is_False_With_Alias()
    {
        new MethodParameter("string", "value") { Alias = "Value" }
            .IsBoundByName
            .Should()
            .BeFalse();
    }

    [TestMethod]
    [DataRow("string?", true)]
    [DataRow("string", false)]
    [DataRow("IEnumerable<int?>", false)]
    public void IsNullable_Checks_The_Outer_Type(string type, bool expected)
    {
        new MethodParameter(type, "value")
            .IsNullable
            .Should()
            .Be(expected);
    }
}
