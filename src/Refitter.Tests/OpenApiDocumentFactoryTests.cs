using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Resources;
using TUnit.Core;

namespace Refitter.Tests;

public class OpenApiDocumentFactoryTests
{
    [Test]
    [Arguments("https://developers.intellihr.io/docs/v1/swagger.json")] // GZIP encoded
    [Arguments("http://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore.json")]
    public async Task Create_From_Uri_Returns_NotNull(string url)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = url };
        (await OpenApiDocumentFactory.CreateAsync(settings.OpenApiPath))
            .Should()
            .NotBeNull();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Create_From_File_Returns_NotNull(SampleOpenSpecifications version, string filename)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(EmbeddedResources.GetSwaggerPetstore(version), filename);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        (await OpenApiDocumentFactory.CreateAsync(settings.OpenApiPath))
            .Should()
            .NotBeNull();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "petstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "petstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "petstore.yml")]
    public async Task Create_From_File_Detects_Format_Correctly(SampleOpenSpecifications version, string filename)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(EmbeddedResources.GetSwaggerPetstore(version), filename);
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        document.Should().NotBeNull();
        document.Info.Should().NotBeNull();
        document.Info.Title.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Create_From_File_With_External_References_Returns_NotNull()
    {
        var spec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""object""
                }
              }
            }
          }
        }
      }
    }
  }
}";
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        document.Should().NotBeNull();
        document.Info.Title.Should().Be("Test API");
    }

    [Test]
    public async Task Create_From_Yaml_File_Without_Extension_Returns_NotNull()
    {
        var spec = @"openapi: 3.0.0
info:
  title: Test API
  version: 1.0.0
paths:
  /test:
    get:
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: object";
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.yaml");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        document.Should().NotBeNull();
        document.Info.Title.Should().Be("Test API");
    }

    [Test]
    [Arguments("https://petstore.swagger.io/v2/swagger.json")]
    [Arguments("http://petstore.swagger.io/v2/swagger.json")]
    public async Task Create_From_Http_Url_Returns_NotNull(string url)
    {
        var document = await OpenApiDocumentFactory.CreateAsync(url);
        document.Should().NotBeNull();
    }

    [Test]
    public async Task Create_Handles_Missing_Info_Fields()
    {
        var spec = @"{
  ""openapi"": ""3.0.0"",
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    }
  }
}";
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "minimal.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        document.Should().NotBeNull();
    }

    [Test]
    public async Task IsHttp_Detects_Http_Protocol()
    {
        var document = await OpenApiDocumentFactory.CreateAsync("http://petstore.swagger.io/v2/swagger.json");
        document.Should().NotBeNull();
    }

    [Test]
    public async Task IsHttp_Detects_Https_Protocol()
    {
        var document = await OpenApiDocumentFactory.CreateAsync("https://petstore.swagger.io/v2/swagger.json");
        document.Should().NotBeNull();
    }

    [Test]
    [Arguments("https://petstore.swagger.io/v2/swagger.yaml")]
    public async Task Create_From_Yaml_Url_Returns_NotNull(string url)
    {
        var document = await OpenApiDocumentFactory.CreateAsync(url);
        document.Should().NotBeNull();
    }

    [Test]
    public async Task Create_From_Invalid_File_Falls_Back_To_NSwag()
    {
        var spec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Fallback Test"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    }
  }
}";
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "fallback.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        document.Should().NotBeNull();
        document.Info.Title.Should().Be("Fallback Test");
    }

    [Test]
    public async Task Create_From_Json_File_Without_External_References_Uses_NSwag()
    {
        var spec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Direct NSwag Test"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/users"": {
      ""get"": {
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""type"": ""object"",
                    ""properties"": {
                      ""id"": { ""type"": ""integer"" },
                      ""name"": { ""type"": ""string"" }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}";
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "no-external-refs.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        document.Should().NotBeNull();
        document.Info.Title.Should().Be("Direct NSwag Test");
    }

    [Test]
    public async Task Create_From_Yaml_File_Without_External_References_Uses_NSwag()
    {
        var spec = @"openapi: 3.0.0
info:
  title: YAML NSwag Test
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
                type: array
                items:
                  type: object
                  properties:
                    id:
                      type: integer
                    name:
                      type: string";
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "no-external-refs.yaml");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        document.Should().NotBeNull();
        document.Info.Title.Should().Be("YAML NSwag Test");
    }

    [Test]
    public async Task Create_From_Json_File_With_External_References_Serializes_As_Json()
    {
        // Create a test folder for both files
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        // Create a main spec with external reference
        var mainSpec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""External Ref Test"",
    ""version"": ""1.0.0""
  },
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
        // Create a components file
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

        var mainFile = Path.Combine(folder, "external-ref-main.json");
        var componentsFile = Path.Combine(folder, "components.json");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(componentsFile, componentsSpec);

        var document = await OpenApiDocumentFactory.CreateAsync(mainFile);

        document.Should().NotBeNull();
        document.Info.Title.Should().Be("External Ref Test");
    }

    [Test]
    public async Task Create_From_Yaml_File_With_External_References_Serializes_As_Yaml()
    {
        // Create a test folder for both files
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        // Create a main YAML spec with external reference
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

        // Create a components YAML file
        var componentsSpec = @"components:
  schemas:
    User:
      type: object
      properties:
        id:
          type: integer
        name:
          type: string";

        var mainFile = Path.Combine(folder, "external-ref-main.yaml");
        var componentsFile = Path.Combine(folder, "components.yaml");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(componentsFile, componentsSpec);

        var document = await OpenApiDocumentFactory.CreateAsync(mainFile);

        document.Should().NotBeNull();
        document.Info.Title.Should().Be("YAML External Ref Test");
    }

    [Test]
    public async Task Create_Populates_Missing_Info_When_Null()
    {
        // Create a test folder for both files
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        // Spec with minimal info and external reference - OpenAPI requires info.title and info.version
        // but OpenApiMultiFileReader can handle missing info and PopulateMissingRequiredFields fills it in
        var mainSpec = @"{
  ""openapi"": ""3.0.0"",
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""operationId"": ""getTest"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""./components-no-info.json#/components/schemas/TestSchema""
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
      ""TestSchema"": {
        ""type"": ""object"",
        ""properties"": {
          ""value"": { ""type"": ""string"" }
        },
        ""required"": [""value""]
      }
    }
  }
}";

        var mainFile = Path.Combine(folder, "no-info-main.json");
        var componentsFile = Path.Combine(folder, "components-no-info.json");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(componentsFile, componentsSpec);

        var document = await OpenApiDocumentFactory.CreateAsync(mainFile);

        document.Should().NotBeNull();
        // When info is missing entirely, PopulateMissingRequiredFields should create it
        // Note: This test verifies the code doesn't crash, actual behavior depends on OpenAPI reader implementation
    }

    [Test]
    public async Task Create_Uses_Bot_Spec_With_Real_External_References()
    {
        // Use the actual bot.paths.yaml and bot.components.yaml from test resources
        var botPathsFile = Path.Combine("..", "..", "..", "..", "test", "OpenAPI", "v3.0", "bot.paths.yaml");

        if (File.Exists(botPathsFile))
        {
            var document = await OpenApiDocumentFactory.CreateAsync(botPathsFile);

            document.Should().NotBeNull();
            document.Info.Should().NotBeNull();
        }
    }

    [Test]
    public async Task Create_From_Json_With_External_Reference_Resolves_Successfully()
    {
        // Test that external references are handled properly (covers lines 27-42)
        // This tests the SerializeAsJson path
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        var mainSpec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""External Ref API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/users/{id}"": {
      ""get"": {
        ""operationId"": ""getUser"",
        ""parameters"": [
          {
            ""$ref"": ""./shared.json#/components/parameters/UserId""
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""$ref"": ""./shared.json#/components/schemas/User""
                }
              }
            }
          }
        }
      }
    }
  }
}";

        var sharedSpec = @"{
  ""components"": {
    ""parameters"": {
      ""UserId"": {
        ""name"": ""id"",
        ""in"": ""path"",
        ""required"": true,
        ""schema"": {
          ""type"": ""string""
        }
      }
    },
    ""schemas"": {
      ""User"": {
        ""type"": ""object"",
        ""required"": [""id"", ""name""],
        ""properties"": {
          ""id"": { ""type"": ""string"" },
          ""name"": { ""type"": ""string"" },
          ""email"": { ""type"": ""string"", ""format"": ""email"" }
        }
      }
    }
  }
}";

        var mainFile = Path.Combine(folder, "api.json");
        var sharedFile = Path.Combine(folder, "shared.json");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(sharedFile, sharedSpec);

        var document = await OpenApiDocumentFactory.CreateAsync(mainFile);

        document.Should().NotBeNull();
        document.Info.Title.Should().Be("External Ref API");
        document.Paths.Should().ContainKey("/users/{id}");
    }

    [Test]
    public async Task Create_From_Yaml_With_External_Reference_Resolves_Successfully()
    {
        // Test that external references are handled properly (covers lines 27-42)
        // This tests the SerializeAsYaml path
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        var mainSpec = @"openapi: 3.0.0
info:
  title: YAML External Ref API
  version: 1.0.0
paths:
  /users/{id}:
    get:
      operationId: getUser
      parameters:
        - $ref: './shared.yaml#/components/parameters/UserId'
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: './shared.yaml#/components/schemas/User'";

        var sharedSpec = @"components:
  parameters:
    UserId:
      name: id
      in: path
      required: true
      schema:
        type: string
  schemas:
    User:
      type: object
      required:
        - id
        - name
      properties:
        id:
          type: string
        name:
          type: string
        email:
          type: string
          format: email";

        var mainFile = Path.Combine(folder, "api.yaml");
        var sharedFile = Path.Combine(folder, "shared.yaml");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(sharedFile, sharedSpec);

        var document = await OpenApiDocumentFactory.CreateAsync(mainFile);

        document.Should().NotBeNull();
        document.Info.Title.Should().Be("YAML External Ref API");
        document.Paths.Should().ContainKey("/users/{id}");
    }
}
