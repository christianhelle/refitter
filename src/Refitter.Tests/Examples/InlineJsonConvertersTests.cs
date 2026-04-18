using FluentAssertions;
using FluentAssertions.Execution;
using NJsonSchema.CodeGeneration.CSharp;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

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

    [Test]
    public async Task Generated_Code_Does_Not_Contain_STJ_JsonConverter_When_Using_Newtonsoft()
    {
        string generatedCode = await GenerateCode(jsonLibrary: CSharpJsonLibrary.NewtonsoftJson);

        using (new AssertionScope())
        {
            generatedCode.Should().NotContain("[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]");
            generatedCode.Should().Contain("public enum PetStatus");
        }
    }

    [Test]
    public async Task Generated_Code_With_Newtonsoft_Can_Build()
    {
        string generatedCode = await GenerateCode(jsonLibrary: CSharpJsonLibrary.NewtonsoftJson);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Places_JsonConverter_On_Internal_Enum_Type()
    {
        string generatedCode = await GenerateCode(typeAccessibility: TypeAccessibility.Internal);

        using (new AssertionScope())
        {
            // [JsonConverter] should appear immediately before the internal enum type declaration
            // Note: import stripping reduces the fully-qualified STJ namespace prefix in the output
            generatedCode.Should().MatchRegex(
                @"\[JsonConverter\(typeof\(JsonStringEnumConverter\)\)\][\r\n\s]+internal\s+(?:partial\s+)?enum\s+PetStatus");
            // The property line itself should NOT be preceded by [JsonConverter]
            generatedCode.Should().NotMatchRegex(
                @"\[JsonConverter\(typeof\(JsonStringEnumConverter[^)]*\)\)\][\r\n\s]+(?:public|internal)\s+PetStatus");
        }
    }

    private static async Task<string> GenerateCode(
        bool inlineJsonConverters = true,
        CSharpJsonLibrary jsonLibrary = CSharpJsonLibrary.SystemTextJson,
        TypeAccessibility typeAccessibility = TypeAccessibility.Public)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                TypeAccessibility = typeAccessibility,
                CodeGeneratorSettings = new CodeGeneratorSettings
                {
                    InlineJsonConverters = inlineJsonConverters,
                    JsonLibrary = jsonLibrary
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
