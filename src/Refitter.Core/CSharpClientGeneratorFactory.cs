using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace Refitter.Core;

public class CSharpClientGeneratorFactory
{
    private readonly RefitGeneratorSettings settings;
    private readonly OpenApiDocument document;

    public CSharpClientGeneratorFactory(RefitGeneratorSettings settings, OpenApiDocument document)
    {
        this.settings = settings;
        this.document = document;
    }

    public CustomCSharpClientGenerator Create() =>
        new(document, new CSharpClientGeneratorSettings
        {
            GenerateClientClasses = false,
            GenerateDtoTypes = true,
            GenerateClientInterfaces = false,
            GenerateExceptionClasses = false,
            CodeGeneratorSettings =
            {
                PropertyNameGenerator = new CustomCSharpPropertyNameGenerator(),
            },
            CSharpGeneratorSettings =
            {
                Namespace = settings.Namespace,
                JsonLibrary = CSharpJsonLibrary.SystemTextJson,
            }
        });
}