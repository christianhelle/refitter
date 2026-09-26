using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

public class ParameterNameDeduplicatorTests
{
    [Test]
    public void Returns_Parameters_Unchanged_Without_Duplicates()
    {
        string[] parameters = ["string id", "[Query] string name", "CancellationToken cancellationToken = default"];

        ParameterNameDeduplicator.Deduplicate(parameters).Should().Equal(parameters);
    }

    [Test]
    public void Keeps_Cancellation_Token_Name_And_Renames_Query_Parameter()
    {
        var result = ParameterNameDeduplicator.Deduplicate(
            ["[Query] string cancellationToken", "CancellationToken cancellationToken = default"]);

        result.Should().Equal(
            "[AliasAs(\"cancellationToken\")] [Query] string cancellationToken2",
            "CancellationToken cancellationToken = default");
    }

    [Test]
    public void Keeps_Request_Options_Name()
    {
        var result = ParameterNameDeduplicator.Deduplicate(
            ["[Query] string options", "[RequestOptions] IApizrRequestOptions options"]);

        result.Should().Equal(
            "[AliasAs(\"options\")] [Query] string options2",
            "[RequestOptions] IApizrRequestOptions options");
    }

    [Test]
    public void Keeps_Existing_Alias_When_Renaming()
    {
        var result = ParameterNameDeduplicator.Deduplicate(
            ["[AliasAs(\"Id\")] string idPath", "[AliasAs(\"id\")] string idPath"]);

        result.Should().Equal("[AliasAs(\"Id\")] string idPath", "[AliasAs(\"id\")] string idPath2");
    }

    [Test]
    public void Adds_Alias_For_Parameters_Bound_By_Name()
    {
        var result = ParameterNameDeduplicator.Deduplicate(["string id", "string id", "StreamPart file"]);

        result.Should().Equal("string id", "[AliasAs(\"id\")] string id2", "StreamPart file");
    }

    [Test]
    [Arguments("[Header(\"id\")] string id", "[Header(\"id\")] string id2")]
    [Arguments("[Body] Body id", "[Body] Body id2")]
    [Arguments("[Body(BodySerializationMethod.UrlEncoded)] Body id", "[Body(BodySerializationMethod.UrlEncoded)] Body id2")]
    public void Does_Not_Add_Alias_For_Parameters_Not_Bound_By_Name(string duplicate, string expected)
    {
        var result = ParameterNameDeduplicator.Deduplicate(["string id", duplicate]);

        result.Should().Equal("string id", expected);
    }

    [Test]
    public void Keeps_Default_Values()
    {
        var result = ParameterNameDeduplicator.Deduplicate(["string page", "[Query] int page = 1"]);

        result.Should().Equal("string page", "[AliasAs(\"page\")] [Query] int page2 = 1");
    }

    [Test]
    public void Drops_Keyword_Escape_When_Renaming()
    {
        var result = ParameterNameDeduplicator.Deduplicate(["[Query] string @class", "string @class"]);

        result.Should().Equal("[Query] string @class", "[AliasAs(\"class\")] string class2");
    }

    [Test]
    public void Skips_Suffixes_Already_Used()
    {
        var result = ParameterNameDeduplicator.Deduplicate(["string id", "string id2", "string id"]);

        result.Should().Equal("string id", "string id2", "[AliasAs(\"id\")] string id3");
    }
}
