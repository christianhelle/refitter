using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

[TestClass]
public class OversizedIntegerDefaultTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.3
        info: {title: T, version: "1.0"}
        paths:
          /items:
            get:
              operationId: getItems
              responses:
                "200":
                  description: OK
                  content:
                    application/json:
                      schema: {$ref: "#/components/schemas/Thing"}
        components:
          schemas:
            Thing:
              type: object
              properties:
                big:
                  type: integer
                  default: 123456789012345678901234567890
                tooBigForInt:
                  type: integer
                  default: 3000000000
                fitsLong:
                  type: integer
                  format: int64
                  default: 3000000000
                fitsInt:
                  type: integer
                  default: 42
                uintTooBigForInt:
                  type: integer
                  format: uint32
                  default: 3000000000
                byteTooBig:
                  type: integer
                  format: byte
                  default: 300
                byteFits:
                  type: integer
                  format: byte
                  default: 200
                widenedByMaximum:
                  type: integer
                  maximum: 4000000000
                  default: 3000000000
                ulongFits:
                  type: integer
                  format: uint64
                  default: 10000000000000000000
        """;

    [TestMethod]
    public async Task Oversized_Integer_Default_Is_Dropped()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("123456789012345678901234567890");
        generatedCode.Should().NotContain("TooBigForInt { get; set; } = 3000000000", "the default does not fit an int");
    }

    [TestMethod]
    public async Task Integer_Defaults_That_Fit_Are_Kept()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("= 3000000000L;");
        generatedCode.Should().Contain("= 42;");
    }

    [TestMethod]
    public async Task Integer_Default_Is_Checked_Against_The_Type_NJsonSchema_Generates()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("(byte)300", "the default does not fit a byte");
        generatedCode.Should().Contain("= (byte)200;");
        generatedCode.Should().NotContain("UintTooBigForInt { get; set; } = 3000000000", "uint32 is generated as int");
        generatedCode.Should().Contain("long WidenedByMaximum { get; set; } = 3000000000;", "the bounds make the property a long");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        RefitGenerator generator = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = swaggerFile });
        return generator.Generate();
    }
}
