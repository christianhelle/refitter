using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class SourceGeneratorDiagnosticsTests
{
    [Test]
    public void GeneratedCode_Equality_Should_Be_Structural_For_Diagnostics()
    {
        var sourceGeneratorAssembly = LoadSourceGeneratorAssembly();
        var diagnosticType = sourceGeneratorAssembly.GetType("Refitter.SourceGenerator.RefitterSourceGenerator+GeneratedDiagnostic", throwOnError: true)!;
        var generatedCodeType = sourceGeneratorAssembly.GetType("Refitter.SourceGenerator.RefitterSourceGenerator+GeneratedCode", throwOnError: true)!;
        var equatableArray = CreateEquatableArray(sourceGeneratorAssembly, diagnosticType);

        var left = Activator.CreateInstance(generatedCodeType, equatableArray, "code", "Output.g.cs");
        var right = Activator.CreateInstance(generatedCodeType, equatableArray, "code", "Output.g.cs");

        left.Should().NotBeNull();
        right.Should().NotBeNull();
        left!.Equals(right).Should().BeTrue();
        left.GetHashCode().Should().Be(right!.GetHashCode());
    }

    [Test]
    public void Generator_Should_Report_Warning_When_No_Refitter_Files_Are_Present()
    {
        var compilation = CSharpCompilation.Create(
            "SourceGeneratorDiagnosticsTests",
            [CSharpSyntaxTree.ParseText("namespace Refitter.SourceGenerator.Tests; public sealed class Stub { }")],
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generatorAssembly = LoadSourceGeneratorAssembly();
        var generator = Activator.CreateInstance(generatorAssembly.GetType("Refitter.SourceGenerator.RefitterSourceGenerator", throwOnError: true)!)!;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [((IIncrementalGenerator)generator).AsSourceGenerator()],
            parseOptions: CSharpParseOptions.Default);

        driver = driver.RunGenerators(compilation);
        var result = driver.GetRunResult();
        var diagnostics = result.Diagnostics.Concat(result.Results.SelectMany(generatorResult => generatorResult.Diagnostics)).ToArray();

        diagnostics.Should().Contain(diagnostic =>
            diagnostic.Id == "REFITTER003" &&
            diagnostic.Severity == DiagnosticSeverity.Warning &&
            diagnostic.GetMessage().Contains("No .refitter files found", StringComparison.Ordinal));
    }

    [Test]
    public void CreateUniqueHintName_Should_Differentiate_SameDirectory_Files_With_Shared_OutputFilename()
    {
        var sourceGeneratorAssembly = LoadSourceGeneratorAssembly();
        var generatorType = sourceGeneratorAssembly.GetType("Refitter.SourceGenerator.RefitterSourceGenerator", throwOnError: true)!;
        var method = generatorType.GetMethod("CreateUniqueHintName", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();

        var directory = Path.Combine(Path.GetTempPath(), "Refitter", Guid.NewGuid().ToString("N"));
        var firstPath = Path.Combine(directory, "first.refitter");
        var secondPath = Path.Combine(directory, "second.refitter");

        var firstHintName = method!.Invoke(null, [firstPath, "SharedOutput.cs"]) as string;
        var secondHintName = method.Invoke(null, [secondPath, "SharedOutput.cs"]) as string;

        firstHintName.Should().StartWith("SharedOutput_");
        secondHintName.Should().StartWith("SharedOutput_");
        firstHintName.Should().NotBe(secondHintName);
    }

    [Test]
    public void GenerateCode_Should_Return_Error_Diagnostic_When_AdditionalText_GetText_Returns_Null()
    {
        var result = InvokeGenerateCode(new StubAdditionalText("C:\\repo\\null.refitter", _ => null));

        result.Code.Should().BeNull();
        result.HintName.Should().BeNull();
        result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Id == "REFITTER000" &&
            diagnostic.Message.Contains("Unable to read .refitter file", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("null.refitter", StringComparison.Ordinal));
    }

    [Test]
    public void GenerateCode_Should_Return_Error_Diagnostic_When_AdditionalText_GetText_Throws()
    {
        var result = InvokeGenerateCode(new StubAdditionalText("C:\\repo\\throw.refitter", _ => throw new InvalidDataException("bad encoding")));

        result.Code.Should().BeNull();
        result.HintName.Should().BeNull();
        result.Diagnostics.Should().Contain(diagnostic =>
            diagnostic.Id == "REFITTER000" &&
            diagnostic.Message.Contains("Unable to read .refitter file", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("bad encoding", StringComparison.Ordinal));
    }

    [Test]
    public void ResolveRelativeSpecPaths_Should_Normalize_Relative_OpenApiPaths_Using_Refitter_File_Directory()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "RefitterSourceGeneratorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);

        try
        {
            var specPath = Path.Combine(workspace, "specs", "https-local.json");
            Directory.CreateDirectory(Path.GetDirectoryName(specPath)!);
            var settingsPath = Path.Combine(workspace, "relative.refitter");
            var settings = CreateRefitGeneratorSettings();
            var settingsType = settings.GetType();
            settingsType.GetProperty("OpenApiPaths")!.SetValue(settings, new[] { "specs/https-local.json" });

            InvokeResolveRelativeSpecPaths(settingsPath, settings);

            settingsType.GetProperty("OpenApiPaths")!.GetValue(settings)
                .Should()
                .BeEquivalentTo(new[] { specPath });
        }
        finally
        {
            if (Directory.Exists(workspace))
            {
                Directory.Delete(workspace, recursive: true);
            }
        }
    }

    [Test]
    public void ResolveRelativeSpecPaths_Should_Treat_HttpPrefixed_File_Names_As_Relative_When_Not_Urls()
    {
        var workspace = Path.Combine(Path.GetTempPath(), "RefitterSourceGeneratorTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);

        try
        {
            var specPath = Path.Combine(workspace, "https-local.json");
            var settingsPath = Path.Combine(workspace, "relative.refitter");
            var settings = CreateRefitGeneratorSettings();
            var settingsType = settings.GetType();
            settingsType.GetProperty("OpenApiPath")!.SetValue(settings, "https-local.json");

            InvokeResolveRelativeSpecPaths(settingsPath, settings);

            settingsType.GetProperty("OpenApiPath")!.GetValue(settings)
                .Should()
                .Be(specPath);
        }
        finally
        {
            if (Directory.Exists(workspace))
            {
                Directory.Delete(workspace, recursive: true);
            }
        }
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences() =>
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location)
    ];

    private static System.Reflection.Assembly LoadSourceGeneratorAssembly()
    {
        var assemblyPath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "Refitter.SourceGenerator",
                "bin",
                "Release",
                "netstandard2.0",
                "Refitter.SourceGenerator.dll"));

        return System.Reflection.Assembly.LoadFrom(assemblyPath);
    }

    private static object CreateEquatableArray(System.Reflection.Assembly sourceGeneratorAssembly, Type diagnosticType)
    {
        var diagnostic = Activator.CreateInstance(
            diagnosticType,
            "REFITTER002",
            "Warning",
            "message",
            DiagnosticSeverity.Warning,
            true)!;

        var immutableArrayCreate = typeof(ImmutableArray)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(ImmutableArray.Create) &&
                method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 1 &&
                method.GetParameters()[0].ParameterType.IsArray);

        var items = Array.CreateInstance(diagnosticType, 1);
        items.SetValue(diagnostic, 0);

        var immutableArray = immutableArrayCreate
            .MakeGenericMethod(diagnosticType)
            .Invoke(null, [items])!;

        var equatableArrayType = typeof(EquatableArray)
            .Assembly
            .GetType("H.Generators.Extensions.EquatableArray`1", throwOnError: true)!
            .MakeGenericType(diagnosticType);

        return equatableArrayType
            .GetMethod("FromImmutableArray", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [immutableArray])!;
    }

    private static GenerateCodeResult InvokeGenerateCode(AdditionalText additionalText)
    {
        var sourceGeneratorAssembly = LoadSourceGeneratorAssembly();
        var generatorType = sourceGeneratorAssembly.GetType("Refitter.SourceGenerator.RefitterSourceGenerator", throwOnError: true)!;
        var generatedCodeType = sourceGeneratorAssembly.GetType("Refitter.SourceGenerator.RefitterSourceGenerator+GeneratedCode", throwOnError: true)!;
        var diagnosticType = sourceGeneratorAssembly.GetType("Refitter.SourceGenerator.RefitterSourceGenerator+GeneratedDiagnostic", throwOnError: true)!;
        var method = generatorType.GetMethod("GenerateCode", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();

        var result = method!.Invoke(null, [additionalText, CancellationToken.None]);
        result.Should().NotBeNull();

        var diagnosticsValue = generatedCodeType.GetProperty("Diagnostics")!.GetValue(result!);
        diagnosticsValue.Should().BeAssignableTo<System.Collections.IEnumerable>();

        var diagnostics = ((System.Collections.IEnumerable)diagnosticsValue!)
            .Cast<object>()
            .Select(diagnostic => new GeneratedDiagnosticResult(
                (string)diagnosticType.GetProperty("Id")!.GetValue(diagnostic)!,
                (string)diagnosticType.GetProperty("Message")!.GetValue(diagnostic)!))
            .ToArray();

        return new GenerateCodeResult(
            diagnostics,
            generatedCodeType.GetProperty("Code")!.GetValue(result!) as string,
            generatedCodeType.GetProperty("HintName")!.GetValue(result!) as string);
    }

    private static object CreateRefitGeneratorSettings()
    {
        var sourceGeneratorAssembly = LoadSourceGeneratorAssembly();
        var generatorType = sourceGeneratorAssembly.GetType("Refitter.SourceGenerator.RefitterSourceGenerator", throwOnError: true)!;
        var method = generatorType.GetMethod("ResolveRelativeSpecPaths", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        var settingsType = method!.GetParameters()[1].ParameterType;
        return Activator.CreateInstance(settingsType)!;
    }

    private static void InvokeResolveRelativeSpecPaths(string settingsFilePath, object settings)
    {
        var sourceGeneratorAssembly = LoadSourceGeneratorAssembly();
        var generatorType = sourceGeneratorAssembly.GetType("Refitter.SourceGenerator.RefitterSourceGenerator", throwOnError: true)!;
        var method = generatorType.GetMethod("ResolveRelativeSpecPaths", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        method!.Invoke(null, [settingsFilePath, settings]);
    }

    private sealed record GenerateCodeResult(
        IReadOnlyList<GeneratedDiagnosticResult> Diagnostics,
        string? Code,
        string? HintName);

    private sealed record GeneratedDiagnosticResult(string Id, string Message);

    private sealed class StubAdditionalText(string path, Func<CancellationToken, SourceText?> getText) : AdditionalText
    {
        public override string Path => path;

        public override SourceText? GetText(CancellationToken cancellationToken = default) => getText(cancellationToken);
    }
}
