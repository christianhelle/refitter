using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

public class OutputRootFileWriterTests
{
    private static readonly string BaseDirectory = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "repo", "project"));
    private static readonly string OutputRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "repo", "project", "obj", "net8.0", "Refitter"));

    [Test]
    public void Rebase_Should_Preserve_Path_Relative_To_Base_Directory()
    {
        var path = Path.Combine(BaseDirectory, "Generated", "Petstore.cs");

        var result = OutputRootFileWriter.Rebase(path, BaseDirectory, OutputRoot);

        result.Should().Be(Path.Combine(OutputRoot, "Generated", "Petstore.cs"));
    }

    [Test]
    public void Rebase_Should_Place_File_In_Base_Directory_Directly_Under_Root()
    {
        var path = Path.Combine(BaseDirectory, "Petstore.cs");

        var result = OutputRootFileWriter.Rebase(path, BaseDirectory, OutputRoot);

        result.Should().Be(Path.Combine(OutputRoot, "Petstore.cs"));
    }

    [Test]
    public void Rebase_Should_Keep_Files_Outside_Base_Directory_Under_Root()
    {
        var path = Path.Combine(BaseDirectory, "..", "Shared", "Contracts.cs");

        var result = OutputRootFileWriter.Rebase(path, BaseDirectory, OutputRoot);

        result.Should().Be(Path.Combine(OutputRoot, OutputRootFileWriter.ParentDirectorySegment, "Shared", "Contracts.cs"));
        result.Should().StartWith(OutputRoot);
    }

    [Test]
    public void Rebase_Should_Escape_Literal_Segments_That_Start_With_Escape_Character()
    {
        var path = Path.Combine(BaseDirectory, "_parent", "__", "Contracts.cs");

        var result = OutputRootFileWriter.Rebase(path, BaseDirectory, OutputRoot);

        result.Should().Be(Path.Combine(OutputRoot, "__parent", "___", "Contracts.cs"));
    }

    [Test]
    public void Rebase_Should_Resolve_Relative_Paths_Against_Current_Directory()
    {
        var baseDirectory = Directory.GetCurrentDirectory();
        var path = Path.Combine("Generated", "Petstore.cs");

        var result = OutputRootFileWriter.Rebase(path, baseDirectory, OutputRoot);

        result.Should().Be(Path.Combine(OutputRoot, "Generated", "Petstore.cs"));
    }

    [Test]
    public void Rebase_Should_Keep_Path_Below_Root_When_On_Different_Drive()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var baseDirectory = @"C:\repo\project";
        var path = @"Z:\shared\Contracts.cs";

        var result = OutputRootFileWriter.Rebase(path, baseDirectory, OutputRoot);

        result.Should().Be(Path.Combine(OutputRoot, "shared", "Contracts.cs"));
    }

    [Test]
    public void Constructor_Should_Throw_When_Inner_Writer_Is_Null()
    {
        var act = () => new OutputRootFileWriter(null!, BaseDirectory, OutputRoot);

        act.Should().Throw<ArgumentNullException>().WithParameterName("inner");
    }

    [Test]
    public void CreateFileWriter_Should_Return_Cli_Writer_When_Output_Root_Is_Not_Set()
    {
        var writer = GenerationOrchestrator.CreateFileWriter(new Settings(), new SilentGenerationReporter());

        writer.Should().BeOfType<CliFileWriter>();
    }

    [Test]
    public async Task CreateFileWriter_Should_Rebase_Against_Current_Directory_Without_Settings_File()
    {
        var outputRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            var writer = GenerationOrchestrator.CreateFileWriter(
                new Settings { OutputRoot = outputRoot },
                new SilentGenerationReporter());

            await writer.WriteAsync(new PlannedFile(Path.Combine("Generated", "Petstore.cs"), "code"));

            File.Exists(Path.Combine(outputRoot, "Generated", "Petstore.cs")).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(outputRoot))
                Directory.Delete(outputRoot, recursive: true);
        }
    }

    [Test]
    public async Task WriteAsync_Should_Pass_Rebased_Path_To_Inner_Writer()
    {
        var inner = new RecordingFileWriter();
        var writer = new OutputRootFileWriter(inner, BaseDirectory, OutputRoot);

        await writer.WriteAsync(new PlannedFile(Path.Combine(BaseDirectory, "Generated", "Petstore.cs"), "code"));

        inner.Files.Should().ContainSingle().Which.Should().Be(
            new PlannedFile(Path.Combine(OutputRoot, "Generated", "Petstore.cs"), "code"));
    }

    private sealed class RecordingFileWriter : IFileWriter
    {
        public List<PlannedFile> Files { get; } = [];

        public Task WriteAsync(PlannedFile file, CancellationToken cancellationToken = default)
        {
            Files.Add(file);
            return Task.CompletedTask;
        }
    }
}
