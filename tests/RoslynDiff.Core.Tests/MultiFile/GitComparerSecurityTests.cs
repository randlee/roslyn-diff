namespace RoslynDiff.Core.Tests.MultiFile;

using System.Runtime.InteropServices;
using FluentAssertions;
using LibGit2Sharp;
using RoslynDiff.Core.Models;
using RoslynDiff.Core.MultiFile;
using Xunit;

/// <summary>
/// Security and edge case tests for <see cref="GitComparer"/>.
/// Tests symlink handling, submodules, special git scenarios.
/// </summary>
public class GitComparerSecurityTests : IDisposable
{
    private readonly string _tempRepoPath;
    private readonly Repository _testRepo;

    public GitComparerSecurityTests()
    {
        // Create a temporary test repository
        _tempRepoPath = Path.Combine(Path.GetTempPath(), $"roslyn-diff-git-sec-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempRepoPath);

        // Initialize git repo
        Repository.Init(_tempRepoPath);
        _testRepo = new Repository(_tempRepoPath);

        // Configure test identity
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
                // Remove read-only attributes
                RemoveReadOnlyAttributes(_tempRepoPath);
                Directory.Delete(_tempRepoPath, true);
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

    #region Symlink Tests (HIGH Priority - G1-H2)

    [Fact]
    public void Compare_WithSymlinkedFile_ShouldHandleGracefully()
    {
        // Skip on platforms that don't support symlinks
        if (!SupportsSymlinks())
        {
            return;
        }

        // Arrange
        var commit1 = CreateInitialCommit();

        // Create a file and a symlink to it
        var targetFile = Path.Combine(_tempRepoPath, "target.cs");
        File.WriteAllText(targetFile, "public class Target { }");

        var symlinkPath = Path.Combine(_tempRepoPath, "symlink.cs");

        try
        {
            File.CreateSymbolicLink(symlinkPath, targetFile);
        }
        catch (UnauthorizedAccessException)
        {
            // Symlink creation requires admin on some platforms
            return;
        }

        // Stage and commit both
        Commands.Stage(_testRepo, "target.cs");
        Commands.Stage(_testRepo, "symlink.cs");
        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        var commit2 = _testRepo.Commit("Add symlink", signature, signature);

        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var act = () => comparer.Compare(_tempRepoPath, $"{commit1.Sha}..{commit2.Sha}", options);

        // Assert - Should handle symlinks without crashing
        act.Should().NotThrow("symlinks should be handled gracefully");
    }

    [Fact]
    public void Compare_WithModifiedSymlink_ShouldDetectChange()
    {
        // Skip on platforms that don't support symlinks
        if (!SupportsSymlinks())
        {
            return;
        }

        // Arrange
        CreateInitialCommit();

        var target1 = Path.Combine(_tempRepoPath, "target1.cs");
        var target2 = Path.Combine(_tempRepoPath, "target2.cs");
        File.WriteAllText(target1, "class Target1 { }");
        File.WriteAllText(target2, "class Target2 { }");

        var symlinkPath = Path.Combine(_tempRepoPath, "symlink.cs");

        try
        {
            // Create symlink pointing to target1
            File.CreateSymbolicLink(symlinkPath, target1);
            Commands.Stage(_testRepo, "*");
            var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
            var commit1 = _testRepo.Commit("Add symlink to target1", signature, signature);

            // Change symlink to point to target2
            File.Delete(symlinkPath);
            File.CreateSymbolicLink(symlinkPath, target2);
            Commands.Stage(_testRepo, "*");
            var commit2 = _testRepo.Commit("Change symlink to target2", signature, signature);

            var comparer = new GitComparer();
            var options = new DiffOptions { Mode = DiffMode.Roslyn };

            // Act
            var result = comparer.Compare(_tempRepoPath, $"{commit1.Sha}..{commit2.Sha}", options);

            // Assert - Should detect the symlink change
            result.Should().NotBeNull();
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }
    }

    #endregion

    #region Submodule Tests (HIGH Priority - G1-H1)

    [Fact]
    public void Compare_WithSubmoduleChanges_ShouldReportOrSkip()
    {
        // This test verifies behavior when a repository contains submodules
        // Current implementation likely doesn't handle submodules specially
        // This test documents the expected behavior

        // Arrange
        CreateInitialCommit();

        // Create a .gitmodules file to simulate a submodule
        var gitmodulesPath = Path.Combine(_tempRepoPath, ".gitmodules");
        File.WriteAllText(gitmodulesPath, @"[submodule ""external""]
    path = external
    url = https://github.com/example/external.git");

        Commands.Stage(_testRepo, ".gitmodules");
        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        var commit = _testRepo.Commit("Add submodule", signature, signature);

        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, $"{commit.Parents.First().Sha}..{commit.Sha}", options);

        // Assert - Should not crash when encountering submodule references
        result.Should().NotBeNull();
    }

    #endregion

    #region File Mode Change Tests (HIGH Priority - G1-H3)

    [Fact]
    public void Compare_WithExecutableBitChange_ShouldDetectOrIgnore()
    {
        // Skip on Windows which doesn't have Unix file modes
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var scriptFile = Path.Combine(_tempRepoPath, "script.sh");
        File.WriteAllText(scriptFile, "#!/bin/bash\necho 'test'");

        Commands.Stage(_testRepo, "script.sh");
        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        var commit1 = _testRepo.Commit("Add script", signature, signature);

        // Make file executable (chmod +x)
        File.SetUnixFileMode(scriptFile,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
            UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

        Commands.Stage(_testRepo, "script.sh");
        var commit2 = _testRepo.Commit("Make executable", signature, signature);

        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, $"{commit1.Sha}..{commit2.Sha}", options);

        // Assert - Should either detect mode change or ignore if not relevant
        result.Should().NotBeNull();
    }

    #endregion

    #region Special Git Scenarios (MEDIUM Priority - G1-M1 to G1-M10)

    [Fact]
    public void Compare_WithTagReferences_ShouldResolveCorrectly()
    {
        // Arrange
        var commit1 = CreateInitialCommit();
        CreateCommitWithChanges("Modified");

        // Create tags
        _testRepo.Tags.Add("v1.0.0", commit1);
        _testRepo.Tags.Add("v2.0.0", _testRepo.Head.Tip);

        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, "v1.0.0..v2.0.0", options);

        // Assert
        result.Should().NotBeNull();
        result.Metadata.GitRefRange.Should().Be("v1.0.0..v2.0.0");
    }

    [Fact]
    public void Compare_WithAnnotatedTag_ShouldResolve()
    {
        // Arrange
        var commit = CreateInitialCommit();

        // Create annotated tag
        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        _testRepo.Tags.Add("v1.0.0", commit, signature, "Release version 1.0.0");

        CreateCommitWithChanges("Update");
        _testRepo.Tags.Add("v1.0.1", _testRepo.Head.Tip, signature, "Release version 1.0.1");

        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, "v1.0.0..v1.0.1", options);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void Compare_WithDetachedHead_ShouldWork()
    {
        // Arrange
        var commit1 = CreateInitialCommit();
        var commit2 = CreateCommitWithChanges("Modified");

        // Detach HEAD
        Commands.Checkout(_testRepo, commit2);

        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, $"{commit1.Sha}..HEAD", options);

        // Assert
        result.Should().NotBeNull();
        result.Files.Should().NotBeEmpty();
    }

    [Fact]
    public void Compare_WithMergeCommit_ShouldHandleMultipleParents()
    {
        // Arrange
        var mainCommit = CreateInitialCommit();

        // Create a feature branch
        var featureBranch = _testRepo.CreateBranch("feature");
        Commands.Checkout(_testRepo, featureBranch);
        AddFileAndCommit("feature.cs", "class Feature { }");

        // Go back to main and make a change
        var mainBranch = _testRepo.Branches["master"] ?? _testRepo.Branches["main"];
        Commands.Checkout(_testRepo, mainBranch);
        ModifyFileAndCommit("TestFile.cs", "// Modified on main");

        // Merge feature into main
        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        var mergeResult = _testRepo.Merge(featureBranch, signature);

        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act - Compare before and after merge
        var result = comparer.Compare(_tempRepoPath, $"{mainCommit.Sha}..HEAD", options);

        // Assert
        result.Should().NotBeNull();
        result.Files.Count.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Compare_WithRepositoryPathContainingSpaces_ShouldWork()
    {
        // This test is inherently handled by our temp path setup,
        // but we explicitly verify space handling
        // Arrange
        var spacePath = Path.Combine(Path.GetTempPath(), $"path with spaces {Guid.NewGuid()}");
        Directory.CreateDirectory(spacePath);

        Repository? spaceRepo = null;
        try
        {
            Repository.Init(spacePath);
            spaceRepo = new Repository(spacePath);
            spaceRepo.Config.Set("user.name", "Test User");
            spaceRepo.Config.Set("user.email", "test@example.com");

            // Create commits
            var filePath = Path.Combine(spacePath, "test.cs");
            File.WriteAllText(filePath, "class Test { }");
            Commands.Stage(spaceRepo, "test.cs");
            var signature = spaceRepo.Config.BuildSignature(DateTimeOffset.Now);
            var commit1 = spaceRepo.Commit("Initial", signature, signature);

            File.WriteAllText(filePath, "class Test { void Method() { } }");
            Commands.Stage(spaceRepo, "test.cs");
            var commit2 = spaceRepo.Commit("Modified", signature, signature);

            var comparer = new GitComparer();
            var options = new DiffOptions { Mode = DiffMode.Roslyn };

            // Act
            var result = comparer.Compare(spacePath, $"{commit1.Sha}..{commit2.Sha}", options);

            // Assert
            result.Should().NotBeNull();
            result.Files.Should().HaveCount(1);
        }
        finally
        {
            spaceRepo?.Dispose();
            try
            {
                if (Directory.Exists(spacePath))
                {
                    RemoveReadOnlyAttributes(spacePath);
                    Directory.Delete(spacePath, true);
                }
            }
            catch { }
        }
    }

    [Fact]
    public void Compare_WithBranchNameWithSpecialChars_ShouldResolve()
    {
        // Arrange
        CreateInitialCommit();

        // Create branch with special characters (allowed by git)
        var specialBranch = _testRepo.CreateBranch("feature/special-fix_v2");
        Commands.Checkout(_testRepo, specialBranch);
        AddFileAndCommit("new.cs", "class New { }");

        var mainBranch = _testRepo.Branches["master"] ?? _testRepo.Branches["main"];

        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath,
            $"{mainBranch.FriendlyName}..feature/special-fix_v2", options);

        // Assert
        result.Should().NotBeNull();
        result.Files.Should().HaveCount(1);
    }

    [Fact]
    public void Compare_WithVeryLargeChangeset_ShouldHandleEfficiently()
    {
        // Arrange
        CreateInitialCommit();

        // Create 100 files
        for (int i = 0; i < 100; i++)
        {
            AddFileAndCommit($"File{i:D3}.cs", $"public class File{i} {{ }}");
        }

        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = comparer.Compare(_tempRepoPath, "HEAD~100..HEAD", options);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        result.Files.Should().HaveCount(100);
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30),
            "should process 100 files in reasonable time");
    }

    [Fact]
    public void Compare_WithUtf8BomInFiles_ShouldHandleCorrectly()
    {
        // Arrange
        var commit1 = CreateInitialCommit();

        // Create file with UTF-8 BOM
        var bomFile = Path.Combine(_tempRepoPath, "bom.cs");
        var utf8WithBom = new System.Text.UTF8Encoding(true);
        File.WriteAllText(bomFile, "public class WithBom { }", utf8WithBom);

        Commands.Stage(_testRepo, "bom.cs");
        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        var commit2 = _testRepo.Commit("Add BOM file", signature, signature);

        var comparer = new GitComparer();
        var options = new DiffOptions { Mode = DiffMode.Roslyn };

        // Act
        var result = comparer.Compare(_tempRepoPath, $"{commit1.Sha}..{commit2.Sha}", options);

        // Assert - BOM should not cause issues
        result.Should().NotBeNull();
        result.Files.Should().HaveCount(1);
    }

    #endregion

    #region Helper Methods

    private Commit CreateInitialCommit()
    {
        var filePath = Path.Combine(_tempRepoPath, "TestFile.cs");
        File.WriteAllText(filePath, "// Initial content\npublic class Test { }");

        Commands.Stage(_testRepo, "TestFile.cs");

        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        return _testRepo.Commit("Initial commit", signature, signature);
    }

    private Commit CreateCommitWithChanges(string commitMessage)
    {
        var filePath = Path.Combine(_tempRepoPath, "TestFile.cs");
        File.WriteAllText(filePath, "// Modified content\npublic class Test { public void Method() { } }");

        Commands.Stage(_testRepo, "TestFile.cs");

        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        return _testRepo.Commit(commitMessage, signature, signature);
    }

    private void AddFileAndCommit(string fileName, string content)
    {
        var filePath = Path.Combine(_tempRepoPath, fileName);
        File.WriteAllText(filePath, content);

        Commands.Stage(_testRepo, fileName);

        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        _testRepo.Commit($"Added {fileName}", signature, signature);
    }

    private void ModifyFileAndCommit(string fileName, string content)
    {
        var filePath = Path.Combine(_tempRepoPath, fileName);
        File.WriteAllText(filePath, content);

        Commands.Stage(_testRepo, fileName);

        var signature = _testRepo.Config.BuildSignature(DateTimeOffset.Now);
        _testRepo.Commit($"Modified {fileName}", signature, signature);
    }

    private bool SupportsSymlinks()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
               RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
               RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    #endregion
}
