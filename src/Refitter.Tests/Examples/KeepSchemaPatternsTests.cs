using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
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
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Keeps_Unreferenced_Schema()
    {
        string generatedCode = await GenerateCode();

        generatedCode.Should().NotContain("class UserComponent2");
        generatedCode.Should().Contain("class UserComponent");
        generatedCode.Should().Contain("class SomeComponent");
        generatedCode.Should().Contain("class Component");
        generatedCode.Should().Contain("class Metadata");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile, TrimUnusedSchema = true, KeepSchemaPatterns = new[] { "^UserComponent$" }, };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
