using System.Reflection;

namespace Refitter;

internal static class ConventionMapper
{
    public static void Map<TSource, TDest>(TSource source, TDest destination)
        where TSource : class
        where TDest : class
    {
        var sourceProps = typeof(TSource)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name);

        var destProps = typeof(TDest)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name);

        foreach (var (name, sourceProp) in sourceProps)
        {
            if (!destProps.TryGetValue(name, out var destProp))
                continue;

            if (destProp.PropertyType != sourceProp.PropertyType)
                continue;

            destProp.SetValue(destination, sourceProp.GetValue(source));
        }
    }
}
