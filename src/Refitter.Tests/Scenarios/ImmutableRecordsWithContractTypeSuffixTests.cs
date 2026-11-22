using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// ImmutableRecords combined with ContractTypeSuffix must name the explicit record constructor after the
/// suffixed record (#1431).
/// </summary>
[TestClass]
public class ImmutableRecordsWithContractTypeSuffixTests
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
                      schema: {$ref: "#/components/schemas/Item"}
        components:
          schemas:
            Item:
              type: object
              properties: {name: {type: string}}
        """;

    [TestMethod]
    public async Task Record_Constructor_Is_Renamed_With_Suffix()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("record ItemDto");
        generatedCode.Should().Contain("public ItemDto(");
        generatedCode.Should().NotContain("public Item(");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ImmutableRecords = true,
            ContractTypeSuffix = "Dto",
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
