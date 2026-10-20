using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// With UseMultipartFormObject the non-file fields of a multipart body are wrapped in a
/// [FormObject] model, so Refit writes each as a plain text part instead of a JSON part (#1346).
/// </summary>
public class MultipartFormObjectTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: MultipartFormObject, version: v1 }
        paths:
          /api/v1/messages:
            post:
              operationId: SendMessage
              requestBody:
                content:
                  multipart/form-data:
                    schema:
                      type: object
                      required: [message]
                      properties:
                        message: { type: string }
                        messageType: { $ref: '#/components/schemas/MessageType' }
                        attachments: { type: array, items: { type: string, format: binary } }
              responses: { '204': { description: ok } }
          /api/v1/files:
            post:
              operationId: UploadFile
              requestBody:
                content:
                  multipart/form-data:
                    schema:
                      type: object
                      properties:
                        file: { type: string, format: binary }
              responses: { '204': { description: ok } }
        components:
          schemas:
            MessageType:
              type: string
              enum: [CHAT, SYSTEM]
        """;

    [Test]
    public async Task Wraps_Non_File_Fields_In_FormObject_Parameter()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task SendMessage([FormObject] SendMessageFormData formData, IEnumerable<StreamPart> attachments);");
    }

    [Test]
    public async Task Generates_FormObject_Type_With_Aliased_Properties()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public class SendMessageFormData");
        generatedCode.Should().Contain("[AliasAs(\"messageType\")]");
        generatedCode.Should().Contain("public MessageType? MessageType { get; set; }");
    }

    [Test]
    public async Task Initializes_Required_Fields_Through_Constructor()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public SendMessageFormData(string message)");
        generatedCode.Should().Contain("this.Message = message;");
    }

    [Test]
    public async Task Keeps_Separate_Parameters_When_Disabled()
    {
        var generatedCode = await GenerateCode(useMultipartFormObject: false);
        generatedCode.Should().Contain("Task SendMessage(string message, MessageType? messageType, IEnumerable<StreamPart> attachments);");
        generatedCode.Should().NotContain("[FormObject]");
    }

    [Test]
    public async Task Does_Not_Wrap_Multipart_Body_With_Only_Files()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task UploadFile(StreamPart file);");
        generatedCode.Should().NotContain("UploadFileFormData");
    }

    [Test]
    public async Task Names_FormObject_Type_After_Endpoint_Interface()
    {
        var generatedCode = await GenerateCode(multipleInterfaces: MultipleInterfaces.ByEndpoint);
        generatedCode.Should().Contain("Task Execute([FormObject] SendMessageFormData formData, IEnumerable<StreamPart> attachments);");
        generatedCode.Should().Contain("public class SendMessageFormData");
    }

    [Test]
    public async Task Emits_FormObject_Type_As_Separate_File()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            UseMultipartFormObject = true,
            MultipleInterfaces = MultipleInterfaces.ByTag,
            GenerateMultipleFiles = true,
        });

        var generatedFiles = sut.GenerateMultipleFiles().Files;
        generatedFiles.Should().Contain(f => f.Filename == "MultipartFormObjectSendMessageFormData.cs" && f.Content.Contains("public class MultipartFormObjectSendMessageFormData"));
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    [Arguments(MultipleInterfaces.ByEndpoint)]
    [Arguments(MultipleInterfaces.ByTag)]
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces(MultipleInterfaces multipleInterfaces)
    {
        var generatedCode = await GenerateCode(multipleInterfaces: multipleInterfaces);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(
        bool useMultipartFormObject = true,
        MultipleInterfaces multipleInterfaces = MultipleInterfaces.Unset)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var sut = await RefitGenerator.CreateAsync(new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            UseMultipartFormObject = useMultipartFormObject,
            MultipleInterfaces = multipleInterfaces,
        });
        return sut.Generate();
    }
}
