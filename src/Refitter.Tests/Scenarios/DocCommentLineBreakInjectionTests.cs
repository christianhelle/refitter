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
    ""title"": ""Injection<LB>public class <M>Title {}"",
    ""description"": ""Injection<LB>public class <M>Info {}"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/api/{id}"": {
      ""get"": {
        ""operationId"": ""GetU"",
        ""summary"": ""Summary<LB>public class <M>Summary {}"",
        ""description"": ""Description<LB>public class <M>Description {}"",
        ""parameters"": [
          {
            ""name"": ""id"",
            ""in"": ""path"",
            ""required"": true,
            ""description"": ""Param<LB>public class <M>Param {}"",
            ""schema"": { ""type"": ""string"" }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Ok<LB>public class <M>Ok {}"",
            ""content"": {
              ""application/json"": {
                ""schema"": { ""$ref"": ""#/components/schemas/Item"" }
              }
            }
          },
          ""400"": { ""description"": ""Bad<LB>public class <M>Bad {}"" }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Item"": {
        ""type"": ""object"",
        ""description"": ""Item<LB>public class <M>Item {}"",
        ""properties"": {
          ""name"": { ""type"": ""string"", ""description"": ""Name<LB>public class <M>Name {}"" }
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

    [TestMethod]
    [DataRow("\\u2028")]
    [DataRow("\\u2029")]
    [DataRow("\\u0085")]
    public async Task Serializer_Context_Does_Not_Register_Types_From_Doc_Comments(string jsonEscape)
    {
        string generatedCode = await GenerateCode(jsonEscape, serializerContext: true);
        generatedCode.Should().Contain("JsonSerializable(typeof(Item))");
        generatedCode.Should().NotContain("typeof(" + Marker);
    }

    [TestCategory("Integration")]
    [TestMethod]
    [DataRow("\\u2028")]
    [DataRow("\\u2029")]
    [DataRow("\\u0085")]
    public async Task Generated_Code_With_Serializer_Context_Compiles(string jsonEscape)
    {
        string generatedCode = await GenerateCode(jsonEscape, serializerContext: true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [TestMethod]
    [DataRow("\\u2028")]
    [DataRow("\\u2029")]
    [DataRow("\\u0085")]
    public async Task Serializer_Context_Does_Not_Register_Types_From_Doc_Comments_In_Multiple_Files(string jsonEscape)
    {
        GeneratorOutput output = await GenerateMultipleFiles(jsonEscape, serializerContext: true);
        string generatedCode = string.Join(Environment.NewLine, output.Files.Select(f => f.Content));
        generatedCode.Should().Contain("JsonSerializable(typeof(Item))");
        generatedCode.Should().NotContain("typeof(" + Marker);
    }

    [TestMethod]
    [DataRow("\\u2028", 0x2028)]
    [DataRow("\\u2029", 0x2029)]
    [DataRow("\\u0085", 0x0085)]
    public async Task Enum_Value_Keeps_Unicode_Line_Terminator(string jsonEscape, int terminator)
    {
        string spec = EnumSpecTemplate.Replace("<LB>", jsonEscape);
        string generatedCode = await WithGenerator(spec, false, generator => generator.Generate());
        generatedCode.Should().Contain("EnumMember(Value = @\"a" + (char)terminator + "b\")");
    }

    private const string EnumSpecTemplate = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": { ""title"": ""Enums"", ""version"": ""1.0.0"" },
  ""paths"": {
    ""/api"": {
      ""get"": {
        ""operationId"": ""GetU"",
        ""responses"": {
          ""200"": {
            ""description"": ""ok"",
            ""content"": {
              ""application/json"": {
                ""schema"": { ""$ref"": ""#/components/schemas/Kind"" }
              }
            }
          }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Kind"": { ""type"": ""string"", ""enum"": [ ""a<LB>b"", ""c"" ] }
    }
  }
}
";

    private static void ShouldKeepInjectedTextInComments(string generatedCode)
    {
        string injectedClass = "public class " + Marker;
        generatedCode.Should().Contain(injectedClass);
        generatedCode
            .Split(CSharpLineTerminators)
            .Where(line => line.Contains(injectedClass))
            .Should()
            .OnlyContain(line => line.TrimStart().StartsWith("//"));
    }

    private static async Task<string> GenerateCode(string jsonEscape, bool serializerContext = false) =>
        await WithGenerator(Spec(jsonEscape), serializerContext, generator => generator.Generate());

    private static async Task<GeneratorOutput> GenerateMultipleFiles(
        string jsonEscape,
        bool serializerContext = false) =>
        await WithGenerator(Spec(jsonEscape), serializerContext, generator => generator.GenerateMultipleFiles());

    private static string Spec(string jsonEscape) =>
        SpecTemplate.Replace("<LB>", jsonEscape).Replace("<M>", Marker);

    private static async Task<T> WithGenerator<T>(
        string spec,
        bool serializerContext,
        Func<RefitGenerator, T> generate)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);
        try
        {
            RefitGeneratorSettings settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateXmlDocCodeComments = true,
                GenerateJsonSerializerContext = serializerContext,
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
