using System.Text;
using NSwag;

namespace Refitter.Core;

internal class InterfaceGenerator
{
    private const string Separator = "    ";

    private readonly RefitGeneratorSettings settings;
    private readonly OpenApiDocument document;
    private readonly CustomCSharpClientGenerator generator;
    private readonly XmlDocumentationGenerator docGenerator;
    private readonly IReturnTypeGenerator returnTypeGenerator;
    private readonly IMethodAttributeGenerator methodAttributeGenerator;
    private readonly IMethodSignatureGenerator methodSignatureGenerator;

    internal InterfaceGenerator(
        RefitGeneratorSettings settings,
        OpenApiDocument document,
        CustomCSharpClientGenerator generator,
        XmlDocumentationGenerator docGenerator,
        IParameterExtractor? parameterExtractor = null)
        : this(
            settings,
            document,
            generator,
            docGenerator,
            new ReturnTypeGenerator(settings, generator),
            new MethodAttributeGenerator(settings, document),
            new MethodSignatureGenerator(settings, parameterExtractor ?? new ParameterAggregator()))
    {
    }

    private InterfaceGenerator(
        RefitGeneratorSettings settings,
        OpenApiDocument document,
        CustomCSharpClientGenerator generator,
        XmlDocumentationGenerator docGenerator,
        IReturnTypeGenerator returnTypeGenerator,
        IMethodAttributeGenerator methodAttributeGenerator,
        IMethodSignatureGenerator methodSignatureGenerator)
    {
        this.settings = settings;
        this.document = document;
        this.generator = generator;
        this.docGenerator = docGenerator;
        this.returnTypeGenerator = returnTypeGenerator;
        this.methodAttributeGenerator = methodAttributeGenerator;
        this.methodSignatureGenerator = methodSignatureGenerator;
        generator.BaseSettings.OperationNameGenerator = new OperationNameGenerator(document, settings);
    }

    public IEnumerable<GeneratedCode> Generate(IInterfacePartitioning partitioning)
    {
        var operations = document.Paths
            .SelectMany(
                path => path.Value,
                (path, op)
                    => new OpenApiOperationInfo(path.Key, op.Key, op.Value))
            .ToList();

        var groups = operations
            .GroupBy(partitioning.GetGroupKey)
            .ToList();

        var knownInterfaceIdentifiers = new HashSet<string>();
        var title = settings.Naming.UseOpenApiTitle && !string.IsNullOrWhiteSpace(document.Info?.Title)
            ? document.Info!.Title.Sanitize()
            : settings.Naming.InterfaceName;

        if (partitioning.IsSingleInterface)
        {
            var singleGroup = groups.Count > 0 ? groups[0].ToList() : new();
            yield return GenerateSingleInterface(singleGroup, partitioning, title, knownInterfaceIdentifiers);
        }
        else
        {
            foreach (var group in groups)
            {
                var nonDeprecatedOperations = group
                    .Where(op => settings.GenerateDeprecatedOperations || !op.Operation.IsDeprecated)
                    .ToList();

                if (nonDeprecatedOperations.Count == 0)
                    continue;

                var generateMultipleInterface = GenerateMultipleInterface(
                    nonDeprecatedOperations,
                    partitioning,
                    title,
                    knownInterfaceIdentifiers);

                foreach (var generatedCode in generateMultipleInterface)
                {
                    yield return generatedCode;
                }
            }
        }
    }

    private GeneratedCode GenerateSingleInterface(
        List<OpenApiOperationInfo> operations,
        IInterfacePartitioning partitioning,
        string title,
        HashSet<string> knownInterfaceIdentifiers)
    {
        var baseOperationName = operations.Count > 0 ? GetBaseOperationName(operations[0]) : string.Empty;
        var rawInterfaceName = partitioning.GetInterfaceName(string.Empty, title, baseOperationName);
        var interfaceNameSuffix = partitioning.GetInterfaceNameSuffix();
        var interfaceName = IdentifierUtils.Counted(knownInterfaceIdentifiers, rawInterfaceName, interfaceNameSuffix);

        var code = new StringBuilder();
        var dynamicQuerystringParametersCodeBuilder = new StringBuilder();
        var representativeOperation = operations.Count > 0
            ? operations[0]
            : new(string.Empty, string.Empty, new());
        partitioning.AppendInterfaceDocumentation(
            document,
            docGenerator,
            string.Empty,
            representativeOperation,
            code);

        var interfaceDeclaration = GenerateInterfaceDeclaration(interfaceName, partitioning.IsSingleInterface);
        code.AppendLine(interfaceDeclaration);
        code.AppendLine($"{Separator}{{");

        var knownMethodIdentifiers = new HashSet<string>();

        foreach (var op in operations)
        {
            var (dynamicQuerystringParams, _) = GenerateMethod(
                op,
                interfaceName,
                partitioning,
                knownMethodIdentifiers,
                code);
            if (!string.IsNullOrWhiteSpace(dynamicQuerystringParams))
            {
                dynamicQuerystringParametersCodeBuilder.AppendLine(dynamicQuerystringParams);
            }
        }

        code.AppendLine($"{Separator}}}");
        code.AppendLine(dynamicQuerystringParametersCodeBuilder.ToString());

        return new(interfaceName, code.ToString());
    }

    private IEnumerable<GeneratedCode> GenerateMultipleInterface(
        List<OpenApiOperationInfo> operations,
        IInterfacePartitioning partitioning,
        string title,
        HashSet<string> knownInterfaceIdentifiers)
    {
        var representativeOperation = operations[0];
        var baseOperationName = GetBaseOperationName(representativeOperation);
        var groupKey = partitioning.GetGroupKey(representativeOperation);
        var rawInterfaceName = partitioning.GetInterfaceName(groupKey, title, baseOperationName);
        var interfaceNameSuffix = partitioning.GetInterfaceNameSuffix();
        var interfaceName = IdentifierUtils.Counted(knownInterfaceIdentifiers, rawInterfaceName, interfaceNameSuffix);

        var code = new StringBuilder();
        partitioning.AppendInterfaceDocumentation(document, docGenerator, groupKey, representativeOperation, code);
        var interfaceDeclaration = GenerateInterfaceDeclaration(interfaceName, partitioning.IsSingleInterface);
        code.AppendLine(interfaceDeclaration);
        code.AppendLine($"{Separator}{{");

        var knownMethodIdentifiers = new HashSet<string>();

        foreach (var op in operations)
        {
            var (dynamicQuerystringParams, dynamicQuerystringType) = GenerateMethod(
                op,
                interfaceName,
                partitioning,
                knownMethodIdentifiers,
                code);
            if (!string.IsNullOrWhiteSpace(dynamicQuerystringParams))
            {
                yield return new GeneratedCode(dynamicQuerystringType, dynamicQuerystringParams);
            }
        }

        code.AppendLine($"{Separator}}}");

        yield return new(interfaceName, code.ToString());
    }

    private (string DynamicQuerystringParameters, string DynamicQuerystringParameterType) GenerateMethod(
        OpenApiOperationInfo op,
        string interfaceName,
        IInterfacePartitioning partitioning,
        HashSet<string> knownMethodIdentifiers,
        StringBuilder code)
    {
        var operation = op.Operation;

        if (!settings.GenerateDeprecatedOperations && operation.IsDeprecated)
        {
            return (string.Empty, string.Empty);
        }

        var returnType = returnTypeGenerator.Generate(operation);
        var verb = op.Verb.CapitalizeFirstCharacter();
        var baseOperationName = GetBaseOperationName(op);
        var rawMethodName = partitioning.GetMethodName(op, interfaceName, baseOperationName);
        var methodName = IdentifierUtils.Counted(knownMethodIdentifiers, rawMethodName);
        knownMethodIdentifiers.Add(methodName);

        var dynamicQuerystringParameterType =
            partitioning.GetDynamicQuerystringParameterType(interfaceName, methodName);
        var operationModel = generator.CreateOperationModel(operation);
        operationModel.Path = op.Path;

        var (parametersString, parameters, operationDynamicQuerystringParameters) =
            methodSignatureGenerator.Generate(operationModel, operation, dynamicQuerystringParameterType);

        var hasDynamicQuerystringParameter = !string.IsNullOrWhiteSpace(operationDynamicQuerystringParameters);
        var hasApizrRequestOptionsParameter = settings.ApizrSettings?.WithRequestOptions == true;
        var hasCancellationToken = settings.UseCancellationTokens && !hasApizrRequestOptionsParameter;
        var isApiResponseType = returnTypeGenerator.IsApiResponseType(returnType);

        if (settings.GenerateXmlDocCodeComments)
        {
            docGenerator.AppendMethodDocumentation(
                operationModel,
                isApiResponseType,
                hasDynamicQuerystringParameter,
                hasApizrRequestOptionsParameter,
                hasCancellationToken,
                code);
        }

        foreach (var attribute in methodAttributeGenerator.Generate(operation, operationModel))
        {
            code.AppendLine($"{Separator}{Separator}{attribute}");
        }

        code.AppendLine($"{Separator}{Separator}[{verb}(\"{op.Path}\")]")
            .AppendLine($"{Separator}{Separator}{returnType} {methodName}({parametersString});")
            .AppendLine();

        if (parametersString.Contains("?") && settings is { OptionalParameters: true, ApizrSettings: not null })
        {
            if (settings.GenerateXmlDocCodeComments)
            {
                docGenerator.AppendMethodDocumentation(
                    operationModel,
                    isApiResponseType,
                    false,
                    hasApizrRequestOptionsParameter,
                    hasCancellationToken,
                    code);
            }

            foreach (var attribute in methodAttributeGenerator.Generate(operation, operationModel))
            {
                code.AppendLine($"{Separator}{Separator}{attribute}");
            }

            var nonOptionalParametersString = string.Join(
                ", ",
                parameters.Where(parameter => !parameter.Contains("?")));

            code.AppendLine($"{Separator}{Separator}[{verb}(\"{op.Path}\")]")
                .AppendLine($"{Separator}{Separator}{returnType} {methodName}({nonOptionalParametersString});")
                .AppendLine();
        }

        return (operationDynamicQuerystringParameters ?? string.Empty, dynamicQuerystringParameterType);
    }

    private string GetBaseOperationName(OpenApiOperationInfo op)
    {
        return generator
            .BaseSettings
            .OperationNameGenerator
            .GetOperationName(document, op.Path, op.Verb, op.Operation);
    }

    private string GenerateInterfaceDeclaration(string interfaceName, bool isSingleInterface)
    {
        var inheritance = isSingleInterface && settings.GenerateDisposableClients
            ? " : IDisposable"
            : null;

        var modifier = settings.TypeAccessibility.ToString().ToLowerInvariant();
        return $"""
                {Separator}{GetGeneratedCodeAttribute()}
                {Separator}{modifier} partial interface {interfaceName}{inheritance}
                """;
    }

    private string GetGeneratedCodeAttribute() =>
        $"""
         [System.CodeDom.Compiler.GeneratedCode("Refitter", "{GetType().Assembly.GetName().Version}")]
         """;
}
