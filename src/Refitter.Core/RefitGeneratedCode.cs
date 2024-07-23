namespace Refitter.Core;

internal record RefitGeneratedCode(string SourceCode, params string[] InterfaceNames)
{
    public string SourceCode { get; init; } = SourceCode;
    public string[] InterfaceNames { get; init; } = InterfaceNames;

    public override string ToString()
    {
        return SourceCode;
    }
}

