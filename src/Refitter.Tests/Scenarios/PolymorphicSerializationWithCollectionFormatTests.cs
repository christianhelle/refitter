using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class PolymorphicSerializationWithCollectionFormatTests
{
    private const string OpenApiSpec = @"
{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Polymorphic API"",
    ""version"": ""1.0.0""
  },
  ""paths"": {
    ""/api/animals"": {
      ""get"": {
        ""operationId"": ""getAnimals"",
        ""summary"": ""Get animals with filters"",
        ""parameters"": [
          {
            ""name"": ""types"",
            ""in"": ""query"",
            ""required"": false,
            ""schema"": {
              ""type"": ""array"",
              ""items"": {
                ""type"": ""string""
              }
            }
          },
          {
            ""name"": ""ids"",
            ""in"": ""query"",
            ""required"": false,
            ""schema"": {
              ""type"": ""array"",
              ""items"": {
                ""type"": ""integer""
              }
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
                  ""items"": {
                    ""$ref"": ""#/components/schemas/Animal""
                  }
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
      ""Animal"": {
        ""type"": ""object"",
        ""discriminator"": {
          ""propertyName"": ""animalType"",
          ""mapping"": {
            ""dog"": ""#/components/schemas/Dog"",
            ""cat"": ""#/components/schemas/Cat""
          }
        },
        ""required"": [""animalType"", ""name""],
        ""properties"": {
          ""animalType"": {
            ""type"": ""string""
          },
          ""name"": {
            ""type"": ""string""
          }
        }
      },
      ""Dog"": {
        ""allOf"": [
          {
            ""$ref"": ""#/components/schemas/Animal""
          },
          {
            ""type"": ""object"",
            ""properties"": {
              ""breed"": {
                ""type"": ""string""
              }
            }
          }
        ]
      },
      ""Cat"": {
        ""allOf"": [
          {
            ""$ref"": ""#/components/schemas/Animal""
          },
          {
            ""type"": ""object"",
            ""properties"": {
              ""color"": {
                ""type"": ""string""
              }
            }
          }
        ]
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
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Contains_Csv_Collection_Format()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Query(CollectionFormat.Csv)");
        generatedCode.Should().Contain("[Query(CollectionFormat.Csv)] IEnumerable<string> types");
        generatedCode.Should().Contain("[Query(CollectionFormat.Csv)] IEnumerable<int> ids");
    }

    [Test]
    public async Task Generated_Code_Contains_Polymorphic_Attributes()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("JsonDerivedType");
        generatedCode.Should().Contain("Dog");
        generatedCode.Should().Contain("Cat");
    }

    [Test]
    public async Task Generated_Code_Contains_Discriminator()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("animalType");
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                UsePolymorphicSerialization = true,
                CollectionFormat = CollectionFormat.Csv
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
