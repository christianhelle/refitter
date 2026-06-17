using System;
using System.Collections.Generic;

namespace Refitter.Core;

public sealed class SettingsValidationException : Exception
{
    public IReadOnlyList<string> ValidationErrors { get; }

    public SettingsValidationException(IReadOnlyList<string> errors)
        : base($"Settings validation failed with {errors.Count} error(s):{Environment.NewLine}{string.Join(Environment.NewLine, errors)}")
    {
        ValidationErrors = errors;
    }
}
