using FluentAssertions;
using Refitter.Tests.TestUtilities;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class CustomDateFormatTests
{

    private const string OpenApiSpec = @"
{
  ""swagger"": ""2.0"",
  ""info"": {
    ""title"": ""XX"",
    ""version"": ""0.0.0""
  },
  ""host"": ""x.io"",
  ""basePath"": ""/"",
  ""schemes"": [
    ""https""
  ],
  ""paths"": {
    ""/t/dummy/{employee_id}"": {
      ""get"": {
        ""summary"": ""X"",
        ""description"": ""X"",
        ""operationId"": ""dummy"",
        ""parameters"": [
          {
            ""name"": ""employee_id"",
            ""in"": ""path"",
            ""description"": ""the specific employee"",
            ""required"": true,
            ""format"": ""int64"",
            ""type"": ""integer""
          },
          {
            ""name"": ""valid_from"",
            ""in"": ""query"",
            ""description"": ""the start of the period"",
            ""required"": true,
            ""format"": ""date"",
            ""type"": ""string""
          },
          {
            ""name"": ""valid_to"",
            ""in"": ""query"",
            ""description"": ""the end of the period"",
            ""required"": true,
            ""format"": ""date"",
            ""type"": ""string""
          },
          {
            ""name"": ""test_time"",
            ""in"": ""query"",
            ""description"": ""test parameter"",
            ""required"": true,
            ""format"": ""time"",
            ""type"": ""string""
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""No response was specified""
          }
        }
      }
    },
  }
}
";


    [Fact]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GeneratedCode_Contains_Date_Format_String()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain(@"[Query(Format = ""yyyy-MM-ddThh:mm:ssZ"")] ");
    }

    [Fact]
    public async Task GeneratedCode_Contains_TimeSpan_Parameter()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Query] System.TimeSpan");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                DateFormat = "yyyy-MM-ddThh:mm:ssZ"
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

}
