namespace RoslynDiff.Core.Tests.MultiFile;

using RoslynDiff.Core.Models;
using RoslynDiff.Core.MultiFile;
using Xunit;

/// <summary>
/// Tests for <see cref="FolderComparer"/>.
/// </summary>
public sealed class FolderComparerTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _oldFolder;
    private readonly string _newFolder;

    public FolderComparerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"FolderComparerTests_{Guid.NewGuid()}");
        _oldFolder = Path.Combine(_tempRoot, "old");
        _newFolder = Path.Combine(_tempRoot, "new");

        Directory.CreateDirectory(_oldFolder);
        Directory.CreateDirectory(_newFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Compare_EmptyFolders_ReturnsEmptyResult()
    {
        // Arrange
        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Files);
        Assert.Equal(0, result.Summary.TotalFiles);
        Assert.Equal("folder", result.Metadata.Mode);
    }

    [Fact]
    public void Compare_AddedFile_DetectsAddition()
    {
        // Arrange
        var newFile = Path.Combine(_newFolder, "added.cs");
        File.WriteAllText(newFile, "public class Added { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Single(result.Files);
        Assert.Equal(FileChangeStatus.Added, result.Files[0].Status);
        Assert.Null(result.Files[0].OldPath);
        Assert.NotNull(result.Files[0].NewPath);
        Assert.Equal(1, result.Summary.AddedFiles);
    }

    [Fact]
    public void Compare_RemovedFile_DetectsRemoval()
    {
        // Arrange
        var oldFile = Path.Combine(_oldFolder, "removed.cs");
        File.WriteAllText(oldFile, "public class Removed { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Single(result.Files);
        Assert.Equal(FileChangeStatus.Removed, result.Files[0].Status);
        Assert.NotNull(result.Files[0].OldPath);
        Assert.Null(result.Files[0].NewPath);
        Assert.Equal(1, result.Summary.RemovedFiles);
    }

    [Fact]
    public void Compare_ModifiedFile_DetectsModification()
    {
        // Arrange
        var oldFile = Path.Combine(_oldFolder, "modified.cs");
        var newFile = Path.Combine(_newFolder, "modified.cs");
        File.WriteAllText(oldFile, "public class Modified { }");
        File.WriteAllText(newFile, "public class Modified { public void Method() { } }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Single(result.Files);
        Assert.Equal(FileChangeStatus.Modified, result.Files[0].Status);
        Assert.NotNull(result.Files[0].OldPath);
        Assert.NotNull(result.Files[0].NewPath);
        Assert.Equal(1, result.Summary.ModifiedFiles);
    }

    [Fact]
    public void Compare_UnchangedFile_SkipsFile()
    {
        // Arrange
        var oldFile = Path.Combine(_oldFolder, "unchanged.cs");
        var newFile = Path.Combine(_newFolder, "unchanged.cs");
        var content = "public class Unchanged { }";
        File.WriteAllText(oldFile, content);
        File.WriteAllText(newFile, content);

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Empty(result.Files);
        Assert.Equal(0, result.Summary.TotalFiles);
    }

    [Fact]
    public void Compare_MultipleFiles_DetectsAllChanges()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "removed.cs"), "class Removed { }");
        File.WriteAllText(Path.Combine(_oldFolder, "modified.cs"), "class Modified { }");
        File.WriteAllText(Path.Combine(_newFolder, "added.cs"), "class Added { }");
        File.WriteAllText(Path.Combine(_newFolder, "modified.cs"), "class Modified { void Method() { } }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Equal(3, result.Files.Count);
        Assert.Equal(1, result.Summary.AddedFiles);
        Assert.Equal(1, result.Summary.RemovedFiles);
        Assert.Equal(1, result.Summary.ModifiedFiles);
    }

    [Fact]
    public void Compare_NonRecursive_OnlyTopLevelFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "top.cs"), "class Top { }");
        Directory.CreateDirectory(Path.Combine(_oldFolder, "sub"));
        File.WriteAllText(Path.Combine(_oldFolder, "sub", "nested.cs"), "class Nested { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions { Recursive = false };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Single(result.Files);
        Assert.Contains(result.Files, f => f.OldPath?.Contains("top.cs") == true);
        Assert.DoesNotContain(result.Files, f => f.OldPath?.Contains("nested.cs") == true);
    }

    [Fact]
    public void Compare_Recursive_IncludesNestedFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "top.cs"), "class Top { }");
        Directory.CreateDirectory(Path.Combine(_oldFolder, "sub"));
        File.WriteAllText(Path.Combine(_oldFolder, "sub", "nested.cs"), "class Nested { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions { Recursive = true };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Equal(2, result.Files.Count);
        Assert.Contains(result.Files, f => f.OldPath?.Contains("top.cs") == true);
        Assert.Contains(result.Files, f => f.OldPath?.Contains("nested.cs") == true);
    }

    [Fact]
    public void Compare_IncludePattern_FiltersByPattern()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "file.cs"), "class File { }");
        File.WriteAllText(Path.Combine(_oldFolder, "file.txt"), "text");
        File.WriteAllText(Path.Combine(_oldFolder, "file.vb"), "Class File\nEnd Class");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            IncludePatterns = new[] { "*.cs" }
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Single(result.Files);
        Assert.Contains(result.Files, f => f.OldPath?.EndsWith(".cs") == true);
    }

    [Fact]
    public void Compare_ExcludePattern_FiltersByPattern()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "file.cs"), "class File { }");
        File.WriteAllText(Path.Combine(_oldFolder, "file.Designer.cs"), "// designer");
        File.WriteAllText(Path.Combine(_oldFolder, "file.g.cs"), "// generated");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            ExcludePatterns = new[] { "*.Designer.cs", "*.g.cs" }
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Single(result.Files);
        Assert.Contains(result.Files, f => f.OldPath?.EndsWith("file.cs") == true);
        Assert.DoesNotContain(result.Files, f => f.OldPath?.Contains("Designer") == true);
        Assert.DoesNotContain(result.Files, f => f.OldPath?.Contains(".g.") == true);
    }

    [Fact]
    public void Compare_MultipleIncludePatterns_MatchesAny()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "file.cs"), "class File { }");
        File.WriteAllText(Path.Combine(_oldFolder, "file.vb"), "Class File\nEnd Class");
        File.WriteAllText(Path.Combine(_oldFolder, "file.txt"), "text");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            IncludePatterns = new[] { "*.cs", "*.vb" }
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Equal(2, result.Files.Count);
        Assert.Contains(result.Files, f => f.OldPath?.EndsWith(".cs") == true);
        Assert.Contains(result.Files, f => f.OldPath?.EndsWith(".vb") == true);
    }

    [Fact]
    public void Compare_ExcludeTakesPrecedenceOverInclude()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "file.cs"), "class File { }");
        File.WriteAllText(Path.Combine(_oldFolder, "test.cs"), "class Test { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            IncludePatterns = new[] { "*.cs" },
            ExcludePatterns = new[] { "test.cs" }
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Single(result.Files);
        Assert.Contains(result.Files, f => f.OldPath?.EndsWith("file.cs") == true);
        Assert.DoesNotContain(result.Files, f => f.OldPath?.Contains("test") == true);
    }

    [Fact]
    public void Compare_RecursiveGlobPattern_MatchesNestedFiles()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_oldFolder, "src"));
        Directory.CreateDirectory(Path.Combine(_oldFolder, "tests"));
        File.WriteAllText(Path.Combine(_oldFolder, "src", "code.cs"), "class Code { }");
        File.WriteAllText(Path.Combine(_oldFolder, "tests", "test.cs"), "class Test { }");
        File.WriteAllText(Path.Combine(_oldFolder, "readme.md"), "# Readme");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            Recursive = true,
            IncludePatterns = new[] { "**/*.cs" }
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Equal(2, result.Files.Count);
        Assert.All(result.Files, f => Assert.True(f.OldPath?.EndsWith(".cs")));
    }

    [Fact]
    public void Compare_ExcludeDirectory_FiltersEntireDirectory()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_oldFolder, "src"));
        Directory.CreateDirectory(Path.Combine(_oldFolder, "bin"));
        File.WriteAllText(Path.Combine(_oldFolder, "src", "code.cs"), "class Code { }");
        File.WriteAllText(Path.Combine(_oldFolder, "bin", "output.dll"), "binary");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            Recursive = true,
            ExcludePatterns = new[] { "bin/**" }
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Single(result.Files);
        Assert.Contains(result.Files, f => f.OldPath?.Contains("src") == true);
        Assert.DoesNotContain(result.Files, f => f.OldPath?.Contains("bin") == true);
    }

    [Fact]
    public void Compare_QuestionMarkWildcard_MatchesSingleCharacter()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "file1.cs"), "class File1 { }");
        File.WriteAllText(Path.Combine(_oldFolder, "file2.cs"), "class File2 { }");
        File.WriteAllText(Path.Combine(_oldFolder, "file10.cs"), "class File10 { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            IncludePatterns = new[] { "file?.cs" }
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Equal(2, result.Files.Count);
        Assert.DoesNotContain(result.Files, f => f.OldPath?.Contains("file10") == true);
    }

    [Fact]
    public void CompareParallel_MultipleFiles_ProcessesInParallel()
    {
        // Arrange
        for (int i = 0; i < 20; i++)
        {
            File.WriteAllText(Path.Combine(_oldFolder, $"file{i}.cs"), $"class File{i} {{ }}");
            File.WriteAllText(Path.Combine(_newFolder, $"file{i}.cs"), $"class File{i} {{ void Method() {{ }} }}");
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.CompareParallel(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Equal(20, result.Files.Count);
        Assert.All(result.Files, f => Assert.Equal(FileChangeStatus.Modified, f.Status));
    }

    [Fact]
    public void Compare_ThrowsArgumentNullException_WhenOldPathIsNull()
    {
        // Arrange
        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            comparer.Compare(null!, _newFolder, options, folderOptions));
    }

    [Fact]
    public void Compare_ThrowsArgumentNullException_WhenNewPathIsNull()
    {
        // Arrange
        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            comparer.Compare(_oldFolder, null!, options, folderOptions));
    }

    [Fact]
    public void Compare_ThrowsDirectoryNotFoundException_WhenOldDirectoryDoesNotExist()
    {
        // Arrange
        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();
        var nonExistentPath = Path.Combine(_tempRoot, "nonexistent");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() =>
            comparer.Compare(nonExistentPath, _newFolder, options, folderOptions));
    }

    [Fact]
    public void Compare_ThrowsDirectoryNotFoundException_WhenNewDirectoryDoesNotExist()
    {
        // Arrange
        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();
        var nonExistentPath = Path.Combine(_tempRoot, "nonexistent");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() =>
            comparer.Compare(_oldFolder, nonExistentPath, options, folderOptions));
    }

    [Fact]
    public void Compare_CaseInsensitivePaths_MatchesFilesCorrectly()
    {
        // Arrange
        var oldFile = Path.Combine(_oldFolder, "File.CS");
        var newFile = Path.Combine(_newFolder, "file.cs");
        File.WriteAllText(oldFile, "class File { }");
        File.WriteAllText(newFile, "class File { void Method() { } }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        // FolderComparer uses case-insensitive file matching by design (StringComparer.OrdinalIgnoreCase)
        // So File.CS and file.cs should match as the same file on all filesystems
        var singleFile = Assert.Single(result.Files);
        Assert.Equal(FileChangeStatus.Modified, singleFile.Status);
    }

    [Fact]
    public void Compare_NestedDirectories_MatchesFilesByRelativePath()
    {
        // Arrange
        var oldSub = Path.Combine(_oldFolder, "sub", "nested");
        var newSub = Path.Combine(_newFolder, "sub", "nested");
        Directory.CreateDirectory(oldSub);
        Directory.CreateDirectory(newSub);

        File.WriteAllText(Path.Combine(oldSub, "deep.cs"), "class Deep { }");
        File.WriteAllText(Path.Combine(newSub, "deep.cs"), "class Deep { void Method() { } }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions { Recursive = true };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Single(result.Files);
        Assert.Equal(FileChangeStatus.Modified, result.Files[0].Status);
    }

    [Fact]
    public void Compare_ComplexGlobPattern_FiltersCorrectly()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_oldFolder, "src"));
        Directory.CreateDirectory(Path.Combine(_oldFolder, "tests"));
        File.WriteAllText(Path.Combine(_oldFolder, "src", "code.cs"), "class Code { }");
        File.WriteAllText(Path.Combine(_oldFolder, "src", "code.g.cs"), "// generated");
        File.WriteAllText(Path.Combine(_oldFolder, "tests", "test.cs"), "class Test { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            Recursive = true,
            IncludePatterns = new[] { "src/**/*.cs" },
            ExcludePatterns = new[] { "**/*.g.cs" }
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Single(result.Files);
        Assert.Contains(result.Files, f => f.OldPath?.Contains("code.cs") == true && !f.OldPath.Contains(".g."));
    }

    [Fact]
    public void Compare_SummaryStatistics_AggregateCorrectly()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "removed.cs"), "class Removed { }");
        File.WriteAllText(Path.Combine(_oldFolder, "modified.cs"), "class Modified { }");
        File.WriteAllText(Path.Combine(_newFolder, "added.cs"), "class Added { }");
        File.WriteAllText(Path.Combine(_newFolder, "modified.cs"), "class Modified { public void NewMethod() { } }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Equal(3, result.Summary.TotalFiles);
        Assert.Equal(1, result.Summary.AddedFiles);
        Assert.Equal(1, result.Summary.RemovedFiles);
        Assert.Equal(1, result.Summary.ModifiedFiles);
        Assert.True(result.Summary.TotalChanges > 0);
    }

    [Fact]
    public void Compare_MetadataFields_SetCorrectly()
    {
        // Arrange
        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        Assert.Equal("folder", result.Metadata.Mode);
        Assert.Equal(Path.GetFullPath(_oldFolder), result.Metadata.OldRoot);
        Assert.Equal(Path.GetFullPath(_newFolder), result.Metadata.NewRoot);
        Assert.Null(result.Metadata.GitRefRange);
        Assert.True(result.Metadata.Timestamp <= DateTime.UtcNow);
    }
}
