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
        var typeNames = new HashSet<string>();
        var typeDeclarations = root.DescendantNodes()
            .Where(n => n is ClassDeclarationSyntax
                     || n is RecordDeclarationSyntax
                     || n is StructDeclarationSyntax
                     || n is EnumDeclarationSyntax)
            .OfType<BaseTypeDeclarationSyntax>()
            .ToList();

        foreach (var typeDecl in typeDeclarations)
        {
            var typeName = typeDecl.Identifier.Text;

            // Skip if already has suffix to prevent double-suffixing (#1013)
            if (!typeName.EndsWith(suffix, StringComparison.Ordinal))
            {
                typeNames.Add(typeName);
            }
        }

        if (typeNames.Count == 0)
            return generatedCode;

        // Create a mapping of original type names to suffixed names
        var typeRenameMap = typeNames.ToDictionary(
            name => name,
            name => name + suffix);

        // Second pass: rewrite the syntax tree with renamed types
        var rewriter = new TypeSuffixRewriter(typeRenameMap);
        var newRoot = rewriter.Visit(root);

        // Return the modified code with original formatting preserved
        return newRoot.ToFullString();
    }

    /// <summary>
    /// Syntax rewriter that renames type declarations and all references to them
    /// </summary>
    private class TypeSuffixRewriter : CSharpSyntaxRewriter
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
            if (typeRenameMap.TryGetValue(node.Identifier.Text, out var newName))
            {
                return SyntaxFactory.IdentifierName(newName)
                    .WithTriviaFrom(node);
            }

            return node;
        }

        /// <summary>
        /// Rename generic type references (GenericNameSyntax like "Task&lt;Pet&gt;")
        /// The type arguments will be visited separately by VisitIdentifierName
        /// </summary>
        public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
        {
            var newNode = (GenericNameSyntax)base.VisitGenericName(node)!;

            if (typeRenameMap.TryGetValue(node.Identifier.Text, out var newName))
            {
                newNode = newNode.WithIdentifier(
                    SyntaxFactory.Identifier(newName)
                        .WithTriviaFrom(node.Identifier));
            }

            return newNode;
        }
    }
}
