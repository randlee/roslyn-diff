namespace RoslynDiff.Core.Tests.Tfm;

using FluentAssertions;
using RoslynDiff.Core.Models;
using RoslynDiff.Core.Tfm;
using Xunit;

/// <summary>
/// Unit tests for <see cref="TfmResultMerger"/>.
/// </summary>
public class TfmResultMergerTests
{
    #region Empty and Single TFM Cases

    [Fact]
    public void Merge_EmptyList_ReturnsEmptyResult()
    {
        // Arrange
        var tfmResults = new List<(string Tfm, DiffResult Result)>();
        var options = new DiffOptions();

        // Act
        var result = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        result.Should().NotBeNull();
        result.Mode.Should().Be(DiffMode.Roslyn);
        result.AnalyzedTfms.Should().NotBeNull().And.BeEmpty();
        result.FileChanges.Should().BeEmpty();
    }

    [Fact]
    public void Merge_NullList_ReturnsEmptyResult()
    {
        // Arrange
        List<(string Tfm, DiffResult Result)> tfmResults = null!;
        var options = new DiffOptions();

        // Act
        var result = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        result.Should().NotBeNull();
        result.Mode.Should().Be(DiffMode.Roslyn);
        result.AnalyzedTfms.Should().NotBeNull().And.BeEmpty();
        result.FileChanges.Should().BeEmpty();
    }

    [Fact]
    public void Merge_SingleTfm_PreservesResultWithAnalyzedTfms()
    {
        // Arrange
        var change = CreateChange(ChangeType.Added, "NewMethod", ChangeKind.Method);
        var fileChange = CreateFileChange("Test.cs", change);
        var diffResult = CreateDiffResult([fileChange], additions: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", diffResult)
        };
        var options = new DiffOptions();

        // Act
        var result = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        result.AnalyzedTfms.Should().ContainSingle().Which.Should().Be("net8.0");
        result.FileChanges.Should().HaveCount(1);
        result.FileChanges[0].Changes.Should().HaveCount(1);
    }

    #endregion

    #region Two TFMs - Identical Changes

    [Fact]
    public void Merge_TwoTfmsWithIdenticalChanges_SetsEmptyApplicableToTfms()
    {
        // Arrange
        var change1 = CreateChange(ChangeType.Added, "NewMethod", ChangeKind.Method);
        var change2 = CreateChange(ChangeType.Added, "NewMethod", ChangeKind.Method);

        var fileChange1 = CreateFileChange("Test.cs", change1);
        var fileChange2 = CreateFileChange("Test.cs", change2);

        var result1 = CreateDiffResult([fileChange1], additions: 1);
        var result2 = CreateDiffResult([fileChange2], additions: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result1),
            ("net10.0", result2)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.AnalyzedTfms.Should().BeEquivalentTo(["net8.0", "net10.0"]);
        merged.FileChanges.Should().HaveCount(1);
        merged.FileChanges[0].Changes.Should().HaveCount(1);
        merged.FileChanges[0].Changes[0].ApplicableToTfms.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Merge_TwoTfmsWithIdenticalChangesAndLocations_SetsEmptyApplicableToTfms()
    {
        // Arrange
        var location = CreateLocation("Test.cs", 10, 15);
        var change1 = CreateChange(ChangeType.Added, "NewMethod", ChangeKind.Method, newLocation: location);
        var change2 = CreateChange(ChangeType.Added, "NewMethod", ChangeKind.Method, newLocation: location);

        var fileChange1 = CreateFileChange("Test.cs", change1);
        var fileChange2 = CreateFileChange("Test.cs", change2);

        var result1 = CreateDiffResult([fileChange1], additions: 1);
        var result2 = CreateDiffResult([fileChange2], additions: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result1),
            ("net10.0", result2)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.FileChanges[0].Changes[0].ApplicableToTfms.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region Two TFMs - Different Changes

    [Fact]
    public void Merge_TwoTfmsWithDifferentChanges_SetsSpecificApplicableToTfms()
    {
        // Arrange
        var change1 = CreateChange(ChangeType.Added, "Net8Method", ChangeKind.Method);
        var change2 = CreateChange(ChangeType.Added, "Net10Method", ChangeKind.Method);

        var fileChange1 = CreateFileChange("Test.cs", change1);
        var fileChange2 = CreateFileChange("Test.cs", change2);

        var result1 = CreateDiffResult([fileChange1], additions: 1);
        var result2 = CreateDiffResult([fileChange2], additions: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result1),
            ("net10.0", result2)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.AnalyzedTfms.Should().BeEquivalentTo(["net8.0", "net10.0"]);
        merged.FileChanges.Should().HaveCount(1);
        merged.FileChanges[0].Changes.Should().HaveCount(2);

        var net8Change = merged.FileChanges[0].Changes.First(c => c.Name == "Net8Method");
        net8Change.ApplicableToTfms.Should().ContainSingle().Which.Should().Be("net8.0");

        var net10Change = merged.FileChanges[0].Changes.First(c => c.Name == "Net10Method");
        net10Change.ApplicableToTfms.Should().ContainSingle().Which.Should().Be("net10.0");
    }

    [Fact]
    public void Merge_TwoTfmsWithPartialOverlap_SetsCorrectApplicableToTfms()
    {
        // Arrange
        var sharedChange1 = CreateChange(ChangeType.Added, "SharedMethod", ChangeKind.Method);
        var sharedChange2 = CreateChange(ChangeType.Added, "SharedMethod", ChangeKind.Method);
        var net8OnlyChange = CreateChange(ChangeType.Added, "Net8OnlyMethod", ChangeKind.Method);

        var fileChange1 = CreateFileChange("Test.cs", sharedChange1, net8OnlyChange);
        var fileChange2 = CreateFileChange("Test.cs", sharedChange2);

        var result1 = CreateDiffResult([fileChange1], additions: 2);
        var result2 = CreateDiffResult([fileChange2], additions: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result1),
            ("net10.0", result2)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.FileChanges[0].Changes.Should().HaveCount(2);

        var sharedChange = merged.FileChanges[0].Changes.First(c => c.Name == "SharedMethod");
        sharedChange.ApplicableToTfms.Should().NotBeNull().And.BeEmpty();

        var net8OnlyChangeResult = merged.FileChanges[0].Changes.First(c => c.Name == "Net8OnlyMethod");
        net8OnlyChangeResult.ApplicableToTfms.Should().ContainSingle().Which.Should().Be("net8.0");
    }

    #endregion

    #region Three TFMs - Complex Scenarios

    [Fact]
    public void Merge_ThreeTfmsWithSomeChangesInAll_SetsCorrectApplicableToTfms()
    {
        // Arrange
        var universalChange1 = CreateChange(ChangeType.Added, "UniversalMethod", ChangeKind.Method);
        var universalChange2 = CreateChange(ChangeType.Added, "UniversalMethod", ChangeKind.Method);
        var universalChange3 = CreateChange(ChangeType.Added, "UniversalMethod", ChangeKind.Method);

        var partialChange1 = CreateChange(ChangeType.Added, "PartialMethod", ChangeKind.Method);
        var partialChange2 = CreateChange(ChangeType.Added, "PartialMethod", ChangeKind.Method);

        var specificChange = CreateChange(ChangeType.Added, "SpecificMethod", ChangeKind.Method);

        var fileChange1 = CreateFileChange("Test.cs", universalChange1, partialChange1, specificChange);
        var fileChange2 = CreateFileChange("Test.cs", universalChange2, partialChange2);
        var fileChange3 = CreateFileChange("Test.cs", universalChange3);

        var result1 = CreateDiffResult([fileChange1], additions: 3);
        var result2 = CreateDiffResult([fileChange2], additions: 2);
        var result3 = CreateDiffResult([fileChange3], additions: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result1),
            ("net9.0", result2),
            ("net10.0", result3)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.AnalyzedTfms.Should().BeEquivalentTo(["net8.0", "net9.0", "net10.0"]);
        merged.FileChanges[0].Changes.Should().HaveCount(3);

        var universalChange = merged.FileChanges[0].Changes.First(c => c.Name == "UniversalMethod");
        universalChange.ApplicableToTfms.Should().NotBeNull().And.BeEmpty();

        var partialChange = merged.FileChanges[0].Changes.First(c => c.Name == "PartialMethod");
        partialChange.ApplicableToTfms.Should().BeEquivalentTo(["net8.0", "net9.0"]);

        var specificChangeResult = merged.FileChanges[0].Changes.First(c => c.Name == "SpecificMethod");
        specificChangeResult.ApplicableToTfms.Should().ContainSingle().Which.Should().Be("net8.0");
    }

    #endregion

    #region Stats Aggregation

    [Fact]
    public void Merge_CountsUniqueChangesOnlyOnce()
    {
        // Arrange
        var sharedChange1 = CreateChange(ChangeType.Added, "SharedMethod", ChangeKind.Method);
        var sharedChange2 = CreateChange(ChangeType.Added, "SharedMethod", ChangeKind.Method);
        var uniqueChange = CreateChange(ChangeType.Removed, "OldMethod", ChangeKind.Method);

        var fileChange1 = CreateFileChange("Test.cs", sharedChange1, uniqueChange);
        var fileChange2 = CreateFileChange("Test.cs", sharedChange2);

        var result1 = CreateDiffResult([fileChange1], additions: 1, deletions: 1);
        var result2 = CreateDiffResult([fileChange2], additions: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result1),
            ("net10.0", result2)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.Stats.Additions.Should().Be(1); // SharedMethod counted once
        merged.Stats.Deletions.Should().Be(1); // OldMethod counted once
        merged.Stats.TotalChanges.Should().Be(2);
    }

    [Fact]
    public void Merge_AggregatesAllChangeTypes()
    {
        // Arrange
        var addedChange = CreateChange(ChangeType.Added, "NewMethod", ChangeKind.Method);
        var removedChange = CreateChange(ChangeType.Removed, "OldMethod", ChangeKind.Method);
        var modifiedChange = CreateChange(ChangeType.Modified, "ChangedMethod", ChangeKind.Method);
        var movedChange = CreateChange(ChangeType.Moved, "MovedMethod", ChangeKind.Method);
        var renamedChange = CreateChange(ChangeType.Renamed, "RenamedMethod", ChangeKind.Method);

        var fileChange = CreateFileChange("Test.cs", addedChange, removedChange, modifiedChange, movedChange, renamedChange);
        var result = CreateDiffResult([fileChange], additions: 1, deletions: 1, modifications: 1, moves: 1, renames: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.Stats.Additions.Should().Be(1);
        merged.Stats.Deletions.Should().Be(1);
        merged.Stats.Modifications.Should().Be(1);
        merged.Stats.Moves.Should().Be(1);
        merged.Stats.Renames.Should().Be(1);
        merged.Stats.TotalChanges.Should().Be(5);
    }

    [Fact]
    public void Merge_AggregatesImpactStats()
    {
        // Arrange
        var breakingPublicChange = CreateChange(ChangeType.Added, "Method1", ChangeKind.Method, impact: ChangeImpact.BreakingPublicApi);
        var breakingInternalChange = CreateChange(ChangeType.Added, "Method2", ChangeKind.Method, impact: ChangeImpact.BreakingInternalApi);
        var nonBreakingChange = CreateChange(ChangeType.Added, "Method3", ChangeKind.Method, impact: ChangeImpact.NonBreaking);
        var formattingChange = CreateChange(ChangeType.Modified, "Method4", ChangeKind.Method, impact: ChangeImpact.FormattingOnly);

        var fileChange = CreateFileChange("Test.cs", breakingPublicChange, breakingInternalChange, nonBreakingChange, formattingChange);
        var result = CreateDiffResult([fileChange], additions: 3, modifications: 1,
            breakingPublicApi: 1, breakingInternalApi: 1, nonBreaking: 1, formattingOnly: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.Stats.BreakingPublicApiCount.Should().Be(1);
        merged.Stats.BreakingInternalApiCount.Should().Be(1);
        merged.Stats.NonBreakingCount.Should().Be(1);
        merged.Stats.FormattingOnlyCount.Should().Be(1);
        merged.Stats.HasBreakingChanges.Should().BeTrue();
        merged.Stats.RequiresReview.Should().BeTrue();
    }

    #endregion

    #region Location Handling

    [Fact]
    public void Merge_ChangesWithDifferentLocations_TreatedAsDifferent()
    {
        // Arrange
        var location1 = CreateLocation("Test.cs", 10, 15);
        var location2 = CreateLocation("Test.cs", 20, 25);

        var change1 = CreateChange(ChangeType.Added, "NewMethod", ChangeKind.Method, newLocation: location1);
        var change2 = CreateChange(ChangeType.Added, "NewMethod", ChangeKind.Method, newLocation: location2);

        var fileChange1 = CreateFileChange("Test.cs", change1);
        var fileChange2 = CreateFileChange("Test.cs", change2);

        var result1 = CreateDiffResult([fileChange1], additions: 1);
        var result2 = CreateDiffResult([fileChange2], additions: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result1),
            ("net10.0", result2)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.FileChanges[0].Changes.Should().HaveCount(2);
        merged.FileChanges[0].Changes[0].ApplicableToTfms.Should().ContainSingle();
        merged.FileChanges[0].Changes[1].ApplicableToTfms.Should().ContainSingle();
    }

    [Fact]
    public void Merge_ChangesWithNullLocations_GroupedCorrectly()
    {
        // Arrange
        var change1 = CreateChange(ChangeType.Added, "NewMethod", ChangeKind.Method);
        var change2 = CreateChange(ChangeType.Added, "NewMethod", ChangeKind.Method);

        var fileChange1 = CreateFileChange("Test.cs", change1);
        var fileChange2 = CreateFileChange("Test.cs", change2);

        var result1 = CreateDiffResult([fileChange1], additions: 1);
        var result2 = CreateDiffResult([fileChange2], additions: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result1),
            ("net10.0", result2)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.FileChanges[0].Changes.Should().HaveCount(1);
        merged.FileChanges[0].Changes[0].ApplicableToTfms.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Merge_ChangesWithMixedNullAndNonNullLocations_TreatedAsDifferent()
    {
        // Arrange
        var location = CreateLocation("Test.cs", 10, 15);

        var change1 = CreateChange(ChangeType.Added, "NewMethod", ChangeKind.Method, newLocation: location);
        var change2 = CreateChange(ChangeType.Added, "NewMethod", ChangeKind.Method); // null location

        var fileChange1 = CreateFileChange("Test.cs", change1);
        var fileChange2 = CreateFileChange("Test.cs", change2);

        var result1 = CreateDiffResult([fileChange1], additions: 1);
        var result2 = CreateDiffResult([fileChange2], additions: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result1),
            ("net10.0", result2)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.FileChanges[0].Changes.Should().HaveCount(2);
    }

    #endregion

    #region Complex Nested Changes

    [Fact]
    public void Merge_NestedChanges_MergedRecursively()
    {
        // Arrange
        var childChange1 = CreateChange(ChangeType.Added, "ChildMethod", ChangeKind.Method);
        var childChange2 = CreateChange(ChangeType.Added, "ChildMethod", ChangeKind.Method);

        var parentChange1 = CreateChange(ChangeType.Modified, "ParentClass", ChangeKind.Class, children: [childChange1]);
        var parentChange2 = CreateChange(ChangeType.Modified, "ParentClass", ChangeKind.Class, children: [childChange2]);

        var fileChange1 = CreateFileChange("Test.cs", parentChange1);
        var fileChange2 = CreateFileChange("Test.cs", parentChange2);

        var result1 = CreateDiffResult([fileChange1], modifications: 1);
        var result2 = CreateDiffResult([fileChange2], modifications: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result1),
            ("net10.0", result2)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.FileChanges[0].Changes.Should().HaveCount(1);
        var mergedParent = merged.FileChanges[0].Changes[0];
        mergedParent.ApplicableToTfms.Should().NotBeNull().And.BeEmpty();
        mergedParent.Children.Should().HaveCount(1);
        mergedParent.Children![0].ApplicableToTfms.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Merge_NestedChangesWithDifferentChildren_MergedCorrectly()
    {
        // Arrange
        var childChange1 = CreateChange(ChangeType.Added, "Net8Child", ChangeKind.Method);
        var childChange2 = CreateChange(ChangeType.Added, "Net10Child", ChangeKind.Method);

        var parentChange1 = CreateChange(ChangeType.Modified, "ParentClass", ChangeKind.Class, children: [childChange1]);
        var parentChange2 = CreateChange(ChangeType.Modified, "ParentClass", ChangeKind.Class, children: [childChange2]);

        var fileChange1 = CreateFileChange("Test.cs", parentChange1);
        var fileChange2 = CreateFileChange("Test.cs", parentChange2);

        var result1 = CreateDiffResult([fileChange1], modifications: 1);
        var result2 = CreateDiffResult([fileChange2], modifications: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result1),
            ("net10.0", result2)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.FileChanges[0].Changes.Should().HaveCount(1);
        var mergedParent = merged.FileChanges[0].Changes[0];
        mergedParent.ApplicableToTfms.Should().NotBeNull().And.BeEmpty();
        mergedParent.Children.Should().HaveCount(2);

        var net8Child = mergedParent.Children!.First(c => c.Name == "Net8Child");
        net8Child.ApplicableToTfms.Should().ContainSingle().Which.Should().Be("net8.0");

        var net10Child = mergedParent.Children!.First(c => c.Name == "Net10Child");
        net10Child.ApplicableToTfms.Should().ContainSingle().Which.Should().Be("net10.0");
    }

    [Fact]
    public void Merge_NestedChangesIncludedInStats()
    {
        // Arrange
        var childChange1 = CreateChange(ChangeType.Added, "ChildMethod", ChangeKind.Method);
        var childChange2 = CreateChange(ChangeType.Added, "ChildMethod", ChangeKind.Method);

        var parentChange1 = CreateChange(ChangeType.Modified, "ParentClass", ChangeKind.Class, children: [childChange1]);
        var parentChange2 = CreateChange(ChangeType.Modified, "ParentClass", ChangeKind.Class, children: [childChange2]);

        var fileChange1 = CreateFileChange("Test.cs", parentChange1);
        var fileChange2 = CreateFileChange("Test.cs", parentChange2);

        var result1 = CreateDiffResult([fileChange1], additions: 1, modifications: 1);
        var result2 = CreateDiffResult([fileChange2], additions: 1, modifications: 1);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result1),
            ("net10.0", result2)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.Stats.Modifications.Should().Be(1); // Parent counted once
        merged.Stats.Additions.Should().Be(1); // Child counted once
        merged.Stats.TotalChanges.Should().Be(2);
    }

    #endregion

    #region Multiple Files

    [Fact]
    public void Merge_MultipleFiles_MergedSeparately()
    {
        // Arrange
        var change1File1 = CreateChange(ChangeType.Added, "Method1", ChangeKind.Method);
        var change2File1 = CreateChange(ChangeType.Added, "Method1", ChangeKind.Method);

        var change1File2 = CreateChange(ChangeType.Added, "Method2", ChangeKind.Method);
        var change2File2 = CreateChange(ChangeType.Added, "Method3", ChangeKind.Method);

        var fileChange1A = CreateFileChange("File1.cs", change1File1);
        var fileChange1B = CreateFileChange("File2.cs", change1File2);

        var fileChange2A = CreateFileChange("File1.cs", change2File1);
        var fileChange2B = CreateFileChange("File2.cs", change2File2);

        var result1 = CreateDiffResult([fileChange1A, fileChange1B], additions: 2);
        var result2 = CreateDiffResult([fileChange2A, fileChange2B], additions: 2);

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result1),
            ("net10.0", result2)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.FileChanges.Should().HaveCount(2);

        var file1 = merged.FileChanges.First(fc => fc.Path == "File1.cs");
        file1.Changes.Should().HaveCount(1);
        file1.Changes[0].Name.Should().Be("Method1");
        file1.Changes[0].ApplicableToTfms.Should().NotBeNull().And.BeEmpty();

        var file2 = merged.FileChanges.First(fc => fc.Path == "File2.cs");
        file2.Changes.Should().HaveCount(2);
    }

    #endregion

    #region Path Preservation

    [Fact]
    public void Merge_PreservesOldAndNewPaths()
    {
        // Arrange
        var change = CreateChange(ChangeType.Added, "Method", ChangeKind.Method);
        var fileChange = CreateFileChange("Test.cs", change);
        var result = CreateDiffResult([fileChange], additions: 1, oldPath: "old/path.cs", newPath: "new/path.cs");

        var tfmResults = new List<(string Tfm, DiffResult Result)>
        {
            ("net8.0", result)
        };
        var options = new DiffOptions();

        // Act
        var merged = TfmResultMerger.Merge(tfmResults, options);

        // Assert
        merged.OldPath.Should().Be("old/path.cs");
        merged.NewPath.Should().Be("new/path.cs");
    }

    #endregion

    #region Helper Methods

    private static Change CreateChange(
        ChangeType type,
        string? name,
        ChangeKind kind,
        Location? oldLocation = null,
        Location? newLocation = null,
        ChangeImpact impact = ChangeImpact.NonBreaking,
        IReadOnlyList<Change>? children = null)
    {
        return new Change
        {
            Type = type,
            Name = name,
            Kind = kind,
            OldLocation = oldLocation,
            NewLocation = newLocation,
            Impact = impact,
            Children = children
        };
    }

    private static Location CreateLocation(string file, int startLine, int endLine)
    {
        return new Location
        {
            File = file,
            StartLine = startLine,
            EndLine = endLine,
            StartColumn = 1,
            EndColumn = 1
        };
    }

    private static FileChange CreateFileChange(string path, params Change[] changes)
    {
        return new FileChange
        {
            Path = path,
            Changes = changes
        };
    }

    private static DiffResult CreateDiffResult(
        IReadOnlyList<FileChange> fileChanges,
        int additions = 0,
        int deletions = 0,
        int modifications = 0,
        int moves = 0,
        int renames = 0,
        int breakingPublicApi = 0,
        int breakingInternalApi = 0,
        int nonBreaking = 0,
        int formattingOnly = 0,
        string? oldPath = null,
        string? newPath = null)
    {
        return new DiffResult
        {
            OldPath = oldPath,
            NewPath = newPath,
            Mode = DiffMode.Roslyn,
            FileChanges = fileChanges,
            Stats = new DiffStats
            {
                Additions = additions,
                Deletions = deletions,
                Modifications = modifications,
                Moves = moves,
                Renames = renames,
                BreakingPublicApiCount = breakingPublicApi,
                BreakingInternalApiCount = breakingInternalApi,
                NonBreakingCount = nonBreaking,
                FormattingOnlyCount = formattingOnly
            }
        };
    }

    #endregion
}
