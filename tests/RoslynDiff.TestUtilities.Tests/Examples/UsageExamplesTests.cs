using FluentAssertions;
using RoslynDiff.Core.Models;
using RoslynDiff.TestUtilities.Comparers;
using RoslynDiff.TestUtilities.Validators;
using Xunit;

namespace RoslynDiff.TestUtilities.Tests.Examples;

/// <summary>
/// Demonstrates practical usage of the line number validation infrastructure
/// with realistic diff scenarios.
/// </summary>
public class UsageExamplesTests
{
    [Fact]
    public void Example_ValidateDiffResultChanges_NoOverlaps()
    {
        // Arrange - Simulate a diff result with multiple changes
        var changes = new[]
        {
            new Change
            {
                Type = ChangeType.Modified,
                Kind = ChangeKind.Method,
                Name = "CalculateTotal",
                OldLocation = new Location { File = "Calculator.cs", StartLine = 10, EndLine = 15 },
                NewLocation = new Location { File = "Calculator.cs", StartLine = 10, EndLine = 18 }
            },
            new Change
            {
                Type = ChangeType.Added,
                Kind = ChangeKind.Method,
                Name = "ValidateInput",
                NewLocation = new Location { File = "Calculator.cs", StartLine = 20, EndLine = 25 }
            },
            new Change
            {
                Type = ChangeType.Removed,
                Kind = ChangeKind.Property,
                Name = "OldProperty",
                OldLocation = new Location { File = "Calculator.cs", StartLine = 30, EndLine = 32 }
            }
        };

        // Act - Extract line ranges from the old locations and validate
        var oldRanges = changes
            .Where(c => c.OldLocation != null)
            .Select(c => new LineRange(
                c.OldLocation!.StartLine,
                c.OldLocation.EndLine,
                $"{c.Kind} {c.Name}"))
            .ToList();

        var result = LineNumberValidator.ValidateNoOverlaps(oldRanges, "Old file changes");

        // Assert
        result.Passed.Should().BeTrue();
        result.Context.Should().Be("Old file changes");
    }

    [Fact]
    public void Example_ValidateDiffResultChanges_DetectsOverlap()
    {
        // Arrange - Simulate a problematic diff with overlapping line ranges
        var changes = new[]
        {
            new Change
            {
                Type = ChangeType.Modified,
                Kind = ChangeKind.Method,
                Name = "Method1",
                OldLocation = new Location { File = "Test.cs", StartLine = 10, EndLine = 20 }
            },
            new Change
            {
                Type = ChangeType.Modified,
                Kind = ChangeKind.Method,
                Name = "Method2",
                OldLocation = new Location { File = "Test.cs", StartLine = 15, EndLine = 25 } // Overlaps with Method1
            }
        };

        // Act
        var oldRanges = changes
            .Where(c => c.OldLocation != null)
            .Select(c => new LineRange(
                c.OldLocation!.StartLine,
                c.OldLocation.EndLine,
                $"{c.Kind} {c.Name}"))
            .ToList();

        var result = LineNumberValidator.ValidateNoOverlaps(oldRanges, "Old file changes");

        // Assert
        result.Passed.Should().BeFalse();
        result.Issues.Should().ContainSingle();
        result.Issues[0].Should().Contain("Method1")
            .And.Contain("Method2")
            .And.Contain("overlaps");
    }

    [Fact]
    public void Example_ValidateSequentialChanges_InOrder()
    {
        // Arrange - Changes that should be sequential
        var changes = new[]
        {
            new Change
            {
                Type = ChangeType.Modified,
                Kind = ChangeKind.Class,
                Name = "ClassA",
                NewLocation = new Location { File = "Code.cs", StartLine = 1, EndLine = 50 }
            },
            new Change
            {
                Type = ChangeType.Modified,
                Kind = ChangeKind.Class,
                Name = "ClassB",
                NewLocation = new Location { File = "Code.cs", StartLine = 52, EndLine = 100 }
            },
            new Change
            {
                Type = ChangeType.Modified,
                Kind = ChangeKind.Class,
                Name = "ClassC",
                NewLocation = new Location { File = "Code.cs", StartLine = 102, EndLine = 150 }
            }
        };

        // Act
        var newRanges = changes
            .Where(c => c.NewLocation != null)
            .Select(c => new LineRange(
                c.NewLocation!.StartLine,
                c.NewLocation.EndLine,
                $"{c.Kind} {c.Name}"))
            .ToList();

        var result = LineNumberValidator.ValidateRangesSequential(newRanges, "New file changes");

        // Assert
        result.Passed.Should().BeTrue();
        result.Message.Should().Contain("All 3 range(s) are sequential");
    }

    [Fact]
    public void Example_ValidateDuplicateLineNumbers()
    {
        // Arrange - Simulate extracting start lines from changes
        var changes = new[]
        {
            new Change
            {
                Type = ChangeType.Modified,
                OldLocation = new Location { StartLine = 10, EndLine = 15 }
            },
            new Change
            {
                Type = ChangeType.Modified,
                OldLocation = new Location { StartLine = 20, EndLine = 25 }
            },
            new Change
            {
                Type = ChangeType.Modified,
                OldLocation = new Location { StartLine = 10, EndLine = 12 } // Duplicate start line
            }
        };

        // Act
        var startLines = changes
            .Where(c => c.OldLocation != null)
            .Select(c => c.OldLocation!.StartLine)
            .ToList();

        var result = LineNumberValidator.ValidateNoDuplicates(startLines, "Change start lines");

        // Assert
        result.Passed.Should().BeFalse();
        result.Issues.Should().ContainSingle();
        result.Issues[0].Should().Contain("Line 10");
    }

    [Fact]
    public void Example_ComprehensiveValidation_AllChecks()
    {
        // Arrange - A complex scenario with multiple checks
        var changes = new[]
        {
            new Change
            {
                Type = ChangeType.Added,
                Kind = ChangeKind.Method,
                Name = "NewMethod1",
                NewLocation = new Location { File = "Service.cs", StartLine = 10, EndLine = 20 }
            },
            new Change
            {
                Type = ChangeType.Added,
                Kind = ChangeKind.Method,
                Name = "NewMethod2",
                NewLocation = new Location { File = "Service.cs", StartLine = 22, EndLine = 30 }
            },
            new Change
            {
                Type = ChangeType.Added,
                Kind = ChangeKind.Method,
                Name = "NewMethod3",
                NewLocation = new Location { File = "Service.cs", StartLine = 32, EndLine = 40 }
            }
        };

        // Act - Comprehensive validation with sequential requirement
        var newRanges = changes
            .Where(c => c.NewLocation != null)
            .Select(c => new LineRange(
                c.NewLocation!.StartLine,
                c.NewLocation.EndLine,
                $"{c.Kind} {c.Name}"))
            .ToList();

        var result = LineNumberValidator.ValidateRanges(
            newRanges,
            "Added methods in Service.cs",
            requireSequential: true);

        // Assert
        result.Passed.Should().BeTrue();
        result.Message.Should().Contain("overlap and sequential");
    }

    [Fact]
    public void Example_TestResultFormatting()
    {
        // Arrange
        var ranges = new[]
        {
            new LineRange(1, 10, "Change1"),
            new LineRange(5, 15, "Change2")
        };

        // Act
        var result = LineNumberValidator.ValidateNoOverlaps(ranges, "Example validation");

        // Assert - Show that TestResult provides useful output
        var output = result.ToString();

        output.Should().Contain("[FAIL]");
        output.Should().Contain("Example validation");
        output.Should().Contain("Issues:");
        output.Should().Contain("Change1");
        output.Should().Contain("Change2");
    }

    [Fact]
    public void Example_ExtractingRangesFromNestedChanges()
    {
        // Arrange - A change with child changes (hierarchical)
        var classChange = new Change
        {
            Type = ChangeType.Modified,
            Kind = ChangeKind.Class,
            Name = "MyClass",
            OldLocation = new Location { File = "MyClass.cs", StartLine = 1, EndLine = 100 },
            Children = new[]
            {
                new Change
                {
                    Type = ChangeType.Modified,
                    Kind = ChangeKind.Method,
                    Name = "Method1",
                    OldLocation = new Location { File = "MyClass.cs", StartLine = 10, EndLine = 20 }
                },
                new Change
                {
                    Type = ChangeType.Modified,
                    Kind = ChangeKind.Method,
                    Name = "Method2",
                    OldLocation = new Location { File = "MyClass.cs", StartLine = 30, EndLine = 40 }
                }
            }
        };

        // Act - Extract ranges from child changes
        var childRanges = classChange.Children!
            .Where(c => c.OldLocation != null)
            .Select(c => new LineRange(
                c.OldLocation!.StartLine,
                c.OldLocation.EndLine,
                c.Name ?? "Unknown"))
            .ToList();

        var result = LineNumberValidator.ValidateRanges(
            childRanges,
            $"Methods in {classChange.Name}",
            requireSequential: true);

        // Assert
        result.Passed.Should().BeTrue();
    }
}
