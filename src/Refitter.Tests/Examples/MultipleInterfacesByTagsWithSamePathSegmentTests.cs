using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

/// <summary>
/// Regression tests for https://github.com/christianhelle/refitter/issues/XXX
/// When using ByTag, method names should not have numeric suffixes added for
/// cross-interface uniqueness - deduplication should be per-interface.
/// </summary>
public class MultipleInterfacesByTagsWithSamePathSegmentTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: 'Test API'
  version: '1.0'
paths:
  /api/manage/2fa:
    post:
      tags:
      - 'Manage'
      responses:
        '200':
          description: 'ok'
  /api/manage/info:
    get:
      tags:
      - 'Manage'
      responses:
        '200':
          description: 'ok'
    post:
      tags:
      - 'Manage'
      responses:
        '200':
          description: 'ok'
  /api/account/info:
    get:
      tags:
      - 'Account'
      responses:
        '200':
          description: 'ok'
  /api/user/info:
    get:
      tags:
      - 'User'
      responses:
        '200':
          description: 'ok'
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_Manage_Interface()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("partial interface IManageApi");
    }

    [Test]
    public async Task Generates_Account_Interface()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("partial interface IAccountApi");
    }

    [Test]
    public async Task Generates_User_Interface()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("partial interface IUserApi");
    }

    [Test]
    public async Task Account_Interface_Does_Not_Have_Numeric_Suffix()
    {
        string generatedCode = await GenerateCode();
        // IAccountApi's GET /api/account/info should not get a numeric suffix
        // even though IManageApi also has a GET /api/manage/info operation
        generatedCode.Should().NotContainAny("Info2(", "Info3(", "InfoGet2(", "InfoGet3(", "InfoGET2(", "InfoGET3(");
    }

    [Test]
    public async Task User_Interface_Does_Not_Have_Numeric_Suffix()
    {
        string generatedCode = await GenerateCode();
        // IUserApi's GET /api/user/info should not get a numeric suffix
        generatedCode.Should().NotContainAny("Info2(", "Info3(", "InfoGet2(", "InfoGet3(", "InfoGET2(", "InfoGET3(");
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

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByTag,
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
