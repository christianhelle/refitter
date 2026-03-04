using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

[ExcludeFromCodeCoverage]
internal record RefitGeneratedCode(
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
