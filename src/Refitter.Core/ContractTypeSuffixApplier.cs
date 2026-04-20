using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refitter.Core;

internal static class ContractTypeSuffixApplier
{
    public static string ApplySuffix(string generatedCode, string suffix)
    {
        if (string.IsNullOrWhiteSpace(suffix))
            return generatedCode;

        // Parse the generated code into a syntax tree
        var tree = CSharpSyntaxTree.ParseText(generatedCode);
        var root = (CompilationUnitSyntax)tree.GetRoot();

        // First pass: collect all contract type names that need suffixing
        var typeDeclarations = root.DescendantNodes()
            .Where(n => n is ClassDeclarationSyntax
                     || n is RecordDeclarationSyntax
                     || n is StructDeclarationSyntax
                     || n is EnumDeclarationSyntax)
            .OfType<BaseTypeDeclarationSyntax>()
            .ToList();

        var declaredTypeNames = new HashSet<string>(
            typeDeclarations.Select(typeDeclaration => typeDeclaration.Identifier.Text),
            StringComparer.Ordinal);

        var typeRenameMap = typeDeclarations
            .Select(typeDeclaration => typeDeclaration.Identifier.Text)
            .Where(typeName => !typeName.EndsWith(suffix, StringComparison.Ordinal))
            .Where(typeName => !declaredTypeNames.Contains(typeName + suffix))
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(
                typeName => typeName,
                typeName => typeName + suffix,
                StringComparer.Ordinal);

        if (typeRenameMap.Count == 0)
            return generatedCode;

        // Second pass: rewrite the syntax tree with renamed types
        var rewriter = new TypeSuffixRewriter(typeRenameMap);
        var newRoot = rewriter.Visit(root);

        // Return the modified code with original formatting preserved
        return newRoot.ToFullString();
    }

    /// <summary>
    /// Syntax rewriter that renames type declarations and all references to them
    /// </summary>
    private sealed class TypeSuffixRewriter : CSharpSyntaxRewriter
    {
        private readonly Dictionary<string, string> typeRenameMap;

        public TypeSuffixRewriter(Dictionary<string, string> typeRenameMap)
        {
            this.typeRenameMap = typeRenameMap;
        }

        /// <summary>
        /// Rename class declarations
        /// </summary>
        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var newNode = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;

            if (typeRenameMap.TryGetValue(node.Identifier.Text, out var newName))
            {
                newNode = newNode.WithIdentifier(
                    SyntaxFactory.Identifier(newName)
                        .WithTriviaFrom(node.Identifier));
            }

            return newNode;
        }

        /// <summary>
        /// Rename record declarations
        /// </summary>
        public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            var newNode = (RecordDeclarationSyntax)base.VisitRecordDeclaration(node)!;

            if (typeRenameMap.TryGetValue(node.Identifier.Text, out var newName))
            {
                newNode = newNode.WithIdentifier(
                    SyntaxFactory.Identifier(newName)
                        .WithTriviaFrom(node.Identifier));
            }

            return newNode;
        }

        /// <summary>
        /// Rename struct declarations
        /// </summary>
        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var newNode = (StructDeclarationSyntax)base.VisitStructDeclaration(node)!;

            if (typeRenameMap.TryGetValue(node.Identifier.Text, out var newName))
            {
                newNode = newNode.WithIdentifier(
                    SyntaxFactory.Identifier(newName)
                        .WithTriviaFrom(node.Identifier));
            }

            return newNode;
        }

        /// <summary>
        /// Rename enum declarations
        /// </summary>
        public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var newNode = (EnumDeclarationSyntax)base.VisitEnumDeclaration(node)!;

            if (typeRenameMap.TryGetValue(node.Identifier.Text, out var newName))
            {
                newNode = newNode.WithIdentifier(
                    SyntaxFactory.Identifier(newName)
                        .WithTriviaFrom(node.Identifier));
            }

            return newNode;
        }

        /// <summary>
        /// Rename type references (IdentifierNameSyntax like "Pet" in properties, parameters, etc.)
        /// </summary>
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            var newNode = (IdentifierNameSyntax)base.VisitIdentifierName(node)!;

            if (IsTypeReferenceContext(node) &&
                typeRenameMap.TryGetValue(node.Identifier.Text, out var newName))
            {
                return newNode.WithIdentifier(
                    SyntaxFactory.Identifier(newName)
                        .WithTriviaFrom(newNode.Identifier));
            }

            return newNode;
        }

        /// <summary>
        /// Rename generic type references (GenericNameSyntax like "Task&lt;Pet&gt;")
        /// The type arguments will be visited separately by VisitIdentifierName
        /// </summary>
        public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
        {
            var newNode = (GenericNameSyntax)base.VisitGenericName(node)!;

            if (IsTypeReferenceContext(node) &&
                typeRenameMap.TryGetValue(node.Identifier.Text, out var newName))
            {
                newNode = newNode.WithIdentifier(
                    SyntaxFactory.Identifier(newName)
                        .WithTriviaFrom(node.Identifier));
            }

            return newNode;
        }

        private static bool IsTypeReferenceContext(SyntaxNode node)
        {
            return node.Parent switch
            {
                QualifiedNameSyntax qualifiedName => IsTypeReferenceContext(qualifiedName),
                AliasQualifiedNameSyntax aliasQualifiedName => IsTypeReferenceContext(aliasQualifiedName),
                ArrayTypeSyntax => true,
                CastExpressionSyntax castExpression when castExpression.Type == node => true,
                DeclarationExpressionSyntax declarationExpression when declarationExpression.Type == node => true,
                DefaultExpressionSyntax defaultExpression when defaultExpression.Type == node => true,
                EventDeclarationSyntax eventDeclaration when eventDeclaration.Type == node => true,
                ForEachStatementSyntax forEachStatement when forEachStatement.Type == node => true,
                LocalFunctionStatementSyntax localFunction when localFunction.ReturnType == node => true,
                MethodDeclarationSyntax methodDeclaration when methodDeclaration.ReturnType == node => true,
                NullableTypeSyntax => true,
                ObjectCreationExpressionSyntax objectCreationExpression when objectCreationExpression.Type == node => true,
                ParameterSyntax parameterSyntax when parameterSyntax.Type == node => true,
                PointerTypeSyntax => true,
                PropertyDeclarationSyntax propertyDeclaration when propertyDeclaration.Type == node => true,
                RefTypeSyntax => true,
                SimpleBaseTypeSyntax => true,
                SizeOfExpressionSyntax sizeOfExpression when sizeOfExpression.Type == node => true,
                TupleElementSyntax tupleElement when tupleElement.Type == node => true,
                TypeArgumentListSyntax => true,
                TypeConstraintSyntax => true,
                TypeOfExpressionSyntax typeOfExpression when typeOfExpression.Type == node => true,
                VariableDeclarationSyntax variableDeclaration when variableDeclaration.Type == node => true,
                _ => false,
            };
        }
    }
}
