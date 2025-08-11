namespace Refitter.Core;

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