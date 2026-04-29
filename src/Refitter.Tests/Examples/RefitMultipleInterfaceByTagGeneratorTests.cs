using System.Text.RegularExpressions;
using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests;

public class RefitMultipleInterfaceByTagGeneratorTests
{
    [Test]
    public async Task RefitMultipleInterfaceByTagGenerator_Creates_Ungrouped_Interface_When_Operation_Has_No_Tags()
    {
        const string spec = @"
openapi: '3.0.0'
info:
  title: 'Test API'
  version: '1.0'
paths:
  /api/tagged:
    get:
      tags:
        - 'TestTag'
      operationId: 'GetTaggedResource'
      responses:
        '200':
          description: 'Success'
  /api/untagged:
    get:
      operationId: 'GetUntaggedResource'
      responses:
        '200':
          description: 'Success'
";

        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByTag
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().Contain("interface ITestTagApi");
        generatedCode.Should().Contain("GetTaggedResource");
        generatedCode.Should().Contain("GetUntaggedResource");
        Regex.IsMatch(generatedCode, @"interface I\w+Api.*GetUntaggedResource", RegexOptions.Singleline).Should().BeTrue();
    }

    [Test]
    public async Task RefitMultipleInterfaceByTagGenerator_Groups_All_Tagged_Operations()
    {
        const string spec = @"
openapi: '3.0.0'
info:
  title: 'Test API'
  version: '1.0'
paths:
  /api/foo:
    get:
      tags:
        - 'Foo'
      operationId: 'GetFoo'
      responses:
        '200':
          description: 'Success'
  /api/bar:
    get:
      tags:
        - 'Bar'
      operationId: 'GetBar'
      responses:
        '200':
          description: 'Success'
";

        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByTag
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().Contain("interface IFooApi");
        generatedCode.Should().Contain("interface IBarApi");
        generatedCode.Should().Contain("GetFoo");
        generatedCode.Should().Contain("GetBar");
    }

    [Test]
    public async Task RefitMultipleInterfaceByTagGenerator_Generated_Code_Compiles()
    {
        const string spec = @"
openapi: '3.0.0'
info:
  title: 'Test API'
  version: '1.0'
paths:
  /api/tagged:
    get:
      tags:
        - 'TestTag'
      operationId: 'GetTaggedResource'
      responses:
        '200':
          description: 'Success'
  /api/untagged:
    get:
      operationId: 'GetUntaggedResource'
      responses:
        '200':
          description: 'Success'
";

        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByTag
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }
}
