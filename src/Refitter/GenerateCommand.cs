using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi;
using Refitter.Core;
using Refitter.Validation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Refitter;

[ExcludeFromCodeCoverage]
public sealed class GenerateCommand : AsyncCommand<Settings>
{
    private const string AsciiArt =
"""
  ██████╗ ███████╗███████╗██╗████████╗████████╗███████╗██████╗
  ██╔══██╗██╔════╝██╔════╝██║╚══██╔══╝╚══██╔══╝██╔════╝██╔══██╗
  ██████╔╝█████╗  █████╗  ██║   ██║      ██║   █████╗  ██████╔╝
  ██╔══██╗██╔══╝  ██╔══╝  ██║   ██║      ██║   ██╔══╝  ██╔══██╗
  ██║  ██║███████╗██║     ██║   ██║      ██║   ███████╗██║  ██║
  ╚═╝  ╚═╝╚══════╝╚═╝     ╚═╝   ╚═╝      ╚═╝   ╚══════╝╚═╝  ╚═╝
""";

    private static readonly string Crlf = Environment.NewLine;

    protected override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (!settings.NoLogging)
            Analytics.Configure();

        if (context.Arguments.Any(a => a.Equals("--version", StringComparison.OrdinalIgnoreCase)) ||
            context.Arguments.Any(a => a.Equals("-v", StringComparison.OrdinalIgnoreCase)))
            return ValidationResult.Success();

        return SettingsValidator.Validate(settings);
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        RefitGeneratorSettings refitGeneratorSettings;

        try
        {
            // When settings file is provided, deserialize it first and use as source of truth
            if (!string.IsNullOrWhiteSpace(settings.SettingsFilePath))
            {
                var json = await File.ReadAllTextAsync(settings.SettingsFilePath);
                refitGeneratorSettings = Serializer.Deserialize<RefitGeneratorSettings>(json);

                // Allow CLI to override OpenApiPath if explicitly provided
                if (!string.IsNullOrWhiteSpace(settings.OpenApiPath))
                    refitGeneratorSettings.OpenApiPath = settings.OpenApiPath;

                ApplySettingsFileDefaults(settings.SettingsFilePath, refitGeneratorSettings);
            }
            else
            {
                // No settings file - build from CLI arguments
                refitGeneratorSettings = CreateRefitGeneratorSettings(settings);
            }
            var stopwatch = Stopwatch.StartNew();
            var version = GetType().Assembly.GetName().Version!.ToString();
            if (version == "1.0.0.0")
                version += " (local build)";

            // Header with branding
            if (settings.SimpleOutput)
            {
                Console.WriteLine();
                Console.WriteLine($"Refitter v{version}");
                Console.WriteLine("OpenAPI to Refit Interface Generator");
                Console.WriteLine();
            }
            else
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold cyan]{AsciiArt}[/]");
                AnsiConsole.MarkupLine($"[bold cyan]╔═══════════════════════════════════════════════════════════════╗[/]");
                AnsiConsole.MarkupLine($"[bold cyan]║[/] [bold white]🚀 Refitter v{version,-48}[/] [bold cyan]║[/]");
                AnsiConsole.MarkupLine($"[bold cyan]║[/] [dim]   OpenAPI to Refit Interface Generator[/]{new string(' ', 22)} [bold cyan]║[/]");
                AnsiConsole.MarkupLine($"[bold cyan]╚═══════════════════════════════════════════════════════════════╝[/]");
                AnsiConsole.WriteLine();
            }

            // Support information
            var supportKey = settings.NoLogging
                ? "Unavailable when logging is disabled"
                : SupportInformation.GetSupportKey();

            if (settings.SimpleOutput)
            {
                Console.WriteLine($"Support key: {supportKey}");
                Console.WriteLine();
            }
            else
            {
                AnsiConsole.MarkupLine($"[dim]🔑 Support key: {supportKey}[/]");
                AnsiConsole.WriteLine();
            }

            if (context.Arguments.Any(a => a.Equals("--version", StringComparison.OrdinalIgnoreCase)) ||
                context.Arguments.Any(a => a.Equals("-v", StringComparison.OrdinalIgnoreCase)))
            {
                return 0;
            }

            var generator = await RefitGenerator.CreateAsync(refitGeneratorSettings);
            if (!settings.SkipValidation)
                await ValidateOpenApiSpec(refitGeneratorSettings.OpenApiPath, settings);

            await (refitGeneratorSettings.GenerateMultipleFiles
                ? WriteMultipleFiles(generator, settings, refitGeneratorSettings)
                : WriteSingleFile(generator, settings, refitGeneratorSettings));

            Analytics.LogFeatureUsage(settings, refitGeneratorSettings);

            // Generate .refitter settings file if not using existing settings file
            if (string.IsNullOrWhiteSpace(settings.SettingsFilePath))
            {
                await WriteRefitterSettingsFile(settings, refitGeneratorSettings);
            }

            if (refitGeneratorSettings.IncludePathMatches.Length > 0 &&
                generator.OpenApiDocument.Paths.Count == 0)
            {
                Console.WriteLine("⚠️ WARNING: All API paths were filtered out by --match-path patterns. ⚠️");
                Console.WriteLine($"   Match Patterns used: {string.Join(", ", refitGeneratorSettings.IncludePathMatches)}");
                Console.WriteLine();
                Console.WriteLine("   This could indicate that:");
                Console.WriteLine("     1. The regex patterns don't match any available paths");
                Console.WriteLine("     2. There's a syntax error in the regex patterns");
                Console.WriteLine("     3. The patterns were corrupted by command line interpretation");
                Console.WriteLine();
                Console.WriteLine("   This commonly happens when using the Windows Command Prompt (CMD) instead of PowerShell.");
                Console.WriteLine("   The ^ character in regex patterns is interpreted as an escape character in CMD.");
                Console.WriteLine();
                Console.WriteLine("   Solutions:");
                Console.WriteLine("     1. Use PowerShell instead of CMD");
                Console.WriteLine("     2. In CMD, escape the ^ character or use different quoting");
                Console.WriteLine("     3. Use a .refitter settings file instead of command line arguments");
                Console.WriteLine();
            }

            // Success summary with performance metrics
            stopwatch.Stop();
            if (settings.SimpleOutput)
            {
                Console.WriteLine("Generation completed successfully!");
                Console.WriteLine($"Duration: {stopwatch.Elapsed:mm\\:ss\\.ffff}");
                Console.WriteLine($"Performance: {(refitGeneratorSettings.GenerateMultipleFiles ? "Multi-file" : "Single-file")} generation");
                Console.WriteLine();
            }
            else
            {
                var successPanel = new Panel(
                    $"[bold green]✅ Generation completed successfully![/]\n\n" +
                    $"[dim]📊 Duration:[/] [green]{stopwatch.Elapsed:mm\\:ss\\.ffff}[/]\n" +
                    $"[dim]🚀 Performance:[/] [green]{(refitGeneratorSettings.GenerateMultipleFiles ? "Multi-file" : "Single-file")} generation[/]"
                )
                .BorderColor(Color.Green)
                .RoundedBorder()
                .Header("[bold green]🎉 Success[/]")
                .HeaderAlignment(Justify.Center);

                AnsiConsole.Write(successPanel);
                AnsiConsole.WriteLine();
            }

            if (!settings.NoBanner)
            {
                if (settings.SimpleOutput)
                    SimpleDonationBanner();
                else
                    DonationBanner();
            }

            ShowWarnings(refitGeneratorSettings, settings);
            return 0;
        }
        catch (Exception exception)
        {
            // Error summary panel
            if (settings.SimpleOutput)
            {
                Console.WriteLine("Generation failed!");
                Console.WriteLine();
            }
            else
            {
                var errorPanel = new Panel("[bold red]❌ Generation failed![/]")
                    .BorderColor(Color.Red)
                    .RoundedBorder()
                    .Header("[bold red]🚨 Error[/]")
                    .HeaderAlignment(Justify.Center);

                AnsiConsole.Write(errorPanel);
                AnsiConsole.WriteLine();
            }

            if (exception is OpenApiUnsupportedSpecVersionException unsupportedSpecVersionException)
            {
                if (settings.SimpleOutput)
                {
                    Console.WriteLine($"Unsupported OpenAPI version: {unsupportedSpecVersionException.SpecificationVersion}");
                    Console.WriteLine();
                }
                else
                {
                    var versionPanel = new Panel(
                        $"[bold red]🚫 Unsupported OpenAPI version: {unsupportedSpecVersionException.SpecificationVersion}[/]"
                    )
                    .BorderColor(Color.Red)
                    .RoundedBorder();

                    AnsiConsole.Write(versionPanel);
                    AnsiConsole.WriteLine();
                }
            }

            if (exception is not OpenApiValidationException)
            {
                if (settings.SimpleOutput)
                {
                    Console.WriteLine("Exception Details:");
                    Console.WriteLine(exception.ToString());
                    Console.WriteLine();
                }
                else
                {
                    AnsiConsole.MarkupLine("[bold red]🐛 Exception Details:[/]");
                    AnsiConsole.WriteException(exception);
                    AnsiConsole.WriteLine();
                }
            }

            if (!settings.SkipValidation)
            {
                if (settings.SimpleOutput)
                {
                    Console.WriteLine("Suggestion");
                    Console.WriteLine("Try using the --skip-validation argument.");
                    Console.WriteLine();
                }
                else
                {
                    var suggestionPanel = new Panel(
                        "💡 Try using the --skip-validation argument."
                    )
                    .BorderColor(Color.Yellow)
                    .RoundedBorder()
                    .Header("Suggestion");

                    AnsiConsole.Write(suggestionPanel);
                    AnsiConsole.WriteLine();
                }
            }

            if (settings.SimpleOutput)
            {
                Console.WriteLine("Support");
                Console.WriteLine("Need Help?");
                Console.WriteLine();
                Console.WriteLine("Report an issue: https://github.com/christianhelle/refitter/issues");
                Console.WriteLine();
            }
            else
            {
                var helpPanel = new Panel(
                    "🆘 Need Help?\n\n" +
                    "🐛 Report an issue: https://github.com/christianhelle/refitter/issues"
                )
                .BorderColor(Color.Yellow)
                .RoundedBorder()
                .Header("Support")
                .HeaderAlignment(Justify.Center);

                AnsiConsole.Write(helpPanel);
                AnsiConsole.WriteLine();
            }

            await Analytics.LogError(exception, settings);
            return exception.HResult;
        }
    }

    private static RefitGeneratorSettings CreateRefitGeneratorSettings(Settings settings)
    {
        return new RefitGeneratorSettings
        {
            OpenApiPath = settings.OpenApiPath!,
            Namespace = settings.Namespace ?? "GeneratedCode",
            PropertyNamingPolicy = settings.PropertyNamingPolicy,
            AddAutoGeneratedHeader = !settings.NoAutoGeneratedHeader,
            AddAcceptHeaders = !settings.NoAcceptHeaders,
            GenerateContracts = !settings.InterfaceOnly,
            GenerateClients = !settings.ContractOnly,
            ReturnIApiResponse = settings.ReturnIApiResponse,
            ReturnIObservable = settings.ReturnIObservable,
            UseCancellationTokens = settings.UseCancellationTokens,
            GenerateOperationHeaders = !settings.NoOperationHeaders,
            IgnoredOperationHeaders = settings.IgnoredOperationHeaders ?? Array.Empty<string>(),
            UseIsoDateFormat = settings.UseIsoDateFormat,
            TypeAccessibility = settings.InternalTypeAccessibility
                ? TypeAccessibility.Internal
                : TypeAccessibility.Public,
            AdditionalNamespaces = settings.AdditionalNamespaces!,
            ExcludeNamespaces = settings.ExcludeNamespaces ?? Array.Empty<string>(),
            MultipleInterfaces = settings.MultipleInterfaces,
            IncludePathMatches = settings.MatchPaths ?? Array.Empty<string>(),
            IncludeTags = settings.Tags ?? Array.Empty<string>(),
            GenerateDeprecatedOperations = !settings.NoDeprecatedOperations,
            OperationNameTemplate = settings.OperationNameTemplate,
            OptionalParameters = settings.OptionalNullableParameters,
            TrimUnusedSchema = settings.TrimUnusedSchema,
            KeepSchemaPatterns = settings.KeepSchemaPatterns ?? Array.Empty<string>(),
            IncludeInheritanceHierarchy = settings.IncludeInheritanceHierarchy,
            OperationNameGenerator = settings.OperationNameGenerator,
            GenerateDefaultAdditionalProperties = !settings.SkipDefaultAdditionalProperties,
            ImmutableRecords = settings.ImmutableRecords,
            ApizrSettings = settings.UseApizr ? new ApizrSettings() : null,
            UseDynamicQuerystringParameters = settings.UseDynamicQuerystringParameters,
            GenerateMultipleFiles = settings.GenerateMultipleFiles || !string.IsNullOrWhiteSpace(settings.ContractsOutputPath),
            ContractsOutputFolder = settings.ContractsOutputPath ?? settings.OutputPath,
            ContractsNamespace = settings.ContractsNamespace,
            UsePolymorphicSerialization = settings.UsePolymorphicSerialization,
            GenerateDisposableClients = settings.GenerateDisposableClients,
            CollectionFormat = settings.CollectionFormat,
            GenerateXmlDocCodeComments = !settings.NoXmlDocCodeComments,
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                InlineJsonConverters = !settings.NoInlineJsonConverters,
                IntegerType = settings.IntegerType
            },
            CustomTemplateDirectory = settings.CustomTemplateDirectory,
            AuthenticationHeaderStyle = settings.GenerateAuthenticationHeader,
            SecurityScheme = settings.SecurityScheme,
            GenerateJsonSerializerContext = settings.GenerateJsonSerializerContext,
        };
    }
    private static async Task WriteSingleFile(
        RefitGenerator generator,
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings)
    {
        // Show progress while generating
        if (settings.SimpleOutput)
        {
            Console.WriteLine("Generating code...");
        }
        else
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("[yellow]🔧 Generating code...[/]", async _ =>
                {
                    await Task.Delay(100); // Brief delay to show spinner
                });
        }

        var code = generator.Generate().ReplaceLineEndings();
        var outputPath = GetOutputPath(settings, refitGeneratorSettings);

        var fileName = Path.GetFileName(outputPath);
        var directory = Path.GetDirectoryName(outputPath) ?? "";
        var sizeFormatted = FormatFileSize(code.Length);
        var lines = code.Split('\n').Length;

        if (settings.SimpleOutput)
        {
            Console.WriteLine("Generated Output");
            Console.WriteLine($"File: {fileName}");
            Console.WriteLine($"Directory: {directory}");
            Console.WriteLine($"Size: {sizeFormatted}");
            Console.WriteLine($"Lines: {lines:N0}");
            Console.WriteLine();
        }
        else
        {
            // Create a table for better formatting
            var table = new Table()
                .RoundedBorder()
                .BorderColor(Color.Yellow)
                .AddColumn(new TableColumn("[bold yellow]📁 Generated Output[/]").Centered());

            table.AddRow($"[bold white]📄 File:[/] [cyan]{fileName}[/]");
            table.AddRow($"[bold white]📂 Directory:[/] [dim]{directory}[/]");
            table.AddRow($"[bold white]📊 Size:[/] [green]{sizeFormatted}[/]");
            table.AddRow($"[bold white]📝 Lines:[/] [green]{lines:N0}[/]");

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory) && !Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        await File.WriteAllTextAsync(outputPath, code);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        double size = bytes;
        int suffixIndex = 0;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:F1} {suffixes[suffixIndex]}";
    }
    private async Task WriteMultipleFiles(
        RefitGenerator generator,
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings)
    {
        // Show progress while generating
        GeneratorOutput generatorOutput;
        if (settings.SimpleOutput)
        {
            Console.WriteLine("Generating multiple files...");
            generatorOutput = generator.GenerateMultipleFiles();
        }
        else
        {
            generatorOutput = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("green bold"))
                .StartAsync("[yellow]🔧 Generating multiple files...[/]", async _ =>
                {
                    await Task.Delay(100); // Brief delay to show spinner
                    return generator.GenerateMultipleFiles();
                });
        }

        var totalSize = 0L;
        var totalLines = 0;

        Table? table = null;

        if (settings.SimpleOutput)
        {
            Console.WriteLine("Generated Output Files");
            Console.WriteLine($"{"File",-30} {"Size",-10} {"Lines",-10}");
            Console.WriteLine(new string('-', 55));
        }
        else
        {
            // Create a table for better formatting
            table = new Table()
                .RoundedBorder()
                .BorderColor(Color.Yellow)
                .AddColumn(new TableColumn("[bold white]📄 File[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold white]📂 Directory[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold white]📊 Size[/]").RightAligned())
                .AddColumn(new TableColumn("[bold white]📝 Lines[/]").RightAligned());

            table.Title = new TableTitle("[bold yellow]📁 Generated Output Files[/]");
        }

        foreach (var outputFile in generatorOutput.Files)
        {
            if (
                !string.IsNullOrWhiteSpace(refitGeneratorSettings.ContractsOutputFolder)
                && refitGeneratorSettings.ContractsOutputFolder != RefitGeneratorSettings.DefaultOutputFolder
                && outputFile.Filename == $"{TypenameConstants.Contracts}.cs"
            )
            {
                var root = string.IsNullOrWhiteSpace(settings.SettingsFilePath)
                    ? string.Empty
                    : Path.GetDirectoryName(settings.SettingsFilePath) ?? string.Empty;

                var contractsFolder = Path.GetFullPath(Path.Combine(root, refitGeneratorSettings.ContractsOutputFolder));
                if (!string.IsNullOrWhiteSpace(contractsFolder) && !Directory.Exists(contractsFolder))
                    Directory.CreateDirectory(contractsFolder);

                var contractsFile = Path.Combine(contractsFolder ?? "./", outputFile.Filename);
                var sizeFormatted = FormatFileSize(outputFile.Content.Length);
                var contractsDir = Path.GetDirectoryName(contractsFile) ?? "";
                var lines = outputFile.Content.Split('\n').Length;

                if (settings.SimpleOutput)
                {
                    Console.WriteLine($"{outputFile.Filename,-30} {sizeFormatted,-10} {lines,-10:N0}");
                }
                else
                {
                    table?.AddRow(
                        $"[cyan]{outputFile.Filename}[/]",
                        $"[dim]{contractsDir}[/]",
                        $"[green]{sizeFormatted}[/]",
                        $"[green]{lines:N0}[/]"
                    );
                }

                totalSize += outputFile.Content.Length;
                totalLines += lines;

                await File.WriteAllTextAsync(contractsFile, outputFile.Content);
                continue;
            }

            var code = outputFile.Content;
            var outputPath = GetOutputPath(settings, refitGeneratorSettings, outputFile);
            var formattedSize = FormatFileSize(code.Length);
            var outputDirectory = Path.GetDirectoryName(outputPath) ?? "";
            var fileLines = code.Split('\n').Length;

            if (settings.SimpleOutput)
            {
                Console.WriteLine($"{outputFile.Filename,-30} {formattedSize,-10} {fileLines,-10:N0}");
            }
            else
            {
                table?.AddRow(
                    $"[cyan]{outputFile.Filename}[/]",
                    $"[dim]{outputDirectory}[/]",
                    $"[green]{formattedSize}[/]",
                    $"[green]{fileLines:N0}[/]"
                );
            }

            totalSize += code.Length;
            totalLines += fileLines;

            var fileDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(fileDirectory) && !Directory.Exists(fileDirectory))
                Directory.CreateDirectory(fileDirectory);

            await File.WriteAllTextAsync(outputPath, code);
        }

        if (settings.SimpleOutput)
        {
            Console.WriteLine(new string('-', 55));
            Console.WriteLine($"{"Total (" + generatorOutput.Files.Count + " files)",-30} {FormatFileSize(totalSize),-10} {totalLines,-10:N0}");
            Console.WriteLine();
        }
        else
        {
            // Add summary row
            table?.AddEmptyRow();
            table?.AddRow(
                $"[bold yellow]📊 Total ({generatorOutput.Files.Count} files)[/]",
                "[dim]---[/]",
                $"[bold green]{FormatFileSize(totalSize)}[/]",
                $"[bold green]{totalLines:N0}[/]"
            );

            if (table != null)
            {
                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
            }
        }
    }
    private static void ShowWarnings(RefitGeneratorSettings refitGeneratorSettings, Settings settings)
    {
        var warnings = new List<(string title, string description)>();

        if (refitGeneratorSettings.UseIsoDateFormat &&
            refitGeneratorSettings.CodeGeneratorSettings?.DateFormat is not null)
        {
            warnings.Add((
                "Date Format Override",
                "'codeGeneratorSettings.dateFormat' will be ignored due to 'useIsoDateFormat' set to true"
            ));
        }

#pragma warning disable CS0618 // Type or member is obsolete
        if (refitGeneratorSettings.DependencyInjectionSettings?.UsePolly is true)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            warnings.Add((
                "Deprecated Setting",
                "The 'usePolly' property is deprecated. Use 'transientErrorHandler: Polly' instead"
            ));
        }

        if (warnings.Any())
        {
            if (settings.SimpleOutput)
            {
                Console.WriteLine("Configuration Warnings");
                foreach (var (title, description) in warnings)
                {
                    Console.WriteLine($"Warning: {title}");
                    Console.WriteLine($"Description: {description}");
                    Console.WriteLine();
                }
            }
            else
            {
                var table = new Table()
                    .RoundedBorder()
                    .BorderColor(Color.Orange3)
                    .AddColumn(new TableColumn("[bold white]Warning[/]").LeftAligned())
                    .AddColumn(new TableColumn("[bold white]Description[/]").LeftAligned());

                table.Title = new TableTitle("[bold yellow]⚠️ Configuration Warnings[/]");

                foreach (var (title, description) in warnings)
                {
                    table.AddRow(
                        $"[bold orange3]{title}[/]",
                        $"[orange3]{description}[/]"
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
            }
        }
    }
    private static void DonationBanner()
    {
        var panel = new Panel(
            "[yellow]💖 [bold]Enjoying Refitter?[/] Consider supporting the project![/]\n\n" +
            "[cyan]🎯 Sponsor:[/] [link]https://github.com/sponsors/christianhelle[/]\n" +
            "[yellow]☕ Buy me a coffee:[/] [link]https://www.buymeacoffee.com/christianhelle[/]\n\n" +
            "[red]🐛 Found an issue?[/] [link]https://github.com/christianhelle/refitter/issues[/]"
        )
        .BorderColor(Color.Grey)
        .RoundedBorder()
        .Header("[bold dim]💝 Support[/]")
        .HeaderAlignment(Justify.Center);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    private static void SimpleDonationBanner()
    {
        Console.WriteLine("Support");
        Console.WriteLine("Enjoying Refitter? Consider supporting the project!");
        Console.WriteLine();
        Console.WriteLine("Sponsor: https://github.com/sponsors/christianhelle");
        Console.WriteLine("Buy me a coffee: https://www.buymeacoffee.com/christianhelle");
        Console.WriteLine();
        Console.WriteLine("Found an issue? https://github.com/christianhelle/refitter/issues");
        Console.WriteLine();
    }

    private static string GetOutputPath(Settings settings, RefitGeneratorSettings refitGeneratorSettings)
    {
        if (UsesDirectCliOutput(settings))
        {
            return settings.OutputPath!;
        }

        if (UsesDirectCliDefaults(settings))
        {
            return Settings.DefaultOutputPath;
        }

        // Determine root directory based on settings file location
        var root = string.IsNullOrWhiteSpace(settings.SettingsFilePath)
            ? string.Empty
            : Path.GetDirectoryName(settings.SettingsFilePath) ?? string.Empty;

        var outputPath = settings.OutputPath != Settings.DefaultOutputPath &&
                         !string.IsNullOrWhiteSpace(settings.OutputPath)
            ? settings.OutputPath
            : refitGeneratorSettings.OutputFilename ?? "Output.cs";

        if (!string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFolder))
        {
            outputPath = Path.Combine(refitGeneratorSettings.OutputFolder, outputPath);
        }

        // Root the output path relative to settings file location if applicable
        if (!string.IsNullOrWhiteSpace(root))
        {
            outputPath = Path.Combine(root, outputPath);
        }

        return outputPath;
    }

    private static void ApplySettingsFileDefaults(string settingsFilePath, RefitGeneratorSettings refitGeneratorSettings)
    {
        // Re-apply multi-file trigger logic
        if (!string.IsNullOrWhiteSpace(refitGeneratorSettings.ContractsOutputFolder))
            refitGeneratorSettings.GenerateMultipleFiles = true;

        // Default outputFolder to ./Generated if not specified (property initializer not invoked by JSON deserialization)
        if (string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFolder))
        {
            refitGeneratorSettings.OutputFolder = RefitGeneratorSettings.DefaultOutputFolder;
        }

        // Default outputFilename to .refitter filename if not specified
        if (string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFilename))
        {
            var refitterFileName = Path.GetFileNameWithoutExtension(settingsFilePath);
            refitGeneratorSettings.OutputFilename = $"{refitterFileName}.cs";
        }
    }

    private string GetOutputPath(
        Settings settings,
        RefitGeneratorSettings refitGeneratorSettings,
        GeneratedCode outputFile)
    {
        if (IsDirectCliGeneration(settings))
        {
            var outputDirectory = UsesDirectCliOutput(settings)
                ? settings.OutputPath!
                : ".";

            return Path.Combine(outputDirectory, outputFile.Filename);
        }

        var root = string.IsNullOrWhiteSpace(settings.SettingsFilePath)
            ? string.Empty
            : Path.GetDirectoryName(settings.SettingsFilePath) ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(refitGeneratorSettings.OutputFolder))
        {
            return Path.Combine(root, refitGeneratorSettings.OutputFolder, outputFile.Filename);
        }

        return !string.IsNullOrWhiteSpace(settings.OutputPath) && settings.OutputPath != Settings.DefaultOutputPath
            ? Path.Combine(root, settings.OutputPath, outputFile.Filename)
            : Path.Combine(root, outputFile.Filename);
    }

    private static bool IsDirectCliGeneration(Settings settings) =>
        string.IsNullOrWhiteSpace(settings.SettingsFilePath);

    private static bool UsesDirectCliOutput(Settings settings) =>
        IsDirectCliGeneration(settings) &&
        !string.IsNullOrWhiteSpace(settings.OutputPath) &&
        settings.OutputPath != Settings.DefaultOutputPath;

    private static bool UsesDirectCliDefaults(Settings settings) =>
        IsDirectCliGeneration(settings) &&
        (string.IsNullOrWhiteSpace(settings.OutputPath) || settings.OutputPath == Settings.DefaultOutputPath);
    private static async Task ValidateOpenApiSpec(string openApiPath, Settings settings)
    {
        OpenApiValidationResult validationResult;
        if (settings.SimpleOutput)
        {
            Console.WriteLine("Validating OpenAPI specification...");
            validationResult = await Validation.OpenApiValidator.Validate(openApiPath);
        }
        else
        {
            validationResult = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("cyan bold"))
                .StartAsync("[cyan]🔍 Validating OpenAPI specification...[/]", async _ =>
                {
                    return await Validation.OpenApiValidator.Validate(openApiPath);
                });
        }

        if (!validationResult.IsValid)
        {
            if (settings.SimpleOutput)
            {
                Console.WriteLine();
                Console.WriteLine("OpenAPI validation failed!");
                Console.WriteLine();
            }
            else
            {
                AnsiConsole.WriteLine();
                var errorPanel = new Panel("[red]❌ OpenAPI validation failed![/]")
                    .BorderColor(Color.Red)
                    .RoundedBorder();
                AnsiConsole.Write(errorPanel);
                AnsiConsole.WriteLine();
            }

            foreach (var error in validationResult.Diagnostics.Errors)
            {
                TryWriteLine(error, "red", "Error", settings.SimpleOutput);
            }

            foreach (var warning in validationResult.Diagnostics.Warnings)
            {
                TryWriteLine(warning, "yellow", "Warning", settings.SimpleOutput);
            }

            validationResult.ThrowIfInvalid();
        }

        if (settings.SimpleOutput)
        {
            Console.WriteLine("OpenAPI Analysis Results");
            var stats = validationResult.Statistics.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in stats)
            {
                if (line.Trim().StartsWith("-"))
                {
                    var parts = line.Trim().TrimStart('-').Trim().Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var label = parts[0].Trim();
                        var value = parts[1].Trim();
                        var description = GetStatsDescription(label);
                        Console.WriteLine($"{label}: {value} - {description}");
                    }
                }
            }
            Console.WriteLine();
        }
        else
        {
            // Create statistics table
            var table = new Table()
                .RoundedBorder()
                .BorderColor(Color.Blue)
                .AddColumn(new TableColumn("[bold white]📊 Metric[/]").LeftAligned())
                .AddColumn(new TableColumn("[bold white]📈 Count[/]").RightAligned())
                .AddColumn(new TableColumn("[bold white]📝 Details[/]").LeftAligned());

            table.Title = new TableTitle("[bold cyan]📊 OpenAPI Analysis Results[/]");

            var stats = validationResult.Statistics.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in stats)
            {
                if (line.Trim().StartsWith("-"))
                {
                    var parts = line.Trim().TrimStart('-').Trim().Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var label = parts[0].Trim();
                        var value = parts[1].Trim();
                        var icon = GetStatsIcon(label);
                        var description = GetStatsDescription(label);

                        table.AddRow(
                            $"{icon} [bold]{label}[/]",
                            $"[green]{value}[/]",
                            $"[dim]{description}[/]"
                        );
                    }
                }
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }
    }
    private static string GetStatsIcon(string label)
    {
        return label.ToLowerInvariant() switch
        {
            var l when l.Contains("path") => "📝",
            var l when l.Contains("operation") => "⚡",
            var l when l.Contains("parameter") => "📝",
            var l when l.Contains("request") => "📤",
            var l when l.Contains("response") => "📥",
            var l when l.Contains("link") => "🔗",
            var l when l.Contains("callback") => "🔄",
            var l when l.Contains("schema") => "📋",
            _ => "📊"
        };
    }

    private static string GetStatsDescription(string label)
    {
        return label.ToLowerInvariant() switch
        {
            var l when l.Contains("path") => "API endpoints defined",
            var l when l.Contains("operation") => "HTTP operations available",
            var l when l.Contains("parameter") => "Input parameters defined",
            var l when l.Contains("request") => "Request body schemas",
            var l when l.Contains("response") => "Response schemas defined",
            var l when l.Contains("link") => "Operation links",
            var l when l.Contains("callback") => "Callback definitions",
            var l when l.Contains("schema") => "Data schemas defined",
            _ => "API specification metric"
        };
    }

    private static void TryWriteLine(
        OpenApiError error,
        string color,
        string label,
        bool simpleOutput = false)
    {
        if (simpleOutput)
        {
            Console.WriteLine($"{label}:{Crlf}{error}{Crlf}");
            return;
        }

        try
        {
            AnsiConsole.MarkupLine($"[{color}]{label}:{Crlf}{error}{Crlf}[/]");
        }
        catch
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color switch
            {
                "red" => ConsoleColor.Red,
                "yellow" => ConsoleColor.Yellow,
                _ => originalColor
            };

            Console.WriteLine($"{label}:{Crlf}{error}{Crlf}");

            Console.ForegroundColor = originalColor;
        }
    }

    internal static async Task WriteRefitterSettingsFile(Settings settings, RefitGeneratorSettings refitGeneratorSettings)
    {
        var settingsFilePath = DetermineSettingsFilePath(settings);
        var settingsDirectory = Path.GetDirectoryName(settingsFilePath);

        if (!string.IsNullOrWhiteSpace(settingsDirectory) && !Directory.Exists(settingsDirectory))
            Directory.CreateDirectory(settingsDirectory);

        var json = Serializer.Serialize(refitGeneratorSettings);
        await File.WriteAllTextAsync(settingsFilePath, json);

        if (settings.SimpleOutput)
        {
            Console.WriteLine($"Settings file written to: {settingsFilePath}");
            Console.WriteLine();
        }
        else
        {
            var fileName = Path.GetFileName(settingsFilePath);
            var directory = Path.GetDirectoryName(settingsFilePath) ?? "";

            var panel = new Panel(
                $"[bold white]📄 File:[/] [cyan]{fileName}[/]\n" +
                $"[bold white]📂 Directory:[/] [dim]{directory}[/]"
            )
            .BorderColor(Color.Green)
            .RoundedBorder()
            .Header("[bold green]💾 Settings File Generated[/]")
            .HeaderAlignment(Justify.Center);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }
    }

    internal static string DetermineSettingsFilePath(Settings settings)
    {
        // If output path is specified and is a directory, put .refitter file there
        if (!string.IsNullOrWhiteSpace(settings.OutputPath) &&
            settings.OutputPath != Settings.DefaultOutputPath)
        {
            var outputDir = settings.GenerateMultipleFiles || !string.IsNullOrWhiteSpace(settings.ContractsOutputPath)
                ? settings.OutputPath
                : Path.GetDirectoryName(settings.OutputPath);

            if (!string.IsNullOrWhiteSpace(outputDir))
            {
                return Path.Combine(outputDir, FileExtensionConstants.Refitter);
            }
        }

        // Default: put .refitter file in current directory
        return FileExtensionConstants.Refitter;
    }
}
