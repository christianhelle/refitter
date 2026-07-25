using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class AcceptedResponseObjectTests
{
    private const string OpenApiSpec =
        @"
openapi: '3.0.0'
info:
  version: '1.0.0'
  title: 'Accepted Response API'
  description: 'An API that returns 202 Accepted for long running operations'
servers:
  - url: 'https://api.example.com/v1'
paths:
  /jobs:
    post:
      tags:
        - 'Jobs'
      summary: 'Start a job'
      description: 'Accepts the request and returns a handle for polling'
      operationId: 'createJob'
      responses:
        '202':
          description: 'Job accepted for processing'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Job'
  /jobs/{id}:
    get:
      tags:
        - 'Jobs'
      summary: 'Get job status'
      description: 'Returns 200 when the job is finished and 202 while it is still running'
      operationId: 'getJob'
      parameters:
        - name: 'id'
          in: 'path'
          description: 'Job ID'
          required: true
          schema:
            type: 'string'
      responses:
        '200':
          description: 'Job completed'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Job'
        '202':
          description: 'Job still running'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Job'
  /batches:
    post:
      tags:
        - 'Jobs'
      summary: 'Start a batch'
      description: 'Accepts the request with no response content'
      operationId: 'createBatch'
      responses:
        '202':
          description: 'Batch accepted for processing'
  /exports:
    post:
      tags:
        - 'Jobs'
      summary: 'Start an export'
      description: 'Accepts the request and streams back the export payload'
      operationId: 'createExport'
      responses:
        '202':
          description: 'Export accepted for processing'
          content:
            application/octet-stream:
              schema:
                type: 'string'
                format: 'binary'
components:
  schemas:
    Job:
      type: 'object'
      properties:
        id:
          type: 'string'
        status:
          type: 'string'
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Should_Generate_Accepted_Response_Return_Type()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<Job> CreateJob();");
    }

    [Test]
    public async Task Should_Prefer_Ok_Over_Accepted_When_Both_Are_Present()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<Job> GetJob(string id);");
    }

    [Test]
    public async Task Should_Not_Generate_Return_Type_When_Accepted_Has_No_Content()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task CreateBatch();");
    }

    [Test]
    public async Task Should_Generate_HttpResponseMessage_When_Accepted_Returns_Binary_Content()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<HttpResponseMessage> CreateExport();");
    }

    [Test]
    public async Task Should_Generate_Job_Contract()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public partial class Job");
        generatedCode.Should().Contain("public string Id { get; set; }");
        generatedCode.Should().Contain("public string Status { get; set; }");
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile, UseCancellationTokens = false };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
