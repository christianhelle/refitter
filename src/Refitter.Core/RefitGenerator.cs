using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NSwag;

namespace Refitter.Core;

/// <summary>
/// Generates Refit clients and interfaces based on an OpenAPI specification.
/// </summary>
public class RefitGenerator(RefitGeneratorSettings settings, OpenApiDocument document)
{
    private static readonly Regex JsonStringEnumConverterAttributeRegex = new(
        @"^\s*\[(System\.Text\.Json\.Serialization\.)?JsonConverter\(typeof\((System\.Text\.Json\.Serialization\.)?JsonStringEnumConverter(?:<[\w.]+>)?\)\)\]\s*\r?\n?",
        RegexOptions.Compiled | RegexOptions.Multiline,
        TimeSpan.FromSeconds(1));

    private static readonly Regex EnumDeclarationRegex = new(
        @"^(\s*)((?:public|internal)\s+(?:partial\s+)?enum\s+\w+\b)",
        RegexOptions.Compiled | RegexOptions.Multiline,
        TimeSpan.FromSeconds(1));

    /// <summary>
    /// OpenAPI specifications used to generate Refit clients and interfaces.
    /// </summary>
    public OpenApiDocument OpenApiDocument => document;

    /// <summary>
    /// Creates a new instance of the <see cref="RefitGenerator"/> class asynchronously.
    /// </summary>
    /// <param name="settings">The settings used to configure the generator.</param>
    /// <returns>A new instance of the <see cref="RefitGenerator"/> class.</returns>
    public static async Task<RefitGenerator> CreateAsync(RefitGeneratorSettings settings)
    {
        var openApiDocument = await GetOpenApiDocument(settings).ConfigureAwait(false);

        ProcessTagFilters(openApiDocument, settings.IncludeTags);
        ProcessPathFilters(openApiDocument, settings.IncludePathMatches);
        ProcessContractFilter(openApiDocument, settings.TrimUnusedSchema, settings.KeepSchemaPatterns, settings.IncludeInheritanceHierarchy);

        return new RefitGenerator(settings, openApiDocument);
    }

    private static async Task<OpenApiDocument> GetOpenApiDocument(RefitGeneratorSettings settings)
    {
        if (settings.OpenApiPaths is { Length: > 0 })
            return await OpenApiDocumentFactory.CreateAsync(settings.OpenApiPaths).ConfigureAwait(false);
        return await OpenApiDocumentFactory.CreateAsync(settings.OpenApiPath!).ConfigureAwait(false);
    }

    private static void ProcessContractFilter(OpenApiDocument openApiDocument, bool removeUnusedSchema, string[] includeSchemaMatches,
        bool includeInheritanceHierarchy)
    {
        if (!removeUnusedSchema)
        {
            return;
        }
        var cleaner = new SchemaCleaner(openApiDocument, includeSchemaMatches)
        {
            IncludeInheritanceHierarchy = includeInheritanceHierarchy
        };
        cleaner.RemoveUnreferencedSchema();
    }

    private static void ProcessTagFilters(OpenApiDocument document, IReadOnlyCollection<string> includeTags)
    {
        if (includeTags.Count == 0)
        {
            return;
        }
        var clonedPaths = document.Paths.Where(pair => pair.Value != null)
            // as we modify the document.Paths
            // we have to enumerate on a snapshot of the items
            .ToArray();
        foreach (var path in clonedPaths)
        {
            if (path.Value == null) continue;

            var methods = path.Value.Where(pair => pair.Value != null)
                // same reason as with document.Paths
                .ToArray();
            foreach (var method in methods)
            {
                if (method.Value == null) continue;

                var exclude = method.Value.Tags?.Exists(includeTags.Contains) != true;
                if (exclude)
                {
                    path.Value.Remove(method.Key);
                }
                if (path.Value.Count == 0)
                {
                    document.Paths.Remove(path.Key);
                }
            }
        }
    }

    private static void ProcessPathFilters(OpenApiDocument document,
        string[] pathMatchExpressions)
    {
        if (pathMatchExpressions.Length == 0)
        {
            return;
        }

        // compile all expressions here once, as we will use them more than once
        var regexes = pathMatchExpressions
            .Select(x => new Regex(x, RegexOptions.Compiled, TimeSpan.FromSeconds(1)))
            .ToArray();
        var paths = document.Paths.Keys
            .Where(
                pathKey =>
                {
                    for (var i = 0; i < regexes.Length; i++)
                    {
                        if (regexes[i].IsMatch(pathKey))
                            return false;
                    }

                    return true;
                })
            .ToArray();

        foreach (string pathKey in paths)
        {
            document.Paths.Remove(pathKey);
        }
    }

    /// <summary>
    /// Generates Refit clients and interfaces based on an OpenAPI specification and returns the generated code as a string.
    /// </summary>
    /// <returns>The generated code as a string.</returns>
    public string Generate()
    {
        var factory = new CSharpClientGeneratorFactory(settings, document);
        var generator = factory.Create();
        var docGenerator = new XmlDocumentationGenerator(settings);

        // Create the interface generator before calling GenerateFile() so that
        // OperationNameGenerator.CheckForDuplicateOperationIds() sees the original
        // (pre-generation) operation IDs. GenerateFile() auto-populates operation IDs
        // with globally unique names which would prevent the switch to the path segments
        // generator, causing unnecessary numeric suffixes in ByTag mode.
        var interfaceGenerator = CreateInterfaceGenerator(generator, docGenerator);

        var contracts = generator.GenerateFile();
        contracts = SanitizeGeneratedContracts(contracts);
        var serializerContext = GenerateJsonSerializerContext(contracts);

        if (settings.GenerateClients)
        {
            contracts = RefitInterfaceImports
                .GetImportedNamespaces(settings)
                .Aggregate(
                    contracts,
                    (current, import) => current.Replace($"{import}.", string.Empty));
        }

        var refitInterfaces = GenerateClient(interfaceGenerator);
        var interfaceNames = refitInterfaces.Select(c => c.TypeName).ToArray();
        var refitInterfacesCode = string.Join("", refitInterfaces.Select(c => c.Content));
        var title = settings.Naming.UseOpenApiTitle && !string.IsNullOrWhiteSpace(document.Info?.Title)
            ? document.Info!.Title.Sanitize()
            : settings.Naming.InterfaceName;
        var result = new StringBuilder()
            .AppendLine(settings.GenerateClients ? refitInterfacesCode : string.Empty)
            .AppendLine()
            .AppendLine(settings.GenerateContracts ? contracts : string.Empty)
            .AppendLine(serializerContext)
            .AppendLine(
                settings.ApizrSettings != null
                    ? ApizrRegistrationGenerator.Generate(settings, interfaceNames, title)
                    : DependencyInjectionGenerator.Generate(settings, interfaceNames))
            .ToString()
            .TrimEnd();

        var contractTypeSuffix = settings.ContractTypeSuffix;
        if (contractTypeSuffix is not null && !string.IsNullOrWhiteSpace(contractTypeSuffix))
        {
            result = ContractTypeSuffixApplier.ApplySuffix(result, contractTypeSuffix);
        }

        return result;
    }

    /// <summary>
    /// Generates multiple files containing Refit interfaces and contracts.
    /// </summary>
    /// <returns>A GeneratorOutput containing all generated code files.</returns>
    public GeneratorOutput GenerateMultipleFiles()
    {
        var factory = new CSharpClientGeneratorFactory(settings, document);
        var generator = factory.Create();
        var docGenerator = new XmlDocumentationGenerator(settings);

        // Create the interface generator before calling GenerateFile() so that
        // OperationNameGenerator.CheckForDuplicateOperationIds() sees the original
        // (pre-generation) operation IDs. GenerateFile() auto-populates operation IDs
        // with globally unique names which would prevent the switch to the path segments
        // generator, causing unnecessary numeric suffixes in ByTag mode.
        var interfaceGenerator = CreateInterfaceGenerator(generator, docGenerator);

        var contracts = generator.GenerateFile();
        contracts = SanitizeGeneratedContracts(contracts);
        var serializerContext = GenerateJsonSerializerContext(contracts);

        var generatedFiles = new List<GeneratedCode>();

        var refitInterfaces = GenerateClient(interfaceGenerator);
        generatedFiles.AddRange(refitInterfaces);

        if (settings.GenerateContracts)
        {
            generatedFiles.Add(
                new GeneratedCode(
                    TypenameConstants.Contracts,
                    contracts));
        }

        if (!string.IsNullOrWhiteSpace(serializerContext))
        {
            generatedFiles.Add(
                new GeneratedCode(
                    JsonSerializerContextGenerator.GetContextTypeName(settings, document.Info?.Title),
                    serializerContext));
        }

        if (settings.DependencyInjectionSettings is not null || settings.ApizrSettings is not null)
        {
            var title = settings.Naming.UseOpenApiTitle && !string.IsNullOrWhiteSpace(document.Info?.Title)
                ? document.Info!.Title.Sanitize()
                : settings.Naming.InterfaceName;

            var interfaceNames = refitInterfaces.Select(c => c.TypeName).ToArray();
            var configurationCode = settings.ApizrSettings != null
                ? ApizrRegistrationGenerator.Generate(settings, interfaceNames, title)
                : DependencyInjectionGenerator.Generate(settings, interfaceNames);

            if (!string.IsNullOrWhiteSpace(configurationCode))
            {
                generatedFiles.Add(
                    new GeneratedCode(
                        TypenameConstants.DependencyInjection,
                        configurationCode));
            }
        }

        var contractTypeSuffix = settings.ContractTypeSuffix;
        if (contractTypeSuffix is not null && !string.IsNullOrWhiteSpace(contractTypeSuffix))
        {
            generatedFiles = generatedFiles
                .Select(f => f with
                {
                    Content = ContractTypeSuffixApplier.ApplySuffix(f.Content, contractTypeSuffix)
                })
                .ToList();
        }

        return new GeneratorOutput(generatedFiles);
    }

    /// <summary>
    /// Creates the appropriate interface generator based on settings.
    /// This must be called before GenerateFile() to ensure operation ID detection works correctly.
    /// </summary>
    private IRefitInterfaceGenerator CreateInterfaceGenerator(
        CustomCSharpClientGenerator generator,
        XmlDocumentationGenerator docGenerator)
    {
        return settings.MultipleInterfaces switch
        {
            MultipleInterfaces.ByEndpoint => new RefitMultipleInterfaceGenerator(settings, document, generator, docGenerator),
            MultipleInterfaces.ByTag => new RefitMultipleInterfaceByTagGenerator(settings, document, generator, docGenerator),
            _ => new RefitInterfaceGenerator(settings, document, generator, docGenerator),
        };
    }

    private string SanitizeGeneratedContracts(string contracts)
    {
        contracts = NormalizeSwagger2OptionalReferencePropertyNullability(contracts);

        if (settings.CodeGeneratorSettings is not { InlineJsonConverters: false })
        {
            // InlineJsonConverters = true (default): move [JsonConverter] from enum properties to enum type declarations.
            // This allows users to override the converter via JsonSerializerOptions.Converters (e.g. to use
            // JsonStringEnumMemberConverter for enums with [EnumMember] values containing special characters).
            contracts = JsonStringEnumConverterAttributeRegex.Replace(contracts, string.Empty);
            var newLine = GetPreferredNewLine(contracts);
            return EnumDeclarationRegex
                .Replace(
                    contracts,
                    match =>
                        $"{match.Groups[1].Value}[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]{newLine}{match.Groups[1].Value}{match.Groups[2].Value}")
                .TrimEnd();
        }

        // InlineJsonConverters = false: remove all [JsonConverter(typeof(JsonStringEnumConverter))] attributes.
        return JsonStringEnumConverterAttributeRegex
            .Replace(contracts, string.Empty)
            .TrimEnd();
    }

    private static string GetPreferredNewLine(string content) =>
        content.Contains("\r\n", StringComparison.Ordinal)
            ? "\r\n"
            : "\n";

    private string NormalizeSwagger2OptionalReferencePropertyNullability(string contracts)
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

    private string GenerateJsonSerializerContext(string contracts) =>
        settings.GenerateJsonSerializerContext && settings.GenerateContracts
            ? JsonSerializerContextGenerator.Generate(contracts, settings, document.Info?.Title)
            : string.Empty;

    /// <summary>
    /// Generates the client code based on the specified interface generator.
    /// </summary>
    /// <param name="interfaceGenerator">The interface generator used to generate the client code.</param>
    /// <returns>The generated client code as a string.</returns>
    private IReadOnlyCollection<GeneratedCode> GenerateClient(IRefitInterfaceGenerator interfaceGenerator)
    {
        var code = new StringBuilder();
        GenerateAutoGeneratedHeader(code);

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

        var refitInterfaces = interfaceGenerator.GenerateCode();
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

    /// <summary>
    /// Generates the auto-generated header if the setting is enabled.
    /// </summary>
    /// <param name="code">The string builder to append the header to.</param>
    private void GenerateAutoGeneratedHeader(StringBuilder code)
    {
        if (!settings.AddAutoGeneratedHeader)
            return;

        code.AppendLine("""
            // <auto-generated>
            //     This code was generated by Refitter.
            // </auto-generated>

            """);
    }
}
