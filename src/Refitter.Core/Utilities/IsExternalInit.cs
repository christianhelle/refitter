// Lets netstandard2.0 use records and init accessors, which need a type that only newer frameworks define
namespace System.Runtime.CompilerServices;

[Diagnostics.CodeAnalysis.SuppressMessage(
    "Minor Code Smell",
    "S2094:Classes should not be empty",
    Justification = "The compiler only needs the type to exist")]
internal static class IsExternalInit
{
}
