using System.Text;
using System.Text.RegularExpressions;
using NSwag;

namespace Refitter.Core;

/// <summary>
/// Generates Refit clients and interfaces based on an OpenAPI specification.
/// </summary>
public class RefitGenerator(RefitGeneratorSettings settings, OpenApiDocument document)
{
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
        if (settings.OpenApiPaths.Length > 0)
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
        var regexes = pathMatchExpressions.Select(x => new Regex(x, RegexOptions.Compiled, TimeSpan.FromSeconds(1))).ToList();
        var paths = document.Paths.Keys
            .Where(pathKey => regexes.TrueForAll(regex => !regex.IsMatch(pathKey)))
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

        IRefitInterfaceGenerator interfaceGenerator = settings.MultipleInterfaces switch
        {
            MultipleInterfaces.ByEndpoint => new RefitMultipleInterfaceGenerator(settings, document, generator, docGenerator),
            MultipleInterfaces.ByTag => new RefitMultipleInterfaceByTagGenerator(settings, document, generator, docGenerator),
            _ => new RefitInterfaceGenerator(settings, document, generator, docGenerator),
        };

        var refitInterfaces = GenerateClient(interfaceGenerator);
        var interfaceNames = refitInterfaces.Select(c => c.TypeName).ToArray();
        var refitInterfacesCode = string.Join("", refitInterfaces.Select(c => c.Content));
        var title = settings.Naming.UseOpenApiTitle && !string.IsNullOrWhiteSpace(document.Info?.Title)
            ? document.Info!.Title.Sanitize()
            : settings.Naming.InterfaceName;
        return new StringBuilder()
            .AppendLine(settings.GenerateClients ? refitInterfacesCode : string.Empty)
            .AppendLine()
            .AppendLine(settings.GenerateContracts ? contracts : string.Empty)
            .AppendLine(
                settings.ApizrSettings != null
                    ? ApizrRegistrationGenerator.Generate(settings, interfaceNames, title)
                    : DependencyInjectionGenerator.Generate(settings, interfaceNames))
            .ToString()
            .TrimEnd();
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
        var contracts = generator.GenerateFile();
        contracts = SanitizeGeneratedContracts(contracts);

        IRefitInterfaceGenerator interfaceGenerator = settings.MultipleInterfaces switch
        {
            MultipleInterfaces.ByEndpoint
                => new RefitMultipleInterfaceGenerator(settings, document, generator, docGenerator),
            MultipleInterfaces.ByTag
                => new RefitMultipleInterfaceByTagGenerator(settings, document, generator, docGenerator),
            _ => new RefitInterfaceGenerator(settings, document, generator, docGenerator),
        };

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

        return new GeneratorOutput(generatedFiles);
    }

    private string SanitizeGeneratedContracts(string contracts)
    {
        if (settings.CodeGeneratorSettings is not { InlineJsonConverters: false })
        {
            return contracts;
        }

        const string pattern = @"^\s*\[(System\.Text\.Json\.Serialization\.)?JsonConverter\(typeof\((System\.Text\.Json\.Serialization\.)?JsonStringEnumConverter\)\)\]\s*$";
        var lines = contracts.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var filteredLines = lines
            .Where(
                line => !Regex.IsMatch(
                    line,
                    pattern,
                    RegexOptions.None,
                    TimeSpan.FromSeconds(1)))
            .ToArray();
        return string.Join(Environment.NewLine, filteredLines);
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
