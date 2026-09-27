using System.Reflection;
using AwesomeAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

/// <summary>
/// Guards the Refitter.Core public API against exposing NSwag, NJsonSchema or Microsoft.OpenApi types.
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
            .SelectMany(type => FindViolations(type, ExposesNSwag))
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToList();

        violations.Should().BeEmpty();
    }

    [Test]
    public void Public_Api_Does_Not_Expose_Microsoft_OpenApi_Types()
    {
        var violations = typeof(RefitGenerator).Assembly
            .GetExportedTypes()
            .SelectMany(type => FindViolations(type, ExposesMicrosoftOpenApi))
            .OrderBy(violation => violation, StringComparer.Ordinal)
            .ToList();

        violations.Should().BeEmpty();
    }

    private static IEnumerable<string> FindViolations(Type type, Func<Type, bool> isForbidden)
    {
        foreach (var inherited in new[] { type.BaseType }.Concat(type.GetInterfaces()))
        {
            if (inherited != null && isForbidden(inherited))
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

            foreach (var exposedType in exposed.Where(isForbidden))
                yield return $"{type.FullName}.{member.Name} exposes {exposedType.FullName}";
        }
    }

    private static bool ExposesMicrosoftOpenApi(Type type) =>
        ExposesAssembly(type, name => name.StartsWith("Microsoft.OpenApi", StringComparison.Ordinal));

    private static bool ExposesNSwag(Type type) =>
        ExposesAssembly(
            type,
            name => name.StartsWith("NSwag", StringComparison.Ordinal)
                    || name.StartsWith("NJsonSchema", StringComparison.Ordinal));

    private static bool ExposesAssembly(Type type, Func<string, bool> isForbiddenAssembly)
    {
        if (type.HasElementType)
            return ExposesAssembly(type.GetElementType()!, isForbiddenAssembly);

        if (type.IsGenericType && type.GetGenericArguments().Any(t => ExposesAssembly(t, isForbiddenAssembly)))
            return true;

        return isForbiddenAssembly(type.Assembly.GetName().Name ?? string.Empty);
    }
}
