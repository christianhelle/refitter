using System.Text.Json.Serialization;

namespace Refitter.Core.Settings;

public sealed record OutputConfigSlice(
    [property: JsonPropertyName("namespace")] string Namespace = "GeneratedCode",
    [property: JsonPropertyName("contractsNamespace")] string? ContractsNamespace = null,
    [property: JsonPropertyName("outputFolder")] string OutputFolder = "./Generated",
    [property: JsonPropertyName("contractsOutputFolder")] string? ContractsOutputFolder = null,
    [property: JsonPropertyName("outputFilename")] string? OutputFilename = null,
    [property: JsonPropertyName("generateMultipleFiles")] bool GenerateMultipleFiles = false);
