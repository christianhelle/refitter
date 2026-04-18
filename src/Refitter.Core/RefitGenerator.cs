using System.Text;
using System.Text.RegularExpressions;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;

namespace Refitter.Core;

/// <summary>
/// Generates Refit clients and interfaces based on an OpenAPI specification.
/// </summary>
public class RefitGenerator(RefitGeneratorSettings settings, OpenApiDocument document)
{
    private static readonly Regex JsonStringEnumConverterAttributeRegex = new(
        @"^\s*\[(System\.Text\.Json\.Serialization\.)?JsonConverter\(typeof\((System\.Text\.Json\.Serialization\.)?JsonStringEnumConverter(?:<[^)]*>)?\)\)\]\s*\r?\n?",
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
        var openApiDocument = await GetOpenApiDocument(settings);

        ProcessTagFilters(openApiDocument, settings.IncludeTags);
        ProcessPathFilters(openApiDocument, settings.IncludePathMatches);
        ProcessContractFilter(openApiDocument, settings.TrimUnusedSchema, settings.KeepSchemaPatterns, settings.IncludeInheritanceHierarchy);

        return new RefitGenerator(settings, openApiDocument);
    }

    private static async Task<OpenApiDocument> GetOpenApiDocument(RefitGeneratorSettings settings)
    {
        if (settings.OpenApiPaths is { Length: > 0 })
            return await OpenApiDocumentFactory.CreateAsync(settings.OpenApiPaths);
        return await OpenApiDocumentFactory.CreateAsync(settings.OpenApiPath);
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
        IRefitInterfaceGenerator interfaceGenerator = settings.MultipleInterfaces switch
        {
            MultipleInterfaces.ByEndpoint => new RefitMultipleInterfaceGenerator(settings, document, generator, docGenerator),
            MultipleInterfaces.ByTag => new RefitMultipleInterfaceByTagGenerator(settings, document, generator, docGenerator),
            _ => new RefitInterfaceGenerator(settings, document, generator, docGenerator),
        };

        var contracts = generator.GenerateFile();
        contracts = SanitizeGeneratedContracts(contracts);

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
        IRefitInterfaceGenerator interfaceGenerator = settings.MultipleInterfaces switch
        {
            MultipleInterfaces.ByEndpoint
                => new RefitMultipleInterfaceGenerator(settings, document, generator, docGenerator),
            MultipleInterfaces.ByTag
                => new RefitMultipleInterfaceByTagGenerator(settings, document, generator, docGenerator),
            _ => new RefitInterfaceGenerator(settings, document, generator, docGenerator),
        };

        var contracts = generator.GenerateFile();
        contracts = SanitizeGeneratedContracts(contracts);

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

    private string SanitizeGeneratedContracts(string contracts)
    {
        var isSystemTextJson = settings.CodeGeneratorSettings?.JsonLibrary is not CSharpJsonLibrary.NewtonsoftJson;

        if (settings.CodeGeneratorSettings is not { InlineJsonConverters: false })
        {
            if (!isSystemTextJson)
            {
                // Newtonsoft.Json: no STJ-specific attributes to inject or strip.
                return contracts.TrimEnd();
            }

            // InlineJsonConverters = true (default): move [JsonConverter] from enum properties to enum type declarations.
            // This allows users to override the converter via JsonSerializerOptions.Converters (e.g. to use
            // JsonStringEnumMemberConverter for enums with [EnumMember] values containing special characters).
            contracts = JsonStringEnumConverterAttributeRegex.Replace(contracts, string.Empty);
            return EnumDeclarationRegex
                .Replace(
                    contracts,
                    "$1[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]\n$1$2")
                .TrimEnd();
        }

        if (!isSystemTextJson)
        {
            // Newtonsoft.Json: no STJ-specific attributes to strip.
            return contracts.TrimEnd();
        }

        // InlineJsonConverters = false: remove all [JsonConverter(typeof(JsonStringEnumConverter))] attributes.
        return JsonStringEnumConverterAttributeRegex
            .Replace(contracts, string.Empty)
            .TrimEnd();
    }

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
