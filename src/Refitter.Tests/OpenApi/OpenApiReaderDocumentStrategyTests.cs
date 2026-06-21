using FluentAssertions;
using Microsoft.OpenApi.Reader;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;


[Category("Unit")]
public class OpenApiReaderDocumentStrategyTests
{


    [Test]
    public async Task Returns_Null_When_No_External_References()
    {
        var spec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": { ""title"": ""Test"", ""version"": ""1.0.0"" },
  ""paths"": {}
}";
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "no-refs.json");
        var strategy = new OpenApiReaderDocumentStrategy();
        var result = await strategy.TryLoadAsync(swaggerFile);

        result.Should().BeNull();
    }

    [Test]
    public async Task Handles_Json_With_External_References()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        var mainSpec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": { ""title"": ""External Ref Test"", ""version"": ""1.0.0"" },
  ""paths"": {
    ""/users"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""./components.json#/components/schemas/User""
                }
              }
            }
          }
        }
      }
    }
  }
}";
        var componentsSpec = @"{
  ""components"": {
    ""schemas"": {
      ""User"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": { ""type"": ""integer"" },
          ""name"": { ""type"": ""string"" }
        }
      }
    }
  }
}";

        var mainFile = Path.Combine(folder, "main.json");
        var componentsFile = Path.Combine(folder, "components.json");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(componentsFile, componentsSpec);

        var strategy = new OpenApiReaderDocumentStrategy();
        var result = await strategy.TryLoadAsync(mainFile);

        result.Should().NotBeNull();
        result!.Info.Title.Should().Be("External Ref Test");

        Directory.Delete(folder, true);
    }

    [Test]
    public async Task Handles_Yaml_With_External_References()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        var mainSpec = @"openapi: 3.0.0
info:
  title: YAML External Ref Test
  version: 1.0.0
paths:
  /users:
    get:
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: './components.yaml#/components/schemas/User'";

        var componentsSpec = @"components:
  schemas:
    User:
      type: object
      properties:
        id:
          type: integer
        name:
          type: string";

        var mainFile = Path.Combine(folder, "main.yaml");
        var componentsFile = Path.Combine(folder, "components.yaml");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(componentsFile, componentsSpec);

        var strategy = new OpenApiReaderDocumentStrategy();
        var result = await strategy.TryLoadAsync(mainFile);

        result.Should().NotBeNull();
        result!.Info.Title.Should().Be("YAML External Ref Test");

        Directory.Delete(folder, true);
    }

    [Test]
    public async Task Returns_Null_On_Invalid_Spec()
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(
            "not valid",
            "invalid.json");
        var strategy = new OpenApiReaderDocumentStrategy();
        var result = await strategy.TryLoadAsync(swaggerFile);

        result.Should().BeNull();
    }
}
