using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class OneOfDiscriminatorTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.1
info:
  title: Test
  version: v1
paths:
  /api/identityproviders:
    get:
      operationId: GetIdentityProvider
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/IdentityProviderResponse'
components:
  schemas:
    IdentityProviderResponse:
      type: object
      properties:
        identityProvider:
          $ref: '#/components/schemas/IdentityProvider'
        identityProviders:
          type: array
          items:
            $ref: '#/components/schemas/IdentityProvider'
    IdentityProvider:
      oneOf:
        - $ref: '#/components/schemas/AppleIdentityProvider'
        - $ref: '#/components/schemas/FacebookIdentityProvider'
      discriminator:
        propertyName: type
        mapping:
          Apple: '#/components/schemas/AppleIdentityProvider'
          Facebook: '#/components/schemas/FacebookIdentityProvider'
    AppleIdentityProvider:
      type: object
      properties:
        type:
          type: string
        bundleId:
          type: string
    FacebookIdentityProvider:
      type: object
      properties:
        type:
          type: string
        appId:
          type: string
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Base_Type_For_OneOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("class IdentityProvider");
    }

    [Test]
    public async Task Does_Not_Generate_Anonymous_Type_For_OneOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("IdentityProvider2");
    }

    [Test]
    public async Task Generates_All_Subtypes_For_OneOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("class AppleIdentityProvider");
        generatedCode.Should().Contain("class FacebookIdentityProvider");
    }

    [Test]
    public async Task Generates_Inheritance_Hierarchy_For_OneOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("AppleIdentityProvider : IdentityProvider");
        generatedCode.Should().Contain("FacebookIdentityProvider : IdentityProvider");
    }

    [Test]
    public async Task Generates_Correct_Property_Type_For_OneOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("IdentityProvider IdentityProvider");
        generatedCode.Should().Contain("ICollection<IdentityProvider> IdentityProviders");
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
    public async Task Can_Generate_Code_With_Polymorphic_Serialization()
    {
        string generatedCode = await GenerateCode(usePolymorphicSerialization: true);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_JsonPolymorphic_Attributes_For_OneOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode(usePolymorphicSerialization: true);
        generatedCode.Should().Contain("[JsonPolymorphic(TypeDiscriminatorPropertyName = \"type\"");
        generatedCode.Should().Contain("[JsonDerivedType(typeof(AppleIdentityProvider)");
        generatedCode.Should().Contain("[JsonDerivedType(typeof(FacebookIdentityProvider)");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Polymorphic_Serialization()
    {
        string generatedCode = await GenerateCode(usePolymorphicSerialization: true);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(bool usePolymorphicSerialization = false)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
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
