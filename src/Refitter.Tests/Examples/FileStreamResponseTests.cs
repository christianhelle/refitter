using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class FileStreamResponseTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: File Download API
  version: 1.0.0
paths:
  '/files/{fileId}/download':
    get:
      operationId: downloadFile
      summary: Download a file
      parameters:
        - name: fileId
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          description: File downloaded successfully
          content:
            application/octet-stream:
              schema:
                type: string
                format: binary
  '/images/{imageId}':
    get:
      operationId: getImage
      summary: Get an image
      parameters:
        - name: imageId
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          description: Image retrieved successfully
          content:
            image/jpeg:
              schema:
                type: string
                format: binary
            image/png:
              schema:
                type: string
                format: binary
  '/documents/{docId}':
    get:
      operationId: getDocument
      summary: Get a PDF document
      parameters:
        - name: docId
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          description: Document retrieved successfully
          content:
            application/pdf:
              schema:
                type: string
                format: binary
  '/forms/{formId}/submit':
    get:
      operationId: submitForm
      summary: Submit form data
      parameters:
        - name: formId
          in: path
          required: true
          schema:
            type: string
      responses:
        '200':
          description: Form submission result
          content:
            application/x-www-form-urlencoded:
              schema:
                type: object
                properties:
                  result:
                    type: string
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generates_HttpResponseMessage_For_OctetStream()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<HttpResponseMessage> DownloadFile(string fileId");
    }

    [Test]
    public async Task Generates_HttpResponseMessage_For_ImageJpeg()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<HttpResponseMessage> GetImage(string imageId");
    }

    [Test]
    public async Task Generates_HttpResponseMessage_For_ApplicationPdf()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task<HttpResponseMessage> GetDocument(string docId");
    }

    [Test]
    public async Task Does_Not_Generate_HttpResponseMessage_For_FormUrlEncoded()
    {
        string generatedCode = await GenerateCode();
        generatedCode.Should().Contain("submitForm");
        generatedCode.Should().NotContain("Task<HttpResponseMessage> SubmitForm(");
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

    [Test]
    public async Task Generates_HttpResponseMessage_With_IApiResponse_Setting()
    {
        string generatedCode = await GenerateCode(returnIApiResponse: true);
        generatedCode.Should().Contain("Task<HttpResponseMessage> DownloadFile(string fileId");
    }

    private static async Task<string> GenerateCode(bool returnIApiResponse = false)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ReturnIApiResponse = returnIApiResponse
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
