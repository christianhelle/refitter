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
    private static void CreateDirectoryLink(string link, string target)
    {
        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            Assert.Inconclusive("Symbolic links cannot be created in this environment: " + ex.Message);
        }
    }

    private static async Task InWorkspaceAsync(Func<string, Task> body)
    {
        var workspace = Path.Combine(
            AppContext.BaseDirectory,
            "CliFileWriterTests",
            Guid.NewGuid().ToString("N"));
        var previous = Directory.GetCurrentDirectory();
        Directory.CreateDirectory(workspace);
        try
        {
            Directory.SetCurrentDirectory(workspace);
            await body(workspace);
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
            if (Directory.Exists(workspace))
                Directory.Delete(workspace, recursive: true);
        }
    }

    /// <summary>
    /// GHSA-7q2f-m7jf-3gp6: a symbolic link to a directory below the working directory must not be followed.
    /// </summary>
    [TestMethod]
    [DoNotParallelize]
    public async Task WriteAsync_Refuses_Symlinked_Parent_Directory()
    {
        await InWorkspaceAsync(async workspace =>
        {
            var outside = Path.Combine(workspace, "..", "victim-" + Guid.NewGuid().ToString("N"));
            outside = Path.GetFullPath(outside);
            Directory.CreateDirectory(outside);
            try
            {
                CreateDirectoryLink(Path.Combine(workspace, "Generated"), outside);
                var writer = new CliFileWriter(new SimpleGenerationReporter());

                Func<Task> act = () => writer.WriteAsync(
                    new PlannedFile(Path.Combine("Generated", "Client.cs"), "// injected"), default);

                await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Generated*symbolic link*");
                Directory.GetFileSystemEntries(outside).Should().BeEmpty();
            }
            finally
            {
                Directory.Delete(outside, recursive: true);
            }
        });
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task WriteAsync_Refuses_Symlinked_Ancestor_Of_New_Directories()
    {
        await InWorkspaceAsync(async workspace =>
        {
            var outside = Path.GetFullPath(Path.Combine(workspace, "..", "victim-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(outside);
            try
            {
                CreateDirectoryLink(Path.Combine(workspace, "Generated"), outside);
                var writer = new CliFileWriter(new SimpleGenerationReporter());

                Func<Task> act = () => writer.WriteAsync(
                    new PlannedFile(Path.Combine("Generated", "Sub", "Client.cs"), "// injected"), default);

                await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*symbolic link*");
                Directory.GetFileSystemEntries(outside).Should().BeEmpty();
            }
            finally
            {
                Directory.Delete(outside, recursive: true);
            }
        });
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task WriteAsync_Creates_Nested_New_Directories_Under_Normal_Folder()
    {
        await InWorkspaceAsync(async workspace =>
        {
            Directory.CreateDirectory(Path.Combine(workspace, "Generated"));
            var writer = new CliFileWriter(new SimpleGenerationReporter());
            var relative = Path.Combine("Generated", "A", "B", "Client.cs");

            await writer.WriteAsync(new PlannedFile(relative, "// ok"), default);

            (await File.ReadAllTextAsync(Path.Combine(workspace, relative))).Should().Be("// ok");
        });
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task WriteAsync_Does_Not_Check_Directories_Above_Working_Directory()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "CliFileWriterTests", Guid.NewGuid().ToString("N"));
        var real = Path.Combine(root, "real");
        var previous = Directory.GetCurrentDirectory();
        Directory.CreateDirectory(Path.Combine(real, "work"));
        try
        {
            var link = Path.Combine(root, "link");
            CreateDirectoryLink(link, real);
            Directory.SetCurrentDirectory(Path.Combine(link, "work"));
            var writer = new CliFileWriter(new SimpleGenerationReporter());

            await writer.WriteAsync(new PlannedFile(Path.Combine("Generated", "Client.cs"), "// ok"), default);

            File.Exists(Path.Combine(real, "work", "Generated", "Client.cs")).Should().BeTrue();
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task WriteAsync_Refuses_Symlinked_Final_Directory_Outside_Working_Directory()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "CliFileWriterTests", Guid.NewGuid().ToString("N"));
        var outside = Path.Combine(root, "victim");
        Directory.CreateDirectory(outside);
        try
        {
            var link = Path.Combine(root, "linked");
            CreateDirectoryLink(link, outside);
            var writer = new CliFileWriter(new SimpleGenerationReporter());

            Func<Task> act = () => writer.WriteAsync(new PlannedFile(Path.Combine(link, "Client.cs"), "// injected"), default);

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*symbolic link*");
            Directory.GetFileSystemEntries(outside).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void IsUnder_Treats_Filesystem_Root_Descendants_As_Inside()
    {
        if (OperatingSystem.IsWindows())
        {
            CliFileWriter.IsUnder(@"C:\", @"C:\Generated\Sub").Should().BeTrue();
            CliFileWriter.IsUnder(@"C:\", @"C:\Generated").Should().BeTrue();
        }
        else
        {
            CliFileWriter.IsUnder("/", "/Generated/Sub").Should().BeTrue();
            CliFileWriter.IsUnder("/", "/Generated").Should().BeTrue();
        }
    }

    [TestMethod]
    public void IsUnder_Handles_Ordinary_Roots()
    {
        var root = Path.Combine(Path.GetTempPath(), "root");
        var sep = Path.DirectorySeparatorChar;
        CliFileWriter.IsUnder(root, root + sep + "child").Should().BeTrue();
        CliFileWriter.IsUnder(root + sep, root + sep + "child" + sep + "grand").Should().BeTrue();
        CliFileWriter.IsUnder(root, root).Should().BeFalse();
        CliFileWriter.IsUnder(root, root + "-sibling" + sep + "child").Should().BeFalse();
    }
}
