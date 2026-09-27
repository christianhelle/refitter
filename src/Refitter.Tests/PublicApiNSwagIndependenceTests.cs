using System.Reflection;
using AwesomeAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

/// <summary>
/// Guards the Refitter.Core public API against exposing NSwag or NJsonSchema types.
/// See docs/nswag-removal-plan.md.
/// </summary>
public class PublicApiNSwagIndependenceTests
{
    private const BindingFlags DeclaredPublicMembers =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    [Test]
    public void Public_Api_Does_Not_Expose_NSwag_Types()
    {
        var violations = typeof(RefitGenerator).Assembly
            .GetExportedTypes()
            .SelectMany(FindViolations)
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToList();

        violations.Should().BeEmpty();
    }

    private static IEnumerable<string> FindViolations(Type type)
    {
        foreach (var inherited in new[] { type.BaseType }.Concat(type.GetInterfaces()))
        {
            if (inherited != null && ExposesNSwag(inherited))
                yield return $"{type.FullName} inherits {inherited.FullName}";
        }

        foreach (var member in type.GetMembers(DeclaredPublicMembers))
        {
            var exposed = member switch
            {
                PropertyInfo property => [property.PropertyType],
                FieldInfo field => [field.FieldType],
                EventInfo @event => [@event.EventHandlerType!],
                MethodBase method => method.GetParameters()
                    .Select(p => p.ParameterType)
                    .Concat(method is MethodInfo m ? [m.ReturnType] : []),
                _ => Enumerable.Empty<Type>(),
            };

            foreach (var exposedType in exposed.Where(ExposesNSwag))
                yield return $"{type.FullName}.{member.Name} exposes {exposedType.FullName}";
        }
    }

    private static bool ExposesNSwag(Type type)
    {
        if (type.HasElementType)
            return ExposesNSwag(type.GetElementType()!);

        if (type.IsGenericType && type.GetGenericArguments().Any(ExposesNSwag))
            return true;

        var assemblyName = type.Assembly.GetName().Name ?? string.Empty;
        return assemblyName.StartsWith("NSwag", StringComparison.Ordinal)
               || assemblyName.StartsWith("NJsonSchema", StringComparison.Ordinal);
    }
}
