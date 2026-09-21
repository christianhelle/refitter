using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Parameters;

public class ParameterFormattingBranchTests
{
    [Test]
    public async Task Query_Date_Parameters_Use_The_Configured_Date_Format()
    {
        const string spec = @"
openapi: '3.0.0'
info:
  title: 'Test'
  version: '1.0'
paths:
  /foo:
    get:
      operationId: 'Get foos'
      parameters:
        - in: 'query'
          name: 'from'
          schema:
            type: 'string'
            format: 'date'
      responses:
        '200':
          description: 'ok'
";
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            CodeGeneratorSettings = new CodeGeneratorSettings { DateFormat = "dd/MM/yyyy" },
        };

        var generatedCode = (await RefitGenerator.CreateAsync(settings)).Generate();

        generatedCode.Should().Contain("[Query(Format = \"dd/MM/yyyy\")]");
    }

    [Test]
    public async Task Query_DateTime_Parameters_Use_The_Configured_DateTime_Format()
    {
        const string spec = @"
openapi: '3.0.0'
info:
  title: 'Test'
  version: '1.0'
paths:
  /foo:
    get:
      operationId: 'Get foos'
      parameters:
        - in: 'query'
          name: 'from'
          schema:
            type: 'string'
            format: 'date-time'
      responses:
        '200':
          description: 'ok'
";
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            CodeGeneratorSettings = new CodeGeneratorSettings { DateTimeFormat = "yyyy-MM-ddTHH:mm:ss" },
        };

        var generatedCode = (await RefitGenerator.CreateAsync(settings)).Generate();

        generatedCode.Should().Contain("[Query(Format = \"yyyy-MM-ddTHH:mm:ss\")]");
    }

    [Test]
    public async Task Path_Parameters_Missing_From_The_Url_Template_Are_Ordered_Last()
    {
        const string spec = @"
openapi: '3.0.0'
info:
  title: 'Test'
  version: '1.0'
paths:
  /foo/{id}:
    get:
      operationId: 'Get foo'
      parameters:
        - in: 'path'
          name: 'orphan'
          required: true
          schema:
            type: 'string'
        - in: 'path'
          name: 'id'
          required: true
          schema:
            type: 'string'
      responses:
        '200':
          description: 'ok'
";
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };

        var generatedCode = (await RefitGenerator.CreateAsync(settings)).Generate();

        generatedCode.Should().Contain("GetFoo(string id, string orphan)");
    }
}
