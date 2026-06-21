using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;


[Category("Unit")]
public class SourceGeneratorFileWriterTests
{
    [Test]
    public async Task WriteAsync_Creates_Directory_And_Writes_File()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "SourceGeneratorFileWriterTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var writer = new SourceGeneratorFileWriter();
            var filePath = Path.Combine(workspace, "Generated", "Output.g.cs");
            var planned = new PlannedFile(filePath, "// sourcegen test");

            await writer.WriteAsync(planned, default);

            File.Exists(filePath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Be("// sourcegen test");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task WriteAsync_Skips_Write_When_Content_Identical()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "SourceGeneratorFileWriterTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(workspace);
            var filePath = Path.Combine(workspace, "Output.g.cs");
            await File.WriteAllTextAsync(filePath, "// original content");

            // Capture the timestamp after initial write
            var originalTimestamp = File.GetLastWriteTimeUtc(filePath);

            var writer = new SourceGeneratorFileWriter();
            var planned = new PlannedFile(filePath, "// original content");

            // This should NOT write because content is the same
            await writer.WriteAsync(planned, default);

            // Verify timestamp hasn't changed (proving no write occurred)
            var currentTimestamp = File.GetLastWriteTimeUtc(filePath);
            currentTimestamp.Should().Be(originalTimestamp);

            // File should still exist with original content
            File.Exists(filePath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Be("// original content");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [Test]
    public async Task WriteAsync_Overwrites_When_Content_Differs()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "SourceGeneratorFileWriterTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(workspace);
            var filePath = Path.Combine(workspace, "Output.g.cs");
            await File.WriteAllTextAsync(filePath, "// old content");

            var writer = new SourceGeneratorFileWriter();
            var planned = new PlannedFile(filePath, "// new content");

            await writer.WriteAsync(planned, default);

            File.Exists(filePath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Be("// new content");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }
}
