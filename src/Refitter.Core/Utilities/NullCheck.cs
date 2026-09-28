#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

/// <summary>
/// The string checks of <see cref="string"/>, annotated so the compiler knows the value is not null when they
/// return false. netstandard2.0 does not annotate its own.
/// </summary>
internal static class NullCheck
{
    public static bool IsNullOrEmpty([NotNullWhen(false)] string? value) => string.IsNullOrEmpty(value);

    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] string? value) => string.IsNullOrWhiteSpace(value);
}
