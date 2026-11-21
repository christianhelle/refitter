using AwesomeAssertions;
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
