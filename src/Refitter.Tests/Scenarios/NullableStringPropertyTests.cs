using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Test for Issue #580: Nullable string properties should generate with string? (not string)
/// https://github.com/christianhelle/refitter/issues/580
/// </summary>
public class NullableStringPropertyTests
{
    private const string OpenApiSpecWithNullableProperties = @"
openapi: '3.0.0'
info:
  title: Address API
  version: '1.0.0'
paths:
  /address:
    post:
      operationId: 'CreateAddress'
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/Address'
      responses:
        '201':
          description: 'Created'
components:
  schemas:
    Address:
      type: object
      required:
        - street
        - postalCode
      properties:
        street:
          type: string
          nullable: false
        city:
          type: string
          nullable: true
        state:
          type: string
          nullable: true
        postalCode:
          type: string
          nullable: false
        country:
          type: string
          nullable: true
        latitude:
          type: number
          format: double
          nullable: true
        longitude:
          type: number
          format: double
          nullable: true
";

    private const string OpenApiSpecWithNullableInRequired = @"
openapi: '3.0.0'
info:
  title: Person API
  version: '1.0.0'
paths:
  /person:
    post:
      operationId: 'CreatePerson'
      requestBody:
        required: true
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/Person'
      responses:
        '201':
          description: 'Created'
components:
  schemas:
    Person:
      type: object
      required:
        - firstName
        - middleName
      properties:
        firstName:
          type: string
          nullable: false
        middleName:
          type: string
          nullable: true
        lastName:
          type: string
          nullable: true
";

    [Test]
    public async Task Test_Nullable_StringProperty_GeneratedWithQuestionMark()
    {
        // Arrange & Act
        string generatedCode = await GenerateCode(OpenApiSpecWithNullableProperties);

        // Assert - City, State, Country should be string? (nullable: true in spec)
        generatedCode.Should().Contain("public string? City { get; init; }",
            "city property is marked nullable: true");
        generatedCode.Should().Contain("public string? State { get; init; }",
            "state property is marked nullable: true");
        generatedCode.Should().Contain("public string? Country { get; init; }",
            "country property is marked nullable: true");

        // Street and PostalCode should be non-nullable string (nullable: false)
        generatedCode.Should().Contain("public string Street { get; init; }",
            "street property is marked nullable: false");
        generatedCode.Should().Contain("public string PostalCode { get; init; }",
            "postalCode property is marked nullable: false");
    }

    [Test]
    public async Task Test_Nullable_DoubleProperty_GeneratedWithQuestionMark()
    {
        // Arrange & Act
        string generatedCode = await GenerateCode(OpenApiSpecWithNullableProperties);

        // Assert - Latitude and Longitude should be double? (nullable: true)
        // This should already work correctly - verify it still does
        generatedCode.Should().Contain("public double? Latitude { get; init; }",
            "latitude property is marked nullable: true");
        generatedCode.Should().Contain("public double? Longitude { get; init; }",
            "longitude property is marked nullable: true");
    }

    [Test]
    public async Task Test_Nullable_StringNotInRequired_Field()
    {
        // Arrange & Act
        string generatedCode = await GenerateCode(OpenApiSpecWithNullableInRequired);

        // Assert - LastName is NOT in required array AND marked nullable: true
        // Should generate as string?
        generatedCode.Should().Contain("public string? LastName { get; init; }",
            "lastName is nullable: true and not in required array");
    }

    [Test]
    public async Task Test_Nullable_StringInRequired_Field_Still_Nullable()
    {
        // Arrange & Act
        string generatedCode = await GenerateCode(OpenApiSpecWithNullableInRequired);

        // Assert - MiddleName is IN required array BUT marked nullable: true
        // Should STILL generate as string? (nullable: true takes precedence)
        generatedCode.Should().Contain("public string? MiddleName { get; init; }",
            "middleName is in required array but marked nullable: true - nullable should take precedence");

        // FirstName is in required AND nullable: false
        generatedCode.Should().Contain("public string FirstName { get; init; }",
            "firstName is in required array and marked nullable: false");
    }

    [Test]
    public async Task Test_NullableReferenceTypes_Directive_Present()
    {
        // Arrange & Act
        string generatedCode = await GenerateCode(OpenApiSpecWithNullableProperties);

        // Assert - Should have #nullable enable directive
        generatedCode.Should().Contain("#nullable enable");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Nullable_Strings()
    {
        // Arrange & Act
        string generatedCode = await GenerateCode(OpenApiSpecWithNullableProperties);

        // Assert - Generated code should compile successfully
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue("generated code with nullable strings should compile");
    }

    [Test]
    public async Task Test_Multiple_Nullable_Properties_In_Same_Model()
    {
        // Arrange & Act
        string generatedCode = await GenerateCode(OpenApiSpecWithNullableProperties);

        // Assert - Verify Address class has correct mix of nullable and non-nullable
        var addressClassStart = generatedCode.IndexOf("record Address", StringComparison.Ordinal);
        addressClassStart.Should().BeGreaterThan(0);

        var addressClassEnd = generatedCode.IndexOf("}", addressClassStart);
        var addressClass = generatedCode.Substring(addressClassStart, addressClassEnd - addressClassStart);

        // Count nullable properties
        var nullableStringCount = System.Text.RegularExpressions.Regex.Matches(addressClass, @"string\?").Count;
        nullableStringCount.Should().Be(3, "Address should have 3 nullable string properties (City, State, Country)");

        var nullableDoubleCount = System.Text.RegularExpressions.Regex.Matches(addressClass, @"double\?").Count;
        nullableDoubleCount.Should().Be(2, "Address should have 2 nullable double properties (Latitude, Longitude)");
    }

    private static async Task<string> GenerateCode(string spec)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ImmutableRecords = true,
            CodeGeneratorSettings = new()
            {
                GenerateNullableReferenceTypes = true
            }
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
