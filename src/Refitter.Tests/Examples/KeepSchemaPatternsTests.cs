using FluentAssertions;

using Refitter.Core;
using Refitter.Tests.Build;

using Xunit;

namespace Refitter.Tests.Examples;

public class KeepSchemaPatternsTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.1
paths: {}
components:
  schemas:
    Metadata:
      type: object
      properties:
        createdAt:
          type: string
          format: date-time
        createdBy:
          type: string
          nullable: true
        lastModifiedAt:
          type: string
          format: date-time
        lastModifiedBy:
          type: string
          nullable: true
      additionalProperties: false
    SomeComponent:
      required:
        - $type
      type: object
      allOf:
        - $ref: '#/components/schemas/Component'
      properties:
        $type:
          type: string
        typeId:
          type: integer
          format: int64
      additionalProperties: false
      discriminator:
        propertyName: $type
    SomeComponentState:
      enum:
        - Active
        - Inactive
        - Blocked
        - Deleted
      type: string
    SomeComponentType:
      type: object
      allOf:
        - $ref: '#/components/schemas/Component'
      properties:
        state:
          $ref: '#/components/schemas/SomeComponentState'
        isBaseRole:
          type: boolean
        name:
          type: string
          nullable: true
        numberingId:
          type: string
          nullable: true
      additionalProperties: false
    Component:
      type: object
      properties:
        id:
          type: integer
          format: int64
        metadata:
          $ref: '#/components/schemas/Metadata'
      additionalProperties: false
    LoadingAddress:
      type: object
      allOf:
        - $ref: '#/components/schemas/SomeComponent'
      properties:
        info:
          type: string
          nullable: true
      additionalProperties: false
    Warehouse:
      type: object
      allOf:
        - $ref: '#/components/schemas/SomeComponent'
      properties:
        info:
          type: string
          nullable: true
      additionalProperties: false
    UserComponent:
      type: object
      allOf:
        - $ref: '#/components/schemas/SomeComponent'
      properties:
        info:
          type: string
          nullable: true
      additionalProperties: false
    UserComponent2:
      type: object
      allOf:
        - $ref: '#/components/schemas/UserComponent'
      properties:
        info2:
          type: string
          nullable: true
      additionalProperties: false
";

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Keeps_Unreferenced_Schema()
    {
        string generateCode = await GenerateCode();

        generateCode.Should().NotContain("class UserComponent2");
        generateCode.Should().Contain("class UserComponent");
        generateCode.Should().Contain("class SomeComponent");
        generateCode.Should().Contain("class Component");
        generateCode.Should().Contain("class Metadata");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generateCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings {OpenApiPath = swaggerFile, TrimUnusedSchema = true, KeepSchemaPatterns = new[] {"^UserComponent$"},};

        var sut = await RefitGenerator.CreateAsync(settings);
        var generateCode = sut.Generate();
        return generateCode;
    }

    private static async Task<string> CreateSwaggerFile(string contents)
    {
        var filename = $"{Guid.NewGuid()}.yml";
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}