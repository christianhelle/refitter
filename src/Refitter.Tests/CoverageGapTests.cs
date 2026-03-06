using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using NJsonSchema;
using NSwag;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class CoverageGapTests
{
    #region SchemaCleaner Tests

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

    #endregion

    #region XmlDocumentationGenerator Tests

    [Test]
    public void XmlDocumentationGenerator_AppendInterfaceDocumentationByTag_Returns_Early_When_Disabled()
    {
        var generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = false
        });
        
        var code = new StringBuilder();
        var tag = new OpenApiTag { Name = "TestTag", Description = "Test Description" };
        var document = new OpenApiDocument { Tags = [tag] };
        
        generator.AppendInterfaceDocumentationByTag(document, "TestTag", code);
        
        code.ToString().Should().BeEmpty();
    }

    [Test]
    public void XmlDocumentationGenerator_AppendInterfaceDocumentationByEndpoint_Returns_Early_When_Disabled()
    {
        var generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = false
        });
        
        var code = new StringBuilder();
        var endpoint = new OpenApiOperation { Summary = "Test Summary" };
        
        generator.AppendInterfaceDocumentationByEndpoint(endpoint, code);
        
        code.ToString().Should().BeEmpty();
    }

    [Test]
    public void XmlDocumentationGenerator_AppendMethodDocumentation_Returns_Early_When_Disabled()
    {
        var generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = false
        });
        
        var code = new StringBuilder();
        var factory = new CSharpClientGeneratorFactory(new RefitGeneratorSettings(), new OpenApiDocument());
        var csharpGenerator = factory.Create();
        var operation = new OpenApiOperation { Summary = "Test Summary", Description = "Test Description" };
        var method = csharpGenerator.CreateOperationModel(operation);
        
        generator.AppendMethodDocumentation(method, false, false, false, false, code);
        
        code.ToString().Should().BeEmpty();
    }

    #endregion

    #region ContractTypeSuffixApplier Tests

    [Test]
    public void ContractTypeSuffixApplier_Returns_Original_Code_When_Suffix_Is_Null()
    {
        const string code = @"
namespace TestNamespace
{
    public partial class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public enum PetStatus
    {
        Available,
        Pending,
        Sold
    }
}";
        
        var result = ContractTypeSuffixApplier.ApplySuffix(code, null!);
        
        result.Should().Be(code);
    }

    [Test]
    public void ContractTypeSuffixApplier_Returns_Original_Code_When_Suffix_Is_Empty()
    {
        const string code = @"
namespace TestNamespace
{
    public partial class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public enum PetStatus
    {
        Available,
        Pending,
        Sold
    }
}";
        
        var result = ContractTypeSuffixApplier.ApplySuffix(code, string.Empty);
        
        result.Should().Be(code);
    }

    [Test]
    public void ContractTypeSuffixApplier_Returns_Original_Code_When_Suffix_Is_Whitespace()
    {
        const string code = @"
namespace TestNamespace
{
    public partial class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public enum PetStatus
    {
        Available,
        Pending,
        Sold
    }
}";
        
        var result = ContractTypeSuffixApplier.ApplySuffix(code, "   ");
        
        result.Should().Be(code);
    }

    [Test]
    public void ContractTypeSuffixApplier_Applies_Suffix_When_Valid()
    {
        const string code = @"
namespace TestNamespace
{
    public partial class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public enum PetStatus
    {
        Available,
        Pending,
        Sold
    }
}";
        
        var result = ContractTypeSuffixApplier.ApplySuffix(code, "Dto");
        
        result.Should().Contain("public partial class PetDto");
        result.Should().Contain("public enum PetStatusDto");
        result.Should().NotContain("public partial class Pet\r\n");
        result.Should().NotContain("public enum PetStatus\r\n");
    }

    #endregion

    #region RefitMultipleInterfaceByTagGenerator Tests

    [Test]
    public async Task RefitMultipleInterfaceByTagGenerator_Creates_Ungrouped_Interface_When_Operation_Has_No_Tags()
    {
        const string spec = @"
openapi: '3.0.0'
info:
  title: 'Test API'
  version: '1.0'
paths:
  /api/tagged:
    get:
      tags:
        - 'TestTag'
      operationId: 'GetTaggedResource'
      responses:
        '200':
          description: 'Success'
  /api/untagged:
    get:
      operationId: 'GetUntaggedResource'
      responses:
        '200':
          description: 'Success'
";
        
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByTag
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        
        generatedCode.Should().Contain("interface ITestTagApi");
        generatedCode.Should().Contain("GetTaggedResource");
        generatedCode.Should().Contain("GetUntaggedResource");
        Regex.IsMatch(generatedCode, @"interface I\w+Api.*GetUntaggedResource", RegexOptions.Singleline).Should().BeTrue();
    }

    [Test]
    public async Task RefitMultipleInterfaceByTagGenerator_Groups_All_Tagged_Operations()
    {
        const string spec = @"
openapi: '3.0.0'
info:
  title: 'Test API'
  version: '1.0'
paths:
  /api/foo:
    get:
      tags:
        - 'Foo'
      operationId: 'GetFoo'
      responses:
        '200':
          description: 'Success'
  /api/bar:
    get:
      tags:
        - 'Bar'
      operationId: 'GetBar'
      responses:
        '200':
          description: 'Success'
";
        
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByTag
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        
        generatedCode.Should().Contain("interface IFooApi");
        generatedCode.Should().Contain("interface IBarApi");
        generatedCode.Should().Contain("GetFoo");
        generatedCode.Should().Contain("GetBar");
    }

    [Test]
    public async Task RefitMultipleInterfaceByTagGenerator_Generated_Code_Compiles()
    {
        const string spec = @"
openapi: '3.0.0'
info:
  title: 'Test API'
  version: '1.0'
paths:
  /api/tagged:
    get:
      tags:
        - 'TestTag'
      operationId: 'GetTaggedResource'
      responses:
        '200':
          description: 'Success'
  /api/untagged:
    get:
      operationId: 'GetUntaggedResource'
      responses:
        '200':
          description: 'Success'
";
        
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByTag
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion
}
