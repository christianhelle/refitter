using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

// C# treats U+0085, U+2028 and U+2029 as line terminators. Text from the OpenAPI document that
// contains one of them must not end a generated /// comment and leave the rest of the text as code.
[TestClass]
public class DocCommentLineBreakInjectionTests
{
    private const string Marker = "InjectedMarker";

    private const string SpecTemplate = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Injection<LB>class <M>Title {}"",
    ""description"": ""Injection<LB>class <M>Info {}"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/api/{id}"": {
      ""get"": {
        ""operationId"": ""GetU"",
        ""summary"": ""Summary<LB>class <M>Summary {}"",
        ""description"": ""Description<LB>class <M>Description {}"",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""required"": true,
            ""description"": ""Param<LB>class <M>Param {}"",
            ""schema"": { ""type"": ""string"" }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Ok<LB>class <M>Ok {}"",
            ""content"": {
              ""application/json"": {
                ""schema"": { ""$ref"": ""#/components/schemas/Item"" }
              }
            }
          },
          ""400"": { ""description"": ""Bad<LB>class <M>Bad {}"" }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Item"": {
        ""type"": ""object"",
        ""description"": ""Item<LB>class <M>Item {}"",
        ""properties"": {
          ""name"": { ""type"": ""string"", ""description"": ""Name<LB>class <M>Name {}"" }
        }
      }
    }
  }
}
";

    private static readonly char[] CSharpLineTerminators =
        [(char)0x000D, (char)0x000A, (char)0x0085, (char)0x2028, (char)0x2029];

    [TestMethod]
    [DataRow("\\u2028")]
    [DataRow("\\u2029")]
    [DataRow("\\u0085")]
    public async Task Unicode_Line_Break_Does_Not_Escape_Doc_Comment(string jsonEscape)
    {
        string generatedCode = await GenerateCode(jsonEscape);
        ShouldKeepInjectedTextInComments(generatedCode);
    }

    [TestMethod]
    [DataRow("\\u2028")]
    [DataRow("\\u2029")]
    [DataRow("\\u0085")]
    public async Task Unicode_Line_Break_Does_Not_Escape_Doc_Comment_In_Multiple_Files(string jsonEscape)
    {
        GeneratorOutput output = await GenerateMultipleFiles(jsonEscape);
        ShouldKeepInjectedTextInComments(string.Join(Environment.NewLine, output.Files.Select(f => f.Content)));
    }

    [TestCategory("Integration")]
    [TestMethod]
    [DataRow("\\u2028")]
    [DataRow("\\u2029")]
    [DataRow("\\u0085")]
    public async Task Generated_Code_With_Unicode_Line_Break_Compiles(string jsonEscape)
    {
        string generatedCode = await GenerateCode(jsonEscape);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static void ShouldKeepInjectedTextInComments(string generatedCode)
    {
        string injectedClass = "class " + Marker;
        generatedCode.Should().Contain(injectedClass);
        generatedCode
            .Split(CSharpLineTerminators)
            .Where(line => line.Contains(injectedClass))
            .Should()
            .OnlyContain(line => line.TrimStart().StartsWith("//"));
    }

    private static async Task<string> GenerateCode(string jsonEscape) =>
        await WithGenerator(jsonEscape, generator => generator.Generate());

    private static async Task<GeneratorOutput> GenerateMultipleFiles(string jsonEscape) =>
        await WithGenerator(jsonEscape, generator => generator.GenerateMultipleFiles());

    private static async Task<T> WithGenerator<T>(string jsonEscape, Func<RefitGenerator, T> generate)
    {
        string spec = SpecTemplate.Replace("<LB>", jsonEscape).Replace("<M>", Marker);
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);
        try
        {
            RefitGeneratorSettings settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateXmlDocCodeComments = true,
            };
            RefitGenerator generator = await RefitGenerator.CreateAsync(settings);
            return generate(generator);
        }
        finally
        {
            if (File.Exists(swaggerFile))
                File.Delete(swaggerFile);
        }
    }
}
