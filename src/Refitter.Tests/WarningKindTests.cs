using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class WarningKindTests
{
    [TestMethod]
    public void DetectWarnings_Reports_Date_Format_Override_Kind()
    {
        var warnings = new List<Warning>();
        var settings = new RefitGeneratorSettings
        {
            UseIsoDateFormat = true,
            CodeGeneratorSettings = new CodeGeneratorSettings { DateFormat = "yyyy-MM-dd" },
        };

        RefitterRunner.DetectWarnings(settings, warnings);

        warnings.Should().ContainSingle().Which.Kind.Should().Be(WarningKind.DateFormatOverride);
    }

    [TestMethod]
    public void DetectWarnings_Reports_Deprecated_Setting_Kind()
    {
        var warnings = new List<Warning>();
#pragma warning disable CS0618
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings { UsePolly = true },
        };
#pragma warning restore CS0618

        RefitterRunner.DetectWarnings(settings, warnings);

        warnings.Should().ContainSingle().Which.Kind.Should().Be(WarningKind.DeprecatedSetting);
    }
}
