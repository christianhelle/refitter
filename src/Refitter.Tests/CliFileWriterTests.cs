using AwesomeAssertions;
using Refitter;
using Refitter.Core;

namespace Refitter.Tests;


[TestClass]
public class CliFileWriterTests
{
    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    /// <summary>
    /// GHSA-7q2f-m7jf-3gp6: a pre-existing symbolic link at the output path must not be written through.
    /// </summary>
    [TestMethod]
    public async Task WriteAsync_Refuses_To_Write_Through_Symbolic_Link()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "CliFileWriterTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(workspace);
            var outside = Path.Combine(workspace, "outside.txt");
            await File.WriteAllTextAsync(outside, "original");
            var linkPath = Path.Combine(workspace, "Output.cs");
            try
            {
                File.CreateSymbolicLink(linkPath, outside);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
            {
                Assert.Inconclusive("Symbolic links cannot be created in this environment: " + ex.Message);
                return;
            }

            var writer = new CliFileWriter(new SimpleGenerationReporter());

            Func<Task> act = () => writer.WriteAsync(new PlannedFile(linkPath, "// injected"), default);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*symbolic link*");
            (await File.ReadAllTextAsync(outside)).Should().Be("original");
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [TestMethod]
    public async Task WriteAsync_Overwrites_Existing_Regular_File()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "CliFileWriterTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(workspace);
            var filePath = Path.Combine(workspace, "Output.cs");
            await File.WriteAllTextAsync(filePath, "old content that is longer");
            var writer = new CliFileWriter(new SimpleGenerationReporter());

            await writer.WriteAsync(new PlannedFile(filePath, "// new"), default);

            (await File.ReadAllTextAsync(filePath)).Should().Be("// new");
            Directory.GetFiles(workspace).Should().ContainSingle();
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    [TestMethod]
    public async Task WriteAsync_Succeeds_For_Very_Long_Output_File_Name()
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "CliFileWriterTests",
            Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(workspace);
            var filePath = Path.Combine(workspace, new string('a', 236) + ".cs");
            try
            {
                await File.WriteAllTextAsync(filePath, "probe");
                File.Delete(filePath);
            }
            catch (Exception ex) when (ex is IOException or PathTooLongException or UnauthorizedAccessException)
            {
                Assert.Inconclusive("The test filesystem does not accept a long file name: " + ex.Message);
            }

            var writer = new CliFileWriter(new SimpleGenerationReporter());

            await writer.WriteAsync(new PlannedFile(filePath, "// long"), default);

            (await File.ReadAllTextAsync(filePath)).Should().Be("// long");
            Directory.GetFiles(workspace).Should().ContainSingle();
        }
        finally
        {
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }
}
