using FluentAssertions;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

/// <summary>
/// Tests for enum types with hyphenated or otherwise special-character values (issue #300).
/// When enum values contain characters that require [EnumMember(Value = "...")] attributes
/// (e.g. "allegro-pl", "AC_1_PHASE"), JsonStringEnumConverter cannot deserialize them because
/// it uses the C# identifier name, not the [EnumMember] value.
/// The fix is to place [JsonConverter(typeof(JsonStringEnumConverter))] on the enum TYPE
/// so users can override it via JsonSerializerOptions.Converters with a converter that
/// respects [EnumMember] (e.g. JsonStringEnumMemberConverter).
/// </summary>
public class HyphenatedEnumValuesTests
{
    private const string OpenApiSpec = @"
{
  ""swagger"": ""2.0"",
  ""info"": {
    ""title"": ""Hyphenated Enum API"",
    ""version"": ""1.0.0""
  },
  ""host"": ""example.com"",
  ""basePath"": ""/"",
  ""schemes"": [
    ""https""
  ],
  ""paths"": {
    ""/api/offers"": {
      ""get"": {
        ""summary"": ""Get offers"",
        ""operationId"": ""getOffers"",
        ""parameters"": [
          {
            ""name"": ""marketplaceId"",
            ""in"": ""query"",
            ""required"": false,
            ""type"": ""string"",
            ""enum"": [""allegro-pl"", ""allegro-cz""]
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""schema"": {
              ""$ref"": ""#/definitions/MarketplaceReference""
            }
          }
        }
      }
    }
  },
  ""definitions"": {
    ""MarketplaceReference"": {
      ""type"": ""object"",
      ""properties"": {
        ""id"": {
          ""type"": ""string"",
          ""enum"": [""allegro-pl"", ""allegro-cz""]
        }
      }
    }
  }
}
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Places_JsonConverter_On_Enum_Type_Not_Property()
    {
        string generatedCode = await GenerateCode();

        using (new AssertionScope())
        {
            // [JsonConverter] should appear immediately before the enum type declaration
            generatedCode.Should().MatchRegex(
                @"\[JsonConverter\(typeof\(JsonStringEnumConverter\)\)\][\r\n\s]+public enum");
            // Enum properties should NOT have [JsonConverter] directly on them
            generatedCode.Should().NotMatchRegex(
                @"\[JsonConverter\(typeof\(JsonStringEnumConverter[^)]*\)\)\][\r\n\s]+public \w+Id\b");
        }
    }

    [Test]
    public async Task Generated_Code_Contains_EnumMember_Attributes()
    {
        string generatedCode = await GenerateCode();

        using (new AssertionScope())
        {
            generatedCode.Should().Contain(@"[System.Runtime.Serialization.EnumMember(Value = @""allegro-pl"")]");
            generatedCode.Should().Contain(@"[System.Runtime.Serialization.EnumMember(Value = @""allegro-cz"")]");
        }
    }

    [Test]
    public async Task Generated_Code_Without_InlineJsonConverters_Builds()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Without_InlineJsonConverters_Has_No_JsonConverter()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);

        using (new AssertionScope())
        {
            generatedCode.Should().NotContain("[JsonConverter(typeof(JsonStringEnumConverter))]");
            generatedCode.Should().NotContain("[JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]");
            generatedCode.Should().NotContain("[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]");
        }
    }

    private static async Task<string> GenerateCode(bool inlineJsonConverters = true)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                CodeGeneratorSettings = new CodeGeneratorSettings
                {
                    InlineJsonConverters = inlineJsonConverters
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
