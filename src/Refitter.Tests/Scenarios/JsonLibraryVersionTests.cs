using FluentAssertions;
using FluentAssertions.Execution;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Tests for JsonLibraryVersion setting that controls enum attribute generation.
/// When JsonLibraryVersion is set to 9.0+, enums may use [JsonStringEnum] attributes
/// (requires NSwag/NJsonSchema version that supports this in their Enum.liquid template).
/// When JsonLibraryVersion is 8.0 or below (default), enums use [EnumMember] attributes.
/// </summary>
public class JsonLibraryVersionTests
{
    private const string OpenApiSpec = @"
{
  ""swagger"": ""2.0"",
  ""info"": {
    ""title"": ""JsonLibraryVersion Test API"",
    ""version"": ""1.0.0""
  },
  ""host"": ""example.com"",
  ""basePath"": ""/"",
  ""schemes"": [
    ""https""
  ],
  ""paths"": {
    ""/api/status"": {
      ""get"": {
        ""summary"": ""Get status"",
        ""operationId"": ""GetStatus"",
        ""parameters"": [
          {
            ""name"": ""environment"",
            ""in"": ""query"",
            ""required"": false,
            ""type"": ""string"",
            ""enum"": [""production"", ""staging"", ""development""]
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""schema"": {
              ""$ref"": ""#/definitions/StatusResponse""
            }
          }
        }
      }
    }
  },
  ""definitions"": {
    ""StatusResponse"": {
      ""type"": ""object"",
      ""properties"": {
        ""status"": {
          ""type"": ""string"",
          ""enum"": [""Active"", ""Inactive"", ""Blocked""]
        },
        ""environment"": {
          ""type"": ""string"",
          ""enum"": [""production"", ""staging"", ""development""]
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
    public async Task Default_JsonLibraryVersion_Should_Generate_EnumMember()
    {
        string generatedCode = await GenerateCode();

        using (new AssertionScope())
        {
            generatedCode.Should().Contain(@"[System.Runtime.Serialization.EnumMember(Value = @""Active"")]");
            generatedCode.Should().Contain(@"[System.Runtime.Serialization.EnumMember(Value = @""Inactive"")]");
            generatedCode.Should().Contain(@"[System.Runtime.Serialization.EnumMember(Value = @""Blocked"")]");
        }
    }

    [Test]
    public async Task JsonLibraryVersion_8_0_Should_Generate_EnumMember()
    {
        string generatedCode = await GenerateCode(jsonLibraryVersion: 8.0m);

        using (new AssertionScope())
        {
            generatedCode.Should().Contain(@"[System.Runtime.Serialization.EnumMember(Value = @""Active"")]");
        }
    }

    [Test]
    public async Task Can_Build_Default_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_JsonLibraryVersion_8_0()
    {
        string generatedCode = await GenerateCode(jsonLibraryVersion: 8.0m);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(decimal? jsonLibraryVersion = null)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                CodeGeneratorSettings = jsonLibraryVersion.HasValue
                    ? new CodeGeneratorSettings { JsonLibraryVersion = jsonLibraryVersion.Value }
                    : null
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
