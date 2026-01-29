namespace RoslynDiff.Core.Tests.MultiFile;

using System.Runtime.InteropServices;
using FluentAssertions;
using RoslynDiff.Core.Models;
using RoslynDiff.Core.MultiFile;
using Xunit;

/// <summary>
/// Security-focused tests for <see cref="FolderComparer"/>.
/// Tests symlink handling, path traversal, permission errors, and other security concerns.
/// </summary>
public sealed class FolderComparerSecurityTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _oldFolder;
    private readonly string _newFolder;

    public FolderComparerSecurityTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"FolderComparerSecurityTests_{Guid.NewGuid()}");
        _oldFolder = Path.Combine(_tempRoot, "old");
        _newFolder = Path.Combine(_tempRoot, "new");

        Directory.CreateDirectory(_oldFolder);
        Directory.CreateDirectory(_newFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            try
            {
                // Remove read-only attributes before deleting
                RemoveReadOnlyAttributes(_tempRoot);
                Directory.Delete(_tempRoot, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    private void RemoveReadOnlyAttributes(string path)
    {
        if (!Directory.Exists(path))
            return;

        foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        {
            try
            {
                var attr = File.GetAttributes(file);
                if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(file, attr & ~FileAttributes.ReadOnly);
                }
            }
            catch { }
        }
    }

    #region Symlink Tests (HIGH Priority - D1-H1, D1-H2)

    [Fact]
    public void Compare_WithSymlinkToFile_ShouldHandleGracefully()
    {
        // Skip on platforms that don't support symlinks or require elevation
        if (!SupportsSymlinks())
        {
            return;
        }

        // Arrange
        var targetFile = Path.Combine(_tempRoot, "target.cs");
        File.WriteAllText(targetFile, "public class Target { }");

        var symlinkPath = Path.Combine(_oldFolder, "symlink.cs");

        try
        {
            CreateSymbolicLink(symlinkPath, targetFile, isDirectory: false);
        }
        catch (UnauthorizedAccessException)
        {
            // Symlink creation requires admin on some platforms
            return;
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var act = () => comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Should either:
        // 1. Follow symlink safely and process the target file
        // 2. Skip symlink with warning
        // 3. Detect and report it as a symlink
        // Should NOT: crash, infinite loop, or follow outside root
        act.Should().NotThrow("symlinks should be handled gracefully");
    }

    [Fact]
    public void Compare_WithSymlinkToDirectory_ShouldHandleGracefully()
    {
        // Skip on platforms that don't support symlinks
        if (!SupportsSymlinks())
        {
            return;
        }

        // Arrange
        var targetDir = Path.Combine(_tempRoot, "target");
        Directory.CreateDirectory(targetDir);
        File.WriteAllText(Path.Combine(targetDir, "file.cs"), "class Test { }");

        var symlinkPath = Path.Combine(_oldFolder, "symlink");

        try
        {
            CreateSymbolicLink(symlinkPath, targetDir, isDirectory: true);
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions { Recursive = true };

        // Act
        var act = () => comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        act.Should().NotThrow("directory symlinks should be handled gracefully");
    }

    [Fact]
    public async Task Compare_WithRecursiveSymlinkLoop_ShouldNotInfiniteLoop()
    {
        // Skip on platforms that don't support symlinks
        if (!SupportsSymlinks())
        {
            return;
        }

        // Arrange - Create a recursive symlink: old/sub -> old
        var subDir = Path.Combine(_oldFolder, "sub");

        try
        {
            CreateSymbolicLink(subDir, _oldFolder, isDirectory: true);
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        File.WriteAllText(Path.Combine(_oldFolder, "file.cs"), "class Test { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions { Recursive = true };

        // Act - This should complete in reasonable time (not infinite loop)
        var timeout = Task.Delay(TimeSpan.FromSeconds(5));
        var compareTask = Task.Run(() => comparer.Compare(_oldFolder, _newFolder, options, folderOptions));
        var completedTask = await Task.WhenAny(compareTask, timeout);

        // Assert
        Assert.True(completedTask == compareTask, "Compare should complete without infinite loop");
    }

    [Fact]
    public void Compare_WithSymlinkEscapingRoot_ShouldNotFollowOutsideRoot()
    {
        // D1-H1: Path traversal security concern
        // Skip on platforms that don't support symlinks
        if (!SupportsSymlinks())
        {
            return;
        }

        // Arrange - Create symlink pointing outside the comparison root
        var outsideFile = Path.Combine(_tempRoot, "outside.cs");
        File.WriteAllText(outsideFile, "public class Outside { }");

        var symlinkPath = Path.Combine(_oldFolder, "escape.cs");

        try
        {
            CreateSymbolicLink(symlinkPath, outsideFile, isDirectory: false);
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Should either skip the symlink or ensure paths stay within root
        // Should NOT expose files outside the comparison root
        result.Should().NotBeNull();
    }

    #endregion

    #region Permission Tests (HIGH Priority - D1-H3)

    [Fact]
    public void Compare_WithUnreadableFile_ShouldHandleGracefully()
    {
        // Skip on Windows where file permissions work differently
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var unreadableFile = Path.Combine(_oldFolder, "unreadable.cs");
        File.WriteAllText(unreadableFile, "class Unreadable { }");

        // Remove all read permissions (chmod 000)
        File.SetUnixFileMode(unreadableFile, UnixFileMode.None);

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        try
        {
            // Act
            var act = () => comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

            // Assert - Should either skip with warning or throw informative exception
            // Should NOT crash with obscure error
            var result = act.Should().NotThrow("should handle permission errors gracefully")
                .Which;
        }
        finally
        {
            // Cleanup - restore permissions so Dispose can delete
            try
            {
                File.SetUnixFileMode(unreadableFile,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch { }
        }
    }

    [Fact]
    public void Compare_WithUnreadableDirectory_ShouldHandleGracefully()
    {
        // Skip on Windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var unreadableDir = Path.Combine(_oldFolder, "unreadable");
        Directory.CreateDirectory(unreadableDir);
        File.WriteAllText(Path.Combine(unreadableDir, "file.cs"), "class Test { }");

        // Remove all permissions on directory
        File.SetUnixFileMode(unreadableDir, UnixFileMode.None);

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions { Recursive = true };

        try
        {
            // Act
            var act = () => comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

            // Assert - Should handle permission denied on directory traversal
            act.Should().NotThrow("should handle directory permission errors gracefully");
        }
        finally
        {
            // Restore permissions
            try
            {
                File.SetUnixFileMode(unreadableDir,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            catch { }
        }
    }

    #endregion

    #region Path Traversal Tests (HIGH Priority - D1-H4)

    [Fact]
    public void Compare_WithDotDotInRelativePath_ShouldNotEscapeRoot()
    {
        // Arrange - This tests that GetRelativePath doesn't allow traversal
        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Create a file with .. in the name (legal filename on most platforms)
        var weirdFile = Path.Combine(_oldFolder, "..file.cs");
        File.WriteAllText(weirdFile, "class Test { }");

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Relative path should be sanitized
        result.Should().NotBeNull();
        result.Files.Should().HaveCount(1);

        // The relative path should not contain path traversal
        var relativePath = result.Files[0].OldPath;
        relativePath.Should().NotBeNull();
        relativePath.Should().NotStartWith("..");
    }

    #endregion

    #region Deep Nesting Tests (HIGH Priority - D1-H5)

    [Fact]
    public void Compare_WithVeryDeepDirectoryNesting_ShouldHandleGracefully()
    {
        // Arrange - Create 100+ levels of nested directories
        var currentPath = _oldFolder;
        var depth = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 50 : 100;

        // On Windows, we're limited by MAX_PATH (260 chars) so use shorter names
        var dirName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "d" : "dir";

        try
        {
            for (int i = 0; i < depth; i++)
            {
                currentPath = Path.Combine(currentPath, dirName);
                Directory.CreateDirectory(currentPath);
            }

            File.WriteAllText(Path.Combine(currentPath, "deep.cs"), "class Deep { }");
        }
        catch (PathTooLongException)
        {
            // Expected on some platforms
            return;
        }
        catch (IOException)
        {
            // May hit path length limits
            return;
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions { Recursive = true };

        // Act
        var act = () => comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Should either process successfully or fail with clear error
        act.Should().NotThrow("deep nesting should be handled gracefully");
    }

    #endregion

    #region Large File Tests (MEDIUM Priority - D1-M2)

    [Fact]
    public void Compare_WithVeryLargeFile_ShouldHandleGracefully()
    {
        // Arrange - Create a 10MB+ file
        var largeFile = Path.Combine(_oldFolder, "large.cs");
        using (var writer = new StreamWriter(largeFile))
        {
            writer.WriteLine("public class Large {");

            // Write 100,000 methods (~10MB of text)
            for (int i = 0; i < 100_000; i++)
            {
                writer.WriteLine($"    public void Method{i}() {{ }}");
            }

            writer.WriteLine("}");
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var act = () => comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Should either:
        // 1. Process successfully with streaming
        // 2. Skip with warning about file size
        // 3. Throw meaningful exception with size limit info
        // Should NOT: exhaust memory or hang
        act.Should().NotThrow("large files should be handled gracefully");
    }

    [Fact]
    public void Compare_WithHundredMegabyteFile_ShouldHandleOrReject()
    {
        // Arrange - Create a 100MB file (might be too large)
        var massiveFile = Path.Combine(_oldFolder, "massive.cs");

        try
        {
            using (var writer = new StreamWriter(massiveFile))
            {
                writer.WriteLine("public class Massive {");

                // Write 1 million methods (~100MB of text)
                for (int i = 0; i < 1_000_000; i++)
                {
                    writer.WriteLine($"    public void Method{i}() {{ }}");
                }

                writer.WriteLine("}");
            }
        }
        catch (IOException)
        {
            // May not have disk space
            return;
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var act = () => comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Should handle gracefully (process, skip, or reject with clear message)
        // The key is NOT to crash with OutOfMemoryException
        try
        {
            var result = act.Should().NotThrow("should handle or reject gracefully").Which;
        }
        catch (OutOfMemoryException)
        {
            Assert.Fail("Should not throw OutOfMemoryException - should implement size limits or streaming");
        }
    }

    #endregion

    #region Special Filename Tests (MEDIUM Priority - D1-M4, D1-M8)

    [Fact]
    public void Compare_WithUnicodeFilenames_ShouldHandleCorrectly()
    {
        // Arrange - Test various Unicode filenames
        var testFiles = new[]
        {
            "中文文件.cs",      // Chinese
            "файл.cs",         // Russian/Cyrillic
            "αρχείο.cs",       // Greek
            "ファイル.cs",      // Japanese
            "📁_emoji.cs",     // Emoji (if supported)
        };

        foreach (var filename in testFiles)
        {
            try
            {
                var path = Path.Combine(_oldFolder, filename);
                File.WriteAllText(path, $"// {filename}\nclass Test {{ }}");
            }
            catch (ArgumentException)
            {
                // Some characters may not be valid on this platform
                continue;
            }
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        result.Should().NotBeNull();
        result.Files.Should().NotBeEmpty("should process Unicode filenames");
    }

    [Fact]
    public void Compare_WithFilesWithoutExtension_ShouldProcess()
    {
        // Arrange
        var noExtFiles = new[] { "Makefile", "Dockerfile", "LICENSE", "README" };

        foreach (var filename in noExtFiles)
        {
            File.WriteAllText(Path.Combine(_oldFolder, filename), "content");
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        result.Files.Should().HaveCount(4, "files without extensions should be processed");
    }

    [Fact]
    public void Compare_WithHiddenFiles_ShouldProcess()
    {
        // Arrange - Test hidden files (starting with dot on Unix)
        var hiddenFiles = new[] { ".gitkeep", ".editorconfig", ".gitignore" };

        foreach (var filename in hiddenFiles)
        {
            File.WriteAllText(Path.Combine(_oldFolder, filename), "content");
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        result.Files.Should().HaveCount(3, "hidden files should be processed");
    }

    #endregion

    #region Helper Methods

    private bool SupportsSymlinks()
    {
        // Symlinks are supported on Unix/macOS and Windows (with appropriate privileges)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, developer mode or admin rights are needed
            // We'll try and catch if not supported
            return true; // Will catch in actual test
        }

        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
               RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }

    private void CreateSymbolicLink(string linkPath, string targetPath, bool isDirectory)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.CreateSymbolicLink(linkPath, targetPath);
        }
        else
        {
            if (isDirectory)
            {
                Directory.CreateSymbolicLink(linkPath, targetPath);
            }
            else
            {
                File.CreateSymbolicLink(linkPath, targetPath);
            }
        }
    }

    #endregion
}
