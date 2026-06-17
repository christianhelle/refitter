using System;
using System.Collections.Generic;

namespace Refitter.Core;

public sealed record SettingsResult(
    IReadOnlyList<string>? Errors,
    SettingsBundle? Bundle = null)
{
    public bool IsValid => Errors is null || Errors.Count == 0;

    public void ThrowIfInvalid()
    {
        if (!IsValid)
            throw new SettingsValidationException(Errors!);
    }
}
