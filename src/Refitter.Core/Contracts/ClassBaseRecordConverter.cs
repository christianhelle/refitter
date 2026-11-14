using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NSwag;

namespace Refitter.Core;

/// <summary>
/// Turns records that derive from a class into classes, since a record can only inherit from
/// <c>object</c> or another record. The class template keeps records deriving directly from a collection
/// base type as classes, and this walks the rest of the inheritance chain, e.g. an <c>allOf</c> schema
/// extending a dictionary-derived contract (#1398).
/// </summary>
internal sealed class ClassBaseRecordConverter : IContractsPostProcessor
{
    public string Process(OpenApiDocument document, RefitGeneratorSettings settings, string contracts) =>
        Convert(contracts);

    public static string Convert(string contracts)
    {
        if (!contracts.Contains("record "))
            return contracts;

        var root = CSharpSyntaxTree.ParseText(contracts).GetRoot();
        var classNames = new HashSet<string>(
            root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Select(declaration => declaration.Identifier.ValueText),
            StringComparer.Ordinal);
        var records = root.DescendantNodes()
            .OfType<RecordDeclarationSyntax>()
            .Where(declaration => !declaration.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword))
            .ToList();

        var converted = new List<RecordDeclarationSyntax>();
        bool changed;
        do
        {
            changed = false;
            foreach (var record in records.Except(converted).ToList())
            {
                if (GetBaseTypeName(record) is not { } baseTypeName || !classNames.Contains(baseTypeName))
                    continue;

                converted.Add(record);
                classNames.Add(record.Identifier.ValueText);
                changed = true;
            }
        } while (changed);

        if (converted.Count == 0)
            return contracts;

        var text = contracts;
        foreach (var span in converted.Select(record => record.Keyword.Span).OrderByDescending(span => span.Start))
            text = text.Remove(span.Start, span.Length).Insert(span.Start, "class");

        return text;
    }

    private static string? GetBaseTypeName(RecordDeclarationSyntax record) =>
        record.BaseList?.Types.FirstOrDefault()?.Type switch
        {
            QualifiedNameSyntax qualifiedName => qualifiedName.Right.Identifier.ValueText,
            SimpleNameSyntax simpleName => simpleName.Identifier.ValueText,
            _ => null,
        };
}
