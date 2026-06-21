using FluentAssertions;
using TUnit.Core;

namespace Refitter.Tests;


[Category("Unit")]
public class OpenApiStatisticsFormatterTests
{
    [Test]
    public void Parse_Returns_Empty_For_Empty_String()
    {
        OpenApiStatisticsFormatter.Parse(string.Empty).Should().BeEmpty();
    }

    [Test]
    public void Parse_Returns_Empty_When_No_Lines_Start_With_Dash()
    {
        OpenApiStatisticsFormatter.Parse("Paths: 5\nOperations: 10").Should().BeEmpty();
    }

    [Test]
    public void Parse_Returns_Label_Value_Pairs_From_Dash_Lines()
    {
        var statistics = $"- Paths: 5{Environment.NewLine}- Operations: 10{Environment.NewLine}- Schemas: 20";

        var result = OpenApiStatisticsFormatter.Parse(statistics);

        result.Should().HaveCount(3);
        result[0].Should().Be(("Paths", "5"));
        result[1].Should().Be(("Operations", "10"));
        result[2].Should().Be(("Schemas", "20"));
    }

    [Test]
    public void Parse_Skips_Lines_Not_Starting_With_Dash()
    {
        var statistics = $"Summary:{Environment.NewLine}- Paths: 5{Environment.NewLine}Footer";

        var result = OpenApiStatisticsFormatter.Parse(statistics);

        result.Should().HaveCount(1);
        result[0].Should().Be(("Paths", "5"));
    }

    [Test]
    public void Parse_Skips_Lines_Without_Colon_Separator()
    {
        var statistics = $"- ValidLine: 1{Environment.NewLine}- NoColon";

        var result = OpenApiStatisticsFormatter.Parse(statistics);

        result.Should().HaveCount(1);
        result[0].Should().Be(("ValidLine", "1"));
    }

    [Test]
    [Arguments("Paths", "📝")]
    [Arguments("Operations", "⚡")]
    [Arguments("Parameters", "📝")]
    [Arguments("Request Bodies", "📤")]
    [Arguments("Responses", "📥")]
    [Arguments("Links", "🔗")]
    [Arguments("Callbacks", "🔄")]
    [Arguments("Schemas", "📋")]
    [Arguments("Unknown Metric", "📊")]
    public void GetIcon_Returns_Expected_Icon(string label, string expectedIcon)
    {
        OpenApiStatisticsFormatter.GetIcon(label).Should().Be(expectedIcon);
    }

    [Test]
    [Arguments("Paths", "API endpoints defined")]
    [Arguments("Operations", "HTTP operations available")]
    [Arguments("Parameters", "Input parameters defined")]
    [Arguments("Request Bodies", "Request body schemas")]
    [Arguments("Responses", "Response schemas defined")]
    [Arguments("Links", "Operation links")]
    [Arguments("Callbacks", "Callback definitions")]
    [Arguments("Schemas", "Data schemas defined")]
    [Arguments("Unknown Metric", "API specification metric")]
    public void GetDescription_Returns_Expected_Description(string label, string expectedDescription)
    {
        OpenApiStatisticsFormatter.GetDescription(label).Should().Be(expectedDescription);
    }
}
