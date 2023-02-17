namespace Refitter.Tests.Resources;

public static class EmbeddedResources
{
    private static readonly Type Type = typeof(EmbeddedResources);

    private static Stream GetStream(string name)
        => Type.Assembly.GetManifestResourceStream(Type, name)!;

    public static string SwaggerPetstoreJsonV2
    {
        get
        {
            using var stream = GetStream("V2.Swagger.json");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreJsonV3
    {
        get
        {
            using var stream = GetStream("V3.Swagger.json");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreYamlV2
    {
        get
        {
            using var stream = GetStream("V2.Swagger.yaml");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreYamlV3
    {
        get
        {
            using var stream = GetStream("V3.Swagger.yaml");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
    
    public static string GetSwaggerPetstore(SwaggerPetstoreVersions version)
    {
        return version switch
        {
            SwaggerPetstoreVersions.JsonV2 => SwaggerPetstoreJsonV2,
            SwaggerPetstoreVersions.JsonV3 => SwaggerPetstoreJsonV3,
            SwaggerPetstoreVersions.YamlV2 => SwaggerPetstoreYamlV2,
            SwaggerPetstoreVersions.YamlV3 => SwaggerPetstoreYamlV3,
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };
    }
}