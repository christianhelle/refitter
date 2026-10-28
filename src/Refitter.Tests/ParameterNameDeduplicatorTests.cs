using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class ParameterNameDeduplicatorTests
{
    [TestMethod]
    public void Returns_Parameters_Unchanged_Without_Duplicates()
    {
        string[] parameters = ["string id", "[Query] string name", "CancellationToken cancellationToken = default"];

        ParameterNameDeduplicator.Deduplicate(parameters, hasAppendedParameter: true).Should().Equal(parameters);
    }

    [TestMethod]
    public void Keeps_Cancellation_Token_Name_And_Renames_Query_Parameter()
    {
        var result = ParameterNameDeduplicator.Deduplicate(
            ["[Query] string cancellationToken", "CancellationToken cancellationToken = default"],
            hasAppendedParameter: true);

        result.Should().Equal(
            "[AliasAs(\"cancellationToken\")] [Query] string cancellationToken2",
            "CancellationToken cancellationToken = default");
    }

    [TestMethod]
    public void Keeps_Request_Options_Name()
    {
        var result = ParameterNameDeduplicator.Deduplicate(
            ["[Query] string options", "[RequestOptions] IApizrRequestOptions options"],
            hasAppendedParameter: true);

        result.Should().Equal(
            "[AliasAs(\"options\")] [Query] string options2",
            "[RequestOptions] IApizrRequestOptions options");
    }

    [TestMethod]
    public void Renames_Operation_Parameter_Typed_Like_The_Appended_Parameter()
    {
        var result = ParameterNameDeduplicator.Deduplicate(
            ["[Body] CancellationToken cancellationToken", "CancellationToken cancellationToken = default"],
            hasAppendedParameter: true);

        result.Should().Equal(
            "[Body] CancellationToken cancellationToken2",
            "CancellationToken cancellationToken = default");
    }

    [TestMethod]
    public void Renames_Duplicates_Of_Any_Type_Without_Appended_Parameter()
    {
        var result = ParameterNameDeduplicator.Deduplicate(
            ["[Body] CancellationToken token", "[Header(\"token\")] CancellationToken token"],
            hasAppendedParameter: false);

        result.Should().Equal("[Body] CancellationToken token", "[Header(\"token\")] CancellationToken token2");
    }

    [TestMethod]
    public void Keeps_Existing_Alias_When_Renaming()
    {
        var result = ParameterNameDeduplicator.Deduplicate(
            ["[AliasAs(\"Id\")] string idPath", "[AliasAs(\"id\")] string idPath"],
            hasAppendedParameter: false);

        result.Should().Equal("[AliasAs(\"Id\")] string idPath", "[AliasAs(\"id\")] string idPath2");
    }

    [TestMethod]
    public void Adds_Alias_For_Parameters_Bound_By_Name()
    {
        var result = ParameterNameDeduplicator.Deduplicate(["string id", "string id", "StreamPart file"], hasAppendedParameter: false);

        result.Should().Equal("string id", "[AliasAs(\"id\")] string id2", "StreamPart file");
    }

    [TestMethod]
    [DataRow("[Header(\"id\")] string id", "[Header(\"id\")] string id2")]
    [DataRow("[Body] Body id", "[Body] Body id2")]
    [DataRow("[Body(BodySerializationMethod.UrlEncoded)] Body id", "[Body(BodySerializationMethod.UrlEncoded)] Body id2")]
    public void Does_Not_Add_Alias_For_Parameters_Not_Bound_By_Name(string duplicate, string expected)
    {
        var result = ParameterNameDeduplicator.Deduplicate(["string id", duplicate], hasAppendedParameter: false);

        result.Should().Equal("string id", expected);
    }

    [TestMethod]
    public void Keeps_Default_Values()
    {
        var result = ParameterNameDeduplicator.Deduplicate(["string page", "[Query] int page = 1"], hasAppendedParameter: false);

        result.Should().Equal("string page", "[AliasAs(\"page\")] [Query] int page2 = 1");
    }

    [TestMethod]
    public void Drops_Keyword_Escape_When_Renaming()
    {
        var result = ParameterNameDeduplicator.Deduplicate(["[Query] string @class", "string @class"], hasAppendedParameter: false);

        result.Should().Equal("[Query] string @class", "[AliasAs(\"class\")] string class2");
    }

    [TestMethod]
    public void Skips_Suffixes_Already_Used()
    {
        var result = ParameterNameDeduplicator.Deduplicate(["string id", "string id2", "string id"], hasAppendedParameter: false);

        result.Should().Equal("string id", "string id2", "[AliasAs(\"id\")] string id3");
    }
}
