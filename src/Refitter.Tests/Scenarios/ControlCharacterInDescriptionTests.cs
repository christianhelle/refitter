using AwesomeAssertions;
using NSwag;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

// XML 1.0 rejects most C0 control characters. Text from the OpenAPI document that contains one
// of them must not crash the XML documentation rendering.
[TestClass]
public class ControlCharacterInDescriptionTests
{
    private const string SpecTemplate = @"
{
  ""openapi"": ""3.0.3"",
  ""info"": { ""title"": ""T<C>itle"", ""version"": ""1.0"" },
  ""paths"": {
    ""/items"": {
      ""get"": {
        ""operationId"": ""getItems"",
        ""summary"": ""sum<C>mary"",
        ""description"": ""first<C>second"",
        ""parameters"": [
          { ""name"": ""q"", ""in"": ""query"", ""description"": ""par<C>am"", ""schema"": { ""type"": ""string"" } }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""o<C>k"",
            ""content"": { ""application/json"": { ""schema"": { ""$ref"": ""#/components/schemas/Item"" } } }
          }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Item"": {
        ""type"": ""object"",
        ""description"": ""item<C>desc"",
        ""properties"": { ""name"": { ""type"": ""string"", ""description"": ""name<C>desc"" } }
      }
    }
  }
}
";

    [TestMethod]
    [DataRow("\\u000b")]
    [DataRow("\\u000c")]
    [DataRow("\\u0000")]
    [DataRow("\\u0001")]
    [DataRow("\\u001f")]
    [DataRow("\\f")]
    [DataRow("\\b")]
    public async Task Control_Character_In_Description_Does_Not_Crash(string jsonEscape)
    {
        string code = await Generate(jsonEscape);
        code.Should().Contain("first");
        code.Should().Contain("second");
        code.Should().Contain("item");
        code.Should().NotContainAny(
            ((char)0x00).ToString(), ((char)0x01).ToString(), ((char)0x0B).ToString(),
            ((char)0x0C).ToString(), ((char)0x1F).ToString());
    }

    [TestCategory("Integration")]
    [TestMethod]
    [DataRow("\\u000b")]
    [DataRow("\\u0000")]
    public async Task Generated_Code_With_Control_Character_Compiles(string jsonEscape)
    {
        string code = await Generate(jsonEscape);
        BuildHelper.BuildCSharp(code).Should().BeTrue();
    }

    [TestMethod]
    public async Task Public_Constructor_With_Parsed_Document_Does_Not_Crash()
    {
        OpenApiDocument document = await OpenApiDocument.FromJsonAsync(
            SpecTemplate.Replace("<C>", "\\u000b"));
        RefitGenerator generator = new RefitGenerator(
            new RefitGeneratorSettings { GenerateXmlDocCodeComments = true },
            document);

        string code = generator.Generate();

        code.Should().Contain("first");
        code.Should().NotContain(((char)0x0B).ToString());
    }

    [TestMethod]
    public async Task Example_Data_Is_Not_Sanitized()
    {
        const string spec = @"
{
  ""openapi"": ""3.0.3"",
  ""info"": { ""title"": ""T"", ""version"": ""1.0"" },
  ""paths"": {
    ""/items"": {
      ""get"": {
        ""operationId"": ""getItems"",
        ""responses"": {
          ""200"": {
            ""description"": ""ok"",
            ""content"": { ""application/json"": { ""schema"": { ""$ref"": ""#/components/schemas/Item"" } } }
          }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Item"": {
        ""type"": ""object"",
        ""example"": { ""description"": ""a\u000bb"" },
        ""properties"": { ""name"": { ""type"": ""string"" } }
      }
    }
  }
}
";
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);
        try
        {
            RefitGenerator generator = await RefitGenerator.CreateAsync(
                new RefitGeneratorSettings { OpenApiPath = swaggerFile, GenerateXmlDocCodeComments = true });
            generator.Generate();

            string example = generator.OpenApiDocument.Components.Schemas["Item"].Example!.ToString()!;
            example.Should().Contain("a\\u000bb");
        }
        finally
        {
            if (File.Exists(swaggerFile))
                File.Delete(swaggerFile);
        }
    }

    private static async Task<string> Generate(string jsonEscape)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(SpecTemplate.Replace("<C>", jsonEscape));
        try
        {
            RefitGeneratorSettings settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateXmlDocCodeComments = true,
            };
            RefitGenerator generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        }
        finally
        {
            if (File.Exists(swaggerFile))
                File.Delete(swaggerFile);
        }
    }
}
