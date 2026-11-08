using AwesomeAssertions;
using Newtonsoft.Json;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

[TestClass]
public class SanitizerJsonTests
{
    [TestMethod]
    [DataRow("\n")]
    [DataRow("\r\n")]
    [DataRow("\r")]
    [DataRow(" ")]
    public void Parse_Keeps_Number_Literals_Whatever_The_Line_Breaks(string lineBreak)
    {
        var json = string.Join(lineBreak, "{", "  \"a\": 1e-30,", "  \"b\": [ 0.12345678901234567890123, 2.50 ],", "  \"c\": 1.7976931348623157E+308", "}");

        SanitizerJson.Parse(json)
            .ToString(Formatting.None)
            .Should()
            .Be("{\"a\":1e-30,\"b\":[0.12345678901234567890123,2.50],\"c\":1.7976931348623157E+308}");
    }

    [TestMethod]
    public void Parse_Returns_A_Scalar_Document_As_Is()
    {
        SanitizerJson.Parse("1.5")
            .ToString(Formatting.None)
            .Should()
            .Be("1.5");
    }
}
