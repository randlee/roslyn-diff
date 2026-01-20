namespace RoslynDiff.Core.Tests.Models;

using System.Text.Json;
using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for TFM (Target Framework Moniker) properties in model types.
/// </summary>
public class TfmModelTests
{
    #region Change.ApplicableToTfms Tests

    [Fact]
    public void Change_ApplicableToTfms_DefaultsToNull()
    {
        // Arrange & Act
        var change = new Change();

        // Assert
        change.ApplicableToTfms.Should().BeNull();
    }

    [Fact]
    public void Change_ApplicableToTfms_CanBeSetToNull()
    {
        // Arrange & Act
        var change = new Change
        {
            Type = ChangeType.Added,
            Kind = ChangeKind.Method,
            ApplicableToTfms = null
        };

        // Assert
        change.ApplicableToTfms.Should().BeNull();
    }

    [Fact]
    public void Change_ApplicableToTfms_CanBeSetToEmptyList()
    {
        // Arrange & Act
        var change = new Change
        {
            Type = ChangeType.Modified,
            Kind = ChangeKind.Class,
            ApplicableToTfms = []
        };

        // Assert
        change.ApplicableToTfms.Should().NotBeNull();
        change.ApplicableToTfms.Should().BeEmpty();
    }

    [Fact]
    public void Change_ApplicableToTfms_CanBeSetToListWithValues()
    {
        // Arrange
        var tfms = new[] { "net8.0", "net10.0" };

        // Act
        var change = new Change
        {
            Type = ChangeType.Added,
            Kind = ChangeKind.Method,
            Name = "NewMethod",
            ApplicableToTfms = tfms
        };

        // Assert
        change.ApplicableToTfms.Should().NotBeNull();
        change.ApplicableToTfms.Should().HaveCount(2);
        change.ApplicableToTfms.Should().Contain("net8.0");
        change.ApplicableToTfms.Should().Contain("net10.0");
    }

    [Fact]
    public void Change_ApplicableToTfms_IsImmutable()
    {
        // Arrange
        var tfms = new[] { "net8.0" };
        var change = new Change { ApplicableToTfms = tfms };

        // Act & Assert - Should not be able to modify through init accessor
        // The init accessor only allows setting during initialization
        change.ApplicableToTfms.Should().NotBeNull();
        change.ApplicableToTfms.Should().HaveCount(1);
    }

    [Fact]
    public void Change_WithApplicableToTfms_RecordEqualityWorks()
    {
        // Arrange
        var tfms = new[] { "net8.0", "net10.0" };
        var change1 = new Change
        {
            Type = ChangeType.Added,
            ApplicableToTfms = tfms
        };
        var change2 = new Change
        {
            Type = ChangeType.Added,
            ApplicableToTfms = tfms
        };

        // Assert
        change1.Should().Be(change2);
    }

    [Fact]
    public void Change_ApplicableToTfms_WithExpression_CreatesNewRecord()
    {
        // Arrange
        var original = new Change
        {
            Type = ChangeType.Added,
            Name = "TestMethod",
            ApplicableToTfms = new[] { "net8.0" }
        };

        // Act
        var modified = original with { ApplicableToTfms = new[] { "net8.0", "net10.0" } };

        // Assert
        original.ApplicableToTfms.Should().HaveCount(1);
        modified.ApplicableToTfms.Should().HaveCount(2);
        modified.Name.Should().Be("TestMethod");
    }

    #endregion

    #region DiffResult.AnalyzedTfms Tests

    [Fact]
    public void DiffResult_AnalyzedTfms_DefaultsToNull()
    {
        // Arrange & Act
        var result = new DiffResult();

        // Assert
        result.AnalyzedTfms.Should().BeNull();
    }

    [Fact]
    public void DiffResult_AnalyzedTfms_CanBeSetToNull()
    {
        // Arrange & Act
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = null
        };

        // Assert
        result.AnalyzedTfms.Should().BeNull();
    }

    [Fact]
    public void DiffResult_AnalyzedTfms_CanBeSetToEmptyList()
    {
        // Arrange & Act
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = []
        };

        // Assert
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().BeEmpty();
    }

    [Fact]
    public void DiffResult_AnalyzedTfms_CanBeSetToListWithValues()
    {
        // Arrange
        var tfms = new[] { "net8.0", "net10.0" };

        // Act
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            OldPath = "old.cs",
            NewPath = "new.cs",
            AnalyzedTfms = tfms
        };

        // Assert
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().HaveCount(2);
        result.AnalyzedTfms.Should().Contain("net8.0");
        result.AnalyzedTfms.Should().Contain("net10.0");
    }

    [Fact]
    public void DiffResult_AnalyzedTfms_IsImmutable()
    {
        // Arrange
        var tfms = new[] { "net8.0", "net10.0" };
        var result = new DiffResult { AnalyzedTfms = tfms };

        // Act & Assert - Should not be able to modify through init accessor
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().HaveCount(2);
    }

    [Fact]
    public void DiffResult_WithAnalyzedTfms_RecordEqualityWorks()
    {
        // Arrange
        var tfms = new[] { "net8.0" };
        var result1 = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = tfms
        };
        var result2 = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = tfms
        };

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void DiffResult_AnalyzedTfms_WithExpression_CreatesNewRecord()
    {
        // Arrange
        var original = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0" }
        };

        // Act
        var modified = original with { AnalyzedTfms = new[] { "net8.0", "net10.0" } };

        // Assert
        original.AnalyzedTfms.Should().HaveCount(1);
        modified.AnalyzedTfms.Should().HaveCount(2);
        modified.Mode.Should().Be(DiffMode.Roslyn);
    }

    #endregion

    #region DiffOptions.TargetFrameworks Tests

    [Fact]
    public void DiffOptions_TargetFrameworks_DefaultsToNull()
    {
        // Arrange & Act
        var options = new DiffOptions();

        // Assert
        options.TargetFrameworks.Should().BeNull();
    }

    [Fact]
    public void DiffOptions_TargetFrameworks_CanBeSetToNull()
    {
        // Arrange & Act
        var options = new DiffOptions
        {
            Mode = DiffMode.Roslyn,
            TargetFrameworks = null
        };

        // Assert
        options.TargetFrameworks.Should().BeNull();
    }

    [Fact]
    public void DiffOptions_TargetFrameworks_CanBeSetToEmptyList()
    {
        // Arrange & Act
        var options = new DiffOptions
        {
            Mode = DiffMode.Roslyn,
            TargetFrameworks = []
        };

        // Assert
        options.TargetFrameworks.Should().NotBeNull();
        options.TargetFrameworks.Should().BeEmpty();
    }

    [Fact]
    public void DiffOptions_TargetFrameworks_CanBeSetToListWithValues()
    {
        // Arrange
        var tfms = new[] { "net8.0", "net10.0" };

        // Act
        var options = new DiffOptions
        {
            Mode = DiffMode.Roslyn,
            TargetFrameworks = tfms
        };

        // Assert
        options.TargetFrameworks.Should().NotBeNull();
        options.TargetFrameworks.Should().HaveCount(2);
        options.TargetFrameworks.Should().Contain("net8.0");
        options.TargetFrameworks.Should().Contain("net10.0");
    }

    [Fact]
    public void DiffOptions_TargetFrameworks_IsImmutable()
    {
        // Arrange
        var tfms = new[] { "net8.0", "net10.0" };
        var options = new DiffOptions { TargetFrameworks = tfms };

        // Act & Assert - Should not be able to modify through init accessor
        options.TargetFrameworks.Should().NotBeNull();
        options.TargetFrameworks.Should().HaveCount(2);
    }

    [Fact]
    public void DiffOptions_WithTargetFrameworks_RecordEqualityWorks()
    {
        // Arrange
        var tfms = new[] { "net8.0" };
        var options1 = new DiffOptions
        {
            Mode = DiffMode.Roslyn,
            TargetFrameworks = tfms
        };
        var options2 = new DiffOptions
        {
            Mode = DiffMode.Roslyn,
            TargetFrameworks = tfms
        };

        // Assert
        options1.Should().Be(options2);
    }

    [Fact]
    public void DiffOptions_TargetFrameworks_WithExpression_CreatesNewRecord()
    {
        // Arrange
        var original = new DiffOptions
        {
            Mode = DiffMode.Roslyn,
            TargetFrameworks = new[] { "net8.0" }
        };

        // Act
        var modified = original with { TargetFrameworks = new[] { "net8.0", "net10.0" } };

        // Assert
        original.TargetFrameworks.Should().HaveCount(1);
        modified.TargetFrameworks.Should().HaveCount(2);
        modified.Mode.Should().Be(DiffMode.Roslyn);
    }

    #endregion

    #region JSON Serialization Tests

    [Fact]
    public void Change_WithNullApplicableToTfms_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var change = new Change
        {
            Type = ChangeType.Added,
            Kind = ChangeKind.Method,
            Name = "TestMethod",
            ApplicableToTfms = null
        };

        // Act
        var json = JsonSerializer.Serialize(change);
        var deserialized = JsonSerializer.Deserialize<Change>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Type.Should().Be(ChangeType.Added);
        deserialized.Name.Should().Be("TestMethod");
        deserialized.ApplicableToTfms.Should().BeNull();
    }

    [Fact]
    public void Change_WithEmptyApplicableToTfms_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var change = new Change
        {
            Type = ChangeType.Modified,
            Kind = ChangeKind.Class,
            Name = "TestClass",
            ApplicableToTfms = []
        };

        // Act
        var json = JsonSerializer.Serialize(change);
        var deserialized = JsonSerializer.Deserialize<Change>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("TestClass");
        deserialized.ApplicableToTfms.Should().NotBeNull();
        deserialized.ApplicableToTfms.Should().BeEmpty();
    }

    [Fact]
    public void Change_WithApplicableToTfms_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var change = new Change
        {
            Type = ChangeType.Added,
            Kind = ChangeKind.Method,
            Name = "NewMethod",
            ApplicableToTfms = new[] { "net8.0", "net10.0" }
        };

        // Act
        var json = JsonSerializer.Serialize(change);
        var deserialized = JsonSerializer.Deserialize<Change>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("NewMethod");
        deserialized.ApplicableToTfms.Should().NotBeNull();
        deserialized.ApplicableToTfms.Should().HaveCount(2);
        deserialized.ApplicableToTfms.Should().Contain("net8.0");
        deserialized.ApplicableToTfms.Should().Contain("net10.0");
    }

    [Fact]
    public void DiffResult_WithNullAnalyzedTfms_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            OldPath = "old.cs",
            NewPath = "new.cs",
            AnalyzedTfms = null
        };

        // Act
        var json = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<DiffResult>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Mode.Should().Be(DiffMode.Roslyn);
        deserialized.OldPath.Should().Be("old.cs");
        deserialized.AnalyzedTfms.Should().BeNull();
    }

    [Fact]
    public void DiffResult_WithAnalyzedTfms_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            OldPath = "old.cs",
            NewPath = "new.cs",
            AnalyzedTfms = new[] { "net8.0", "net10.0" }
        };

        // Act
        var json = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<DiffResult>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Mode.Should().Be(DiffMode.Roslyn);
        deserialized.AnalyzedTfms.Should().NotBeNull();
        deserialized.AnalyzedTfms.Should().HaveCount(2);
        deserialized.AnalyzedTfms.Should().Contain("net8.0");
        deserialized.AnalyzedTfms.Should().Contain("net10.0");
    }

    [Fact]
    public void DiffOptions_WithNullTargetFrameworks_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var options = new DiffOptions
        {
            Mode = DiffMode.Roslyn,
            ContextLines = 5,
            TargetFrameworks = null
        };

        // Act
        var json = JsonSerializer.Serialize(options);
        var deserialized = JsonSerializer.Deserialize<DiffOptions>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Mode.Should().Be(DiffMode.Roslyn);
        deserialized.ContextLines.Should().Be(5);
        deserialized.TargetFrameworks.Should().BeNull();
    }

    [Fact]
    public void DiffOptions_WithTargetFrameworks_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var options = new DiffOptions
        {
            Mode = DiffMode.Roslyn,
            ContextLines = 5,
            TargetFrameworks = new[] { "net8.0", "net10.0" }
        };

        // Act
        var json = JsonSerializer.Serialize(options);
        var deserialized = JsonSerializer.Deserialize<DiffOptions>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Mode.Should().Be(DiffMode.Roslyn);
        deserialized.TargetFrameworks.Should().NotBeNull();
        deserialized.TargetFrameworks.Should().HaveCount(2);
        deserialized.TargetFrameworks.Should().Contain("net8.0");
        deserialized.TargetFrameworks.Should().Contain("net10.0");
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public void CompleteScenario_NullTfms_IndicatesNoTfmAnalysis()
    {
        // Arrange & Act
        var options = new DiffOptions
        {
            Mode = DiffMode.Roslyn,
            TargetFrameworks = null
        };

        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = null,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            ApplicableToTfms = null
                        }
                    ]
                }
            ]
        };

        // Assert - All TFM properties are null, indicating no TFM analysis
        options.TargetFrameworks.Should().BeNull();
        result.AnalyzedTfms.Should().BeNull();
        result.FileChanges[0].Changes[0].ApplicableToTfms.Should().BeNull();
    }

    [Fact]
    public void CompleteScenario_EmptyApplicableToTfms_IndicatesAppliesToAllTfms()
    {
        // Arrange & Act
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "CommonMethod",
                            ApplicableToTfms = [] // Applies to all analyzed TFMs
                        }
                    ]
                }
            ]
        };

        // Assert - Empty ApplicableToTfms means applies to all analyzed TFMs
        result.AnalyzedTfms.Should().HaveCount(2);
        result.FileChanges[0].Changes[0].ApplicableToTfms.Should().NotBeNull();
        result.FileChanges[0].Changes[0].ApplicableToTfms.Should().BeEmpty();
    }

    [Fact]
    public void CompleteScenario_SpecificTfms_IndicatesTfmSpecificChange()
    {
        // Arrange & Act
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "Net8OnlyMethod",
                            ApplicableToTfms = new[] { "net8.0" } // Only in net8.0
                        }
                    ]
                }
            ]
        };

        // Assert - Specific TFMs indicate framework-specific change
        result.AnalyzedTfms.Should().HaveCount(2);
        result.FileChanges[0].Changes[0].ApplicableToTfms.Should().HaveCount(1);
        result.FileChanges[0].Changes[0].ApplicableToTfms.Should().Contain("net8.0");
    }

    [Fact]
    public void CompleteScenario_MultiTfmResult_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn,
            OldPath = "old.cs",
            NewPath = "new.cs",
            AnalyzedTfms = new[] { "net8.0", "net10.0" },
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "CommonMethod",
                            ApplicableToTfms = [] // All TFMs
                        },
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "Net8Method",
                            ApplicableToTfms = new[] { "net8.0" } // net8.0 only
                        }
                    ]
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<DiffResult>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.AnalyzedTfms.Should().HaveCount(2);
        deserialized.FileChanges[0].Changes.Should().HaveCount(2);
        deserialized.FileChanges[0].Changes[0].ApplicableToTfms.Should().BeEmpty();
        deserialized.FileChanges[0].Changes[1].ApplicableToTfms.Should().HaveCount(1);
        deserialized.FileChanges[0].Changes[1].ApplicableToTfms.Should().Contain("net8.0");
    }

    #endregion
}
