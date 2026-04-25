using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using H.Generators.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
}
