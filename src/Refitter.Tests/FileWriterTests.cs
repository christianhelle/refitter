using FluentAssertions;
using TUnit.Core;

namespace Refitter.Tests;

public class FileWriterTests
{
    private static readonly string TestRoot =
        Path.Combine(Path.GetTempPath(), "refitter-filewriter-tests");

    [Test]
    public async Task WriteAsync_Writes_Content_To_Existing_Directory()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"refitter-fw-{Guid.NewGuid()}.cs");
        const string content = "// generated code";

        try
        {
            await FileWriter.WriteAsync(tempFile, content);

            File.Exists(tempFile).Should().BeTrue();
            (await File.ReadAllTextAsync(tempFile)).Should().Be(content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Test]
    public async Task WriteAsync_Creates_Missing_Directory()
    {
        var subDir = Path.Combine(TestRoot, Guid.NewGuid().ToString(), "nested");
        var filePath = Path.Combine(subDir, "Output.cs");
        const string content = "// code in new directory";

        try
        {
            await FileWriter.WriteAsync(filePath, content);

            File.Exists(filePath).Should().BeTrue();
            (await File.ReadAllTextAsync(filePath)).Should().Be(content);
        }
        finally
        {
            if (Directory.Exists(subDir))
                Directory.Delete(subDir, recursive: true);
        }
    }

    [Test]
    public async Task WriteAsync_PlannedFile_Writes_Content()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"refitter-fw-planned-{Guid.NewGuid()}.cs");
        const string content = "// planned content";

        try
        {
            await FileWriter.WriteAsync(new PlannedFile(tempFile, content));

            File.Exists(tempFile).Should().BeTrue();
            (await File.ReadAllTextAsync(tempFile)).Should().Be(content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Test]
    public async Task WriteAsync_PlannedFile_Creates_Missing_Directory()
    {
        var subDir = Path.Combine(TestRoot, Guid.NewGuid().ToString(), "planned");
        var filePath = Path.Combine(subDir, "Planned.cs");
        const string content = "// planned in new dir";

        try
        {
            await FileWriter.WriteAsync(new PlannedFile(filePath, content));

            File.Exists(filePath).Should().BeTrue();
            (await File.ReadAllTextAsync(filePath)).Should().Be(content);
        }
        finally
        {
            if (Directory.Exists(subDir))
                Directory.Delete(subDir, recursive: true);
        }
    }
}
