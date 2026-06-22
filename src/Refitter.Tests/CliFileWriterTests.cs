using FluentAssertions;
using Refitter;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;


public class CliFileWriterTests
{
    [Test]
    public async Task WriteAsync_Creates_Directory_And_Writes_File()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "CliFileWriterTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var reporter = new SimpleGenerationReporter();
            var writer = new CliFileWriter(reporter);
            var filePath = Path.Combine(workspace, "Generated", "Output.cs");
            var planned = new PlannedFile(filePath, "// test content");

            await writer.WriteAsync(planned, default);

            File.Exists(filePath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Be("// test content");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task WriteAsync_Does_Not_Throw_When_Directory_Already_Exists()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "CliFileWriterTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(workspace);
            var existingFile = Path.Combine(workspace, "Existing.cs");
            await File.WriteAllTextAsync(existingFile, "existing");

            var reporter = new SimpleGenerationReporter();
            var writer = new CliFileWriter(reporter);
            var newFile = Path.Combine(workspace, "Output.cs");
            var planned = new PlannedFile(newFile, "// new content");

            await writer.WriteAsync(planned, default);

            File.Exists(existingFile).Should().BeTrue();
            File.Exists(newFile).Should().BeTrue();
            var content = await File.ReadAllTextAsync(newFile);
            content.Should().Be("// new content");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task WriteAsync_Writes_To_File_In_Existing_Directory()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "CliFileWriterTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(workspace);
            var filePath = Path.Combine(workspace, "Output.cs");
            var reporter = new SimpleGenerationReporter();
            var writer = new CliFileWriter(reporter);
            var planned = new PlannedFile(filePath, "// test");

            await writer.WriteAsync(planned, default);

            File.Exists(filePath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Be("// test");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }
}
