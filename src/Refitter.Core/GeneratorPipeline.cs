using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NSwag;

namespace Refitter.Core;

internal sealed class GeneratorPipeline
{
    private static readonly Regex JsonStringEnumConverterAttributeRegex = new(
        @"^\s*\[(System\.Text\.Json\.Serialization\.)?JsonConverter\(typeof\((System\.Text\.Json\.Serialization\.)?JsonStringEnumConverter(?:<[\w.]+>)?\)\)\]\s*\r?\n?",
        RegexOptions.Compiled | RegexOptions.Multiline,
        TimeSpan.FromSeconds(1));

    private static readonly Regex EnumDeclarationRegex = new(
        @"^(\s*)((?:public|internal)\s+(?:partial\s+)?enum\s+\w+\b)",
        RegexOptions.Compiled | RegexOptions.Multiline,
        TimeSpan.FromSeconds(1));

    public GenerationResult Run(
        OpenApiDocument document,
        RefitGeneratorSettings settings,
        CustomCSharpClientGenerator generator)
    {
        var docGenerator = new XmlDocumentationGenerator(settings);

        // Create the interface generator before calling GenerateFile() so that
        // OperationNameGenerator.CheckForDuplicateOperationIds() sees the original
        // (pre-generation) operation IDs. GenerateFile() auto-populates operation IDs
        // with globally unique names which would prevent the switch to the path segments
        // generator, causing unnecessary numeric suffixes in ByTag mode.
        var interfaceGenerator = new InterfaceGenerator(settings, document, generator, docGenerator);

        var contracts = generator.GenerateFile();
        contracts = SanitizeGeneratedContracts(document, settings, contracts);
        var serializerContext = GenerateJsonSerializerContext(document, settings, contracts);
        var interfaces = GenerateClient(document, settings, interfaceGenerator);
        var interfaceNames = interfaces.Select(c => c.TypeName).ToArray();
        var title = settings.Naming.UseOpenApiTitle && !string.IsNullOrWhiteSpace(document.Info?.Title)
            ? document.Info!.Title.Sanitize()
            : settings.Naming.InterfaceName;
        var dependencyInjectionCode = settings.ApizrSettings != null
            ? ApizrRegistrationGenerator.Generate(settings, interfaceNames, title)
            : DependencyInjectionGenerator.Generate(settings, interfaceNames);

        return new GenerationResult(contracts, interfaces, serializerContext, dependencyInjectionCode, interfaceNames);
    }

    private static IInterfacePartitioning GetInterfacePartitioning(OpenApiDocument document, RefitGeneratorSettings settings)
    {
        return settings.MultipleInterfaces switch
        {
            MultipleInterfaces.ByEndpoint => new ByEndpointInterfacePartitioning(settings),
            MultipleInterfaces.ByTag => new ByTagInterfacePartitioning(settings, document),
            _ => new SingleInterfacePartitioning(settings),
        };
    }

    internal static string SanitizeGeneratedContracts(OpenApiDocument document, RefitGeneratorSettings settings, string contracts)
    {
        contracts = NormalizeSwagger2OptionalReferencePropertyNullability(document, settings, contracts);

        if (settings.CodeGeneratorSettings is not { InlineJsonConverters: false })
        {
            contracts = JsonStringEnumConverterAttributeRegex.Replace(contracts, string.Empty);
            var newLine = GetPreferredNewLine(contracts);
            return EnumDeclarationRegex
                .Replace(
                    contracts,
                    match =>
                        $"{match.Groups[1].Value}[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]{newLine}{match.Groups[1].Value}{match.Groups[2].Value}")
                .TrimEnd();
        }

        return JsonStringEnumConverterAttributeRegex
            .Replace(contracts, string.Empty)
            .TrimEnd();
    }

    private static string GetPreferredNewLine(string content) =>
        content.Contains("\r\n", StringComparison.Ordinal)
            ? "\r\n"
            : "\n";

    internal static string NormalizeSwagger2OptionalReferencePropertyNullability(
        OpenApiDocument document,
        RefitGeneratorSettings settings,
        string contracts)
    {
        if (document.SchemaType != NJsonSchema.SchemaType.Swagger2 ||
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

    internal static string GenerateJsonSerializerContext(
        OpenApiDocument document,
        RefitGeneratorSettings settings,
        string contracts) =>
        settings.GenerateJsonSerializerContext && settings.GenerateContracts
            ? JsonSerializerContextGenerator.Generate(contracts, settings, document.Info?.Title)
            : string.Empty;

    private static IReadOnlyCollection<GeneratedCode> GenerateClient(
        OpenApiDocument document,
        RefitGeneratorSettings settings,
        InterfaceGenerator interfaceGenerator)
    {
        var code = new StringBuilder();
        GenerateAutoGeneratedHeader(settings, code);

        code.AppendLine()
            .AppendLine(RefitInterfaceImports.GenerateNamespaceImports(settings))
            .AppendLine();

        if (settings.AdditionalNamespaces.Any())
        {
            foreach (var ns in settings.AdditionalNamespaces)
            {
                code.AppendLine($"using {ns};");
            }

            code.AppendLine();
        }

        code.AppendLine("#nullable enable annotations");
        code.AppendLine();

        var partitioning = GetInterfacePartitioning(document, settings);
        var refitInterfaces = interfaceGenerator.Generate(partitioning);
        var generatedCodes = refitInterfaces as GeneratedCode[] ?? refitInterfaces.ToArray();

        if (settings.GenerateMultipleFiles)
        {
            for (int i = 0; i < generatedCodes.Length; i++)
            {
                generatedCodes[i] = generatedCodes[i] with
                {
                    Content = code +
                              $$"""
                                namespace {{settings.Namespace}}
                                {
                                {{generatedCodes[i].Content}}
                                }
                                """
                };
            }

            return generatedCodes;
        }

        code.AppendLine($"namespace {settings.Namespace}");
        code.AppendLine("{");

        foreach (var generatedCode in generatedCodes)
        {
            code.AppendLine(generatedCode.Content);
        }

        code.Append("}");

        return new[] { new GeneratedCode(generatedCodes.First().TypeName, code.ToString()) }
            .Union(
                generatedCodes
                    .Skip(1)
                    .Select(c => new GeneratedCode(c.TypeName, string.Empty)))
            .ToArray();
    }

    private static void GenerateAutoGeneratedHeader(RefitGeneratorSettings settings, StringBuilder code)
    {
        if (!settings.AddAutoGeneratedHeader)
            return;

        code.AppendLine("""
            // <auto-generated>
            //     This code was generated by Refitter.
            // </auto-generated>

            """);
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
                PredefinedTypeSyntax predefinedType => predefinedType.Keyword.Kind() is SyntaxKind.ObjectKeyword or SyntaxKind.StringKeyword,
                ArrayTypeSyntax => true,
                IdentifierNameSyntax => true,
                GenericNameSyntax => true,
                QualifiedNameSyntax => true,
                AliasQualifiedNameSyntax => true,
                _ => false,
            };
    }
}

internal record GenerationResult(
    string Contracts,
    IReadOnlyCollection<GeneratedCode> Interfaces,
    string SerializerContext,
    string DependencyInjectionCode,
    string[] InterfaceNames);
