using FluentAssertions;
using Refitter.Core;
using Refitter.Core.Validation;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

// Regression tests for GHSA-3fhm-p725-h3g3: an OpenAPI path / header name that closes the
// Refit attribute string and injects a [ModuleInitializer] file class must not break out of
// the generated code, and must be rejected by validation.
public class SecurityAttributeInjectionTests
{
    private const string MaliciousPath =
        @"/p"")] Task<string> Dummy(); } } file class Evil { [System.Runtime.CompilerServices.ModuleInitializer] internal static void Init(){ } } namespace N2 { public partial interface I2 { [Get(""/q";

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
        - name: 'X"")] string a); } file class H {} public partial interface J { [Header(""Y'
          in: header
          schema:
            type: string
      responses:
        '200':
          description: ok
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

    private static async Task<string> GenerateCode(string spec)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        try
        {
            var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
            var generator = await RefitGenerator.CreateAsync(settings);
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
        var directory = Path.GetDirectoryName(swaggerFile);
        if (directory != null && Directory.Exists(directory))
            Directory.Delete(directory, true);
    }
}
