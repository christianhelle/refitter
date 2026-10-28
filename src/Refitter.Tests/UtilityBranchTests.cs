using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class UtilityBranchTests
{
    private enum DescribedEnum
    {
        [Description("has a description")]
        Described,
        Undescribed,
    }

    [TestMethod]
    public void ToDescription_Falls_Back_To_The_Member_Name_Without_A_Description_Attribute()
    {
        DescribedEnum.Described.ToDescription().Should().Be("has a description");
        DescribedEnum.Undescribed.ToDescription().Should().Be("Undescribed");
    }

    [TestMethod]
    public void ToCompilableIdentifier_Returns_Underscore_When_Only_Verbatim_Prefixes_Remain()
    {
        IdentifierUtils.ToCompilableIdentifier("@@").Should().Be("_");
    }
}
