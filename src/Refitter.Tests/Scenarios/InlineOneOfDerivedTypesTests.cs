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

    [Test]
    public async Task Generates_Base_Type_Regardless_Of_OneOf_Order()
    {
        string reversedSpec = OpenApiSpec
            .Replace("- $ref: '#/components/schemas/NewQuestion'", "- $ref: '#/components/schemas/Placeholder'")
            .Replace("- $ref: '#/components/schemas/ExistingQuestion'", "- $ref: '#/components/schemas/NewQuestion'")
            .Replace("- $ref: '#/components/schemas/Placeholder'", "- $ref: '#/components/schemas/ExistingQuestion'");
        reversedSpec.Should().NotBe(OpenApiSpec);

        string generatedCode = await GenerateCode(reversedSpec);
        generatedCode.Should().Contain("ICollection<QuestionnaireQuestion> Questions");
    }

    [Test]
    public async Task Generates_JsonDerivedType_Attributes_On_Base_Type()
    {
        string generatedCode = await GenerateCode(usePolymorphicSerialization: true);
        generatedCode.Should().Contain("[JsonDerivedType(typeof(NewQuestion), typeDiscriminator: \"new\")]");
        generatedCode.Should().Contain("[JsonDerivedType(typeof(ExistingQuestion), typeDiscriminator: \"existing\")]");
    }

    [Test]
    public async Task Generates_Base_Type_When_OneOf_Includes_Base_Type()
    {
        string specWithBaseType = OpenApiSpec.Replace(
            "- $ref: '#/components/schemas/ExistingQuestion'",
            "- $ref: '#/components/schemas/QuestionnaireQuestion'");
        specWithBaseType.Should().NotBe(OpenApiSpec);

        string generatedCode = await GenerateCode(specWithBaseType);
        generatedCode.Should().Contain("ICollection<QuestionnaireQuestion> Questions");
    }

    [Test]
    public async Task Does_Not_Use_Base_Type_When_Schema_Has_Both_OneOf_And_AnyOf()
    {
        string specWithMixedUnion = OpenApiSpec.Replace(
            "              - $ref: '#/components/schemas/ExistingQuestion'",
            "            anyOf:\n              - $ref: '#/components/schemas/ExistingQuestion'");
        specWithMixedUnion.Should().NotBe(OpenApiSpec);

        string generatedCode = await GenerateCode(specWithMixedUnion);
        generatedCode.Should().NotContain("ICollection<QuestionnaireQuestion> Questions");
    }

    [Test]
    public async Task Does_Not_Use_Base_Type_When_Not_All_OneOf_Types_Derive_From_It()
    {
        string specWithUnrelatedType = OpenApiSpec.Replace(
            "- $ref: '#/components/schemas/ExistingQuestion'",
            "- $ref: '#/components/schemas/Questionnaire'");
        specWithUnrelatedType.Should().NotBe(OpenApiSpec);

        string generatedCode = await GenerateCode(specWithUnrelatedType);
        generatedCode.Should().NotContain("ICollection<QuestionnaireQuestion> Questions");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_With_Polymorphic_Serialization()
    {
        string generatedCode = await GenerateCode(usePolymorphicSerialization: true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
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
