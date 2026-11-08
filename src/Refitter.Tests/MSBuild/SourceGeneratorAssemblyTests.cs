using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using AwesomeAssertions;

namespace Refitter.Tests.MSBuild;

/// <summary>
/// MSBuild loads the telemetry task from the generator assembly with GetExportedTypes(). Microsoft.CodeAnalysis
/// isn't available in the MSBuild process, so a public type built on it makes .NET Framework MSBuild (Visual Studio)
/// fall back to a task host that can't create the task, which fails the build.
/// </summary>
[TestClass]
public class SourceGeneratorAssemblyTests
{
    [TestMethod]
    public void Public_Types_Do_Not_Derive_From_Or_Implement_Roslyn_Types()
    {
        using var stream = File.OpenRead(GetSourceGeneratorAssembly());
        using var peReader = new PEReader(stream);
        var metadata = peReader.GetMetadataReader();

        var roslynBasedPublicTypes = metadata.TypeDefinitions
            .Select(metadata.GetTypeDefinition)
            .Where(type => (type.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public)
            .Where(type => GetBaseTypeAndInterfaces(metadata, type).Any(handle => IsRoslynType(metadata, handle)))
            .Select(type => metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name))
            .ToList();

        roslynBasedPublicTypes.Should().BeEmpty();
    }

    private static IEnumerable<EntityHandle> GetBaseTypeAndInterfaces(MetadataReader metadata, TypeDefinition type)
    {
        if (!type.BaseType.IsNil)
        {
            yield return type.BaseType;
        }

        foreach (var implementation in type.GetInterfaceImplementations())
        {
            yield return metadata.GetInterfaceImplementation(implementation).Interface;
        }
    }

    private static bool IsRoslynType(MetadataReader metadata, EntityHandle handle)
    {
        if (handle.Kind != HandleKind.TypeReference)
        {
            return false;
        }

        var scope = metadata.GetTypeReference((TypeReferenceHandle)handle).ResolutionScope;
        return scope.Kind == HandleKind.AssemblyReference &&
               metadata.GetString(metadata.GetAssemblyReference((AssemblyReferenceHandle)scope).Name)
                   .StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal);
    }

    private static string GetSourceGeneratorAssembly() =>
        typeof(SourceGeneratorAssemblyTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RefitterSourceGeneratorAssembly")
            .Value!;
}
