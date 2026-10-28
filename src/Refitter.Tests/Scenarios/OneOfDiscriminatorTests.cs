using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;


[TestClass]
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

    private const string AnyOfOpenApiSpec = @"
openapi: 3.0.1
info:
  title: Test
  version: v1
paths:
  /api/notifications:
    get:
      operationId: GetNotification
      responses:
        '200':
          description: Success
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/NotificationResponse'
components:
  schemas:
    NotificationResponse:
      type: object
      properties:
        notification:
          $ref: '#/components/schemas/Notification'
        notifications:
          type: array
          items:
            $ref: '#/components/schemas/Notification'
    Notification:
      anyOf:
        - $ref: '#/components/schemas/EmailNotification'
        - $ref: '#/components/schemas/SmsNotification'
      discriminator:
        propertyName: channel
        mapping:
          Email: '#/components/schemas/EmailNotification'
          Sms: '#/components/schemas/SmsNotification'
    EmailNotification:
      type: object
      properties:
        channel:
          type: string
        emailAddress:
          type: string
    SmsNotification:
      type: object
      properties:
        channel:
          type: string
        phoneNumber:
          type: string
";

    [TestMethod]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public async Task Generates_Base_Type_For_OneOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("class IdentityProvider");
    }

    [TestMethod]
    public async Task Does_Not_Generate_Anonymous_Type_For_OneOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("IdentityProvider2");
    }

    [TestMethod]
    public async Task Generates_All_Subtypes_For_OneOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("class AppleIdentityProvider");
        generatedCode.Should().Contain("class FacebookIdentityProvider");
    }

    [TestMethod]
    public async Task Generates_Inheritance_Hierarchy_For_OneOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("AppleIdentityProvider : IdentityProvider");
        generatedCode.Should().Contain("FacebookIdentityProvider : IdentityProvider");
    }

    [TestMethod]
    public async Task Generates_Correct_Property_Type_For_OneOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("IdentityProvider IdentityProvider");
        generatedCode.Should().Contain("ICollection<IdentityProvider> IdentityProviders");
    }

    [TestMethod]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [TestMethod]
    public async Task Can_Generate_Code_With_Polymorphic_Serialization()
    {
        string generatedCode = await GenerateCode(usePolymorphicSerialization: true);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public async Task Generates_JsonPolymorphic_Attributes_For_OneOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode(usePolymorphicSerialization: true);
        generatedCode.Should().Contain("[JsonPolymorphic(TypeDiscriminatorPropertyName = \"type\"");
        generatedCode.Should().Contain("[JsonDerivedType(typeof(AppleIdentityProvider)");
        generatedCode.Should().Contain("[JsonDerivedType(typeof(FacebookIdentityProvider)");
    }

    [TestMethod]
    public async Task Can_Build_Generated_Code_With_Polymorphic_Serialization()
    {
        string generatedCode = await GenerateCode(usePolymorphicSerialization: true);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [TestMethod]
    public async Task Can_Generate_Code_For_AnyOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode(spec: AnyOfOpenApiSpec);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    public async Task Generates_Base_Type_For_AnyOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode(spec: AnyOfOpenApiSpec);
        generatedCode.Should().Contain("class Notification");
    }

    [TestMethod]
    public async Task Does_Not_Generate_Anonymous_Type_For_AnyOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode(spec: AnyOfOpenApiSpec);
        generatedCode.Should().NotContain("Notification2");
    }

    [TestMethod]
    public async Task Generates_All_Subtypes_For_AnyOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode(spec: AnyOfOpenApiSpec);
        generatedCode.Should().Contain("class EmailNotification");
        generatedCode.Should().Contain("class SmsNotification");
    }

    [TestMethod]
    public async Task Generates_Inheritance_Hierarchy_For_AnyOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode(spec: AnyOfOpenApiSpec);
        generatedCode.Should().Contain("EmailNotification : Notification");
        generatedCode.Should().Contain("SmsNotification : Notification");
    }

    [TestMethod]
    public async Task Generates_Correct_Property_Type_For_AnyOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode(spec: AnyOfOpenApiSpec);
        generatedCode.Should().Contain("Notification Notification");
        generatedCode.Should().Contain("ICollection<Notification> Notifications");
    }

    [TestMethod]
    public async Task Can_Build_Generated_Code_For_AnyOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode(spec: AnyOfOpenApiSpec);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [TestMethod]
    public async Task Generates_JsonPolymorphic_Attributes_For_AnyOf_With_Discriminator()
    {
        string generatedCode = await GenerateCode(spec: AnyOfOpenApiSpec, usePolymorphicSerialization: true);
        generatedCode.Should().Contain("[JsonPolymorphic(TypeDiscriminatorPropertyName = \"channel\"");
        generatedCode.Should().Contain("[JsonDerivedType(typeof(EmailNotification)");
        generatedCode.Should().Contain("[JsonDerivedType(typeof(SmsNotification)");
    }

    [TestMethod]
    public async Task Can_Build_Generated_Code_For_AnyOf_With_Discriminator_And_Polymorphic_Serialization()
    {
        string generatedCode = await GenerateCode(spec: AnyOfOpenApiSpec, usePolymorphicSerialization: true);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
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
