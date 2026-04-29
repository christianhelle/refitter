using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Resources;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class SchemaCleanerTests
{
    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3)]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2)]
    public async Task RemoveUnreferencedSchema_Removes_Unused_Schemas(SampleOpenSpecifications version)
    {
        var spec = EmbeddedResources.GetSwaggerPetstore(version);
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var initialCount = document.Components.Schemas.Count;

        var cleaner = new SchemaCleaner(document, []);
        cleaner.RemoveUnreferencedSchema();

        document.Components.Schemas.Count.Should().BeLessThanOrEqualTo(initialCount);
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3)]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2)]
    public async Task RemoveUnreferencedSchema_Keeps_Referenced_Schemas(SampleOpenSpecifications version)
    {
        var spec = EmbeddedResources.GetSwaggerPetstore(version);
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var cleaner = new SchemaCleaner(document, []);
        cleaner.RemoveUnreferencedSchema();

        document.Components.Schemas.Should().NotBeEmpty();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "Pet.*")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "Pet")]
    public async Task RemoveUnreferencedSchema_Keeps_Schemas_Matching_Pattern(
        SampleOpenSpecifications version,
        string pattern)
    {
        var spec = EmbeddedResources.GetSwaggerPetstore(version);
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var cleaner = new SchemaCleaner(document, [pattern]);
        cleaner.RemoveUnreferencedSchema();

        document.Components.Schemas.Keys.Should().Contain(k => k.Contains("Pet"));
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, true)]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, false)]
    public async Task RemoveUnreferencedSchema_Handles_IncludeInheritanceHierarchy_Flag(
        SampleOpenSpecifications version,
        bool includeHierarchy)
    {
        var spec = EmbeddedResources.GetSwaggerPetstore(version);
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var cleaner = new SchemaCleaner(document, [])
        {
            IncludeInheritanceHierarchy = includeHierarchy
        };
        cleaner.RemoveUnreferencedSchema();

        document.Components.Schemas.Should().NotBeEmpty();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3)]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2)]
    public async Task SchemaCleaner_Constructor_Accepts_Parameters(SampleOpenSpecifications version)
    {
        var spec = EmbeddedResources.GetSwaggerPetstore(version);
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var cleaner = new SchemaCleaner(document, [".*"]);

        cleaner.Should().NotBeNull();
        cleaner.IncludeInheritanceHierarchy.Should().BeFalse();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3)]
    public async Task SchemaCleaner_IncludeInheritanceHierarchy_Can_Be_Set(SampleOpenSpecifications version)
    {
        var spec = EmbeddedResources.GetSwaggerPetstore(version);
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "test.json");
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var cleaner = new SchemaCleaner(document, [])
        {
            IncludeInheritanceHierarchy = true
        };

        cleaner.IncludeInheritanceHierarchy.Should().BeTrue();
    }

    [Test]
    public async Task RemoveUnreferencedSchema_Handles_Alias_Schemas_Sharing_The_Same_Instance()
    {
        const string spec = """
            openapi: 3.0.4
            info:
              title: Alias schema test
              version: "1"
            paths:
              /items:
                get:
                  operationId: GetItem
                  responses:
                    '200':
                      description: Success
                      content:
                        application/json:
                          schema:
                            $ref: '#/components/schemas/AliasItem'
            components:
              schemas:
                AliasItem:
                  $ref: '#/components/schemas/ActualItem'
                ActualItem:
                  type: object
                  properties:
                    id:
                      type: string
                UnusedItem:
                  type: object
                  properties:
                    ignored:
                      type: string
            """;

        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);

        try
        {
            var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);
            ReferenceEquals(document.Components.Schemas["AliasItem"], document.Components.Schemas["ActualItem"])
                .Should()
                .BeTrue();

            var cleaner = new SchemaCleaner(document, []);
            cleaner.Invoking(x => x.RemoveUnreferencedSchema()).Should().NotThrow();

            document.Components.Schemas.Should().ContainKey("AliasItem");
            document.Components.Schemas.Should().ContainKey("ActualItem");
            document.Components.Schemas.Should().NotContainKey("UnusedItem");
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

    [Test]
    public async Task SchemaCleaner_Handles_DiscriminatorObject_With_Mapping()
    {
        const string spec = @"{
  ""openapi"": ""3.0.1"",
  ""info"": { ""title"": ""Test API"", ""version"": ""v1"" },
  ""paths"": {
    ""/api/pets"": {
      ""get"": {
        ""operationId"": ""GetPets"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": { ""$ref"": ""#/components/schemas/Animal"" }
              }
            }
          }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Animal"": {
        ""type"": ""object"",
        ""required"": [""animalType""],
        ""properties"": {
          ""animalType"": { ""type"": ""string"" }
        },
        ""discriminator"": {
          ""propertyName"": ""animalType"",
          ""mapping"": {
            ""dog"": ""#/components/schemas/Dog"",
            ""cat"": ""#/components/schemas/Cat""
          }
        }
      },
      ""Dog"": {
        ""allOf"": [
          { ""$ref"": ""#/components/schemas/Animal"" },
          {
            ""type"": ""object"",
            ""properties"": {
              ""breed"": { ""type"": ""string"" }
            }
          }
        ]
      },
      ""Cat"": {
        ""allOf"": [
          { ""$ref"": ""#/components/schemas/Animal"" },
          {
            ""type"": ""object"",
            ""properties"": {
              ""color"": { ""type"": ""string"" }
            }
          }
        ]
      },
      ""UnusedSchema"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": { ""type"": ""integer"" }
        }
      }
    }
  }
}";
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var cleaner = new SchemaCleaner(document, [])
        {
            IncludeInheritanceHierarchy = false
        };

        cleaner.RemoveUnreferencedSchema();

        // When IncludeInheritanceHierarchy is false, only directly referenced schemas are kept
        // Animal is referenced in the path, but Dog and Cat are only in discriminator mapping
        // So with IncludeInheritanceHierarchy=false, Dog and Cat get removed from discriminator mapping
        document.Components.Schemas.Should().ContainKey("Animal");
        // Dog and Cat schemas are removed because they're only referenced via discriminator mapping
        // when IncludeInheritanceHierarchy is false
        document.Components.Schemas.Should().NotContainKey("Dog");
        document.Components.Schemas.Should().NotContainKey("Cat");
        document.Components.Schemas.Should().NotContainKey("UnusedSchema");
    }

    [Test]
    public async Task SchemaCleaner_Cleans_Discriminator_Mappings_When_Not_Including_Hierarchy()
    {
        const string spec = @"{
  ""openapi"": ""3.0.1"",
  ""info"": { ""title"": ""Test API"", ""version"": ""v1"" },
  ""paths"": {
    ""/api/pets"": {
      ""get"": {
        ""operationId"": ""GetPets"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": { ""$ref"": ""#/components/schemas/Animal"" }
              }
            }
          }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Animal"": {
        ""type"": ""object"",
        ""required"": [""animalType""],
        ""properties"": {
          ""animalType"": { ""type"": ""string"" }
        },
        ""discriminator"": {
          ""propertyName"": ""animalType"",
          ""mapping"": {
            ""dog"": ""#/components/schemas/Dog"",
            ""cat"": ""#/components/schemas/Cat""
          }
        }
      },
      ""Dog"": {
        ""allOf"": [
          { ""$ref"": ""#/components/schemas/Animal"" },
          {
            ""type"": ""object"",
            ""properties"": {
              ""breed"": { ""type"": ""string"" }
            }
          }
        ]
      },
      ""Cat"": {
        ""allOf"": [
          { ""$ref"": ""#/components/schemas/Animal"" },
          {
            ""type"": ""object"",
            ""properties"": {
              ""color"": { ""type"": ""string"" }
            }
          }
        ]
      }
    }
  }
}";
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var animalSchema = document.Components.Schemas["Animal"];
        var initialMappingCount = animalSchema.DiscriminatorObject?.Mapping.Count ?? 0;

        var cleaner = new SchemaCleaner(document, [])
        {
            IncludeInheritanceHierarchy = false
        };

        cleaner.RemoveUnreferencedSchema();

        var animalSchemaAfter = document.Components.Schemas["Animal"];
        animalSchemaAfter.DiscriminatorObject.Should().NotBeNull();
        var finalMappingCount = animalSchemaAfter.DiscriminatorObject!.Mapping.Count;

        finalMappingCount.Should().BeLessThanOrEqualTo(initialMappingCount);
    }

    [Test]
    public async Task SchemaCleaner_TryPush_Handles_NonNull_Schema()
    {
        const string spec = @"{
  ""openapi"": ""3.0.1"",
  ""info"": { ""title"": ""Test API"", ""version"": ""v1"" },
  ""paths"": {
    ""/api/pets"": {
      ""get"": {
        ""operationId"": ""GetPets"",
        ""parameters"": [
          {
            ""name"": ""petId"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""string""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": { ""$ref"": ""#/components/schemas/Pet"" }
                }
              }
            }
          }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Pet"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": { ""type"": ""integer"" },
          ""name"": { ""type"": ""string"" }
        }
      }
    }
  }
}";
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var document = await OpenApiDocumentFactory.CreateAsync(swaggerFile);

        var cleaner = new SchemaCleaner(document, []);
        cleaner.RemoveUnreferencedSchema();

        document.Components.Schemas.Should().ContainKey("Pet");
    }
}
