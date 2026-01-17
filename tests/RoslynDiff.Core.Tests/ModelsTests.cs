namespace RoslynDiff.Core.Tests;

using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for core model types.
/// </summary>
public class ModelsTests
{
    [Fact]
    public void DiffResult_DefaultValues_ShouldBeEmpty()
    {
        // Arrange & Act
        var result = new DiffResult();

        // Assert
        result.OldPath.Should().BeNull();
        result.NewPath.Should().BeNull();
        result.FileChanges.Should().BeEmpty();
        result.Stats.Should().NotBeNull();
    }

    [Fact]
    public void DiffStats_DefaultValues_ShouldBeZero()
    {
        // Arrange & Act
        var stats = new DiffStats();

        // Assert
        stats.TotalChanges.Should().Be(0);
        stats.Additions.Should().Be(0);
        stats.Deletions.Should().Be(0);
        stats.Modifications.Should().Be(0);
        stats.Moves.Should().Be(0);
        stats.Renames.Should().Be(0);
    }

    [Fact]
    public void Change_WithAllProperties_ShouldBeCreatedCorrectly()
    {
        // Arrange & Act
        var change = new Change
        {
            Type = ChangeType.Modified,
            Kind = ChangeKind.Method,
            Name = "TestMethod",
            OldLocation = new Location { StartLine = 10, EndLine = 20, StartColumn = 1, EndColumn = 1 },
            NewLocation = new Location { StartLine = 15, EndLine = 25, StartColumn = 1, EndColumn = 1 },
            OldContent = "void TestMethod() { }",
            NewContent = "void TestMethod() { return; }"
        };

        // Assert
        change.Type.Should().Be(ChangeType.Modified);
        change.Kind.Should().Be(ChangeKind.Method);
        change.Name.Should().Be("TestMethod");
        change.OldLocation.Should().NotBeNull();
        change.NewLocation.Should().NotBeNull();
    }

    [Fact]
    public void DiffOptions_DefaultContextLines_ShouldBeThree()
    {
        // Arrange & Act
        var options = new DiffOptions();

        // Assert
        options.ContextLines.Should().Be(3);
        options.IgnoreWhitespace.Should().BeFalse();
        options.IgnoreComments.Should().BeFalse();
    }

    #region DiffMode Tests

    [Fact]
    public void DiffMode_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<DiffMode>().Should().Contain(DiffMode.Line);
        Enum.GetValues<DiffMode>().Should().Contain(DiffMode.Roslyn);
    }

    [Fact]
    public void DiffOptions_ModeDefaultsToNull()
    {
        // Arrange & Act
        var options = new DiffOptions();

        // Assert
        options.Mode.Should().BeNull();
    }

    [Fact]
    public void DiffOptions_WithMode_SetsCorrectly()
    {
        // Arrange & Act
        var options = new DiffOptions { Mode = DiffMode.Line };

        // Assert
        options.Mode.Should().Be(DiffMode.Line);
    }

    #endregion

    #region ChangeType Tests

    [Fact]
    public void ChangeType_HasAllExpectedValues()
    {
        // Assert
        var values = Enum.GetValues<ChangeType>();
        values.Should().Contain(ChangeType.Added);
        values.Should().Contain(ChangeType.Removed);
        values.Should().Contain(ChangeType.Modified);
        values.Should().Contain(ChangeType.Unchanged);
        values.Should().Contain(ChangeType.Moved);
        values.Should().Contain(ChangeType.Renamed);
    }

    #endregion

    #region ChangeKind Tests

    [Fact]
    public void ChangeKind_HasSemanticAndLineValues()
    {
        // Assert
        var values = Enum.GetValues<ChangeKind>();
        values.Should().Contain(ChangeKind.Line);
        values.Should().Contain(ChangeKind.Class);
        values.Should().Contain(ChangeKind.Method);
        values.Should().Contain(ChangeKind.Property);
        values.Should().Contain(ChangeKind.Field);
        values.Should().Contain(ChangeKind.Namespace);
    }

    #endregion

    #region Location Tests

    [Fact]
    public void Location_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var loc1 = new Location { StartLine = 10, EndLine = 20, StartColumn = 1, EndColumn = 50 };
        var loc2 = new Location { StartLine = 10, EndLine = 20, StartColumn = 1, EndColumn = 50 };
        var loc3 = new Location { StartLine = 10, EndLine = 21, StartColumn = 1, EndColumn = 50 };

        // Assert
        loc1.Should().Be(loc2);
        loc1.Should().NotBe(loc3);
    }

    [Fact]
    public void Location_WithExpression_CreatesNewRecord()
    {
        // Arrange
        var original = new Location { StartLine = 10, EndLine = 20, StartColumn = 1, EndColumn = 50 };

        // Act
        var modified = original with { EndLine = 25 };

        // Assert
        original.EndLine.Should().Be(20);
        modified.EndLine.Should().Be(25);
        modified.StartLine.Should().Be(10);
    }

    #endregion

    #region FileChange Tests

    [Fact]
    public void FileChange_DefaultValues_ShouldBeEmpty()
    {
        // Arrange & Act
        var fileChange = new FileChange();

        // Assert
        fileChange.Path.Should().BeNull();
        fileChange.Changes.Should().BeEmpty();
    }

    [Fact]
    public void FileChange_WithChanges_ContainsExpectedChanges()
    {
        // Arrange
        var changes = new List<Change>
        {
            new() { Type = ChangeType.Added, Kind = ChangeKind.Line },
            new() { Type = ChangeType.Removed, Kind = ChangeKind.Line }
        };

        // Act
        var fileChange = new FileChange
        {
            Path = "test.cs",
            Changes = changes
        };

        // Assert
        fileChange.Path.Should().Be("test.cs");
        fileChange.Changes.Should().HaveCount(2);
        fileChange.Changes.Should().Contain(c => c.Type == ChangeType.Added);
        fileChange.Changes.Should().Contain(c => c.Type == ChangeType.Removed);
    }

    #endregion

    #region DiffResult Tests

    [Fact]
    public void DiffResult_WithFileChanges_CalculatesTotalCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "old.cs",
            NewPath = "new.cs",
            Mode = DiffMode.Line,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change { Type = ChangeType.Added },
                        new Change { Type = ChangeType.Removed }
                    ]
                }
            ],
            Stats = new DiffStats
            {
                Additions = 1,
                Deletions = 1
            }
        };

        // Assert
        result.FileChanges.Should().HaveCount(1);
        result.FileChanges[0].Changes.Should().HaveCount(2);
        result.Stats.TotalChanges.Should().Be(2);
    }

    [Fact]
    public void DiffResult_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var stats = new DiffStats();
        var result1 = new DiffResult { OldPath = "a.cs", NewPath = "b.cs", Stats = stats };
        var result2 = new DiffResult { OldPath = "a.cs", NewPath = "b.cs", Stats = stats };

        // Assert - Records with same values are equal
        result1.Should().Be(result2);
    }

    #endregion

    #region DiffOptions Tests

    [Fact]
    public void DiffOptions_WithPaths_SetsCorrectly()
    {
        // Arrange & Act
        var options = new DiffOptions
        {
            OldPath = "old/path.cs",
            NewPath = "new/path.cs"
        };

        // Assert
        options.OldPath.Should().Be("old/path.cs");
        options.NewPath.Should().Be("new/path.cs");
    }

    [Fact]
    public void DiffOptions_WithAllSettings_SetsCorrectly()
    {
        // Arrange & Act
        var options = new DiffOptions
        {
            Mode = DiffMode.Roslyn,
            IgnoreWhitespace = true,
            IgnoreComments = true,
            ContextLines = 5,
            OldPath = "old.cs",
            NewPath = "new.cs"
        };

        // Assert
        options.Mode.Should().Be(DiffMode.Roslyn);
        options.IgnoreWhitespace.Should().BeTrue();
        options.IgnoreComments.Should().BeTrue();
        options.ContextLines.Should().Be(5);
    }

    #endregion

    #region Change Tests

    [Fact]
    public void Change_ForAddition_HasNullOldLocation()
    {
        // Arrange & Act
        var change = new Change
        {
            Type = ChangeType.Added,
            Kind = ChangeKind.Line,
            NewContent = "new line",
            NewLocation = new Location { StartLine = 10, EndLine = 10 }
        };

        // Assert
        change.OldLocation.Should().BeNull();
        change.OldContent.Should().BeNull();
        change.NewLocation.Should().NotBeNull();
        change.NewContent.Should().Be("new line");
    }

    [Fact]
    public void Change_ForDeletion_HasNullNewLocation()
    {
        // Arrange & Act
        var change = new Change
        {
            Type = ChangeType.Removed,
            Kind = ChangeKind.Line,
            OldContent = "old line",
            OldLocation = new Location { StartLine = 10, EndLine = 10 }
        };

        // Assert
        change.NewLocation.Should().BeNull();
        change.NewContent.Should().BeNull();
        change.OldLocation.Should().NotBeNull();
        change.OldContent.Should().Be("old line");
    }

    #endregion
}
