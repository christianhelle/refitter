using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

[ExcludeFromCodeCoverage]
#pragma warning disable S1186 // Positional record constructor is compiler-generated
internal record RefitGeneratedCode(
#pragma warning restore S1186
    string SourceCode,
    params string[] InterfaceNames)
{
    public string SourceCode { get; } = SourceCode;
    public string[] InterfaceNames { get; } = InterfaceNames;

    public override string ToString()
    {
        return SourceCode;
    }
}
