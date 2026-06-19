using System.Text.RegularExpressions;
using NSwag;

namespace Refitter.Core;

internal sealed class EnumStringConverterInjector : IContractsPostProcessor
{
    private static readonly Regex JsonStringEnumConverterAttributeRegex = new(
        @"^\s*\[(System\.Text\.Json\.Serialization\.)?JsonConverter\(typeof\((System\.Text\.Json\.Serialization\.)?JsonStringEnumConverter(?:<[\w.]+>)?\)\)\]\s*\r?\n?",
        RegexOptions.Compiled | RegexOptions.Multiline,
        TimeSpan.FromSeconds(1));

    private static readonly Regex EnumDeclarationRegex = new(
        @"^(\s*)((?:public|internal)\s+(?:partial\s+)?enum\s+\w+\b)",
        RegexOptions.Compiled | RegexOptions.Multiline,
        TimeSpan.FromSeconds(1));

    public string Process(OpenApiDocument document, RefitGeneratorSettings settings, string contracts)
    {
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
}
