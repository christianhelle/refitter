using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests.Parity;

/// <summary>
/// Temporary differential test for the NSwag removal: the native contract generator must generate exactly the
/// contracts file NSwag generates, for every parity spec and every setting that shapes contracts.
/// Deleted together with NSwag.
/// </summary>
[Category("Parity")]
public class ContractParityTests
{
    private static readonly string[] ContractVariants =
    [
        "Default",
        "NullCodeGeneratorSettings",
        "Internal",
        "ImmutableRecords",
        "NativeRecords",
        "PolymorphicSerialization",
        "IntegerTypeInt64",
        "NoInlineJsonConverters",
        "JsonLibraryVersion9",
        "NullableReferenceTypes",
        "CustomCollectionTypes",
        "ImmutableCollections",
        "CustomDateTypes",
        "NoDataAnnotations",
        "NoDefaultValues",
        "InternalSetters",
        "InlineNamedTypes",
        "AnyTypeJsonElement",
        "EnforceFlagEnums",
        "SkipDefaultAdditionalProps",
        "PreserveOriginal",
        "ContractsNamespace",
    ];

    public static IEnumerable<Func<ParityCase>> Cases()
    {
        foreach (var spec in ParitySpecs.All)
        {
            var variants = spec.IsLarge ? ParityVariants.LargeSpecVariants : ContractVariants;
            foreach (var variant in variants)
            {
                var id = spec.Id;
                yield return () => new ParityCase(id, variant);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(Cases))]
    public async Task Native_Contracts_Match_NSwag(ParityCase parityCase)
    {
        var path = ParitySpecs.Get(parityCase.Spec).Path;
        var settings = CreateSettings(parityCase, path);

        string expected;
        try
        {
            var nswagDocument = await OpenApiDocumentParser.FromFileAsync(path);
            var nswagGenerator = new CSharpClientGeneratorFactory(settings, nswagDocument).Create();
            expected = nswagGenerator.GenerateFile();
        }
        catch (Exception)
        {
            expected = "!! generation failed";
        }

        string actual;
        try
        {
            var nativeDocument = ApiDocumentLoader.LoadFile(path);
            var nativeGenerator = new ContractGeneratorFactory(CreateSettings(parityCase, path), nativeDocument).Create();
            actual = nativeGenerator.GenerateFile(new MultipleClientsFromOperationIdApiOperationNameGenerator());
        }
        catch (Exception exception)
        {
            actual = expected == "!! generation failed" ? expected : "!! generation failed: " + exception;
        }

        if (expected != actual)
        {
            var folder = Path.Combine(Path.GetTempPath(), "refitter-contract-diff", parityCase.Spec.Replace('/', '_'));
            Directory.CreateDirectory(folder);
            await File.WriteAllTextAsync(Path.Combine(folder, parityCase.Variant + ".expected.cs"), expected);
            await File.WriteAllTextAsync(Path.Combine(folder, parityCase.Variant + ".actual.cs"), actual);
            throw new InvalidOperationException(DocumentModelParityTests.DescribeFirstDifference(expected, actual));
        }
    }

    private static RefitGeneratorSettings CreateSettings(ParityCase parityCase, string path)
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = path,
            CodeGeneratorSettings = new CodeGeneratorSettings(),
        };
        ParityVariants.All[parityCase.Variant](settings);
        return settings;
    }
}
