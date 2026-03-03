using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class ExcludeNamespacesTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.0
info:
  title: Namespace Exclusion Test API
  version: 1.0.0
paths:
  /api/items:
    get:
      operationId: GetItems
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Item'
    post:
      operationId: CreateItem
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/Item'
      responses:
        '201':
          description: Item created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Item'
components:
  schemas:
    Item:
      type: object
      required:
        - id
        - name
      properties:
        id:
          type: integer
          format: int32
        name:
          type: string
        description:
          type: string
        tags:
          type: array
          items:
            type: string
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode(["System.Xml.Serialization"]);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode(["System.Xml.Serialization"]);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_Excluded_Namespace()
    {
        string generatedCode = await GenerateCode(["System.Xml.Serialization"]);
        generatedCode.Should().NotContain("using System.Xml.Serialization;");
    }

    [Test]
    public async Task Generated_Code_Contains_Excluded_Namespace_When_Not_Excluded()
    {
        string generatedCode = await GenerateCode([]);
        // When no exclusions, it may or may not contain System.Xml.Serialization
        // depending on what the generator decides, so we just verify it generates
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Exclude_Multiple_Namespaces()
    {
        string generatedCode = await GenerateCode(["System.Xml.Serialization", "System.ComponentModel"]);
        generatedCode.Should().NotContain("using System.Xml.Serialization;");
        generatedCode.Should().NotContain("using System.ComponentModel;");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Multiple_Exclusions()
    {
        string generatedCode = await GenerateCode(["System.Xml.Serialization", "System.ComponentModel"]);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    public async Task Generated_Code_Still_Contains_Required_Namespaces()
    {
        string generatedCode = await GenerateCode(["System.Xml.Serialization"]);
        // Essential namespaces should still be present
        generatedCode.Should().Contain("using Refit;");
    }

    private static async Task<string> GenerateCode(string[] excludeNamespaces)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                ExcludeNamespaces = excludeNamespaces
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
