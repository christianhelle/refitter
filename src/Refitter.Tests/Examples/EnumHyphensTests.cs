using FluentAssertions;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class EnumHyphensTests
{
    private const string OpenApiSpecWithHyphenatedEnums = @"
{
  ""swagger"": ""2.0"",
  ""info"": {
    ""title"": ""Hyphenated Enum Test API"",
    ""version"": ""1.0.0""
  },
  ""host"": ""example.com"",
  ""basePath"": ""/"",
  ""schemes"": [
    ""https""
  ],
  ""paths"": {
    ""/api/content"": {
      ""post"": {
        ""summary"": ""Create content"",
        ""operationId"": ""createContent"",
        ""parameters"": [
          {
            ""name"": ""body"",
            ""in"": ""body"",
            ""required"": true,
            ""schema"": {
              ""$ref"": ""#/definitions/ContentRequest""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    }
  },
  ""definitions"": {
    ""ContentRequest"": {
      ""type"": ""object"",
      ""properties"": {
        ""contentType"": {
          ""$ref"": ""#/definitions/ContentType""
        },
        ""status"": {
          ""$ref"": ""#/definitions/Status""
        }
      }
    },
    ""ContentType"": {
      ""type"": ""string"",
      ""enum"": [
        ""application/json"",
        ""application/xml"",
        ""text/plain"",
        ""text/html""
      ]
    },
    ""Status"": {
      ""type"": ""string"",
      ""enum"": [
        ""foo-bar"",
        ""baz-qux"",
        ""hello-world""
      ]
    }
  }
}
";

    [Test]
    public async Task Can_Generate_Code_With_Hyphenated_Enums()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Hyphenated_Enums()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Enum_Contains_EnumMember_Attributes_For_Hyphens()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);

        using (new AssertionScope())
        {
            generatedCode.Should().Contain("public enum ContentType");
            generatedCode.Should().Contain("[EnumMember(Value = \"application/json\")]");
            generatedCode.Should().Contain("[EnumMember(Value = \"application/xml\")]");
            generatedCode.Should().Contain("[EnumMember(Value = \"text/plain\")]");
            generatedCode.Should().Contain("[EnumMember(Value = \"text/html\")]");
        }
    }

    [Test]
    public async Task Generated_Enum_Contains_EnumMember_Attributes_For_Hyphenated_Values()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);

        using (new AssertionScope())
        {
            generatedCode.Should().Contain("public enum Status");
            generatedCode.Should().Contain("[EnumMember(Value = \"foo-bar\")]");
            generatedCode.Should().Contain("[EnumMember(Value = \"baz-qux\")]");
            generatedCode.Should().Contain("[EnumMember(Value = \"hello-world\")]");
        }
    }

    [Test]
    public async Task Generated_Enum_Names_Are_Valid_CSharp_Identifiers()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);

        using (new AssertionScope())
        {
            generatedCode.Should().Contain("FooBar");
            generatedCode.Should().Contain("BazQux");
            generatedCode.Should().Contain("HelloWorld");
            generatedCode.Should().Contain("ApplicationJson");
            generatedCode.Should().Contain("ApplicationXml");
            generatedCode.Should().Contain("TextPlain");
            generatedCode.Should().Contain("TextHtml");
        }
    }

    [Test]
    public async Task With_InlineJsonConverters_True_Enum_Has_JsonConverter_Attribute()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: true);

        using (new AssertionScope())
        {
            generatedCode.Should().Contain("[JsonConverter(typeof(JsonStringEnumConverter");
            generatedCode.Should().Contain("ContentType { get; set; }");
            generatedCode.Should().Contain("Status { get; set; }");
        }
    }

    [Test]
    public async Task With_InlineJsonConverters_False_Enum_Has_No_JsonConverter_Attribute()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);

        using (new AssertionScope())
        {
            generatedCode.Should().NotContain("[JsonConverter(typeof(JsonStringEnumConverter))]");
            generatedCode.Should().Contain("ContentType { get; set; }");
            generatedCode.Should().Contain("Status { get; set; }");
            generatedCode.Should().Contain("public enum ContentType");
            generatedCode.Should().Contain("public enum Status");
        }
    }

    [Test]
    public async Task Generated_Code_Without_JsonConverter_Allows_Custom_Converter()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);

        using (new AssertionScope())
        {
            generatedCode.Should().Contain("[EnumMember(Value = \"foo-bar\")]");
            generatedCode.Should().NotContain("[JsonConverter(typeof(JsonStringEnumConverter))]");
            BuildHelper
                .BuildCSharp(generatedCode)
                .Should()
                .BeTrue();
        }
    }

    [Test]
    public async Task Enum_Values_Preserve_Original_Formatting()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);

        using (new AssertionScope())
        {
            generatedCode.Should().Contain("Value = \"foo-bar\"");
            generatedCode.Should().Contain("Value = \"baz-qux\"");
            generatedCode.Should().Contain("Value = \"hello-world\"");
            generatedCode.Should().Contain("Value = \"application/json\"");
            generatedCode.Should().Contain("Value = \"application/xml\"");
            generatedCode.Should().NotContain("Value = \"FooBar\"");
            generatedCode.Should().NotContain("Value = \"ApplicationJson\"");
        }
    }

    [Test]
    public async Task Generated_Code_Builds_With_EnumMember_And_Without_Converter()
    {
        string generatedCode = await GenerateCode(inlineJsonConverters: false);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(bool inlineJsonConverters = true)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpecWithHyphenatedEnums);
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
