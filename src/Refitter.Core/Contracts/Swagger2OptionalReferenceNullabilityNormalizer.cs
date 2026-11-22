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
        // An enum is a value type, so its nullable type is not a nullable reference type
        var enumNames = root.DescendantNodes()
            .OfType<EnumDeclarationSyntax>()
            .Select(declaration => declaration.Identifier.ValueText);
        var enumNameSet = new HashSet<string>(enumNames, StringComparer.Ordinal);
        var rewrittenRoot = new Swagger2OptionalReferencePropertyNullabilityRewriter(enumNameSet).Visit(root);
        return rewrittenRoot!.ToFullString();
    }

    private sealed class Swagger2OptionalReferencePropertyNullabilityRewriter(HashSet<string> enumNames)
        : CSharpSyntaxRewriter
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

        private bool IsReferenceType(TypeSyntax typeSyntax) =>
            !IsEnum(typeSyntax) &&
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

        private bool IsEnum(TypeSyntax typeSyntax) =>
            typeSyntax switch
            {
                IdentifierNameSyntax name => enumNames.Contains(name.Identifier.ValueText),
                QualifiedNameSyntax qualified => enumNames.Contains(qualified.Right.Identifier.ValueText),
                _ => false,
            };
    }
}
