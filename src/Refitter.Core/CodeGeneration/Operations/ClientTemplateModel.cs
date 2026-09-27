namespace Refitter.Core;

/// <summary>
/// The model of the client class template. The client class itself is not part of the output, but rendering it
/// resolves the response types of the operations in the order the contract type names have always depended on.
/// See <see cref="ContractGenerator"/> and THIRD-PARTY-NOTICES.md.
/// </summary>
internal sealed class ClientTemplateModel(
    string controllerClassName,
    IEnumerable<OperationModel> operations,
    ApiDocument document,
    ContractGeneratorSettings settings)
{
    public string Class { get; } = controllerClassName;

    public bool HasBaseClass => false;

    public string? BaseClass => null;

    public bool HasConfigurationClass => false;

    public string? ConfigurationClass => null;

    public bool HasBaseType => false;

    public bool InjectHttpClient => true;

    public bool DisposeHttpClient => true;

    public bool UseHttpClientCreationMethod => false;

    public string HttpClientType => "System.Net.Http.HttpClient";

    public bool UseHttpRequestMessageCreationMethod => false;

    public bool GenerateClientInterfaces => false;

    public bool SuppressClientInterfacesOutput => false;

    public bool SuppressClientClassesOutput => false;

    public string? ClientBaseInterface => null;

    public bool HasClientBaseInterface => false;

    public bool HasBaseUrl => !string.IsNullOrEmpty(BaseUrl);

    public string BaseUrl => string.Empty;

    public bool HasOperations => Operations.Any();

    public string ExceptionClass => "ApiException";

    public bool GenerateOptionalParameters => false;

    public bool UseBaseUrl => true;

    public bool GenerateBaseUrlProperty => true;

    public bool GenerateSyncMethods => false;

    public string ClientClassAccessModifier => "public";

    public string ClientInterfaceAccessModifier => "public";

    public IEnumerable<OperationModel> Operations { get; } = operations;

    public IEnumerable<OperationModel> InterfaceOperations => Operations;

    public bool WrapDtoExceptions => true;

    public string ParameterDateTimeFormat => "s";

    public string ParameterDateFormat => "yyyy-MM-dd";

    public bool ExposeJsonSerializerSettings => false;

    public bool GenerateUpdateJsonSerializerSettingsMethod => true;

    public bool UseRequestAndResponseSerializationSettings => false;

    public bool SerializeTypeInformation => false;

    public string QueryNullValue => string.Empty;

    public bool GeneratePrepareRequestAndProcessResponseAsAsyncMethods => false;

    public string JsonSerializerParameterCode => "new System.Text.Json.JsonSerializerOptions()";

    public string JsonConvertersArrayCode =>
        settings.JsonConverters is { Length: > 0 } converters
            ? "new System.Text.Json.Serialization.JsonConverter[] { " + string.Join(", ", converters.Select(c => "new " + c + "()")) + " }"
            : string.Empty;

    public string? Title => document.Info?.Title;

    public string? Description => document.Info?.Description;

    public string? Version => document.Info?.Version;

    public bool GenerateNullableReferenceTypes => settings.GenerateNullableReferenceTypes;

    public bool WrapResponses => false;

    public string ResponseClass => "SwaggerResponse";

    public bool UseSystemTextJson => true;

    public string JsonSerializerSettingsType => "System.Text.Json.JsonSerializerOptions";
}
