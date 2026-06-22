using FluentAssertions;
using Refitter.Core;
using Refitter.MSBuild;
using TUnit.Core;

namespace Refitter.Tests;


public class MsBuildFileWriterTests
{
    [Test]
    public async Task WriteAsync_Creates_Directory_And_Writes_File()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "MsBuildFileWriterTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            var writer = new MsBuildFileWriter();
            var filePath = Path.Combine(workspace, "Generated", "Output.cs");
            var planned = new PlannedFile(filePath, "// msbuild test");

            await writer.WriteAsync(planned, default);

            File.Exists(filePath).Should().BeTrue();
            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Be("// msbuild test");
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
            "MsBuildFileWriterTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(workspace);

            var writer = new MsBuildFileWriter();
            var filePath = Path.Combine(workspace, "Output.cs");
            var planned = new PlannedFile(filePath, "// test");

            await writer.WriteAsync(planned, default);

            File.Exists(filePath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }
}
