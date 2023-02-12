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
}