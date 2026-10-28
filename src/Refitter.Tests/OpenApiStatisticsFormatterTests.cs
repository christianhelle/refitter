using AwesomeAssertions;

namespace Refitter.Tests;


[TestClass]
public class OpenApiStatisticsFormatterTests
{
    [TestMethod]
    public void Parse_Returns_Empty_For_Empty_String()
    {
        OpenApiStatisticsFormatter.Parse(string.Empty).Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_Returns_Empty_When_No_Lines_Start_With_Dash()
    {
        OpenApiStatisticsFormatter.Parse("Paths: 5\nOperations: 10").Should().BeEmpty();
    }

    [TestMethod]
    public void Parse_Returns_Label_Value_Pairs_From_Dash_Lines()
    {
        var statistics = $"- Paths: 5{Environment.NewLine}- Operations: 10{Environment.NewLine}- Schemas: 20";

        var result = OpenApiStatisticsFormatter.Parse(statistics);

        result.Should().HaveCount(3);
        result[0].Should().Be(("Paths", "5"));
        result[1].Should().Be(("Operations", "10"));
        result[2].Should().Be(("Schemas", "20"));
    }

    [TestMethod]
    public void Parse_Skips_Lines_Not_Starting_With_Dash()
    {
        var statistics = $"Summary:{Environment.NewLine}- Paths: 5{Environment.NewLine}Footer";

        var result = OpenApiStatisticsFormatter.Parse(statistics);

        result.Should().HaveCount(1);
        result[0].Should().Be(("Paths", "5"));
    }

    [TestMethod]
    public void Parse_Skips_Lines_Without_Colon_Separator()
    {
        var statistics = $"- ValidLine: 1{Environment.NewLine}- NoColon";

        var result = OpenApiStatisticsFormatter.Parse(statistics);

        result.Should().HaveCount(1);
        result[0].Should().Be(("ValidLine", "1"));
    }

    [TestMethod]
    [DataRow("Paths", "📝")]
    [DataRow("Operations", "⚡")]
    [DataRow("Parameters", "📝")]
    [DataRow("Request Bodies", "📤")]
    [DataRow("Responses", "📥")]
    [DataRow("Links", "🔗")]
    [DataRow("Callbacks", "🔄")]
    [DataRow("Schemas", "📋")]
    [DataRow("Unknown Metric", "📊")]
    public void GetIcon_Returns_Expected_Icon(string label, string expectedIcon)
    {
        OpenApiStatisticsFormatter.GetIcon(label).Should().Be(expectedIcon);
    }

    [TestMethod]
    [DataRow("Paths", "API endpoints defined")]
    [DataRow("Operations", "HTTP operations available")]
    [DataRow("Parameters", "Input parameters defined")]
    [DataRow("Request Bodies", "Request body schemas")]
    [DataRow("Responses", "Response schemas defined")]
    [DataRow("Links", "Operation links")]
    [DataRow("Callbacks", "Callback definitions")]
    [DataRow("Schemas", "Data schemas defined")]
    [DataRow("Unknown Metric", "API specification metric")]
    public void GetDescription_Returns_Expected_Description(string label, string expectedDescription)
    {
        OpenApiStatisticsFormatter.GetDescription(label).Should().Be(expectedDescription);
    }
}
