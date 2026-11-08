using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class ParameterNameDeduplicatorTests
{
    private static readonly MethodParameter CancellationToken =
        new("CancellationToken", "cancellationToken") { DefaultValue = "default" };

    [TestMethod]
    public void Returns_Parameters_Unchanged_Without_Duplicates()
    {
        MethodParameter[] parameters = [new("string", "id"), Query("string", "name"), CancellationToken];

        Deduplicate(parameters, hasAppendedParameter: true)
            .Should()
            .Equal("string id", "[Query] string name", "CancellationToken cancellationToken = default");
    }

    [TestMethod]
    public void Keeps_Cancellation_Token_Name_And_Renames_Query_Parameter()
    {
        Deduplicate([Query("string", "cancellationToken"), CancellationToken], hasAppendedParameter: true)
            .Should()
            .Equal(
                "[AliasAs(\"cancellationToken\")] [Query] string cancellationToken2",
                "CancellationToken cancellationToken = default");
    }

    [TestMethod]
    public void Keeps_Request_Options_Name()
    {
        var options = new MethodParameter("IApizrRequestOptions", "options") { Attributes = ["RequestOptions"] };

        Deduplicate([Query("string", "options"), options], hasAppendedParameter: true)
            .Should()
            .Equal(
                "[AliasAs(\"options\")] [Query] string options2",
                "[RequestOptions] IApizrRequestOptions options");
    }

    [TestMethod]
    public void Renames_Operation_Parameter_Typed_Like_The_Appended_Parameter()
    {
        var body = new MethodParameter("CancellationToken", "cancellationToken") { Attributes = ["Body"] };

        Deduplicate([body, CancellationToken], hasAppendedParameter: true)
            .Should()
            .Equal(
                "[Body] CancellationToken cancellationToken2",
                "CancellationToken cancellationToken = default");
    }

    [TestMethod]
    public void Renames_Duplicates_Of_Any_Type_Without_Appended_Parameter()
    {
        var body = new MethodParameter("CancellationToken", "token") { Attributes = ["Body"] };
        var header = new MethodParameter("CancellationToken", "token") { Attributes = ["Header(\"token\")"] };

        Deduplicate([body, header], hasAppendedParameter: false)
            .Should()
            .Equal("[Body] CancellationToken token", "[Header(\"token\")] CancellationToken token2");
    }

    [TestMethod]
    public void Keeps_Existing_Alias_When_Renaming()
    {
        Deduplicate(
                [new("string", "idPath") { Alias = "Id" }, new("string", "idPath") { Alias = "id" }],
                hasAppendedParameter: false)
            .Should()
            .Equal("[AliasAs(\"Id\")] string idPath", "[AliasAs(\"id\")] string idPath2");
    }

    [TestMethod]
    public void Adds_Alias_For_Parameters_Bound_By_Name()
    {
        Deduplicate([new("string", "id"), new("string", "id"), new("StreamPart", "file")], hasAppendedParameter: false)
            .Should()
            .Equal("string id", "[AliasAs(\"id\")] string id2", "StreamPart file");
    }

    [TestMethod]
    [DataRow("string", "Header(\"id\")", "[Header(\"id\")] string id2")]
    [DataRow("Body", "Body", "[Body] Body id2")]
    [DataRow("Body", "Body(BodySerializationMethod.UrlEncoded)", "[Body(BodySerializationMethod.UrlEncoded)] Body id2")]
    public void Does_Not_Add_Alias_For_Parameters_Not_Bound_By_Name(string type, string attribute, string expected)
    {
        Deduplicate([new("string", "id"), new(type, "id") { Attributes = [attribute] }], hasAppendedParameter: false)
            .Should()
            .Equal("string id", expected);
    }

    [TestMethod]
    public void Keeps_Default_Values()
    {
        Deduplicate([new("string", "page"), Query("int", "page") with { DefaultValue = "1" }], hasAppendedParameter: false)
            .Should()
            .Equal("string page", "[AliasAs(\"page\")] [Query] int page2 = 1");
    }

    [TestMethod]
    public void Drops_Keyword_Escape_When_Renaming()
    {
        Deduplicate([Query("string", "@class"), new("string", "@class")], hasAppendedParameter: false)
            .Should()
            .Equal("[Query] string @class", "[AliasAs(\"class\")] string class2");
    }

    [TestMethod]
    public void Skips_Suffixes_Already_Used()
    {
        Deduplicate([new("string", "id"), new("string", "id2"), new("string", "id")], hasAppendedParameter: false)
            .Should()
            .Equal("string id", "string id2", "[AliasAs(\"id\")] string id3");
    }

    private static MethodParameter Query(string type, string name) => new(type, name) { Attributes = ["Query"] };

    private static IEnumerable<string> Deduplicate(IReadOnlyList<MethodParameter> parameters, bool hasAppendedParameter) =>
        ParameterNameDeduplicator.Deduplicate(parameters, hasAppendedParameter).Select(parameter => parameter.Render());

    [TestMethod]
    public void Renames_Names_That_Only_Differ_By_Formatting_Characters()
    {
        Deduplicate([new("string", "id"), new("string", "i\u200Cd")], hasAppendedParameter: false)
            .Should()
            .Equal("string id", "[AliasAs(\"i\u200Cd\")] string id2");
    }
}
