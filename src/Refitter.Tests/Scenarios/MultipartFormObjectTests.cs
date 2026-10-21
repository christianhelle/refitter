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
    private const string SwaggerSpec = """
        swagger: '2.0'
        info: { title: MultipartFormObjectSwagger, version: v1 }
        paths:
          /pets/{petId}/image:
            post:
              operationId: UploadImage
              consumes: [multipart/form-data]
              parameters:
                - { name: petId, in: path, required: true, type: integer, format: int64 }
                - { name: caption, in: formData, required: true, type: string }
                - { name: category, in: formData, type: string, enum: [CAT, DOG] }
                - { name: image, in: formData, type: file }
              responses: { '204': { description: ok } }
        """;

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
                        message: { type: string, description: The text of the message }
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
          /api/v1/notes:
            post:
              operationId: CreateNote
              requestBody:
                content:
                  multipart/form-data:
                    schema:
                      type: object
                      required: [title, body]
                      properties:
                        title: { type: string }
                        body: { type: string }
                        tags: { type: array, items: { type: string } }
                        kind: { $ref: '#/components/schemas/StreamPartKind' }
                        attachment: { type: string, format: binary }
              responses: { '204': { description: ok } }
          /api/v1/uploads:
            post:
              operationId: Upload
              requestBody:
                content:
                  multipart/form-data:
                    schema:
                      type: object
                      properties:
                        uploadFormData: { type: string }
                        file: { type: string, format: binary }
              responses: { '204': { description: ok } }
        components:
          schemas:
            MessageType:
              type: string
              enum: [CHAT, SYSTEM]
            StreamPartKind:
              type: string
              enum: [NOTE, MEMO]
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
    public async Task Injects_Every_Required_Field_Through_Constructor()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("public CreateNoteFormData(string title, string body)");
    }

    [Test]
    public async Task Keeps_Fields_Whose_Type_Name_Contains_StreamPart_In_FormObject()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task CreateNote([FormObject] CreateNoteFormData formData, StreamPart attachment);");
        generatedCode.Should().Contain("public StreamPartKind? Kind { get; set; }");
    }

    [Test]
    public async Task Keeps_Nullable_File_Fields_Separate_With_Optional_Parameters()
    {
        var generatedCode = await GenerateCode(configure: settings => settings.OptionalParameters = true);
        generatedCode.Should().Contain("[FormObject] SendMessageFormData formData");
        generatedCode.Should().Contain("IEnumerable<StreamPart>? attachments");
        generatedCode.Should().NotContain("StreamPart? Attachments");
    }

    [Test]
    public async Task Keeps_FormObject_Properties_Public_For_Internal_Types()
    {
        // Refit only reads the public properties of a [FormObject]
        var generatedCode = await GenerateCode(configure: settings => settings.TypeAccessibility = TypeAccessibility.Internal);
        generatedCode.Should().Contain("internal class SendMessageFormData");
        generatedCode.Should().Contain("public MessageType? MessageType { get; set; }");
    }

    [Test]
    public async Task Sends_Collection_Fields_As_Repeated_Parts()
    {
        // Without CollectionFormat.Multi, Refit joins a [FormObject] collection into one comma-separated part
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("[AliasAs(\"tags\"), Query(CollectionFormat.Multi)]");
        generatedCode.Should().Contain("public IEnumerable<string> Tags { get; set; }");
    }

    [Test]
    public async Task Does_Not_Name_A_Property_After_Its_FormObject_Type()
    {
        // A member cannot share its enclosing type's name (CS0542)
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Task Upload([FormObject] UploadFormData formData, StreamPart file);");
        generatedCode.Should().Contain("[AliasAs(\"uploadFormData\")]");
        generatedCode.Should().Contain("public string UploadFormData2 { get; set; }");
    }

    [Test]
    public async Task Generates_Record_With_Init_Setters_For_Immutable_Records()
    {
        var generatedCode = await GenerateCode(configure: settings => settings.ImmutableRecords = true);
        generatedCode.Should().Contain("public record SendMessageFormData");
        generatedCode.Should().Contain("public MessageType? MessageType { get; init; }");
    }

    [Test]
    public async Task Generates_Record_For_Native_Records()
    {
        var generatedCode = await GenerateCode(
            configure: settings => settings.CodeGeneratorSettings = new CodeGeneratorSettings { GenerateNativeRecords = true });
        generatedCode.Should().Contain("public record SendMessageFormData");
    }

    [Test]
    public async Task Wraps_Swagger2_FormData_Parameters()
    {
        var generatedCode = await GenerateCode(SwaggerSpec);
        generatedCode.Should().Contain("Task UploadImage(long petId, [FormObject] UploadImageFormData formData, StreamPart image);");
        generatedCode.Should().Contain("public UploadImageFormData(string caption)");
    }

    [Test]
    public async Task Documents_FormObject_Properties_With_Field_Descriptions()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("/// The text of the message");
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

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_With_Immutable_Records()
    {
        var generatedCode = await GenerateCode(configure: settings => settings.ImmutableRecords = true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code_From_Swagger2()
    {
        var generatedCode = await GenerateCode(SwaggerSpec);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(
        string openApiSpec = OpenApiSpec,
        bool useMultipartFormObject = true,
        MultipleInterfaces multipleInterfaces = MultipleInterfaces.Unset,
        Action<RefitGeneratorSettings>? configure = null)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(openApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            UseMultipartFormObject = useMultipartFormObject,
            MultipleInterfaces = multipleInterfaces,
        };
        configure?.Invoke(settings);
        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
