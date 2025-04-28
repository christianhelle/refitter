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

    public static string SwaggerPetstoreJsonV2WithAuthenticationHeaders
    {
        get
        {
            using var stream = GetStream("V2.SwaggerPetstoreWithAuthenticationHeaders.json");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreJsonV3WithAuthenticationHeaders
    {
        get
        {
            using var stream = GetStream("V3.SwaggerPetstoreWithAuthenticationHeaders.json");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreYamlV2WithAuthenticationHeaders
    {
        get
        {
            using var stream = GetStream("V2.SwaggerPetstoreWithAuthenticationHeaders.yaml");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreYamlV3WithAuthenticationHeaders
    {
        get
        {
            using var stream = GetStream("V3.SwaggerPetstoreWithAuthenticationHeaders.yaml");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreJsonV2WithUnsafeAuthenticationHeaders
    {
        get
        {
            using var stream = GetStream("V2.SwaggerPetstoreWithUnsafeAuthenticationHeaders.json");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreJsonV3WithUnsafeAuthenticationHeaders
    {
        get
        {
            using var stream = GetStream("V3.SwaggerPetstoreWithUnsafeAuthenticationHeaders.json");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreYamlV2WithUnsafeAuthenticationHeaders
    {
        get
        {
            using var stream = GetStream("V2.SwaggerPetstoreWithUnsafeAuthenticationHeaders.yaml");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }

    public static string SwaggerPetstoreYamlV3WithUnsafeAuthenticationHeaders
    {
        get
        {
            using var stream = GetStream("V3.SwaggerPetstoreWithUnsafeAuthenticationHeaders.yaml");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }

    public static string SwaggerIllegalPathsJsonV3
    {
        get
        {
            using var stream = GetStream("V3.SwaggerIllegalPaths.json");
            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }

    public static string SwaggerIllegalSymbolsInTitleJsonV3
    {
        get
        {
            using var stream = GetStream("V3.SwaggerIllegalTitle.json");
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
            SampleOpenSpecifications.SwaggerPetstoreJsonV2WithDifferentHeaders =>
                SwaggerPetstoreJsonV2WithDifferentHeaders,
            SampleOpenSpecifications.SwaggerPetstoreJsonV3WithDifferentHeaders =>
                SwaggerPetstoreJsonV3WithDifferentHeaders,
            SampleOpenSpecifications.SwaggerPetstoreYamlV2WithDifferentHeaders =>
                SwaggerPetstoreYamlV2WithDifferentHeaders,
            SampleOpenSpecifications.SwaggerPetstoreYamlV3WithDifferentHeaders =>
                SwaggerPetstoreYamlV3WithDifferentHeaders,
            SampleOpenSpecifications.SwaggerPetstoreJsonV2WithAuthenticationHeaders =>
                SwaggerPetstoreJsonV2WithAuthenticationHeaders,
            SampleOpenSpecifications.SwaggerPetstoreJsonV3WithAuthenticationHeaders =>
                SwaggerPetstoreJsonV3WithAuthenticationHeaders,
            SampleOpenSpecifications.SwaggerPetstoreYamlV2WithAuthenticationHeaders =>
                SwaggerPetstoreYamlV2WithAuthenticationHeaders,
            SampleOpenSpecifications.SwaggerPetstoreYamlV3WithAuthenticationHeaders =>
                SwaggerPetstoreYamlV3WithAuthenticationHeaders,
            SampleOpenSpecifications.SwaggerPetstoreJsonV2WithUnsafeAuthenticationHeaders =>
                SwaggerPetstoreJsonV2WithUnsafeAuthenticationHeaders,
            SampleOpenSpecifications.SwaggerPetstoreJsonV3WithUnsafeAuthenticationHeaders =>
                SwaggerPetstoreJsonV3WithUnsafeAuthenticationHeaders,
            SampleOpenSpecifications.SwaggerPetstoreYamlV2WithUnsafeAuthenticationHeaders =>
                SwaggerPetstoreYamlV2WithUnsafeAuthenticationHeaders,
            SampleOpenSpecifications.SwaggerPetstoreYamlV3WithUnsafeAuthenticationHeaders =>
                SwaggerPetstoreYamlV3WithUnsafeAuthenticationHeaders,
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };
    }
}
