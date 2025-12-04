using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class MultipleInterfacesByEndpointWithOperationNameTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
paths:
  /foo/{id}:
    get:
      tags:
      - 'Foo'
      operationId: 'Get foo details'
      description: 'Get the details of the specified foo'
      parameters:
        - in: 'path'
          name: 'id'
          description: 'Foo ID'
          required: true
          schema:
            type: 'string'
        - in: 'query'
          name: 'Title'
          description: 'Job title'
          nullable: true
          schema:
            type: 'string'
        - in: 'query'
          name: 'Description'
          description: 'Job description'
          optional: true
          schema:
            type: 'string'
        - in: 'query'
          name: 'Contact'
          description: 'Contact Person'
          schema:
            type: 'string'
      responses:
        '200':
          description: 'successful operation'
  /foo:
    get:
      tags:
      - 'Foo'
      operationId: 'Get all foos'
      description: 'Get all foos' 
      parameters:
        - in: 'query'
          name: 'Title'
          description: 'Foo title'
          nullable: true
          schema:
            type: 'string'       
      responses:
        '200':
          description: 'successful operation'
  /bar:
    get:
      tags:
      - 'Bar'
      operationId: 'Get all bars'
      description: 'Get all bars'      
      responses:
        '200':
          description: 'successful operation'
  /bar/{id}:
    get:
      tags:
      - 'Bar'
      operationId: 'Get bar details'
      description: 'Get the details of the specified bar'   
      parameters:
        - in: 'path'
          name: 'id'
          description: 'Bar ID'
          required: true
          schema:
            type: 'string'
        - in: 'query'
          name: 'Title'
          description: 'Bar title'
          nullable: true
          schema:
            type: 'string'
        - in: 'query'
          name: 'Description'
          description: 'Bar description'
          optional: true
          schema:
            type: 'string'
        - in: 'query'
          name: 'Contact'
          description: 'Contact Person'
          schema:
            type: 'string'
      responses:
        '200':
          description: 'successful operation'
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_IGetAllFooEndpoint()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("partial interface IGetAllFoosEndpoint");
    }

    [Test]
    public async Task Generates_IGetFooDetailsEndpoint()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("partial interface IGetFooDetailsEndpoint");
    }

    [Test]
    public async Task Generates_IGetAllBarEndpoint()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("partial interface IGetAllBarsEndpoint");
    }

    [Test]
    public async Task Generates_IGetBarDetailsEndpoint()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("partial interface IGetBarDetailsEndpoint");
    }

    [Test]
    public async Task Generates_Dynamic_Querystring_Parameters()
    {
        string generatedCode = await GenerateCode(true);
        generatedCode.Should().Contain("GetFooDetailsQueryParams");
        generatedCode.Should().NotContain("GetAllFoosQueryParams");
        generatedCode.Should().Contain("GetBarDetailsQueryParams");
    }

    [Test]
    public async Task Methods_Name_ExecuteAsync()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("ExecuteAsync");
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

    private static async Task<string> GenerateCode(bool useDynamicQuerystringParameters = false)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByEndpoint,
            UseDynamicQuerystringParameters = useDynamicQuerystringParameters,
            OperationNameTemplate = "{operationName}Async",
            ImmutableRecords = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

}
