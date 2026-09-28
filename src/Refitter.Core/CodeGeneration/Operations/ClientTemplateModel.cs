using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core;

/// <summary>
/// The model of the client class template. The client class itself is not part of the output, but rendering it
/// resolves the response types of the operations in the order the contract type names have always depended on.
/// See <see cref="ContractGenerator"/> and THIRD-PARTY-NOTICES.md.
/// </summary>
[SuppressMessage(
    "Minor Code Smell",
    "S2325:Methods and properties that don't access instance data should be static",
    Justification = "The Liquid templates, including custom templates, can only read instance members")]
internal sealed class ClientTemplateModel(
    string controllerClassName,
    IEnumerable<OperationModel> operations,
    ApiDocument document,
    ContractGeneratorSettings settings)
{
    public string Class { get; } = controllerClassName;

    public bool HasBaseClass => false;

    public bool HasConfigurationClass => false;

    public bool HasBaseType => false;

    public bool InjectHttpClient => true;

    public bool DisposeHttpClient => true;

    public string HttpClientType => "System.Net.Http.HttpClient";

    public bool UseHttpRequestMessageCreationMethod => false;

    public bool GenerateClientInterfaces => false;

    public bool HasBaseUrl => !string.IsNullOrEmpty(BaseUrl);

    public string BaseUrl => string.Empty;

    public bool HasOperations => Operations.Any();

    public string ExceptionClass => "ApiException";

    public bool GenerateOptionalParameters => false;

    public bool UseBaseUrl => true;

    public bool GenerateBaseUrlProperty => true;

    public bool GenerateSyncMethods => false;

    public string ClientClassAccessModifier => "public";

    public IEnumerable<OperationModel> Operations { get; } = operations;

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

    public string? Description => document.Info?.Description;

    public bool GenerateNullableReferenceTypes => settings.GenerateNullableReferenceTypes;

    public string ResponseClass => "SwaggerResponse";

    public bool UseSystemTextJson => true;

    public string JsonSerializerSettingsType => "System.Text.Json.JsonSerializerOptions";
}
