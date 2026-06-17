using System;
using System.Collections.Generic;
using Refitter.Core.Settings;

namespace Refitter.Core;

public sealed class SettingsBuilder
{
    private readonly Dictionary<Type, object> slices = new();

    public SettingsBuilder With<T>(T slice)
        where T : class
    {
        slices[typeof(T)] = slice;
        return this;
    }

    public T Get<T>()
        where T : class
    {
        if (slices.TryGetValue(typeof(T), out var slice))
            return (T)slice;

        throw new KeyNotFoundException(
            $"Config slice of type {typeof(T).Name} has not been registered. " +
            "Call SettingsBuilder.With<T>() to register the slice before building.");
    }

    public bool TryGet<T>(out T slice)
        where T : class
    {
        if (slices.TryGetValue(typeof(T), out var raw) && raw is T typed)
        {
            slice = typed;
            return true;
        }

        slice = default!;
        return false;
    }

    public SettingsResult Build()
    {
        var errors = new List<string>();

        Validate(errors);

        return errors.Count > 0
            ? new SettingsResult(errors)
            : new SettingsResult(null, new SettingsBundle(new Dictionary<Type, object>(slices)));
    }

    private void Validate(List<string> errors)
    {
    }
}
