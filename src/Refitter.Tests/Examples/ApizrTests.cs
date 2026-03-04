using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class ApizrTests
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
          required: true
          schema:
            type: 'string'
        - in: 'query'
          name: 'Description'
          description: 'Bar description'
          required: true
          schema:
            type: 'string'
        - in: 'query'
          name: 'Contact'
          description: 'Contact Person'
          nullable: true
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
    public async Task Generates_Nullable_Directive()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("#nullable");
    }

    [Test]
    [Arguments("title")]
    [Arguments("contact")]
    [Arguments("description")]
    public async Task Generates_Nullable_Parameters(string parameterName)
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain($"string? {parameterName}");
        generatedCode.Should().NotContain($"string? {parameterName} = default");
    }

    [Test]
    public async Task DoesNot_Generate_CancellationToken()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("CancellationToken cancellationToken");
    }

    [Test]
    public async Task Generates_RequestOptions_Last()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[RequestOptions] IApizrRequestOptions options);");
    }

    [Test]
    public async Task Generates_An_Overload_Without_Optional_Parameters()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("string id, [RequestOptions] IApizrRequestOptions options);");
    }

    [Test]
    public async Task Generates_Dynamic_Querystring_Parameters()
    {
        string generatedCode = await GenerateCode(true);

        // All nullable query params ending with a nullable query parameter
        generatedCode.Should().Contain("string id, [Query] GetFooDetailsQueryParams? queryParams, [RequestOptions] IApizrRequestOptions options);");
        generatedCode.Should().Contain("public record GetFooDetailsQueryParams");

        // Some required query params ending with a non-nullable query parameter with injected params
        generatedCode.Should().Contain("string id, [Query] GetBarDetailsQueryParams queryParams, [RequestOptions] IApizrRequestOptions options);");
        generatedCode.Should().Contain("public record GetBarDetailsQueryParams");
        generatedCode.Should().Contain("public GetBarDetailsQueryParams(string title, string description)");
        generatedCode.Should().Contain("Title = title;");
        generatedCode.Should().Contain("Description = description;");
    }

    [Test]
    public async Task Generates_Dynamic_Querystring_Parameters_ByTag()
    {
        string generatedCode = await GenerateCode(true, MultipleInterfaces.ByTag);
        generatedCode.Should().Contain("string id, [Query] GetFooDetailsQueryParams? queryParams, [RequestOptions] IApizrRequestOptions options);");
        generatedCode.Should().Contain("public record GetFooDetailsQueryParams");
    }

    [Test]
    public async Task Generates_Dynamic_Querystring_Parameters_ByEndpoint()
    {
        string generatedCode = await GenerateCode(true, MultipleInterfaces.ByEndpoint);
        generatedCode.Should().Contain("string id, [Query] GetFooDetailsQueryParams? queryParams, [RequestOptions] IApizrRequestOptions options);");
        generatedCode.Should().Contain("public record GetFooDetailsQueryParams");
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

    private static async Task<string> GenerateCode(bool useDynamicQuerystringParameters = false, MultipleInterfaces multipleInterfaces = MultipleInterfaces.Unset)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            UseCancellationTokens = true,
            OptionalParameters = true,
            ApizrSettings = new ApizrSettings
            {
                WithRequestOptions = true
            },
            UseDynamicQuerystringParameters = useDynamicQuerystringParameters,
            ImmutableRecords = true,
            MultipleInterfaces = multipleInterfaces
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
