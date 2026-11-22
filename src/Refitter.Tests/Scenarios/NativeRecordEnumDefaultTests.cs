using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

[TestClass]
public class NativeRecordEnumDefaultTests
{
    private const string OpenApiSpec = """
        swagger: "2.0"
        info: {title: T, version: "1.0"}
        host: example.com
        paths:
          /items:
            get:
              operationId: getItems
              responses:
                "200":
                  description: OK
                  schema: {$ref: "#/definitions/Other"}
        definitions:
          Other:
            type: object
            properties:
              o:
                type: integer
                format: int32
                default: 1
                enum: [1, 2]
                x-enum-varnames: [One, Two]
        """;

    [TestMethod]
    public async Task Constructor_Parameter_Matches_Property_Type()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public Other(OtherO? @o)");
        generatedCode.Should().Contain("public OtherO? O { get; init; }");
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
        RefitGenerator generator = await RefitGenerator.CreateAsync(new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            UsePolymorphicSerialization = true,
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                GenerateNativeRecords = true,
                GenerateNullableReferenceTypes = true,
            },
        });
        return generator.Generate();
    }
}
