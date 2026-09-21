using AwesomeAssertions;
using Microsoft.OpenApi.Reader;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;


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
    public async Task Json_External_References_Are_Resolved_By_The_Round_Trip()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        var mainSpec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": { ""title"": ""Round Trip Test"", ""version"": ""1.0.0"" },
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
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(Path.Combine(folder, "components.json"), componentsSpec);

        var strategy = new OpenApiReaderDocumentStrategy();
        var result = await strategy.TryLoadAsync(mainFile);

        result.Should().NotBeNull();

        // Microsoft.OpenApi normalizes the spec version to the latest patch of the
        // detected version, so "3.0.4" proves the document came out of the round trip
        // rather than out of the NSwag fallback, which would report the original "3.0.0".
        result!.OpenApi.Should().Be("3.0.4");

        // The external component is only reachable if NSwag was given the document path.
        result.Components.Schemas.Should().ContainKey("User");
        result.Components.Schemas["User"].Properties.Should().ContainKey("id");
        result.Components.Schemas["User"].Properties.Should().ContainKey("name");

        Directory.Delete(folder, true);
    }

    [Test]
    public async Task Yaml_External_References_Are_Resolved_By_The_Round_Trip()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        var mainSpec = @"openapi: 3.0.0
info:
  title: YAML Round Trip Test
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
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(Path.Combine(folder, "components.yaml"), componentsSpec);

        var strategy = new OpenApiReaderDocumentStrategy();
        var result = await strategy.TryLoadAsync(mainFile);

        result.Should().NotBeNull();
        result!.OpenApi.Should().Be("3.0.4");
        result.Components.Schemas.Should().ContainKey("User");
        result.Components.Schemas["User"].Properties.Should().ContainKey("id");
        result.Components.Schemas["User"].Properties.Should().ContainKey("name");

        Directory.Delete(folder, true);
    }

    [Test]
    public async Task Populates_Missing_Info_On_The_Returned_Document()
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        // No "info" block at all. The NSwag fallback leaves Info null for this
        // document, so a populated Info can only come from the round trip.
        var mainSpec = @"{
  ""openapi"": ""3.0.0"",
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
      ""User"": { ""type"": ""object"", ""properties"": { ""id"": { ""type"": ""integer"" } } }
    }
  }
}";

        var mainFile = Path.Combine(folder, "no-info.json");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(Path.Combine(folder, "components.json"), componentsSpec);

        var strategy = new OpenApiReaderDocumentStrategy();
        var result = await strategy.TryLoadAsync(mainFile);

        result.Should().NotBeNull();
        result!.Info.Should().NotBeNull();
        result.Info.Title.Should().Be("no-info");
        result.Info.Version.Should().NotBeNullOrEmpty();
        result.Components.Schemas.Should().ContainKey("User");

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
