using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Test for Issue #1199: method overloading with same name but different parameters
/// should generate overloads, not counter-suffixed names like CustomformatGet2.
/// </summary>
public class MethodOverloadTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Overload Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/customformat"": {
      ""get"": {
        ""tags"": [""CustomFormat""],
        ""responses"": { ""200"": { ""description"": ""Success"" } }
      }
    },
    ""/customformat/{id}"": {
      ""get"": {
        ""tags"": [""CustomFormat""],
        ""parameters"": [
          { ""in"": ""path"", ""name"": ""id"", ""required"": true, ""schema"": { ""type"": ""integer"" } }
        ],
        ""responses"": { ""200"": { ""description"": ""Success"" } }
      }
    }
  }
}
";

    private const string Swagger2Spec = @"
{
  ""swagger"": ""2.0"",
  ""info"": {
    ""title"": ""Overload Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/customformat"": {
      ""get"": {
        ""tags"": [""CustomFormat""],
        ""responses"": { ""200"": { ""description"": ""Success"" } }
      }
    },
    ""/customformat/{id}"": {
      ""get"": {
        ""tags"": [""CustomFormat""],
        ""parameters"": [
          { ""in"": ""path"", ""name"": ""id"", ""required"": true, ""type"": ""integer"" }
        ],
        ""responses"": { ""200"": { ""description"": ""Success"" } }
      }
    }
  }
}
";

    [Test]
    [Arguments(OpenApiSpec)]
    [Arguments(Swagger2Spec)]
    public async Task Can_Generate_Code(string spec)
    {
        var generatedCode = await GenerateCode(spec);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Arguments(OpenApiSpec)]
    [Arguments(Swagger2Spec)]
    public async Task Same_Name_Different_Params_Should_Use_Overloads_Not_Counter(string spec)
    {
        var generatedCode = await GenerateCode(spec);

        // The second method should use the same name (overload), not have a counter suffix
        generatedCode.Should().NotContain("CustomformatGet2(");
    }

    [Test]
    [Arguments(OpenApiSpec)]
    [Arguments(Swagger2Spec)]
    public async Task Overloads_Should_Have_Different_Parameter_Lists(string spec)
    {
        var generatedCode = await GenerateCode(spec);

        // First overload: no parameters (path /customformat)
        generatedCode.Should().Contain("CustomformatGet()");

        // Second overload: has id parameter (path /customformat/{id})
        generatedCode.Should().Contain("CustomformatGet(int id");
    }

    [Test]
    [Arguments(OpenApiSpec)]
    [Arguments(Swagger2Spec)]
    public async Task Can_Build_Generated_Code(string spec)
    {
        var generatedCode = await GenerateCode(spec);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(string spec)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(spec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                MultipleInterfaces = MultipleInterfaces.ByTag,
                OperationNameGenerator = OperationNameGeneratorTypes.MultipleClientsFromPathSegments
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        }
        finally
        {
            if (File.Exists(swaggerFile))
                File.Delete(swaggerFile);
            var directory = Path.GetDirectoryName(swaggerFile);
            if (directory != null && Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }
}
