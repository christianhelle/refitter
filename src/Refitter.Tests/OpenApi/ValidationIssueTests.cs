using AwesomeAssertions;
using Refitter.Core.Validation;

namespace Refitter.Tests.OpenApi;

public class ValidationIssueTests
{
    [Test]
    public void ToString_Appends_The_Pointer_In_Brackets()
    {
        var issue = new ValidationIssue("#/info", "Info must be a map/object");

        issue.ToString().Should().Be("Info must be a map/object [#/info]");
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    public void ToString_Is_Just_The_Message_Without_A_Pointer(string? pointer)
    {
        var issue = new ValidationIssue(pointer, "bogus is not a valid property at #/");

        issue.ToString().Should().Be("bogus is not a valid property at #/");
    }
}
