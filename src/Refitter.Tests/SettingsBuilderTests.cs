using FluentAssertions;
using Refitter.Core;
using Refitter.Core.Settings;
using TUnit.Core;

namespace Refitter.Tests;

public class SettingsBuilderTests
{
    [Test]
    public void Builder_With_Should_Store_Slice()
    {
        var builder = new SettingsBuilder();
        builder.With(new GenerationConfig());

        var result = builder.Build();
        result.IsValid.Should().BeTrue();
        result.Bundle!.Get<GenerationConfig>().Should().NotBeNull();
    }

    [Test]
    public void Builder_With_Should_Return_Self()
    {
        var builder = new SettingsBuilder();
        var result = builder.With(new GenerationConfig());
        result.Should().BeSameAs(builder);
    }

    [Test]
    public void Builder_Get_Should_Return_Registered_Slice()
    {
        var builder = new SettingsBuilder();
        var config = new GenerationConfig(GenerateContracts: false);
        builder.With(config);

        var result = builder.Get<GenerationConfig>();
        result.Should().BeSameAs(config);
        result.GenerateContracts.Should().BeFalse();
    }

    [Test]
    public void Builder_Get_Should_Throw_When_Slice_Not_Registered()
    {
        var builder = new SettingsBuilder();

        Action action = () => builder.Get<GenerationConfig>();

        action.Should().Throw<KeyNotFoundException>()
            .Which.Message.Should().Contain("GenerationConfig");
    }

    [Test]
    public void Builder_TryGet_Should_Return_True_For_Registered_Slice()
    {
        var builder = new SettingsBuilder();
        var config = new GenerationConfig();
        builder.With(config);

        var found = builder.TryGet<GenerationConfig>(out var result);

        found.Should().BeTrue();
        result.Should().BeSameAs(config);
    }

    [Test]
    public void Builder_TryGet_Should_Return_False_For_Unregistered_Slice()
    {
        var builder = new SettingsBuilder();

        var found = builder.TryGet<GenerationConfig>(out var result);

        found.Should().BeFalse();
        result.Should().BeNull();
    }

    [Test]
    public void Builder_Build_Should_Return_Valid_SettingsResult()
    {
        var builder = new SettingsBuilder();
        builder.With(new GenerationConfig());

        var result = builder.Build();

        result.IsValid.Should().BeTrue();
        result.Bundle.Should().NotBeNull();
        result.Errors.Should().BeNull();
    }

    [Test]
    public void Bundle_Get_Should_Return_Registered_Slice()
    {
        var config = new GenerationConfig(GenerateClients: false);
        var bundle = new SettingsBundle(
            new Dictionary<Type, object> { { typeof(GenerationConfig), config } });

        var result = bundle.Get<GenerationConfig>();

        result.Should().BeSameAs(config);
        result.GenerateClients.Should().BeFalse();
    }

    [Test]
    public void Bundle_Get_Should_Throw_When_Slice_Not_Registered()
    {
        var bundle = new SettingsBundle(new Dictionary<Type, object>());

        Action action = () => bundle.Get<GenerationConfig>();

        action.Should().Throw<KeyNotFoundException>()
            .Which.Message.Should().Contain("GenerationConfig");
    }

    [Test]
    public void Bundle_TryGet_Should_Return_True_For_Registered_Slice()
    {
        var config = new GenerationConfig();
        var bundle = new SettingsBundle(
            new Dictionary<Type, object> { { typeof(GenerationConfig), config } });

        var found = bundle.TryGet<GenerationConfig>(out var result);

        found.Should().BeTrue();
        result.Should().BeSameAs(config);
    }

    [Test]
    public void Bundle_TryGet_Should_Return_False_For_Unregistered_Slice()
    {
        var bundle = new SettingsBundle(new Dictionary<Type, object>());

        var found = bundle.TryGet<GenerationConfig>(out var result);

        found.Should().BeFalse();
        result.Should().BeNull();
    }

    [Test]
    public void Result_IsValid_Should_Be_True_When_Errors_Is_Null()
    {
        var result = new SettingsResult(null, new SettingsBundle(new Dictionary<Type, object>()));

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Result_IsValid_Should_Be_True_When_Errors_Is_Empty()
    {
        var result = new SettingsResult(
            Array.Empty<string>(),
            new SettingsBundle(new Dictionary<Type, object>()));

        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Result_IsValid_Should_Be_False_When_Errors_Exist()
    {
        var result = new SettingsResult(new[] { "error1", "error2" });

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public void Result_ThrowIfInvalid_Should_Throw_When_Errors_Exist()
    {
        var result = new SettingsResult(new[] { "something went wrong" });

        Action action = () => result.ThrowIfInvalid();

        action.Should().Throw<SettingsValidationException>()
            .Which.ValidationErrors.Should().BeEquivalentTo("something went wrong");
    }

    [Test]
    public void Result_ThrowIfInvalid_Should_Not_Throw_When_No_Errors()
    {
        var result = new SettingsResult(null, new SettingsBundle(new Dictionary<Type, object>()));

        Action action = () => result.ThrowIfInvalid();

        action.Should().NotThrow();
    }

    [Test]
    public void ValidationException_Should_Have_ValidationErrors()
    {
        var errors = new[] { "error A", "error B" };
        var exception = new SettingsValidationException(errors);

        exception.ValidationErrors.Should().BeEquivalentTo("error A", "error B");
    }

    [Test]
    public void ValidationException_Should_Have_Message()
    {
        var errors = new[] { "error A", "error B" };
        var exception = new SettingsValidationException(errors);

        exception.Message.Should().Contain("2 error(s)");
        exception.Message.Should().Contain("error A");
        exception.Message.Should().Contain("error B");
    }

    [Test]
    public void ValidationException_Should_Inherit_From_Exception()
    {
        var exception = new SettingsValidationException(new[] { "test" });

        exception.Should().BeAssignableTo<Exception>();
    }

    [Test]
    public void ToLegacySettings_Should_Return_Defaults_When_No_Slices()
    {
        var bundle = new SettingsBundle(new Dictionary<Type, object>());

        var result = bundle.ToLegacySettings();

        result.ReturnIApiResponse.Should().BeFalse();
        result.GenerateDisposableClients.Should().BeFalse();
    }

    [Test]
    public void ToLegacySettings_Should_Map_GenerationConfig()
    {
        var bundle = new SettingsBundle(
            new Dictionary<Type, object>
            {
                {
                    typeof(GenerationConfig),
                    new GenerationConfig(
                        GenerateContracts: false,
                        GenerateClients: false,
                        GenerateDisposableClients: true,
                        GenerateDeprecatedOperations: false,
                        GenerateDefaultAdditionalProperties: false,
                        ReturnIApiResponse: true,
                        ReturnIObservable: true)
                }
            });

        var result = bundle.ToLegacySettings();

        result.GenerateContracts.Should().BeFalse();
        result.GenerateClients.Should().BeFalse();
        result.GenerateDisposableClients.Should().BeTrue();
        result.GenerateDeprecatedOperations.Should().BeFalse();
        result.GenerateDefaultAdditionalProperties.Should().BeFalse();
        result.ReturnIApiResponse.Should().BeTrue();
        result.ReturnIObservable.Should().BeTrue();
    }

    [Test]
    public void ToLegacySettings_Should_Map_OutputConfigSlice()
    {
        var bundle = new SettingsBundle(
            new Dictionary<Type, object>
            {
                {
                    typeof(OutputConfigSlice),
                    new OutputConfigSlice(
                        Namespace: "MyApp",
                        ContractsNamespace: "MyApp.Contracts",
                        OutputFolder: "./out",
                        ContractsOutputFolder: "./contracts",
                        OutputFilename: "output.cs",
                        GenerateMultipleFiles: true)
                }
            });

        var result = bundle.ToLegacySettings();

        result.Namespace.Should().Be("MyApp");
        result.ContractsNamespace.Should().Be("MyApp.Contracts");
        result.OutputFolder.Should().Be("./out");
        result.ContractsOutputFolder.Should().Be("./contracts");
        result.OutputFilename.Should().Be("output.cs");
        result.GenerateMultipleFiles.Should().BeTrue();
    }

    [Test]
    public void ToLegacySettings_Should_Map_FilterConfigSlice_With_Null_Arrays()
    {
        var bundle = new SettingsBundle(
            new Dictionary<Type, object>
            {
                {
                    typeof(FilterConfigSlice),
                    new FilterConfigSlice(
                        IncludeTags: null,
                        IncludePathMatches: null,
                        IgnoredOperationHeaders: null,
                        AdditionalNamespaces: null,
                        ExcludeNamespaces: null)
                }
            });

        var result = bundle.ToLegacySettings();

        result.IncludeTags.Should().BeEmpty();
        result.IncludePathMatches.Should().BeEmpty();
        result.IgnoredOperationHeaders.Should().BeEmpty();
        result.AdditionalNamespaces.Should().BeEmpty();
        result.ExcludeNamespaces.Should().BeEmpty();
    }

    [Test]
    public void ToLegacySettings_Should_Map_ParameterConfigSlice()
    {
        var bundle = new SettingsBundle(
            new Dictionary<Type, object>
            {
                {
                    typeof(ParameterConfigSlice),
                    new ParameterConfigSlice(
                        UseCancellationTokens: true,
                        UseIsoDateFormat: true,
                        OptionalParameters: true,
                        UseDynamicQuerystringParameters: true,
                        CollectionFormat: CollectionFormat.Csv)
                }
            });

        var result = bundle.ToLegacySettings();

        result.UseCancellationTokens.Should().BeTrue();
        result.UseIsoDateFormat.Should().BeTrue();
        result.OptionalParameters.Should().BeTrue();
        result.UseDynamicQuerystringParameters.Should().BeTrue();
        result.CollectionFormat.Should().Be(CollectionFormat.Csv);
    }

    [Test]
    public void ToLegacySettings_Should_Map_OpenApiSourceConfigSlice()
    {
        var bundle = new SettingsBundle(
            new Dictionary<Type, object>
            {
                {
                    typeof(OpenApiSourceConfigSlice),
                    new OpenApiSourceConfigSlice(
                        OpenApiPath: "https://example.com/swagger.json",
                        OpenApiPaths: null)
                }
            });

        var result = bundle.ToLegacySettings();

        result.OpenApiPath.Should().Be("https://example.com/swagger.json");
        result.OpenApiPaths.Should().BeNull();
    }

    [Test]
    public void ToLegacySettings_Should_Map_SchemaConfigSlice()
    {
        var bundle = new SettingsBundle(
            new Dictionary<Type, object>
            {
                {
                    typeof(SchemaConfigSlice),
                    new SchemaConfigSlice(
                        TrimUnusedSchema: true,
                        KeepSchemaPatterns: new[] { ".*Dto", ".*Request" },
                        IncludeInheritanceHierarchy: true)
                }
            });

        var result = bundle.ToLegacySettings();

        result.TrimUnusedSchema.Should().BeTrue();
        result.KeepSchemaPatterns.Should().BeEquivalentTo(".*Dto", ".*Request");
        result.IncludeInheritanceHierarchy.Should().BeTrue();
    }

    [Test]
    public void ToLegacySettings_Should_Map_TypeConfigSlice()
    {
        var bundle = new SettingsBundle(
            new Dictionary<Type, object>
            {
                {
                    typeof(TypeConfigSlice),
                    new TypeConfigSlice(
                        TypeAccessibility: Core.TypeAccessibility.Internal,
                        PropertyNamingPolicy: PropertyNamingPolicy.PreserveOriginal,
                        ImmutableRecords: true,
                        ContractTypeSuffix: "Dto")
                }
            });

        var result = bundle.ToLegacySettings();

        result.TypeAccessibility.Should().Be(Core.TypeAccessibility.Internal);
        result.PropertyNamingPolicy.Should().Be(PropertyNamingPolicy.PreserveOriginal);
        result.ImmutableRecords.Should().BeTrue();
        result.ContractTypeSuffix.Should().Be("Dto");
    }

    [Test]
    public void ToLegacySettings_Should_Map_FeatureConfigSlice()
    {
        var bundle = new SettingsBundle(
            new Dictionary<Type, object>
            {
                {
                    typeof(FeatureConfigSlice),
                    new FeatureConfigSlice(
                        UsePolymorphicSerialization: true,
                        AuthenticationHeaderStyle: AuthenticationHeaderStyle.Method,
                        SecurityScheme: "bearer",
                        GenerateJsonSerializerContext: true)
                }
            });

        var result = bundle.ToLegacySettings();

        result.UsePolymorphicSerialization.Should().BeTrue();
        result.AuthenticationHeaderStyle.Should().Be(AuthenticationHeaderStyle.Method);
        result.SecurityScheme.Should().Be("bearer");
        result.GenerateJsonSerializerContext.Should().BeTrue();
    }
}
