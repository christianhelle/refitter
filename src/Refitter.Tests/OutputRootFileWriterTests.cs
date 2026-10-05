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
