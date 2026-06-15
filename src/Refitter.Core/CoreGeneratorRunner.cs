namespace Refitter.Core;

/// <summary>
/// Direct Core implementation of IGeneratorRunner that calls RefitGenerator in-process.
/// </summary>
public class CoreGeneratorRunner : IGeneratorRunner
{
    /// <summary>
    /// Runs the code generator directly via Core and returns the list of generated file paths.
    /// </summary>
    public async Task<IReadOnlyList<string>> RunAsync(
        RefitGeneratorSettings settings,
        bool skipValidation,
        bool noLogging,
        CancellationToken cancellationToken)
    {
        var generator = await RefitGenerator.CreateAsync(settings).ConfigureAwait(false);

        var generatedFiles = new List<string>();

        if (settings.GenerateMultipleFiles)
        {
            generatedFiles = await WriteMultipleFiles(generator, settings).ConfigureAwait(false);
        }
        else
        {
            generatedFiles = await WriteSingleFile(generator, settings).ConfigureAwait(false);
        }

        return generatedFiles;
    }

    private static async Task<List<string>> WriteSingleFile(
        RefitGenerator generator,
        RefitGeneratorSettings settings)
    {
        var code = generator.Generate();
        code = code.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        var outputPath = GetSingleFileOutputPath(settings);
        var directory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await Task.Run(() => File.WriteAllText(outputPath, code)).ConfigureAwait(false);

        return new[] { Path.GetFullPath(outputPath) }.ToList();
    }

    private static async Task<List<string>> WriteMultipleFiles(
        RefitGenerator generator,
        RefitGeneratorSettings settings)
    {
        var generatorOutput = generator.GenerateMultipleFiles();
        var generatedFiles = new List<string>();

        foreach (var outputFile in generatorOutput.Files)
        {
            var outputPath = GetMultiFileOutputPath(settings, outputFile);

            if (OutputPlanner.ShouldRerouteToContractsFolder(settings, outputFile))
            {
                outputPath = GetContractsOutputPath(settings, outputFile);
            }

            var directory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await Task.Run(() => File.WriteAllText(outputPath, outputFile.Content)).ConfigureAwait(false);
            generatedFiles.Add(Path.GetFullPath(outputPath));
        }

        return generatedFiles;
    }

    private static string GetSingleFileOutputPath(RefitGeneratorSettings settings)
    {
        var filename = settings.OutputFilename ?? "Output.cs";
        var outputPath = !string.IsNullOrWhiteSpace(settings.OutputFolder)
            ? Path.Combine(settings.OutputFolder, filename)
            : filename;

        return Path.GetFullPath(outputPath);
    }

    private static string GetMultiFileOutputPath(RefitGeneratorSettings settings, GeneratedCode outputFile)
    {
        var outputFolder = settings.OutputFolder;

        if (!string.IsNullOrWhiteSpace(outputFolder))
        {
            return Path.GetFullPath(Path.Combine(outputFolder, outputFile.Filename));
        }

        return Path.GetFullPath(outputFile.Filename);
    }

    private static string GetContractsOutputPath(RefitGeneratorSettings settings, GeneratedCode outputFile)
    {
        var contractsFolder = Path.GetFullPath(Path.Combine(settings.ContractsOutputFolder!));
        return Path.Combine(contractsFolder, outputFile.Filename);
    }
}
