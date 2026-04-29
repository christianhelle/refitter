using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using FluentAssertions.Execution;
using NSwag;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class InlineJsonConvertersTests
{
    private const string OpenApiSpec = @"
{
  ""swagger"": ""2.0"",
  ""info"": {
    ""title"": ""Enum Test API"",
    ""version"": ""1.0.0""
  },
  ""host"": ""example.com"",
  ""basePath"": ""/"",
  ""schemes"": [
    ""https""
  ],
  ""paths"": {
    ""/api/pets/{petId}"": {
      ""get"": {
        ""summary"": ""Get pet by ID"",
        ""operationId"": ""getPetById"",
        ""parameters"": [
          {
            ""name"": ""petId"",
            ""in"": ""path"",
            ""required"": true,
            ""type"": ""integer""
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""schema"": {
              ""$ref"": ""#/definitions/Pet""
            }
          }
        }
      }
    }
  },
  ""definitions"": {
    ""Pet"": {
      ""type"": ""object"",
      ""properties"": {
        ""id"": {
          ""type"": ""integer"",
          ""format"": ""int64""
        },
        ""name"": {
          ""type"": ""string""
        },
        ""status"": {
          ""type"": ""string"",
          ""enum"": [
            ""available"",
            ""pending"",
            ""sold""
          ]
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
    public async Task Generated_Code_Contains_JsonConverter_By_Default()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: true);

        using (new AssertionScope())
        {
            generatedCode.Should().Contain("[JsonConverter(typeof(JsonStringEnumConverter))]");
            generatedCode.Should().Contain("public enum PetStatus");
            generatedCode.Should().Contain("Status { get; set; }");
        }
    }

    [Test]
    public async Task Generated_Code_Places_JsonConverter_On_Enum_Type_Not_Property()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: true);

        using (new AssertionScope())
        {
            // [JsonConverter] should appear immediately before the enum type declaration
            generatedCode.Should().MatchRegex(
                @"\[JsonConverter\(typeof\(JsonStringEnumConverter\)\)\][\r\n\s]+public enum PetStatus");
            // The property line itself should NOT be preceded by [JsonConverter]
            generatedCode.Should().NotMatchRegex(
                @"\[JsonConverter\(typeof\(JsonStringEnumConverter[^)]*\)\)\][\r\n\s]+public PetStatus");
        }
    }

    [Test]
    public void Generated_Code_Preserves_CRLF_When_Moving_JsonConverter_Attributes()
    {
        const string contracts = "[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]\r\npublic enum PetStatus\r\n{\r\n}\r\n";
        var settings = new RefitGeneratorSettings
        {
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                InlineJsonConverters = true
            }
        };

        var method = typeof(RefitGenerator).GetMethod("SanitizeGeneratedContracts", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();

        var result = (string)method!.Invoke(new RefitGenerator(settings, new OpenApiDocument()), new object[] { contracts })!;

        result.Should().Contain("[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]\r\npublic enum PetStatus");
        Regex.Matches(result, "(?<!\\r)\\n").Should().BeEmpty();
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_JsonConverter_When_Disabled()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);

        using (new AssertionScope())
        {
            generatedCode.Should().NotContain("[JsonConverter(typeof(JsonStringEnumConverter))]");
            generatedCode.Should().NotContain("[JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]");
            generatedCode.Should().NotContain("[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]");
            generatedCode.Should().Contain("Status { get; set; }");
            generatedCode.Should().Contain("public enum PetStatus");
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
