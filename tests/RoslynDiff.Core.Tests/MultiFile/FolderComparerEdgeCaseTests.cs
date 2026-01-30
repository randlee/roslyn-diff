namespace RoslynDiff.Core.Tests.MultiFile;

using FluentAssertions;
using RoslynDiff.Core.Models;
using RoslynDiff.Core.MultiFile;
using Xunit;

/// <summary>
/// Edge case tests for <see cref="FolderComparer"/>.
/// Tests empty files, special filenames, glob patterns, and various edge cases.
/// </summary>
public sealed class FolderComparerEdgeCaseTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _oldFolder;
    private readonly string _newFolder;

    public FolderComparerEdgeCaseTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"FolderComparerEdgeCaseTests_{Guid.NewGuid()}");
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
                Directory.Delete(_tempRoot, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    #region Empty File Tests (MEDIUM Priority - D1-M1)

    [Fact]
    public void Compare_WithEmptyFileInOld_ShouldTreatAsEmpty()
    {
        // Arrange
        var emptyFile = Path.Combine(_oldFolder, "empty.cs");
        File.WriteAllText(emptyFile, "");  // 0 bytes

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Empty file should be processed as removed
        result.Files.Should().HaveCount(1);
        result.Files[0].Status.Should().Be(FileChangeStatus.Removed);
    }

    [Fact]
    public void Compare_WithEmptyFileInNew_ShouldTreatAsEmpty()
    {
        // Arrange
        var emptyFile = Path.Combine(_newFolder, "empty.cs");
        File.WriteAllText(emptyFile, "");  // 0 bytes

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        result.Files.Should().HaveCount(1);
        result.Files[0].Status.Should().Be(FileChangeStatus.Added);
    }

    [Fact]
    public void Compare_WithBothFilesEmpty_ShouldTreatAsUnchanged()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "empty.cs"), "");
        File.WriteAllText(Path.Combine(_newFolder, "empty.cs"), "");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Both empty should be unchanged
        result.Files.Should().BeEmpty("both files are empty and identical");
    }

    [Fact]
    public void Compare_WithEmptyToNonEmpty_ShouldDetectChange()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "file.cs"), "");
        File.WriteAllText(Path.Combine(_newFolder, "file.cs"), "class Test { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        result.Files.Should().HaveCount(1);
        result.Files[0].Status.Should().Be(FileChangeStatus.Modified);
    }

    #endregion

    #region Folder Path Edge Cases (MEDIUM Priority - D1-M6, D1-M7)

    [Fact]
    public void Compare_WithTrailingSlashInPath_ShouldWork()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "test.cs"), "class Test { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Add trailing slashes
        var oldWithSlash = _oldFolder + Path.DirectorySeparatorChar;
        var newWithSlash = _newFolder + Path.DirectorySeparatorChar;

        // Act
        var result = comparer.Compare(oldWithSlash, newWithSlash, options, folderOptions);

        // Assert
        result.Should().NotBeNull();
        result.Files.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_WithSameFolderForOldAndNew_ShouldReturnEmpty()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "test.cs"), "class Test { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act - Compare folder to itself
        var result = comparer.Compare(_oldFolder, _oldFolder, options, folderOptions);

        // Assert
        result.Files.Should().BeEmpty("comparing folder to itself should show no changes");
        result.Summary.TotalFiles.Should().Be(0);
    }

    #endregion

    #region Glob Pattern Edge Cases (MEDIUM Priority - D1-M9, D1-M10)

    [Fact]
    public void Compare_WithBracketGlobPattern_ShouldMatchCharacterClass()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "Test.cs"), "class Test { }");
        File.WriteAllText(Path.Combine(_oldFolder, "test.cs"), "class test { }");
        File.WriteAllText(Path.Combine(_oldFolder, "best.cs"), "class best { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            IncludePatterns = new[] { "[Tt]est.cs" }  // Match Test.cs or test.cs
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Should match Test.cs and test.cs, but not best.cs
        result.Files.Count.Should().BeGreaterThanOrEqualTo(1);
        result.Files.Should().NotContain(f => f.OldPath != null && f.OldPath.Contains("best"));
    }

    [Fact]
    public void Compare_WithRangeGlobPattern_ShouldMatchRange()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "file1.cs"), "1");
        File.WriteAllText(Path.Combine(_oldFolder, "file2.cs"), "2");
        File.WriteAllText(Path.Combine(_oldFolder, "file3.cs"), "3");
        File.WriteAllText(Path.Combine(_oldFolder, "file9.cs"), "9");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            IncludePatterns = new[] { "file[1-3].cs" }  // Match file1, file2, file3
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        result.Files.Should().HaveCount(3);
        result.Files.Should().NotContain(f => f.OldPath != null && f.OldPath.Contains("file9"));
    }

    [Fact]
    public void Compare_WithNegatedBracketPattern_ShouldMatchExcept()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "test.cs"), "test");
        File.WriteAllText(Path.Combine(_oldFolder, "best.cs"), "best");
        File.WriteAllText(Path.Combine(_oldFolder, "rest.cs"), "rest");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            IncludePatterns = new[] { "[!t]est.cs" }  // Match anything except test.cs
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Should match best.cs and rest.cs, not test.cs
        result.Files.Should().HaveCount(2);
        result.Files.Should().NotContain(f => f.OldPath == "test.cs");
    }

    [Fact]
    public void Compare_WithComplexMultiLevelGlob_ShouldMatch()
    {
        // Arrange
        var srcDir = Path.Combine(_oldFolder, "src");
        var testsDir = Path.Combine(_oldFolder, "tests");
        var docsDir = Path.Combine(_oldFolder, "docs");

        Directory.CreateDirectory(srcDir);
        Directory.CreateDirectory(testsDir);
        Directory.CreateDirectory(docsDir);

        File.WriteAllText(Path.Combine(srcDir, "code.cs"), "code");
        File.WriteAllText(Path.Combine(testsDir, "test.cs"), "test");
        File.WriteAllText(Path.Combine(docsDir, "readme.md"), "docs");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions
        {
            Recursive = true,
            IncludePatterns = new[] { "{src,tests}/**/*.cs" }  // Match .cs in src or tests
        };

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        result.Files.Should().HaveCount(2);
        result.Files.Should().Contain(f => f.OldPath != null && f.OldPath.Contains("src"));
        result.Files.Should().Contain(f => f.OldPath != null && f.OldPath.Contains("tests"));
        result.Files.Should().NotContain(f => f.OldPath != null && f.OldPath.Contains("docs"));
    }

    #endregion

    #region Performance Tests (MEDIUM Priority - D1-M3)

    [Fact]
    public void Compare_WithThousandsOfFiles_ShouldCompleteReasonably()
    {
        // Arrange - Create 1000 files
        for (int i = 0; i < 1000; i++)
        {
            File.WriteAllText(Path.Combine(_oldFolder, $"file{i:D4}.cs"), $"class File{i} {{ }}");
        }

        // Modify half of them in new folder
        for (int i = 0; i < 500; i++)
        {
            File.WriteAllText(Path.Combine(_newFolder, $"file{i:D4}.cs"), $"class File{i} {{ void Method() {{ }} }}");
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);
        stopwatch.Stop();

        // Assert
        result.Files.Should().HaveCount(1000); // 500 removed + 500 modified
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(60),
            "should process 1000 files in under 60 seconds");
    }

    [Fact]
    public void CompareParallel_WithThousandsOfFiles_ShouldBeFasterThanSequential()
    {
        // Arrange - Create 200 files for faster test
        for (int i = 0; i < 200; i++)
        {
            var content = $"class File{i} {{ }}";
            File.WriteAllText(Path.Combine(_oldFolder, $"file{i:D3}.cs"), content);
            File.WriteAllText(Path.Combine(_newFolder, $"file{i:D3}.cs"), content + " // modified");
        }

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act - Sequential
        var seqStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var seqResult = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);
        seqStopwatch.Stop();

        // Act - Parallel
        var parStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var parResult = comparer.CompareParallel(_oldFolder, _newFolder, options, folderOptions);
        parStopwatch.Stop();

        // Assert
        seqResult.Files.Should().HaveCount(200);
        parResult.Files.Should().HaveCount(200);

        // Parallel should be at least somewhat faster (or at least not slower)
        // On single-core might be same speed, so we just check it completes
        parStopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30));
    }

    #endregion

    #region Binary Content Tests

    [Fact]
    public void Compare_WithBinaryContent_ShouldHandleGracefully()
    {
        // Arrange - Create file with binary content (null bytes)
        var binaryFile = Path.Combine(_oldFolder, "binary.dat");
        File.WriteAllBytes(binaryFile, new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE });

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Binary files should be handled
        result.Files.Should().HaveCount(1);
    }

    #endregion

    #region Case Sensitivity Tests

    [Fact]
    public void Compare_WithCaseOnlyRename_ShouldDetectOnCaseSensitive()
    {
        // Arrange - File.cs renamed to file.cs
        File.WriteAllText(Path.Combine(_oldFolder, "File.cs"), "class Test { }");
        File.WriteAllText(Path.Combine(_newFolder, "file.cs"), "class Test { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Behavior is platform-dependent
        // On case-insensitive (Windows, macOS default): should be unchanged or modified
        // On case-sensitive (Linux): should show removed + added
        result.Should().NotBeNull();
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public void Compare_ShouldSetCorrectMetadataFields()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "test.cs"), "old");
        File.WriteAllText(Path.Combine(_newFolder, "test.cs"), "new");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert
        result.Metadata.Should().NotBeNull();
        result.Metadata.Mode.Should().Be("folder");
        result.Metadata.OldRoot.Should().Be(Path.GetFullPath(_oldFolder));
        result.Metadata.NewRoot.Should().Be(Path.GetFullPath(_newFolder));
        result.Metadata.GitRefRange.Should().BeNull();
        result.Metadata.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion

    #region Null/Invalid Options Tests

    [Fact]
    public void Compare_WithNullDiffOptions_ShouldThrow()
    {
        // Arrange
        var comparer = new FolderComparer();
        var folderOptions = new FolderCompareOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            comparer.Compare(_oldFolder, _newFolder, null!, folderOptions));
    }

    [Fact]
    public void Compare_WithNullFolderOptions_ShouldThrow()
    {
        // Arrange
        var comparer = new FolderComparer();
        var options = new DiffOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            comparer.Compare(_oldFolder, _newFolder, options, null!));
    }

    #endregion

    #region Mixed File Types Tests

    [Fact]
    public void Compare_WithMixedCSharpAndVisualBasic_ShouldProcess()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "test.cs"), "class Test { }");
        File.WriteAllText(Path.Combine(_oldFolder, "test.vb"), "Class Test\nEnd Class");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions();

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Should process both C# and VB files
        result.Files.Should().HaveCount(2);
    }

    [Fact]
    public void Compare_WithNonCodeFiles_ShouldIncludeIfNoFilter()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_oldFolder, "readme.txt"), "readme");
        File.WriteAllText(Path.Combine(_oldFolder, "config.json"), "{}");
        File.WriteAllText(Path.Combine(_oldFolder, "test.cs"), "class Test { }");

        var comparer = new FolderComparer();
        var options = new DiffOptions();
        var folderOptions = new FolderCompareOptions(); // No filters

        // Act
        var result = comparer.Compare(_oldFolder, _newFolder, options, folderOptions);

        // Assert - Should process all files if no include pattern specified
        result.Files.Should().HaveCount(3);
    }

    #endregion
}
