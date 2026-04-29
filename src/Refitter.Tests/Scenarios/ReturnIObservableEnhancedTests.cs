using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class ReturnIObservableEnhancedTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.0
info:
  title: Test API
  version: 1.0.0
paths:
  /products:
    get:
      operationId: getProducts
      responses:
        '200':
          description: successful operation
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Product'
  /products/{id}:
    get:
      operationId: getProductById
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          description: successful operation
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Product'
components:
  schemas:
    Product:
      type: object
      properties:
        id:
          type: string
        name:
          type: string
        price:
          type: number
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode(returnIObservable: true);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generated_Code_Contains_IObservable()
    {
        string generatedCode = await GenerateCode(returnIObservable: true);
        generatedCode.Should().Contain("IObservable<");
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_IObservable_When_Disabled()
    {
        string generatedCode = await GenerateCode(returnIObservable: false);
        generatedCode.Should().NotContain("IObservable<");
        generatedCode.Should().Contain("Task<");
    }

    [Test]
    public async Task Can_Build_Generated_Code_Without_IObservable()
    {
        string generatedCode = await GenerateCode(returnIObservable: false);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Generated_Code_Contains_Using_System_Reactive()
    {
        string generatedCode = await GenerateCode(returnIObservable: true);
        generatedCode.Should().Contain("using System.Reactive;");
    }

    [Test]
    public async Task Generated_IObservable_Code_Contains_Multiple_Methods()
    {
        string generatedCode = await GenerateCode(returnIObservable: true);

        // Verify multiple methods are generated with IObservable
        var observableCount = CountOccurrences(generatedCode, "IObservable<");
        observableCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task Generated_Task_Code_Contains_Multiple_Methods()
    {
        string generatedCode = await GenerateCode(returnIObservable: false);

        // Verify multiple methods are generated with Task
        var taskCount = CountOccurrences(generatedCode, "Task<");
        taskCount.Should().BeGreaterThanOrEqualTo(2);
    }

    private static async Task<string> GenerateCode(bool returnIObservable)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                ReturnIObservable = returnIObservable
            };

            var sut = await RefitGenerator.CreateAsync(settings);
            return sut.Generate();
        }
        finally
        {
            if (File.Exists(swaggerFile))
                File.Delete(swaggerFile);
            var directory = Path.GetDirectoryName(swaggerFile);
            if (directory != null && Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private static int CountOccurrences(string text, string substring)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }
}
