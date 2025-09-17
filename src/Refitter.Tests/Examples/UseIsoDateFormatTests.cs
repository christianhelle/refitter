using AutoFixture.Xunit2;
using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples;

public class UseIsoDateFormatTests
{
    private const string OpenApiSpec =
        @"
openapi: '3.0.1'
paths:
  '/t/foo/{id}':
    get:
      operationId: getFoo
      parameters:
        - name: id
          in: path
          required: true
          format: int64
          type: integer
        - name: valid_from
          in: query
          required: true
          format: date
          type: string
        - name: valid_to
          in: query
          required: true
          format: date
          type: string
        - name: test_time
          in: query
          required: true
          format: time
          type: string
        - name: test_datetime
          in: query
          required: true
          format: date-time
          type: string
      responses:
        '200':
          description: No response was specified
";

    [Fact]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GeneratedCode_Contains_Date_Format_String()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain(@"[Query(Format = ""yyyy-MM-dd"")] System.DateTimeOffset valid_from");
        generatedCode.Should().Contain(@"[Query(Format = ""yyyy-MM-dd"")] System.DateTimeOffset valid_to");
    }

    [Fact]
    public async Task GeneratedCode_NotContains_DateTime_Format_String()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain(@"[Query] System.DateTimeOffset test_datetime");
        generatedCode.Should().NotContain(@"[Query(Format = ""yyyy-MM-dd"")] System.DateTimeOffset time_datetime");
    }

    [Fact]
    public async Task GeneratedCode_Contains_TimeSpan_Parameter()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[Query] System.TimeSpan");
    }

    [Fact]
    public async Task GeneratedCode_Contains_Date_Format_String_With_Empty_Settings()
    {
        string generatedCode = await GenerateCode(new CodeGeneratorSettings());
        generatedCode.Should().Contain(@"[Query(Format = ""yyyy-MM-dd"")] System.DateTimeOffset valid_from");
        generatedCode.Should().Contain(@"[Query(Format = ""yyyy-MM-dd"")] System.DateTimeOffset valid_to");
    }

    [Fact]
    public async Task GeneratedCode_NotContains_DateTime_Format_String_With_Empty_Settings()
    {
        string generatedCode = await GenerateCode(new CodeGeneratorSettings());
        generatedCode.Should().Contain(@"[Query] System.DateTimeOffset test_datetime");
        generatedCode.Should().NotContain(@"[Query(Format = ""yyyy-MM-dd"")] System.DateTimeOffset time_datetime");
    }

    [Fact]
    public async Task GeneratedCode_Contains_TimeSpan_Parameter_With_Empty_Settings()
    {
        string generatedCode = await GenerateCode(new CodeGeneratorSettings());
        generatedCode.Should().Contain("[Query] System.TimeSpan");
    }

    [Theory, AutoData]
    public async Task GeneratedCode_Contains_Date_Format_String_With_Settings(
        string dateFormat,
        string dateTimeFormat)
    {
        var generatedCode = await GenerateCode(
            new CodeGeneratorSettings
            {
                DateFormat = dateFormat,
                DateTimeFormat = dateTimeFormat
            });

        generatedCode.Should().Contain(@$"[Query(Format = ""{dateFormat}"")] System.DateTimeOffset valid_from");
        generatedCode.Should().Contain(@$"[Query(Format = ""{dateFormat}"")] System.DateTimeOffset valid_to");
        generatedCode.Should().Contain(@$"[Query(Format = ""{dateTimeFormat}"")] System.DateTimeOffset test_datetime");
        generatedCode.Should().Contain(@$"[Query] System.TimeSpan test_time");
    }

    [Theory, AutoData]
    public async Task GeneratedCode_NotContains_DateTime_Format_String_With_Settings(string format)
    {
        string generatedCode = await GenerateCode(new CodeGeneratorSettings { DateFormat = format });
        generatedCode.Should().Contain(@"[Query] System.DateTimeOffset test_datetime");
        generatedCode.Should().NotContain(@$"[Query(Format = ""{format}"")] System.DateTimeOffset time_datetime");
    }

    [Theory, AutoData]
    public async Task GeneratedCode_Contains_TimeSpan_Parameter_With_Settings(string format)
    {
        string generatedCode = await GenerateCode(new CodeGeneratorSettings { DateFormat = format });
        generatedCode.Should().Contain("[Query] System.TimeSpan");
    }

    [Fact]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(CodeGeneratorSettings? generatorSettings = null)
    {
        var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            UseIsoDateFormat = generatorSettings?.DateFormat is null,
            CodeGeneratorSettings = generatorSettings
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }

    private static async Task<string> CreateSwaggerFile(string contents)
    {
        var filename = $"{Guid.NewGuid()}.yaml";
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}
