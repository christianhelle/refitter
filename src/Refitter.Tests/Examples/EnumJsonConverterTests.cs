using FluentAssertions;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class EnumJsonConverterTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  version: 'v1'
  title: 'Test API'
paths:
  /offers:
    get:
      operationId: getOffers
      responses:
        '200':
          description: 'success'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Offer'
components:
  schemas:
    Offer:
      type: 'object'
      properties:
        marketplace:
          $ref: '#/components/schemas/MarketplaceId'
    MarketplaceId:
      type: string
      enum:
        - allegro-pl
        - allegro-cz
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generated_Code_Contains_EnumMember_With_Hyphen()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain(@"[System.Runtime.Serialization.EnumMember(Value = @""allegro-pl"")]");
    }

    [Test]
    public async Task Generated_Code_Contains_JsonStringEnumConverter_By_Default()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[JsonConverter(typeof(JsonStringEnumConverter");
    }

    [Test]
    public async Task Generated_Code_Uses_Custom_Enum_Converter_When_Specified()
    {
        string generatedCode = await GenerateCode(enumJsonConverter: "Macross.Json.Extensions.JsonStringEnumMemberConverter");

        using (new AssertionScope())
        {
            generatedCode.Should().Contain("[JsonConverter(typeof(Macross.Json.Extensions.JsonStringEnumMemberConverter))]");
            generatedCode.Should().NotContain("JsonStringEnumConverter");
        }
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_JsonConverter_When_Disabled()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);

        using (new AssertionScope())
        {
            generatedCode.Should().NotContain("[JsonConverter(typeof(JsonStringEnumConverter");
            generatedCode.Should().Contain("public enum MarketplaceId");
        }
    }

    [Test]
    public async Task Generated_Code_Without_JsonConverter_Can_Build()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(
        bool inlineJsonConverters = true,
        string? enumJsonConverter = null)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                CodeGeneratorSettings = new CodeGeneratorSettings
                {
                    InlineJsonConverters = inlineJsonConverters,
                    EnumJsonConverter = enumJsonConverter
                }
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        }
        finally
        {
            if (File.Exists(swaggerFile))
            {
                File.Delete(swaggerFile);
            }

            var directory = Path.GetDirectoryName(swaggerFile);
            if (directory != null && Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
