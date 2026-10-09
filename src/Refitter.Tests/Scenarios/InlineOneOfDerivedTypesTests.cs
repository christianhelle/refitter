using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class InlineOneOfDerivedTypesTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.1
info:
  title: Test
  version: v1
paths:
  /api/questionnaires:
    get:
      operationId: GetQuestionnaire
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Questionnaire'
components:
  schemas:
    Questionnaire:
      type: object
      properties:
        questions:
          type: array
          items:
            oneOf:
              - $ref: '#/components/schemas/NewQuestion'
              - $ref: '#/components/schemas/ExistingQuestion'
    QuestionnaireQuestion:
      required:
        - $type
      type: object
      properties:
        $type:
          type: string
      discriminator:
        propertyName: $type
        mapping:
          new: '#/components/schemas/NewQuestion'
          existing: '#/components/schemas/ExistingQuestion'
    NewQuestion:
      allOf:
        - $ref: '#/components/schemas/QuestionnaireQuestion'
        - type: object
          properties:
            text:
              type: string
    ExistingQuestion:
      allOf:
        - $ref: '#/components/schemas/QuestionnaireQuestion'
        - type: object
          properties:
            id:
              type: integer
";

    [Test]
    public async Task Generates_Base_Type_For_Inline_OneOf_Collection()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("ICollection<QuestionnaireQuestion> Questions");
    }

    private static async Task<string> GenerateCode(string spec = OpenApiSpec, bool usePolymorphicSerialization = false)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            UsePolymorphicSerialization = usePolymorphicSerialization,
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
