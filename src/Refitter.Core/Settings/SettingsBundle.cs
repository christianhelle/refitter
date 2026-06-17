using System;
using System.Collections.Generic;

namespace Refitter.Core;

public sealed record SettingsBundle(IReadOnlyDictionary<Type, object> Slices)
{
    public T Get<T>()
        where T : class
    {
        if (Slices.TryGetValue(typeof(T), out var slice))
            return (T)slice;

        throw new KeyNotFoundException(
            $"Config slice of type {typeof(T).Name} has not been registered. " +
            "Call SettingsBuilder.With<T>() to register the slice before building.");
    }

    public bool TryGet<T>(out T slice)
        where T : class
    {
        if (Slices.TryGetValue(typeof(T), out var raw) && raw is T typed)
        {
            slice = typed;
            return true;
        }

        slice = default!;
        return false;
    }
}
