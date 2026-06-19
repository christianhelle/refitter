using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NJsonSchema;
using NSwag;

namespace Refitter.Core;

internal sealed class Swagger2OptionalReferenceNullabilityNormalizer : IContractsPostProcessor
{
    public string Process(OpenApiDocument document, RefitGeneratorSettings settings, string contracts)
    {
        if (document.SchemaType != SchemaType.Swagger2 ||
            settings.CodeGeneratorSettings?.GenerateNullableReferenceTypes != true ||
            settings.CodeGeneratorSettings.GenerateOptionalPropertiesAsNullable)
        {
            return contracts;
        }

        var tree = CSharpSyntaxTree.ParseText(contracts);
        var root = tree.GetCompilationUnitRoot();
        var rewrittenRoot = new Swagger2OptionalReferencePropertyNullabilityRewriter().Visit(root);
        return rewrittenRoot!.ToFullString();
    }

    private sealed class Swagger2OptionalReferencePropertyNullabilityRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            if (node.Type is NullableTypeSyntax nullableType &&
                IsReferenceType(nullableType.ElementType))
            {
                node = node.WithType(nullableType.ElementType.WithTriviaFrom(node.Type));
            }

            return base.VisitPropertyDeclaration(node);
        }

        private static bool IsReferenceType(TypeSyntax typeSyntax) =>
            typeSyntax switch
            {
                PredefinedTypeSyntax predefinedType =>
                    predefinedType.Keyword.Kind() is SyntaxKind.ObjectKeyword or SyntaxKind.StringKeyword,
                ArrayTypeSyntax => true,
                IdentifierNameSyntax => true,
                GenericNameSyntax => true,
                QualifiedNameSyntax => true,
                AliasQualifiedNameSyntax => true,
                _ => false,
            };
    }
}
