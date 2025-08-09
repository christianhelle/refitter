using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

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

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Generates_Nullable_Directive()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("#nullable");
    }

    [Theory]
    [InlineData("title")]
    [InlineData("contact")]
    [InlineData("description")]
    public async Task Generates_Nullable_Parameters(string parameterName)
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain($"string? {parameterName}");
        generateCode.Should().NotContain($"string? {parameterName} = default");
    }

    [Fact]
    public async Task DoesNot_Generate_CancellationToken()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().NotContain("CancellationToken cancellationToken");
    }

    [Fact]
    public async Task Generates_RequestOptions_Last()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("[RequestOptions] IApizrRequestOptions options);");
    }

    [Fact]
    public async Task Generates_An_Overload_Without_Optional_Parameters()
    {
        string generateCode = await GenerateCode();
        generateCode.Should().Contain("string id, [RequestOptions] IApizrRequestOptions options);");
    }

    [Fact]
    public async Task Generates_Dynamic_Querystring_Parameters()
    {
        string generateCode = await GenerateCode(true);

        // All nullable query params ending with a nullable query parameter
        generateCode.Should().Contain("string id, [Query] GetFooDetailsQueryParams? queryParams, [RequestOptions] IApizrRequestOptions options);");
        generateCode.Should().Contain("public record GetFooDetailsQueryParams");

        // Some required query params ending with a non-nullable query parameter with injected params
        generateCode.Should().Contain("string id, [Query] GetBarDetailsQueryParams queryParams, [RequestOptions] IApizrRequestOptions options);");
        generateCode.Should().Contain("public record GetBarDetailsQueryParams");
        generateCode.Should().Contain("public GetBarDetailsQueryParams(string title, string description)");
        generateCode.Should().Contain("Title = title;");
        generateCode.Should().Contain("Description = description;");
    }

    [Fact]
    public async Task Generates_Dynamic_Querystring_Parameters_ByTag()
    {
        string generateCode = await GenerateCode(true, MultipleInterfaces.ByTag);
        generateCode.Should().Contain("string id, [Query] GetFooDetailsQueryParams? queryParams, [RequestOptions] IApizrRequestOptions options);");
        generateCode.Should().Contain("public record GetFooDetailsQueryParams");
    }

    [Fact]
    public async Task Generates_Dynamic_Querystring_Parameters_ByEndpoint()
    {
        string generateCode = await GenerateCode(true, MultipleInterfaces.ByEndpoint);
        generateCode.Should().Contain("string id, [Query] GetFooDetailsQueryParams? queryParams, [RequestOptions] IApizrRequestOptions options);");
        generateCode.Should().Contain("public record GetFooDetailsQueryParams");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generateCode = await GenerateCode();
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    private static async Task<string> GenerateCode(bool useDynamicQuerystringParameters = false, MultipleInterfaces multipleInterfaces = MultipleInterfaces.Unset)
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
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
        var generateCode = sut.Generate();
        return generateCode;
    }

    private static async Task<string> CreateSwaggerFile(string contents)
    {
        var filename = $"{Guid.NewGuid()}.yml";
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}
