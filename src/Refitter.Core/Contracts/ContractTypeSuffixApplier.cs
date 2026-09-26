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
        var existingTypeNames = new HashSet<string>(StringComparer.Ordinal);
        var typeDeclarations = root.DescendantNodes()
            .Where(n => n is ClassDeclarationSyntax
                     || n is RecordDeclarationSyntax
                     || n is StructDeclarationSyntax
                     || n is EnumDeclarationSyntax)
            .OfType<BaseTypeDeclarationSyntax>()
            .ToList();

        // Build set of all existing type names, and the generic arities each name is declared with
        var declaredArities = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
        foreach (var typeDecl in typeDeclarations)
        {
            existingTypeNames.Add(typeDecl.Identifier.Text);

            var arity = (typeDecl as TypeDeclarationSyntax)?.TypeParameterList?.Parameters.Count ?? 0;
            if (!declaredArities.TryGetValue(typeDecl.Identifier.Text, out var arities))
            {
                arities = new HashSet<int>();
                declaredArities[typeDecl.Identifier.Text] = arities;
            }

            arities.Add(arity);
        }

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
        // Check for suffix-target collisions (#1013): reject if Pet + suffix would collide with existing PetDto
        var typeRenameMap = new Dictionary<string, string>();
        foreach (var name in typeNames)
        {
            var suffixedName = name + suffix;
            if (existingTypeNames.Contains(suffixedName))
            {
                // Collision detected: existing type already has the target name
                // Skip renaming this type to avoid duplicate type names
                continue;
            }
            typeRenameMap[name] = suffixedName;
        }

        if (typeRenameMap.Count == 0)
            return generatedCode;

        // Second pass: rewrite the syntax tree with renamed types
        var rewriter = new TypeSuffixRewriter(typeRenameMap, declaredArities);
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
        private readonly Dictionary<string, HashSet<int>> declaredArities;

        public TypeSuffixRewriter(
            Dictionary<string, string> typeRenameMap,
            Dictionary<string, HashSet<int>> declaredArities)
        {
            this.typeRenameMap = typeRenameMap;
            this.declaredArities = declaredArities;
        }

        /// <summary>
        /// Rename class declarations
        /// </summary>
        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            return ((ClassDeclarationSyntax)base.VisitClassDeclaration(node)!)
                .WithIdentifier(GetRenamedIdentifier(node.Identifier));
        }

        /// <summary>
        /// Rename record declarations
        /// </summary>
        public override SyntaxNode? VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            return ((RecordDeclarationSyntax)base.VisitRecordDeclaration(node)!)
                .WithIdentifier(GetRenamedIdentifier(node.Identifier));
        }

        /// <summary>
        /// Rename struct declarations
        /// </summary>
        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
        {
            return ((StructDeclarationSyntax)base.VisitStructDeclaration(node)!)
                .WithIdentifier(GetRenamedIdentifier(node.Identifier));
        }

        /// <summary>
        /// Rename enum declarations
        /// </summary>
        public override SyntaxNode? VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            return ((EnumDeclarationSyntax)base.VisitEnumDeclaration(node)!)
                .WithIdentifier(GetRenamedIdentifier(node.Identifier));
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
        /// Leave type names qualified with a System namespace (e.g. "System.Threading.Tasks.Task")
        /// untouched, since contracts never live there
        /// </summary>
        public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
        {
            var left = node.Left.ToString();
            if (left.StartsWith("global::", StringComparison.Ordinal))
            {
                left = left.Substring("global::".Length);
            }

            if (left != "System" && !left.StartsWith("System.", StringComparison.Ordinal))
                return base.VisitQualifiedName(node);

            // Type arguments such as ICollection<Pet> can still refer to contracts
            return node.Right is GenericNameSyntax genericName
                ? node.WithRight(
                    genericName.WithTypeArgumentList(
                        (TypeArgumentListSyntax)Visit(genericName.TypeArgumentList)!))
                : node;
        }

        /// <summary>
        /// Rename generic type references (GenericNameSyntax like "Page&lt;Pet&gt;") that match the arity
        /// of a declared contract, so that framework types such as "Task&lt;Pet&gt;" are kept.
        /// The type arguments will be visited separately by VisitIdentifierName
        /// </summary>
        public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
        {
            var newNode = (GenericNameSyntax)base.VisitGenericName(node)!;

            if (typeRenameMap.TryGetValue(node.Identifier.Text, out var newName) &&
                declaredArities[node.Identifier.Text].Contains(node.TypeArgumentList.Arguments.Count))
            {
                newNode = newNode.WithIdentifier(
                    SyntaxFactory.Identifier(newName)
                        .WithTriviaFrom(node.Identifier));
            }

            return newNode;
        }

        private SyntaxToken GetRenamedIdentifier(SyntaxToken identifier)
        {
            if (typeRenameMap.TryGetValue(identifier.Text, out var newName))
            {
                return SyntaxFactory.Identifier(newName)
                    .WithTriviaFrom(identifier);
            }

            return identifier;
        }
    }
}
