// Lets netstandard2.0 use records and init accessors, which need a type that only newer frameworks define
namespace System.Runtime.CompilerServices;

internal static class IsExternalInit
{
}
