using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class OpenApiDocumentFactoryMergeTests
{
    [Test]
    public async Task Merge_Preserves_Tags_From_Base_Document()
    {
        // Test that when the base document has tags defined, they are preserved in the merge
        var specWithTags = @"openapi: '3.0.0'
info:
  title: API With Tags
  version: '1.0'
tags:
  - name: users
    description: User operations
  - name: pets
    description: Pet operations
paths:
  /users:
    get:
      operationId: listUsers
      tags:
        - users
      responses:
        '200':
          description: Success
";

        var specWithoutTags = @"openapi: '3.0.0'
info:
  title: API Without Tags
  version: '1.0'
paths:
  /products:
    get:
      operationId: listProducts
      responses:
        '200':
          description: Success
";

        var file1 = await TestFile.CreateSwaggerFile(specWithTags, "with-tags.yaml");
        var file2 = await TestFile.CreateSwaggerFile(specWithoutTags, "without-tags.yaml");

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file2 });

        merged.Should().NotBeNull();
        merged.Tags.Should().NotBeNull();
        merged.Tags.Should().HaveCount(2);
        merged.Tags.Should().Contain(t => t.Name == "users");
        merged.Tags.Should().Contain(t => t.Name == "pets");
    }

    [Test]
    public async Task Merge_Combines_Tags_From_Multiple_Documents()
    {
        // Test that tags from both documents are merged, testing the tag name selector lambda
        var spec1 = @"openapi: '3.0.0'
info:
  title: API 1
  version: '1.0'
tags:
  - name: users
    description: User operations
paths:
  /users:
    get:
      operationId: listUsers
      tags:
        - users
      responses:
        '200':
          description: Success
";

        var spec2 = @"openapi: '3.0.0'
info:
  title: API 2
  version: '1.0'
tags:
  - name: products
    description: Product operations
  - name: orders
    description: Order operations
paths:
  /products:
    get:
      operationId: listProducts
      tags:
        - products
      responses:
        '200':
          description: Success
";

        var file1 = await TestFile.CreateSwaggerFile(spec1, "api1.yaml");
        var file2 = await TestFile.CreateSwaggerFile(spec2, "api2.yaml");

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file2 });

        merged.Should().NotBeNull();
        merged.Tags.Should().NotBeNull();
        merged.Tags.Should().HaveCount(3);
        merged.Tags.Should().Contain(t => t.Name == "users");
        merged.Tags.Should().Contain(t => t.Name == "products");
        merged.Tags.Should().Contain(t => t.Name == "orders");
    }

    [Test]
    public async Task Merge_Does_Not_Duplicate_Tags_With_Same_Name()
    {
        // Test that when multiple documents have the same tag name, it's not duplicated
        var spec1 = @"openapi: '3.0.0'
info:
  title: API 1
  version: '1.0'
tags:
  - name: shared
    description: Shared operations from API 1
paths:
  /users:
    get:
      operationId: listUsers
      tags:
        - shared
      responses:
        '200':
          description: Success
";

        var spec2 = @"openapi: '3.0.0'
info:
  title: API 2
  version: '1.0'
tags:
  - name: shared
    description: Shared operations from API 2
paths:
  /products:
    get:
      operationId: listProducts
      tags:
        - shared
      responses:
        '200':
          description: Success
";

        var file1 = await TestFile.CreateSwaggerFile(spec1, "api1-shared.yaml");
        var file2 = await TestFile.CreateSwaggerFile(spec2, "api2-shared.yaml");

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file2 });

        merged.Should().NotBeNull();
        merged.Tags.Should().NotBeNull();
        merged.Tags.Should().HaveCount(1);
        merged.Tags.Should().Contain(t => t.Name == "shared");
    }

    [Test]
    public async Task Merge_Creates_Tags_Collection_When_Base_Has_None_But_Second_Does()
    {
        // Test that when base document has no tags but second document does, tags are added
        var specWithoutTags = @"openapi: '3.0.0'
info:
  title: API Without Tags
  version: '1.0'
paths:
  /users:
    get:
      operationId: listUsers
      responses:
        '200':
          description: Success
";

        var specWithTags = @"openapi: '3.0.0'
info:
  title: API With Tags
  version: '1.0'
tags:
  - name: products
    description: Product operations
paths:
  /products:
    get:
      operationId: listProducts
      tags:
        - products
      responses:
        '200':
          description: Success
";

        var file1 = await TestFile.CreateSwaggerFile(specWithoutTags, "no-tags.yaml");
        var file2 = await TestFile.CreateSwaggerFile(specWithTags, "with-tags.yaml");

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file2 });

        merged.Should().NotBeNull();
        merged.Tags.Should().NotBeNull();
        merged.Tags.Should().HaveCount(1);
        merged.Tags.Should().Contain(t => t.Name == "products");
    }

    [Test]
    public async Task PopulateMissingRequiredFields_Handles_External_References()
    {
        // Test that external reference handling works correctly
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        var mainSpec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""1.0.0""
  },
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
                  ""$ref"": ""./components.json#/components/schemas/TestModel""
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
      ""TestModel"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": { ""type"": ""string"" }
        }
      }
    }
  }
}";

        var mainFile = Path.Combine(folder, "test-api.json");
        var componentsFile = Path.Combine(folder, "components.json");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(componentsFile, componentsSpec);

        var document = await OpenApiDocumentFactory.CreateAsync(mainFile);

        document.Should().NotBeNull();
        document.Info.Should().NotBeNull();
        document.Info.Title.Should().Be("Test API");
        document.Info.Version.Should().Be("1.0.0");
    }

    [Test]
    public async Task PopulateMissingRequiredFields_Preserves_Existing_Title()
    {
        // Test that when title is present, it's preserved
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        var mainSpec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""My API Spec"",
    ""version"": ""1.0.0""
  },
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
                  ""$ref"": ""./components.json#/components/schemas/TestModel""
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
      ""TestModel"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": { ""type"": ""string"" }
        }
      }
    }
  }
}";

        var mainFile = Path.Combine(folder, "my-api-spec.json");
        var componentsFile = Path.Combine(folder, "components.json");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(componentsFile, componentsSpec);

        var document = await OpenApiDocumentFactory.CreateAsync(mainFile);

        document.Should().NotBeNull();
        document.Info.Should().NotBeNull();
        document.Info.Title.Should().Be("My API Spec");
        document.Info.Version.Should().Be("1.0.0");
    }

    [Test]
    public async Task PopulateMissingRequiredFields_Creates_Info_With_External_Refs()
    {
        // Test that when Info is minimal with external references, it's populated
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        var mainSpec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": {
    ""title"": ""Minimal API"",
    ""version"": ""1.0.0""
  },
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
                  ""$ref"": ""./components.json#/components/schemas/TestModel""
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
      ""TestModel"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": { ""type"": ""string"" }
        }
      }
    }
  }
}";

        var mainFile = Path.Combine(folder, "minimal-info.json");
        var componentsFile = Path.Combine(folder, "components.json");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(componentsFile, componentsSpec);

        var document = await OpenApiDocumentFactory.CreateAsync(mainFile);

        document.Should().NotBeNull();
        document.Info.Should().NotBeNull();
        document.Info.Title.Should().NotBeNullOrEmpty();
        document.Info.Version.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task Merge_Handles_Three_Documents_With_Tags()
    {
        // Test merging more than two documents with various tag scenarios
        var spec1 = @"openapi: '3.0.0'
info:
  title: API 1
  version: '1.0'
tags:
  - name: users
paths:
  /users:
    get:
      operationId: listUsers
      responses:
        '200':
          description: Success
";

        var spec2 = @"openapi: '3.0.0'
info:
  title: API 2
  version: '1.0'
tags:
  - name: products
paths:
  /products:
    get:
      operationId: listProducts
      responses:
        '200':
          description: Success
";

        var spec3 = @"openapi: '3.0.0'
info:
  title: API 3
  version: '1.0'
tags:
  - name: orders
  - name: users
paths:
  /orders:
    get:
      operationId: listOrders
      responses:
        '200':
          description: Success
";

        var file1 = await TestFile.CreateSwaggerFile(spec1, "api1.yaml");
        var file2 = await TestFile.CreateSwaggerFile(spec2, "api2.yaml");
        var file3 = await TestFile.CreateSwaggerFile(spec3, "api3.yaml");

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file2, file3 });

        merged.Should().NotBeNull();
        merged.Tags.Should().NotBeNull();
        merged.Tags.Should().HaveCount(3);
        merged.Tags.Should().Contain(t => t.Name == "users");
        merged.Tags.Should().Contain(t => t.Name == "products");
        merged.Tags.Should().Contain(t => t.Name == "orders");
    }
}
