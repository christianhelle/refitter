using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Refitter.Core;

/// <summary>
/// Generates JsonSerializerContext for AOT compilation support
/// </summary>
internal static class JsonSerializerContextGenerator
{
    /// <summary>
    /// Generates a JsonSerializerContext class with all DTO types registered for source generation
    /// </summary>
    /// <param name="contracts">The generated contracts code containing the DTO types</param>
    /// <param name="settings">The generator settings</param>
    /// <returns>The generated JsonSerializerContext code</returns>
    public static string Generate(string contracts, RefitGeneratorSettings settings)
    {
        if (string.IsNullOrWhiteSpace(contracts))
            return string.Empty;

        var contextNamespace = settings.ContractsNamespace ?? settings.Namespace;
        var typeNames = ExtractTypeNames(contracts, contextNamespace);
        if (typeNames.Count == 0)
            return string.Empty;

        var contextName = GetContextTypeName(settings);
        var sb = new StringBuilder();

        sb.AppendLine($"namespace {contextNamespace}");
        sb.AppendLine("{");

        foreach (var typeName in typeNames.OrderBy(t => t))
        {
            sb.AppendLine($"    [global::System.Text.Json.Serialization.JsonSerializable(typeof({typeName}))]");
        }

        sb.AppendLine($"    internal partial class {contextName} : global::System.Text.Json.Serialization.JsonSerializerContext");
        sb.AppendLine("    {");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString()
            .TrimEnd('\r', '\n');
    }

    /// <summary>
    /// Gets the generated JsonSerializerContext type name.
    /// </summary>
    public static string GetContextTypeName(RefitGeneratorSettings settings)
    {
        var interfaceName = settings.Naming.InterfaceName;
        if (string.IsNullOrWhiteSpace(interfaceName))
        {
            interfaceName = NamingSettings.DefaultInterfaceName;
        }

        if (interfaceName.Length > 1 &&
            interfaceName[0] == 'I' &&
            char.IsUpper(interfaceName[1]))
        {
            interfaceName = interfaceName.Substring(1);
        }

        return $"{interfaceName}SerializerContext";
    }

    /// <summary>
    /// Extracts serializable type names from generated contracts.
    /// </summary>
    private static HashSet<string> ExtractTypeNames(string contracts, string contextNamespace)
    {
        var tree = CSharpSyntaxTree.ParseText(contracts);
        var root = tree.GetCompilationUnitRoot();
        var declaredTypes = root.DescendantNodes()
            .OfType<BaseTypeDeclarationSyntax>()
            .Select(DeclaredTypeInfo.Create)
            .ToArray();

        var typeNames = declaredTypes
            .Where(t => t.Arity == 0)
            .Select(t => t.GetDisplayName(contextNamespace))
            .Aggregate(
                new HashSet<string>(StringComparer.Ordinal),
                (set, typeName) =>
                {
                    set.Add(typeName);
                    return set;
                });

        if (declaredTypes.Length == 0)
            return typeNames;

        foreach (var typeSyntax in root.DescendantNodes().OfType<TypeSyntax>())
        {
            if (TryFormatClosedGenericType(typeSyntax, declaredTypes, contextNamespace, out var typeName))
            {
                typeNames.Add(typeName);
            }
        }

        return typeNames;
    }

    private static bool TryFormatClosedGenericType(
        TypeSyntax typeSyntax,
        IReadOnlyCollection<DeclaredTypeInfo> declaredTypes,
        string contextNamespace,
        out string typeName)
    {
        typeName = string.Empty;

        if (!TryResolveDeclaredType(typeSyntax, declaredTypes, GetCurrentNamespace(typeSyntax), out var declaredType) ||
            declaredType.Arity == 0 ||
            ContainsOpenTypeParameter(typeSyntax))
        {
            return false;
        }

        typeName = FormatTypeSyntax(typeSyntax, declaredTypes, contextNamespace);
        return typeName.Contains('<') && typeName.Contains('>');
    }

    private static bool ContainsOpenTypeParameter(TypeSyntax typeSyntax)
    {
        var scopedTypeParameters = typeSyntax.Ancestors()
            .OfType<TypeDeclarationSyntax>()
            .SelectMany(t => t.TypeParameterList?.Parameters ?? [])
            .Select(p => p.Identifier.Text)
            .Aggregate(
                new HashSet<string>(StringComparer.Ordinal),
                (set, typeName) =>
                {
                    set.Add(typeName);
                    return set;
                });

        return scopedTypeParameters.Count > 0 &&
               typeSyntax.DescendantNodesAndSelf()
                   .OfType<IdentifierNameSyntax>()
                   .Any(identifier => scopedTypeParameters.Contains(identifier.Identifier.Text));
    }

    private static string FormatTypeSyntax(
        TypeSyntax typeSyntax,
        IReadOnlyCollection<DeclaredTypeInfo> declaredTypes,
        string contextNamespace)
    {
        return typeSyntax switch
        {
            NullableTypeSyntax nullableType => $"{FormatTypeSyntax(nullableType.ElementType, declaredTypes, contextNamespace)}?",
            ArrayTypeSyntax arrayType => $"{FormatTypeSyntax(arrayType.ElementType, declaredTypes, contextNamespace)}{string.Concat(arrayType.RankSpecifiers)}",
            QualifiedNameSyntax qualifiedName
                when TryResolveDeclaredType(qualifiedName, declaredTypes, GetCurrentNamespace(typeSyntax), out var declaredQualifiedType)
                => FormatDeclaredTypeName(qualifiedName, declaredQualifiedType, declaredTypes, contextNamespace),
            AliasQualifiedNameSyntax aliasQualifiedName
                when TryResolveDeclaredType(aliasQualifiedName, declaredTypes, GetCurrentNamespace(typeSyntax), out var declaredAliasType)
                => FormatDeclaredTypeName(aliasQualifiedName, declaredAliasType, declaredTypes, contextNamespace),
            GenericNameSyntax genericName
                when TryResolveDeclaredType(genericName, declaredTypes, GetCurrentNamespace(typeSyntax), out var declaredGenericType)
                => FormatDeclaredTypeName(genericName, declaredGenericType, declaredTypes, contextNamespace),
            IdentifierNameSyntax identifierName
                when TryResolveDeclaredType(identifierName, declaredTypes, GetCurrentNamespace(typeSyntax), out var declaredIdentifierType)
                => declaredIdentifierType.GetDisplayName(contextNamespace),
            QualifiedNameSyntax qualifiedName => $"{FormatTypeSyntax(qualifiedName.Left, declaredTypes, contextNamespace)}.{FormatTypeSyntax(qualifiedName.Right, declaredTypes, contextNamespace)}",
            AliasQualifiedNameSyntax aliasQualifiedName => $"{aliasQualifiedName.Alias}::{FormatTypeSyntax(aliasQualifiedName.Name, declaredTypes, contextNamespace)}",
            GenericNameSyntax genericName => $"{genericName.Identifier.Text}<{string.Join(", ", genericName.TypeArgumentList.Arguments.Select(a => FormatTypeSyntax(a, declaredTypes, contextNamespace)))}>",
            _ => typeSyntax.ToString(),
        };
    }

    private static string FormatDeclaredTypeName(
        SimpleNameSyntax typeSyntax,
        DeclaredTypeInfo declaredType,
        IReadOnlyCollection<DeclaredTypeInfo> declaredTypes,
        string contextNamespace)
    {
        if (typeSyntax is not GenericNameSyntax genericName)
            return declaredType.GetDisplayName(contextNamespace);

        var arguments = string.Join(", ", genericName.TypeArgumentList.Arguments.Select(a => FormatTypeSyntax(a, declaredTypes, contextNamespace)));
        return $"{declaredType.GetDisplayName(contextNamespace)}<{arguments}>";
    }

    private static string FormatDeclaredTypeName(
        QualifiedNameSyntax typeSyntax,
        DeclaredTypeInfo declaredType,
        IReadOnlyCollection<DeclaredTypeInfo> declaredTypes,
        string contextNamespace)
    {
        if (typeSyntax.Right is not GenericNameSyntax genericName)
            return declaredType.GetDisplayName(contextNamespace);

        var arguments = string.Join(", ", genericName.TypeArgumentList.Arguments.Select(a => FormatTypeSyntax(a, declaredTypes, contextNamespace)));
        return $"{declaredType.GetDisplayName(contextNamespace)}<{arguments}>";
    }

    private static string FormatDeclaredTypeName(
        AliasQualifiedNameSyntax typeSyntax,
        DeclaredTypeInfo declaredType,
        IReadOnlyCollection<DeclaredTypeInfo> declaredTypes,
        string contextNamespace)
    {
        if (typeSyntax.Name is not GenericNameSyntax genericName)
            return declaredType.GetDisplayName(contextNamespace);

        var arguments = string.Join(", ", genericName.TypeArgumentList.Arguments.Select(a => FormatTypeSyntax(a, declaredTypes, contextNamespace)));
        return $"{declaredType.GetDisplayName(contextNamespace)}<{arguments}>";
    }

    private static bool TryResolveDeclaredType(
        TypeSyntax typeSyntax,
        IReadOnlyCollection<DeclaredTypeInfo> declaredTypes,
        string currentNamespace,
        out DeclaredTypeInfo declaredType)
    {
        var name = GetTypeNameKey(typeSyntax);
        var arity = typeSyntax switch
        {
            GenericNameSyntax genericName => genericName.TypeArgumentList.Arguments.Count,
            QualifiedNameSyntax { Right: GenericNameSyntax genericName } => genericName.TypeArgumentList.Arguments.Count,
            AliasQualifiedNameSyntax { Name: GenericNameSyntax genericName } => genericName.TypeArgumentList.Arguments.Count,
            _ => 0
        };

        var candidates = declaredTypes
            .Where(t => t.Arity == arity &&
                        (t.Name.Equals(name, StringComparison.Ordinal) ||
                         t.RelativeName.Equals(name, StringComparison.Ordinal) ||
                         t.FullyQualifiedName.Equals(name, StringComparison.Ordinal)))
            .ToArray();

        declaredType = candidates
            .FirstOrDefault(t => t.Namespace.Equals(currentNamespace, StringComparison.Ordinal))
            ?? candidates.FirstOrDefault();

        return declaredType is not null;
    }

    private static string GetCurrentNamespace(SyntaxNode node) =>
        string.Join(
            ".",
            node.Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>()
                .Select(n => n.Name.ToString())
                .Reverse());

    private static string GetTypeNameKey(TypeSyntax typeSyntax) =>
        typeSyntax switch
        {
            NullableTypeSyntax nullableType => GetTypeNameKey(nullableType.ElementType),
            IdentifierNameSyntax identifierName => identifierName.Identifier.Text,
            GenericNameSyntax genericName => genericName.Identifier.Text,
            QualifiedNameSyntax qualifiedName => $"{GetTypeNameKey(qualifiedName.Left)}.{GetTypeNameKey(qualifiedName.Right)}",
            AliasQualifiedNameSyntax aliasQualifiedName => $"{aliasQualifiedName.Alias.Identifier.Text}::{GetTypeNameKey(aliasQualifiedName.Name)}",
            _ => typeSyntax.ToString(),
        };

    private sealed record DeclaredTypeInfo(string Namespace, IReadOnlyList<string> Containers, string Name, int Arity)
    {
        public string RelativeName => string.Join(".", Containers.Append(Name));

        public string FullyQualifiedName =>
            string.IsNullOrWhiteSpace(Namespace)
                ? RelativeName
                : $"{Namespace}.{RelativeName}";

        public string GetDisplayName(string contextNamespace) =>
            Namespace.Equals(contextNamespace, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(Namespace)
                ? RelativeName
                : $"global::{FullyQualifiedName}";

        public static DeclaredTypeInfo Create(BaseTypeDeclarationSyntax typeDeclaration)
        {
            var namespaceName = GetCurrentNamespace(typeDeclaration);
            var containers = typeDeclaration.Ancestors()
                .OfType<BaseTypeDeclarationSyntax>()
                .Select(t => t.Identifier.Text)
                .Reverse()
                .ToArray();

            var arity = typeDeclaration switch
            {
                TypeDeclarationSyntax typedDeclaration => typedDeclaration.TypeParameterList?.Parameters.Count ?? 0,
                _ => 0
            };

            return new DeclaredTypeInfo(namespaceName, containers, typeDeclaration.Identifier.Text, arity);
        }
    }
}
