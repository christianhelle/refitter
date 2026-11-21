using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

[TestClass]
public class EnumDefaultNotInEnumTests
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
                color:
                  type: string
                  enum: [red, green]
                  default: blue
                  x-enumNames: [Red, Green]
                valid:
                  type: string
                  enum: [red, green]
                  default: green
                  x-enumNames: [Red, Green]
        """;

    [TestMethod]
    public async Task Can_Generate_Code_When_Enum_Default_Is_Not_An_Enum_Value()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("class Thing");
        generatedCode.Should().NotContain("Blue");
    }

    [TestMethod]
    public async Task Valid_Enum_Default_Is_Kept()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("ThingValid.Green");
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
