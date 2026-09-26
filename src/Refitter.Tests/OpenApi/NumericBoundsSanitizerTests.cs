using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;

public class NumericBoundsSanitizerTests
{
    private const string DecimalMax = "79228162514264337593543950335";

    [Test]
    [Arguments("maximum: 1.7976931348623157e308", "maximum: " + DecimalMax)]
    [Arguments("maximum: 1.7976931348623157E+308", "maximum: " + DecimalMax)]
    [Arguments("minimum: -1.7976931348623157E+308", "minimum: -" + DecimalMax)]
    [Arguments("exclusiveMaximum: 8e28", "exclusiveMaximum: " + DecimalMax)]
    [Arguments("exclusiveMinimum: -8e28", "exclusiveMinimum: -" + DecimalMax)]
    [Arguments("maximum: 100000000000000000000000000000000", "maximum: " + DecimalMax)]
    [Arguments("maximum: 1e999", "maximum: " + DecimalMax)]
    [Arguments("maximum: 7.922816251426434e28", "maximum: " + DecimalMax)]
    [Arguments("\"maximum\": 1.7976931348623157E+308,", "\"maximum\": " + DecimalMax + ",")]
    [Arguments("'maximum': 1e300", "'maximum': " + DecimalMax)]
    [Arguments("{ type: number, maximum: 1e300 }", "{ type: number, maximum: " + DecimalMax + " }")]
    [Arguments("maximum: 1e300 # double.MaxValue", "maximum: " + DecimalMax + " # double.MaxValue")]
    public void Clamps_Bounds_Outside_Decimal_Range(string content, string expected)
    {
        NumericBoundsSanitizer.Sanitize(content).Should().Be(expected);
    }

    [Test]
    [Arguments("maximum: 7.9e28")]
    [Arguments("minimum: -100")]
    [Arguments("maximum: 0.5")]
    [Arguments("\"maximum\": 255,")]
    [Arguments("exclusiveMinimum: true")]
    [Arguments("maxLength: 1e300")]
    [Arguments("description: 'the maximum: 1e300 items'")]
    public void Leaves_Other_Values_Unchanged(string content)
    {
        NumericBoundsSanitizer.Sanitize(content).Should().Be(content);
    }

    [Test]
    public void Clamps_Every_Bound_In_Multiline_Content()
    {
        var content = "minimum: -1e300\r\nmaximum: 1e300\nmultipleOf: 2";

        var result = NumericBoundsSanitizer.Sanitize(content);

        result.Should().Be($"minimum: -{DecimalMax}\r\nmaximum: {DecimalMax}\nmultipleOf: 2");
    }

    [Test]
    public void Returns_Same_Instance_When_Nothing_To_Clamp()
    {
        const string content = "openapi: 3.0.1";

        NumericBoundsSanitizer.Sanitize(content).Should().BeSameAs(content);
    }
}
