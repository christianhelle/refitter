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
            using var stream = GetStream("V2.SwaggerPetstore.json");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreJsonV3
    {
        get
        {
            using var stream = GetStream("V3.SwaggerPetstore.json");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreYamlV2
    {
        get
        {
            using var stream = GetStream("V2.SwaggerPetstore.yaml");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreYamlV3
    {
        get
        {
            using var stream = GetStream("V3.SwaggerPetstore.yaml");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }


    public static string SwaggerPetstoreJsonV2WithDifferentHeaders
    {
        get
        {
            using var stream = GetStream("V2.SwaggerPetstoreWithDifferentHeaders.json");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreJsonV3WithDifferentHeaders
    {
        get
        {
            using var stream = GetStream("V3.SwaggerPetstoreWithDifferentHeaders.json");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreYamlV2WithDifferentHeaders
    {
        get
        {
            using var stream = GetStream("V2.SwaggerPetstoreWithDifferentHeaders.yaml");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreYamlV3WithDifferentHeaders
    {
        get
        {
            using var stream = GetStream("V3.SwaggerPetstoreWithDifferentHeaders.yaml");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }



    public static string GetSwaggerPetstore(SampleOpenSpecifications version)
    {
        return version switch
        {
            SampleOpenSpecifications.SwaggerPetstoreJsonV2 => SwaggerPetstoreJsonV2,
            SampleOpenSpecifications.SwaggerPetstoreJsonV3 => SwaggerPetstoreJsonV3,
            SampleOpenSpecifications.SwaggerPetstoreYamlV2 => SwaggerPetstoreYamlV2,
            SampleOpenSpecifications.SwaggerPetstoreYamlV3 => SwaggerPetstoreYamlV3,
            SampleOpenSpecifications.SwaggerPetstoreJsonV2WithDifferentHeaders => SwaggerPetstoreJsonV2WithDifferentHeaders,
            SampleOpenSpecifications.SwaggerPetstoreJsonV3WithDifferentHeaders => SwaggerPetstoreJsonV3WithDifferentHeaders,
            SampleOpenSpecifications.SwaggerPetstoreYamlV2WithDifferentHeaders => SwaggerPetstoreYamlV2WithDifferentHeaders,
            SampleOpenSpecifications.SwaggerPetstoreYamlV3WithDifferentHeaders => SwaggerPetstoreYamlV3WithDifferentHeaders,
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };
    }
}