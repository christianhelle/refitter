using FluentAssertions;
using Refitter.Core;
using Refitter.Core.Validation;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

// Regression tests for the Refit attribute-injection advisories. An OpenAPI path, header name, or
// content-type key that closes the Refit attribute string and injects a [ModuleInitializer] file
// class must not break out of the generated code, and must be rejected by validation.
// Covers GHSA-3fhm-p725-h3g3 (path), GHSA-58x9-vjvp-6mx8 (header name), GHSA-p32v-8v8j-j534 (content-type).
public class SecurityAttributeInjectionTests
{
    private const string MaliciousPath =
        @"/p"")] Task<string> Dummy(); } } file class Evil { [System.Runtime.CompilerServices.ModuleInitializer] internal static void Init(){ } } namespace N2 { public partial interface I2 { [Get(""/q";

    private const string MaliciousHeaderName =
        @"X"")] string a); } file class H {} public partial interface J { [Header(""Y";

    private const string MaliciousContentType =
        @"application/json"")] Task<string> Dummy(); } } file class CtEvil { [System.Runtime.CompilerServices.ModuleInitializer] internal static void Init(){ } } namespace N3 { public partial interface I3 { [Get(""/ctq";

    private const string PathInjectionSpec = @"
openapi: 3.0.0
info:
  title: Injection
  version: 1.0.0
paths:
  '" + MaliciousPath + @"':
    get:
      operationId: GetU
      responses:
        '200':
          description: ok
";

    private const string PathInjectionSpecV2 = @"
swagger: '2.0'
info:
  title: Injection
  version: 1.0.0
paths:
  '" + MaliciousPath + @"':
    get:
      operationId: GetU
      responses:
        '200':
          description: ok
";

    private const string HeaderInjectionSpec = @"
openapi: 3.0.0
info:
  title: Injection
  version: 1.0.0
paths:
  /api:
    get:
      operationId: GetU
      parameters:
        - name: '" + MaliciousHeaderName + @"'
          in: header
          schema:
            type: string
      responses:
        '200':
          description: ok
";

    private const string HeaderInjectionSpecV2 = @"
swagger: '2.0'
info:
  title: Injection
  version: 1.0.0
paths:
  /api:
    get:
      operationId: GetU
      parameters:
        - name: '" + MaliciousHeaderName + @"'
          in: header
          type: string
      responses:
        '200':
          description: ok
";

    private const string SecuritySchemeHeaderInjectionSpec = @"
openapi: 3.0.0
info:
  title: Injection
  version: 1.0.0
paths:
  /api:
    get:
      operationId: GetU
      security:
        - apiKey: []
      responses:
        '200':
          description: ok
components:
  securitySchemes:
    apiKey:
      type: apiKey
      in: header
      name: '" + MaliciousHeaderName + @"'
";

    private const string SecuritySchemeHeaderInjectionSpecV2 = @"
swagger: '2.0'
info:
  title: Injection
  version: 1.0.0
paths:
  /api:
    get:
      operationId: GetU
      security:
        - apiKey: []
      responses:
        '200':
          description: ok
securityDefinitions:
  apiKey:
    type: apiKey
    in: header
    name: '" + MaliciousHeaderName + @"'
";

    private const string ContentTypeInjectionSpec = @"
openapi: 3.0.0
info:
  title: Injection
  version: 1.0.0
paths:
  /api:
    post:
      operationId: PostU
      requestBody:
        content:
          '" + MaliciousContentType + @"':
            schema:
              type: string
      responses:
        '200':
          description: ok
          content:
            '" + MaliciousContentType + @"':
              schema:
                type: string
";

    [Test]
    public async Task Path_Injection_Does_Not_Break_Out_Of_Attribute()
    {
        string generatedCode = await GenerateCode(PathInjectionSpec);
        generatedCode.Should().Contain("\\\"");
        generatedCode.Should().NotContain("[Get(\"/q\")]");
    }

    [Category("Integration")]
    [Test]
    public async Task Generated_Code_With_Path_Injection_Is_Inert_And_Compiles()
    {
        string generatedCode = await GenerateCode(PathInjectionSpec);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Header_Injection_Does_Not_Break_Out_Of_Attribute()
    {
        string generatedCode = await GenerateCode(HeaderInjectionSpec);
        generatedCode.Should().Contain("\\\"");
        generatedCode.Should().NotContain("[Header(\"Y\")]");
    }

    [Category("Integration")]
    [Test]
    public async Task Generated_Code_With_Header_Injection_Is_Inert_And_Compiles()
    {
        string generatedCode = await GenerateCode(HeaderInjectionSpec);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Validation_Rejects_Path_With_Breakout_Characters()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(PathInjectionSpec);
        try
        {
            var result = await OpenApiValidator.Validate(swaggerFile);
            result.IsValid.Should().BeFalse();
        }
        finally
        {
            CleanUp(swaggerFile);
        }
    }

    [Test]
    public async Task Path_Injection_Does_Not_Break_Out_Of_Attribute_V2()
    {
        string generatedCode = await GenerateCode(PathInjectionSpecV2);
        generatedCode.Should().Contain("\\\"");
        generatedCode.Should().NotContain("[Get(\"/q\")]");
    }

    [Category("Integration")]
    [Test]
    public async Task Generated_Code_With_Path_Injection_Is_Inert_And_Compiles_V2()
    {
        string generatedCode = await GenerateCode(PathInjectionSpecV2);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Header_Injection_Does_Not_Break_Out_Of_Attribute_V2()
    {
        string generatedCode = await GenerateCode(HeaderInjectionSpecV2);
        generatedCode.Should().Contain("\\\"");
        generatedCode.Should().NotContain("[Header(\"Y\")]");
    }

    [Category("Integration")]
    [Test]
    public async Task Generated_Code_With_Header_Injection_Is_Inert_And_Compiles_V2()
    {
        string generatedCode = await GenerateCode(HeaderInjectionSpecV2);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Validation_Rejects_Path_With_Breakout_Characters_V2()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(PathInjectionSpecV2);
        try
        {
            var result = await OpenApiValidator.Validate(swaggerFile);
            result.IsValid.Should().BeFalse();
        }
        finally
        {
            CleanUp(swaggerFile);
        }
    }

    [Test]
    public async Task Validation_Rejects_Header_With_Breakout_Characters()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(HeaderInjectionSpec);
        try
        {
            var result = await OpenApiValidator.Validate(swaggerFile);
            result.IsValid.Should().BeFalse();
        }
        finally
        {
            CleanUp(swaggerFile);
        }
    }

    [Test]
    public async Task Validation_Rejects_Header_With_Breakout_Characters_V2()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(HeaderInjectionSpecV2);
        try
        {
            var result = await OpenApiValidator.Validate(swaggerFile);
            result.IsValid.Should().BeFalse();
        }
        finally
        {
            CleanUp(swaggerFile);
        }
    }

    [Test]
    public async Task SecurityScheme_Header_Injection_Does_Not_Break_Out_Of_Attribute()
    {
        string generatedCode = await GenerateCode(
            SecuritySchemeHeaderInjectionSpec,
            AuthenticationHeaderStyle.Parameter);
        generatedCode.Should().Contain("[Get(\"/api\")]");
        generatedCode.Should().NotContain("[Header(\"Y\")]");
    }

    [Category("Integration")]
    [Test]
    public async Task Generated_Code_With_SecurityScheme_Header_Injection_Is_Inert_And_Compiles()
    {
        string generatedCode = await GenerateCode(
            SecuritySchemeHeaderInjectionSpec,
            AuthenticationHeaderStyle.Parameter);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Validation_Rejects_SecurityScheme_Header_With_Breakout_Characters()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(SecuritySchemeHeaderInjectionSpec);
        try
        {
            var result = await OpenApiValidator.Validate(swaggerFile);
            result.IsValid.Should().BeFalse();
        }
        finally
        {
            CleanUp(swaggerFile);
        }
    }

    [Test]
    public async Task SecurityScheme_Header_Injection_Does_Not_Break_Out_Of_Attribute_V2()
    {
        string generatedCode = await GenerateCode(
            SecuritySchemeHeaderInjectionSpecV2,
            AuthenticationHeaderStyle.Parameter);
        generatedCode.Should().Contain("[Get(\"/api\")]");
        generatedCode.Should().NotContain("[Header(\"Y\")]");
    }

    [Category("Integration")]
    [Test]
    public async Task Generated_Code_With_SecurityScheme_Header_Injection_Is_Inert_And_Compiles_V2()
    {
        string generatedCode = await GenerateCode(
            SecuritySchemeHeaderInjectionSpecV2,
            AuthenticationHeaderStyle.Parameter);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Validation_Rejects_SecurityScheme_Header_With_Breakout_Characters_V2()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(SecuritySchemeHeaderInjectionSpecV2);
        try
        {
            var result = await OpenApiValidator.Validate(swaggerFile);
            result.IsValid.Should().BeFalse();
        }
        finally
        {
            CleanUp(swaggerFile);
        }
    }

    [Test]
    public async Task ContentType_Injection_Does_Not_Break_Out_Of_Attribute()
    {
        string generatedCode = await GenerateCode(ContentTypeInjectionSpec);
        generatedCode.Should().Contain("\\\"");
        generatedCode.Should().NotContain("application/json\")]");
    }

    [Category("Integration")]
    [Test]
    public async Task Generated_Code_With_ContentType_Injection_Is_Inert_And_Compiles()
    {
        string generatedCode = await GenerateCode(ContentTypeInjectionSpec);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Validation_Rejects_ContentType_With_Breakout_Characters()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(ContentTypeInjectionSpec);
        try
        {
            var result = await OpenApiValidator.Validate(swaggerFile);
            result.IsValid.Should().BeFalse();
        }
        finally
        {
            CleanUp(swaggerFile);
        }
    }

    private static async Task<string> GenerateCode(
        string spec,
        AuthenticationHeaderStyle authenticationHeaderStyle = AuthenticationHeaderStyle.None)
    {
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        try
        {
            RefitGeneratorSettings settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                AuthenticationHeaderStyle = authenticationHeaderStyle
            };
            RefitGenerator generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        }
        finally
        {
            CleanUp(swaggerFile);
        }
    }

    private static void CleanUp(string swaggerFile)
    {
        if (File.Exists(swaggerFile))
            File.Delete(swaggerFile);
        string? directory = Path.GetDirectoryName(swaggerFile);
        if (directory != null && Directory.Exists(directory))
            Directory.Delete(directory, true);
    }
}
