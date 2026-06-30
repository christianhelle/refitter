using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;


public class PrimitiveAllOfTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.1
info:
  title: Test
  version: v1
paths:
  /status:
    post:
      operationId: createStatus
      requestBody:
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/StatusUpdateRequest'
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/filters'
components:
  schemas:
    StatusUpdateBase:
      type: object
      properties:
        text:
          type: string
    StatusUpdateRequest:
      allOf:
        - $ref: '#/components/schemas/StatusUpdateBase'
        - type: object
          required:
            - parent
          properties:
            parent:
              allOf:
                - type: string
                  description: The id of parent to send this status update to.
            priority:
              allOf:
                - type: string
                  enum: [low, medium, high]
            count:
              allOf:
                - type: integer
                  format: int32
    filters:
      type: object
      properties:
        name:
          type: string
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Flattens_Single_Primitive_AllOf_To_Primitive_Property()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public string Parent");
    }

    [Test]
    public async Task Does_Not_Derive_Class_From_Sealed_Primitive_Type()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should()
            .NotMatchRegex(@"class \w+ : (string|int|long|double|bool|decimal|float)\b");
    }

    [Test]
    public async Task Preserves_Enum_From_Single_Primitive_AllOf()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("enum");
        generatedCode.Should().Contain("Low");
        generatedCode.Should().Contain("Medium");
        generatedCode.Should().Contain("High");
    }

    [Test]
    public async Task PascalCases_Lowercase_Schema_Name()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("class filters");
        generatedCode.Should().NotContain("enum filters");
    }

    [Category("Integration")]
    [Test]
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
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
