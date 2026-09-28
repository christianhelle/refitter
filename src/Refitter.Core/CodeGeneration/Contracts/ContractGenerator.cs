namespace Refitter.Core;

/// <summary>
/// Generates the contract types (classes, records and enums) of a document into a single C# file.
/// </summary>
/// <remarks>
/// Types are named and generated in the order they are discovered: first the schemas of the document, then
/// the inline schemas of the operation parameters, then the types the generated types refer to.
/// See THIRD-PARTY-NOTICES.md.
/// </remarks>
internal sealed class ContractGenerator
{
    public const string BinaryResponseTypeName = "FileResponse";

    private const string JsonInheritanceConverterName = "JsonInheritanceConverter";
    private const string DateFormatConverterName = "DateFormatConverter";

    private readonly ApiDocument document;
    private readonly ContractTemplateRenderer renderer;

    public ContractGenerator(ApiDocument document, ContractGeneratorSettings settings)
    {
        this.document = document;
        Settings = settings;
        settings.SchemaType = document.SchemaType;
        renderer = new ContractTemplateRenderer(settings);

        var exceptionSchema = document.Definitions.TryGetValue("Exception", out var schema) ? schema : null;
        Resolver = new ContractTypeResolver(settings, exceptionSchema);
        Resolver.RegisterSchemaDefinitions(
            document.Definitions
                .Where(p => p.Value != exceptionSchema)
                .ToDictionary(p => p.Key, p => p.Value));
    }

    public ContractGeneratorSettings Settings { get; }

    public ContractTypeResolver Resolver { get; }

    public IEnumerable<string> GeneratedTypeNames => Resolver.Types.Select(t => t.Value);

    public bool HasGeneratedType(string typeName) => Resolver.Types.Any(t => t.Value == typeName);

    public string? GetTypeName(ApiSchema schema) => Resolver.TryGetTypeName(schema);

    /// <summary>The C# type of a parameter or response schema.</summary>
    public string GetTypeName(ApiSchema? schema, bool isNullable, string? typeNameHint)
    {
        if (schema == null)
            return "void";

        if (schema.ActualTypeSchema.IsBinary)
            return BinaryResponseTypeName;

        return Resolver.Resolve(schema.ActualSchema, isNullable, typeNameHint)
            .Replace(Settings.ArrayType + "<", Settings.ResponseArrayType + "<")
            .Replace(Settings.DictionaryType + "<", Settings.ResponseDictionaryType + "<");
    }

    public OperationModel CreateOperationModel(ApiOperation operation) => new(operation, this);

    /// <summary>Generates the file with all contract types.</summary>
    /// <param name="operationNameGenerator">Groups the operations into clients.</param>
    public string GenerateFile(IApiOperationNameGenerator operationNameGenerator)
    {
        // Contract types are named in the order they are discovered. The operations come first: their parameter
        // types, then (per client, like the client code that used to be rendered alongside) their response types.
        PrepareOperations(operationNameGenerator);

        var classes = Concatenate(GenerateTypes());
        var model = new ContractFileTemplateModel(classes, document, Settings);
        return renderer.Render("File", model)
            .Replace("\r", string.Empty)
            .Replace("\n\n\n\n", "\n\n")
            .Replace("\n\n\n", "\n\n");
    }

    private void PrepareOperations(IApiOperationNameGenerator operationNameGenerator)
    {
        document.GenerateOperationIds();
        var operations = new List<OperationModel>();
        foreach (var description in document.GetOperations())
        {
            var path = description.Path.TrimStart('/');
            var model = CreateOperationModel(description.Operation);
            model.OperationName = GetOperationName(operationNameGenerator, path, description);
            model.ControllerName = operationNameGenerator.GetClientName(document, path, description.Method, description.Operation);
            model.Path = path;
            model.HttpMethod = description.Method;
            operations.Add(model);
        }

        var groups = operationNameGenerator.SupportsMultipleClients
            ? operations.GroupBy(o => o.ControllerName).Select(g => (Name: g.Key ?? string.Empty, Operations: g.ToList()))
            : [(Name: string.Empty, Operations: operations)];

        foreach (var group in groups)
        {
            var className = GenerateControllerName(group.Name);
            var model = new ClientTemplateModel(className, group.Operations, document, Settings);
            if (model.HasOperations)
            {
                renderer.Render("Client.Class", model);
            }
        }
    }

    private string GetOperationName(
        IApiOperationNameGenerator operationNameGenerator,
        string path,
        ApiOperationDescription description)
    {
        var operationName = operationNameGenerator
            .GetOperationName(document, path, description.Method, description.Operation)
            .Replace('.', '_');
        return operationName.EndsWith("Async", StringComparison.Ordinal)
            ? operationName.Substring(0, operationName.Length - "Async".Length)
            : operationName;
    }

    private static string GenerateControllerName(string controllerName)
    {
        controllerName = controllerName.Replace('.', '_').Replace('-', '_');
        return ConversionUtilities.ConvertToUpperCamelCase(controllerName, firstCharacterMustBeAlpha: false) + "Client";
    }

    private IEnumerable<ContractArtifact> GenerateTypes()
    {
        var result = GenerateSchemaTypes();
        var usesInheritanceConverter = false;
        var usesDateFormatConverter = false;
        foreach (var artifact in result)
        {
            usesInheritanceConverter |= artifact.Code.Contains(JsonInheritanceConverterName);
            usesDateFormatConverter |= artifact.Code.Contains(DateFormatConverterName);
        }

        var utilities = new List<ContractArtifact>();
        if (usesInheritanceConverter)
        {
            if (!Settings.ExcludedTypeNames.Contains("JsonInheritanceAttribute"))
            {
                utilities.Add(new ContractArtifact(
                    "JsonInheritanceAttribute",
                    renderer.Render("JsonInheritanceAttribute", new JsonInheritanceConverterTemplateModel(Settings))));
            }

            if (!Settings.ExcludedTypeNames.Contains(JsonInheritanceConverterName))
            {
                utilities.Add(new ContractArtifact(
                    JsonInheritanceConverterName,
                    renderer.Render(JsonInheritanceConverterName, new JsonInheritanceConverterTemplateModel(Settings))));
            }
        }

        if (usesDateFormatConverter && !Settings.ExcludedTypeNames.Contains(DateFormatConverterName))
        {
            utilities.Add(new ContractArtifact(
                DateFormatConverterName,
                renderer.Render(DateFormatConverterName, new DateFormatConverterTemplateModel(Settings))));
        }

        return result.Concat(utilities);
    }

    /// <summary>Generates the types of the schemas, including the ones the generated types refer to.</summary>
    private List<ContractArtifact> GenerateSchemaTypes()
    {
        var processedTypes = new HashSet<string>(StringComparer.Ordinal);
        var artifacts = new Dictionary<string, ContractArtifact>(StringComparer.Ordinal);
        var artifactOrder = new List<string>();

        var pending = GetTypesRequiringGeneration(processedTypes);
        while (pending.Count > 0)
        {
            foreach (var type in pending)
            {
                processedTypes.Add(type.Value);
                var artifact = GenerateType(type.Key, type.Value);
                if (!Settings.ExcludedTypeNames.Contains(artifact.TypeName))
                {
                    if (!artifacts.ContainsKey(artifact.TypeName))
                        artifactOrder.Add(artifact.TypeName);

                    artifacts[artifact.TypeName] = artifact;
                }
            }

            pending = GetTypesRequiringGeneration(processedTypes);
        }

        return artifactOrder.Select(name => artifacts[name]).ToList();
    }

    private List<KeyValuePair<ApiSchema, string>> GetTypesRequiringGeneration(HashSet<string> processedTypes) =>
        Resolver.Types.Where(t => !processedTypes.Contains(t.Value)).ToList();

    private ContractArtifact GenerateType(ApiSchema schema, string typeNameHint)
    {
        var typeName = Resolver.GetOrGenerateTypeName(schema, typeNameHint);
        if (schema.IsEnumeration)
        {
            var enumModel = new EnumTemplateModel(typeName, schema, Settings);
            return new ContractArtifact(typeName, renderer.Render("Enum", enumModel));
        }

        var classModel = new ClassTemplateModel(typeName, Settings, Resolver, schema, document);
        RenamePropertyWithSameNameAsClass(typeName, classModel.PropertiesList);
        return new ContractArtifact(typeName, renderer.Render("Class", classModel));
    }

    private static void RenamePropertyWithSameNameAsClass(string typeName, List<PropertyModel> properties)
    {
        var property = properties.FirstOrDefault(p => p.PropertyName == typeName);
        if (property == null)
            return;

        var number = 1;
        var candidate = typeName + number;
        while (properties.Exists(p => p.PropertyName == candidate))
        {
            number++;
            candidate = typeName + number;
        }

        property.PropertyName += number;
    }

    private static string Concatenate(IEnumerable<ContractArtifact> artifacts)
    {
        var code = string.Join("\n\n", artifacts.Select(a => a.Code));
        return ConversionUtilities.TrimWhiteSpaces(code);
    }

    private sealed record ContractArtifact(string TypeName, string Code);
}
