using Microsoft.CodeAnalysis.CSharp;

namespace Refitter.Core;

internal static class CSharpStringLiteral
{
    public static string Format(string value) =>
        SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Literal(value)).ToFullString();
}
