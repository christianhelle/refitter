using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class InterfaceGeneratorBranchTests
{
    private const string DeprecatedOperationWithOptionalParameterSpec = @"
openapi: '3.0.0'
info:
  title: 'Test'
  version: '1.0'
paths:
  /foo:
    get:
      deprecated: true
      operationId: 'Get foos'
      parameters:
        - in: 'query'
          name: 'Title'
          nullable: true
          schema:
            type: 'string'
      responses:
        '200':
          description: 'successful operation'
";

    private const string CookieParameterSpec = @"
openapi: '3.0.0'
info:
  title: 'Test'
  version: '1.0'
paths:
  /foo:
    get:
      operationId: 'Get foos'
      parameters:
        - in: 'cookie'
          name: 'session'
          schema:
            type: 'string'
      responses:
        '200':
          description: 'successful operation'
";

    [Test]
    public async Task Apizr_Optional_Parameter_Overload_Repeats_Method_Attributes()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(
            DeprecatedOperationWithOptionalParameterSpec);

        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            OptionalParameters = true,
            GenerateDeprecatedOperations = true,
            ApizrSettings = new ApizrSettings { WithRequestOptions = true },
        };

        var generatedCode = (await RefitGenerator.CreateAsync(settings)).Generate();

        // One [System.Obsolete] for the full signature and one for the non-optional overload
        CountOccurrences(generatedCode, "[System.Obsolete]").Should().Be(2);
    }

    [Test]
    public async Task Cookie_Parameters_Are_Modelled_But_Not_Emitted()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(CookieParameterSpec);

        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var generatedCode = (await RefitGenerator.CreateAsync(settings)).Generate();

        // NSwag models the cookie parameter, but Refit has no cookie binding so it is not emitted
        generatedCode.Should().Contain("<param name=\"session\">");
        generatedCode.Should().Contain("Task GetFoos();");
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }
}
