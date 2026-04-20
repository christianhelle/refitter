using System.Collections.Immutable;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Refitter.Tests;

public class RefitterSourceGeneratorTests
{
    [Test]
    public void Should_Report_Warning_When_No_Refitter_Files_Are_Present()
    {
        var generator = LoadSourceGenerator();
        var compilation = CSharpCompilation.Create(
            "NoRefitterFiles",
            [CSharpSyntaxTree.ParseText("public sealed class Placeholder { }")],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator.AsSourceGenerator());
        driver = driver.RunGenerators(compilation);

        var diagnostics = driver.GetRunResult()
            .Results
            .Single()
            .Diagnostics;

        diagnostics.Should().ContainSingle(d => d.Id == "REFITTER003" && d.Severity == DiagnosticSeverity.Warning);
    }

    [Test]
    public void Generated_Source_Result_Should_Use_Value_Equality_For_Diagnostics()
    {
        var assembly = LoadSourceGeneratorAssembly();
        var diagnosticInfoType = assembly.GetType("Refitter.SourceGenerator.RefitterSourceGenerator+DiagnosticInfo", throwOnError: true)!;
        var generatedSourceResultType = assembly.GetType("Refitter.SourceGenerator.RefitterSourceGenerator+GeneratedSourceResult", throwOnError: true)!;

        var leftDiagnostic = CreateInstance(
            diagnosticInfoType,
            "REFITTER000",
            "Error",
            "Boom",
            "Refitter",
            DiagnosticSeverity.Error,
            true);
        var rightDiagnostic = CreateInstance(
            diagnosticInfoType,
            "REFITTER000",
            "Error",
            "Boom",
            "Refitter",
            DiagnosticSeverity.Error,
            true);

        var left = CreateInstance(
            generatedSourceResultType,
            CreateImmutableArray(diagnosticInfoType, leftDiagnostic),
            "public interface IApi { }",
            "Api.g.cs");
        var right = CreateInstance(
            generatedSourceResultType,
            CreateImmutableArray(diagnosticInfoType, rightDiagnostic),
            "public interface IApi { }",
            "Api.g.cs");

        var equals = (bool)generatedSourceResultType
            .GetMethod(nameof(object.Equals), [generatedSourceResultType])!
            .Invoke(left, [right])!;
        var leftHashCode = (int)generatedSourceResultType.GetMethod(nameof(GetHashCode), Type.EmptyTypes)!.Invoke(left, null)!;
        var rightHashCode = (int)generatedSourceResultType.GetMethod(nameof(GetHashCode), Type.EmptyTypes)!.Invoke(right, null)!;

        equals.Should().BeTrue();
        leftHashCode.Should().Be(rightHashCode);
    }

    private static IIncrementalGenerator LoadSourceGenerator()
    {
        var assembly = LoadSourceGeneratorAssembly();
        var generatorType = assembly.GetType("Refitter.SourceGenerator.RefitterSourceGenerator", throwOnError: true)!;
        return (IIncrementalGenerator)Activator.CreateInstance(generatorType)!;
    }

    private static Assembly LoadSourceGeneratorAssembly()
    {
        var root = GetRepositoryRoot();
        var releaseAssemblyPath = Path.Combine(
            root,
            "src",
            "Refitter.SourceGenerator",
            "bin",
            "Release",
            "netstandard2.0",
            "Refitter.SourceGenerator.dll");
        var debugAssemblyPath = Path.Combine(
            root,
            "src",
            "Refitter.SourceGenerator",
            "bin",
            "Debug",
            "netstandard2.0",
            "Refitter.SourceGenerator.dll");
        var assemblyPath = File.Exists(releaseAssemblyPath) ? releaseAssemblyPath : debugAssemblyPath;

        File.Exists(assemblyPath).Should().BeTrue("the source generator project should be built before running these tests");
        return System.Reflection.Assembly.LoadFrom(assemblyPath);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !Directory.Exists(Path.Combine(directory.FullName, "src", "Refitter.SourceGenerator")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("the tests should run from somewhere inside the repository");
        return directory!.FullName;
    }

    private static object CreateInstance(Type type, params object[] args) =>
        Activator.CreateInstance(
            type,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: args,
            culture: null)!;

    private static object CreateImmutableArray(Type itemType, object item)
    {
        var items = Array.CreateInstance(itemType, 1);
        items.SetValue(item, 0);

        var createMethod = typeof(ImmutableArray)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == nameof(ImmutableArray.Create) &&
                method.IsGenericMethodDefinition &&
                method.GetParameters() is [{ ParameterType.IsArray: true }])
            .MakeGenericMethod(itemType);

        return createMethod.Invoke(null, [items])!;
    }
}
