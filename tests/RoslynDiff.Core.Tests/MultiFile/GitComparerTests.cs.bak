namespace RoslynDiff.Core.Tests.MultiFile;

using System.Text;
using FluentAssertions;
using LibGit2Sharp;
using RoslynDiff.Core.Models;
using RoslynDiff.Core.MultiFile;
using Xunit;

/// <summary>
/// Unit tests for <see cref="GitComparer"/>.
/// </summary>
public class GitComparerTests : IDisposable
{
    private readonly string _tempRepoPath;
    private readonly Repository _testRepo;

    public GitComparerTests()
    {
        // Create a temporary test repository
        _tempRepoPath = Path.Combine(Path.GetTempPath(), $"roslyn-diff-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempRepoPath);

        // Initialize git repo
        Repository.Init(_tempRepoPath);
        _testRepo = new Repository(_tempRepoPath);

        // Configure test identity
        var signature = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
        _testRepo.Config.Set("user.name", "Test User");
        _testRepo.Config.Set("user.email", "test@example.com");
    }

    public void Dispose()
    {
        _testRepo?.Dispose();

        // Clean up temp directory
        if (Directory.Exists(_tempRepoPath))
        {
            try
            {
                Directory.Delete(_tempRepoPath, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    #region Git Ref Range Parsing Tests

    [Fact]
    public void Compare_WithValidRefRange_ShouldParseCorrectly()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");
        CreateBranchAndCommit("feature");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, "main..feature", options);

        // Assert
        result.Should().NotBeNull();
        result.Metadata.GitRefRange.Should().Be("main..feature");
        result.Metadata.Mode.Should().Be("git");
    }

    [Fact]
    public void Compare_WithCommitShas_ShouldParseCorrectly()
    {
        // Arrange
        var comparer = new GitComparer();
        var commit1 = CreateInitialCommit("main");
        var commit2 = CreateCommitWithChanges("Modified file");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };
        var refRange = $"{commit1.Sha.Substring(0, 7)}..{commit2.Sha.Substring(0, 7)}";

        // Act
        var result = comparer.Compare(_tempRepoPath, refRange, options);

        // Assert
        result.Should().NotBeNull();
        result.Metadata.GitRefRange.Should().Be(refRange);
    }

    [Fact]
    public void Compare_WithFullCommitShas_ShouldWork()
    {
        // Arrange
        var comparer = new GitComparer();
        var commit1 = CreateInitialCommit("main");
        var commit2 = CreateCommitWithChanges("Modified file");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };
        var refRange = $"{commit1.Sha}..{commit2.Sha}";

        // Act
        var result = comparer.Compare(_tempRepoPath, refRange, options);

        // Assert
        result.Should().NotBeNull();
        result.Files.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("invalid-format")]
    [InlineData("only-one-part")]
    [InlineData("too...many...dots")]
    [InlineData("")]
    public void Compare_WithInvalidRefRangeFormat_ShouldThrowArgumentException(string refRange)
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var act = () => comparer.Compare(_tempRepoPath, refRange, options);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid ref range format*");
    }

    [Fact]
    public void Compare_WithInvalidOldRef_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var act = () => comparer.Compare(_tempRepoPath, "nonexistent..main", options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Could not resolve reference 'nonexistent'*");
    }

    [Fact]
    public void Compare_WithInvalidNewRef_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var act = () => comparer.Compare(_tempRepoPath, "main..nonexistent", options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Could not resolve reference 'nonexistent'*");
    }

    [Fact]
    public void Compare_WithNullRepositoryPath_ShouldThrowArgumentNullException()
    {
        // Arrange
        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var act = () => comparer.Compare(null!, "main..feature", options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Compare_WithNullRefRange_ShouldThrowArgumentNullException()
    {
        // Arrange
        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var act = () => comparer.Compare(_tempRepoPath, null!, options);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Compare_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        // Act
        var act = () => comparer.Compare(_tempRepoPath, "main..main", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Compare_WithNonexistentRepository_ShouldThrowRepositoryNotFoundException()
    {
        // Arrange
        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };
        var nonexistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var act = () => comparer.Compare(nonexistentPath, "main..feature", options);

        // Assert
        act.Should().Throw<RepositoryNotFoundException>();
    }

    #endregion

    #region File Status Detection Tests

    [Fact]
    public void Compare_WithModifiedFile_ShouldDetectModifiedStatus()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");
        var fileName = "TestFile.cs";
        ModifyFileAndCommit(fileName, "// Modified content");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, "HEAD~1..HEAD", options);

        // Assert
        result.Files.Should().HaveCount(1);
        result.Files[0].Status.Should().Be(FileChangeStatus.Modified);
        result.Files[0].OldPath.Should().Be(fileName);
        result.Files[0].NewPath.Should().Be(fileName);
        result.Summary.ModifiedFiles.Should().Be(1);
    }

    [Fact]
    public void Compare_WithAddedFile_ShouldDetectAddedStatus()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");
        var newFileName = "NewFile.cs";
        AddFileAndCommit(newFileName, "// New file content");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, "HEAD~1..HEAD", options);

        // Assert
        result.Files.Should().HaveCount(1);
        result.Files[0].Status.Should().Be(FileChangeStatus.Added);
        result.Files[0].OldPath.Should().BeNull();
        result.Files[0].NewPath.Should().Be(newFileName);
        result.Summary.AddedFiles.Should().Be(1);
    }

    [Fact]
    public void Compare_WithRemovedFile_ShouldDetectRemovedStatus()
    {
        // Arrange
        var comparer = new GitComparer();
        var fileName = "ToRemove.cs";
        CreateFileAndCommit(fileName, "// File to remove");
        RemoveFileAndCommit(fileName);

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, "HEAD~1..HEAD", options);

        // Assert
        result.Files.Should().HaveCount(1);
        result.Files[0].Status.Should().Be(FileChangeStatus.Removed);
        result.Files[0].OldPath.Should().Be(fileName);
        result.Files[0].NewPath.Should().BeNull();
        result.Summary.RemovedFiles.Should().Be(1);
    }

    [Fact]
    public void Compare_WithRenamedFile_ShouldDetectRenamedStatus()
    {
        // Arrange
        var comparer = new GitComparer();
        var oldFileName = "OldName.cs";
        var newFileName = "NewName.cs";
        CreateFileAndCommit(oldFileName, "// File content");
        RenameFileAndCommit(oldFileName, newFileName);

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, "HEAD~1..HEAD", options);

        // Assert
        result.Files.Should().HaveCount(1);
        result.Files[0].Status.Should().Be(FileChangeStatus.Renamed);
        result.Files[0].OldPath.Should().Be(oldFileName);
        result.Files[0].NewPath.Should().Be(newFileName);
        result.Summary.RenamedFiles.Should().Be(1);
    }

    [Fact]
    public void Compare_WithMultipleFileStatuses_ShouldDetectAllCorrectly()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        // Create a commit with multiple changes
        AddFileAndCommit("Added.cs", "// Added file");
        ModifyFileAndCommit("TestFile.cs", "// Modified content");
        AddFileAndCommit("ToRemove.cs", "// Will be removed");
        _testRepo.Index.Remove("ToRemove.cs");
        _testRepo.Index.Write();
        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        _testRepo.Commit("Multiple changes", signature, signature);

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, "HEAD~4..HEAD", options);

        // Assert
        result.Files.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Summary.TotalFiles.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Parallel Processing Tests

    [Fact]
    public void CompareParallel_WithMultipleFiles_ShouldReturnSameResultAsSequential()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        // Create multiple files
        for (int i = 0; i < 10; i++)
        {
            AddFileAndCommit($"File{i}.cs", $"// Content {i}");
        }

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var sequentialResult = comparer.Compare(_tempRepoPath, "HEAD~10..HEAD", options);
        var parallelResult = comparer.CompareParallel(_tempRepoPath, "HEAD~10..HEAD", options);

        // Assert
        parallelResult.Files.Should().HaveCount(sequentialResult.Files.Count);
        parallelResult.Summary.TotalFiles.Should().Be(sequentialResult.Summary.TotalFiles);
        parallelResult.Summary.AddedFiles.Should().Be(sequentialResult.Summary.AddedFiles);
    }

    [Fact]
    public void CompareParallel_WithValidRefRange_ShouldWork()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");
        CreateBranchAndCommit("feature");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.CompareParallel(_tempRepoPath, "main..feature", options);

        // Assert
        result.Should().NotBeNull();
        result.Metadata.GitRefRange.Should().Be("main..feature");
    }

    [Fact]
    public void CompareParallel_WithNullParameters_ShouldThrowArgumentNullException()
    {
        // Arrange
        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => comparer.CompareParallel(null!, "main..feature", options));
        Assert.Throws<ArgumentNullException>(() => comparer.CompareParallel(_tempRepoPath, null!, options));
        Assert.Throws<ArgumentNullException>(() => comparer.CompareParallel(_tempRepoPath, "main..feature", null!));
    }

    [Fact]
    public void CompareParallel_ShouldSortFilesByPath()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        // Add files in non-alphabetical order
        AddFileAndCommit("Zebra.cs", "// Z");
        AddFileAndCommit("Alpha.cs", "// A");
        AddFileAndCommit("Beta.cs", "// B");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.CompareParallel(_tempRepoPath, "HEAD~3..HEAD", options);

        // Assert
        result.Files.Should().HaveCount(3);
        // Results should be sorted by path
        result.Files[0].NewPath.Should().Be("Alpha.cs");
        result.Files[1].NewPath.Should().Be("Beta.cs");
        result.Files[2].NewPath.Should().Be("Zebra.cs");
    }

    #endregion

    #region Summary Statistics Tests

    [Fact]
    public void Compare_ShouldCalculateCorrectSummaryStatistics()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        AddFileAndCommit("Added1.cs", "// Added file 1");
        AddFileAndCommit("Added2.cs", "// Added file 2");
        ModifyFileAndCommit("TestFile.cs", "// Modified");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, "HEAD~3..HEAD", options);

        // Assert
        result.Summary.TotalFiles.Should().Be(3);
        result.Summary.AddedFiles.Should().Be(2);
        result.Summary.ModifiedFiles.Should().Be(1);
        result.Summary.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Compare_WithNoChanges_ShouldReturnEmptyResult()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, "HEAD..HEAD", options);

        // Assert
        result.Files.Should().BeEmpty();
        result.Summary.TotalFiles.Should().Be(0);
        result.Summary.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Compare_ShouldAggregateImpactBreakdown()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        // Add a file with a public class change (breaking)
        AddFileAndCommit("PublicClass.cs", @"
public class TestClass
{
    public void Method1() { }
}");

        ModifyFileAndCommit("PublicClass.cs", @"
public class TestClass
{
    public void Method1(int param) { }  // Breaking change - signature changed
}");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, "HEAD~1..HEAD", options);

        // Assert
        result.Summary.ImpactBreakdown.Should().NotBeNull();
        // At minimum, we should have some impact classification
        var totalImpact = result.Summary.ImpactBreakdown.BreakingPublicApi +
                         result.Summary.ImpactBreakdown.BreakingInternalApi +
                         result.Summary.ImpactBreakdown.NonBreaking +
                         result.Summary.ImpactBreakdown.FormattingOnly;
        totalImpact.Should().BeGreaterThan(0);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void Compare_WithBinaryFile_ShouldHandleGracefully()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        // Add a binary file
        var binaryPath = Path.Combine(_tempRepoPath, "binary.dll");
        File.WriteAllBytes(binaryPath, new byte[] { 0x4D, 0x5A, 0x90, 0x00 }); // PE header
        Commands.Stage(_testRepo, "binary.dll");
        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        _testRepo.Commit("Add binary", signature, signature);

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, "HEAD~1..HEAD", options);

        // Assert
        // Binary files should either be skipped or handled as empty content
        result.Files.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_WithInvalidCSharpSyntax_ShouldNotCrash()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        AddFileAndCommit("Invalid.cs", "this is not valid C# code { { {");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var act = () => comparer.Compare(_tempRepoPath, "HEAD~1..HEAD", options);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Compare_WithEmptyFile_ShouldHandleGracefully()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        AddFileAndCommit("Empty.cs", "");

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, "HEAD~1..HEAD", options);

        // Assert
        result.Files.Should().HaveCount(1);
        result.Files[0].Status.Should().Be(FileChangeStatus.Added);
    }

    [Fact]
    public void Compare_WithLargeFile_ShouldHandleGracefully()
    {
        // Arrange
        var comparer = new GitComparer();
        CreateInitialCommit("main");

        // Create a large C# file
        var largeContent = new StringBuilder();
        largeContent.AppendLine("public class LargeClass {");
        for (int i = 0; i < 1000; i++)
        {
            largeContent.AppendLine($"    public void Method{i}() {{ }}");
        }
        largeContent.AppendLine("}");

        AddFileAndCommit("Large.cs", largeContent.ToString());

        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var act = () => comparer.Compare(_tempRepoPath, "HEAD~1..HEAD", options);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Helper Methods

    private Commit CreateInitialCommit(string branchName)
    {
        var filePath = Path.Combine(_tempRepoPath, "TestFile.cs");
        File.WriteAllText(filePath, "// Initial content\npublic class Test { }");

        Commands.Stage(_testRepo, "TestFile.cs");

        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        var commit = _testRepo.Commit("Initial commit", signature, signature);

        // Get the current branch (usually "master")
        var currentBranch = _testRepo.Head;

        // If we want a different main branch name, create it
        if ((branchName == "main" || branchName == "master") && currentBranch.FriendlyName != branchName)
        {
            var newBranch = _testRepo.Branches.Add(branchName, commit);
            Commands.Checkout(_testRepo, newBranch);
        }
        else if (branchName != "master" && branchName != "main")
        {
            _testRepo.Branches.Add(branchName, commit);
        }

        return commit;
    }

    private void CreateBranchAndCommit(string branchName)
    {
        var branch = _testRepo.CreateBranch(branchName);
        Commands.Checkout(_testRepo, branch);

        var filePath = Path.Combine(_tempRepoPath, "FeatureFile.cs");
        File.WriteAllText(filePath, "// Feature content\npublic class Feature { }");

        Commands.Stage(_testRepo, "FeatureFile.cs");

        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        _testRepo.Commit($"Commit on {branchName}", signature, signature);

        Commands.Checkout(_testRepo, _testRepo.Branches["master"] ?? _testRepo.Branches["main"]);
    }

    private Commit CreateCommitWithChanges(string commitMessage)
    {
        var filePath = Path.Combine(_tempRepoPath, "TestFile.cs");
        File.WriteAllText(filePath, "// Modified content\npublic class Test { public void Method() { } }");

        Commands.Stage(_testRepo, "TestFile.cs");

        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        return _testRepo.Commit(commitMessage, signature, signature);
    }

    private void ModifyFileAndCommit(string fileName, string content)
    {
        var filePath = Path.Combine(_tempRepoPath, fileName);
        File.WriteAllText(filePath, content);

        Commands.Stage(_testRepo, fileName);

        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        _testRepo.Commit($"Modified {fileName}", signature, signature);
    }

    private void AddFileAndCommit(string fileName, string content)
    {
        var filePath = Path.Combine(_tempRepoPath, fileName);
        File.WriteAllText(filePath, content);

        Commands.Stage(_testRepo, fileName);

        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        _testRepo.Commit($"Added {fileName}", signature, signature);
    }

    private void CreateFileAndCommit(string fileName, string content)
    {
        var filePath = Path.Combine(_tempRepoPath, fileName);
        File.WriteAllText(filePath, content);

        Commands.Stage(_testRepo, fileName);

        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        _testRepo.Commit($"Created {fileName}", signature, signature);
    }

    private void RemoveFileAndCommit(string fileName)
    {
        var filePath = Path.Combine(_tempRepoPath, fileName);
        File.Delete(filePath);

        Commands.Remove(_testRepo, fileName);

        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        _testRepo.Commit($"Removed {fileName}", signature, signature);
    }

    private void RenameFileAndCommit(string oldName, string newName)
    {
        var oldPath = Path.Combine(_tempRepoPath, oldName);
        var newPath = Path.Combine(_tempRepoPath, newName);

        File.Move(oldPath, newPath);

        Commands.Remove(_testRepo, oldName);
        Commands.Stage(_testRepo, newName);

        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        _testRepo.Commit($"Renamed {oldName} to {newName}", signature, signature);
    }

    #endregion
}
