using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// A schema named Task shadows System.Threading.Tasks.Task for void operations (#1275).
/// </summary>
public class TaskSchemaNameTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Tasks, version: v1 }
        paths:
          /tasks:
            get:
              operationId: GetTasks
              responses: { '200': { description: ok, content: { application/json: { schema: { type: array, items: { $ref: '#/components/schemas/Task' } } } } } }
            delete:
              operationId: DeleteTasks
              responses: { '204': { description: ok } }
        components:
          schemas:
            Task: { type: object, properties: { id: { type: integer } } }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Fully_Qualifies_Task_For_Void_Operations()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("System.Threading.Tasks.Task DeleteTasks(");
    }

    [Test]
    public async Task Keeps_Generic_Task_For_Operations_With_Result()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<ICollection<Task>> GetTasks(");
    }

    [Test]
    public async Task Does_Not_Qualify_Task_Without_Task_Schema()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec.Replace("Task", "Job"));
        var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings { OpenApiPath = swaggerFile });
        var generatedCode = sut.Generate();

        generatedCode.Should().Contain("Task DeleteJobs(");
        generatedCode.Should().NotContain("System.Threading.Tasks.Task");
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces_And_Cancellation_Tokens()
    {
        var generatedCode = await GenerateCode(settings =>
        {
            settings.MultipleInterfaces = MultipleInterfaces.ByEndpoint;
            settings.UseCancellationTokens = true;
        });
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_With_Contract_Type_Suffix()
    {
        var generatedCode = await GenerateCode(settings => settings.ContractTypeSuffix = "Dto");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(Action<RefitGeneratorSettings>? configure = null)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        configure?.Invoke(settings);

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
