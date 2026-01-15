namespace RoslynDiff.Output.Tests;

using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="PlainTextFormatter"/>.
/// </summary>
public class PlainTextFormatterTests
{
    private readonly PlainTextFormatter _formatter = new();

    [Fact]
    public void Format_ShouldBePlain()
    {
        // Assert
        _formatter.Format.Should().Be("plain");
    }

    [Fact]
    public void ContentType_ShouldBeTextPlain()
    {
        // Assert
        _formatter.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public void Format_EmptyResult_ShouldProduceValidOutput()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().NotBeNullOrWhiteSpace();
        output.Should().Contain("Diff:");
        output.Should().Contain("Mode:");
    }

    [Fact]
    public void Format_WithPaths_ShouldIncludePathsInHeader()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "old.cs",
            NewPath = "new.cs"
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("old.cs");
        output.Should().Contain("new.cs");
        output.Should().Contain("->");
    }

    [Fact]
    public void Format_WithRoslynMode_ShouldShowSemanticMode()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("Roslyn (semantic)");
    }

    [Fact]
    public void Format_WithLineMode_ShouldShowTextMode()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Line
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("Line (text)");
    }

    [Fact]
    public void Format_WithStats_ShouldIncludeSummary()
    {
        // Arrange
        var result = new DiffResult
        {
            Stats = new DiffStats
            {
                TotalChanges = 6,
                Additions = 2,
                Deletions = 1,
                Modifications = 3
            }
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("Summary:");
        output.Should().Contain("+2");
        output.Should().Contain("-1");
        output.Should().Contain("~3");
        output.Should().Contain("6 total changes");
    }

    [Fact]
    public void Format_WithStatsDisabled_ShouldNotIncludeSummary()
    {
        // Arrange
        var result = new DiffResult
        {
            Stats = new DiffStats
            {
                TotalChanges = 6,
                Additions = 2
            }
        };
        var options = new OutputOptions { IncludeStats = false };

        // Act
        var output = _formatter.FormatResult(result, options);

        // Assert
        output.Should().NotContain("Summary:");
    }

    [Fact]
    public void Format_WithNoChanges_ShouldIndicateNoChanges()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("No changes detected");
    }

    [Fact]
    public void Format_WithAddedChange_ShouldShowPlusMarker()
    {
        // Arrange
        var result = new DiffResult
        {
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
                            Kind = ChangeKind.Class,
                            Name = "NewClass",
                            NewLocation = new Location { StartLine = 10, EndLine = 20 }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[+]");
        output.Should().Contain("Class:");
        output.Should().Contain("NewClass");
        output.Should().Contain("line 10-20");
    }

    [Fact]
    public void Format_WithRemovedChange_ShouldShowMinusMarker()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Removed,
                            Kind = ChangeKind.Method,
                            Name = "OldMethod",
                            OldLocation = new Location { StartLine = 5, EndLine = 15 }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[-]");
        output.Should().Contain("Method:");
        output.Should().Contain("OldMethod");
    }

    [Fact]
    public void Format_WithModifiedChange_ShouldShowTildeMarker()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Property,
                            Name = "Value",
                            OldContent = "int Value",
                            NewContent = "string Value",
                            NewLocation = new Location { StartLine = 25, EndLine = 25 }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[~]");
        output.Should().Contain("Property:");
        output.Should().Contain("Value");
        output.Should().Contain("Body modified");
    }

    [Fact]
    public void Format_WithSingleLineLocation_ShouldShowSingleLine()
    {
        // Arrange
        var result = new DiffResult
        {
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
                            Kind = ChangeKind.Field,
                            Name = "_field",
                            NewLocation = new Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("(line 10)");
        output.Should().NotContain("line 10-10");
    }

    [Fact]
    public void Format_WithNestedChanges_ShouldShowHierarchy()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Class,
                            Name = "MyClass",
                            Children =
                            [
                                new Change
                                {
                                    Type = ChangeType.Added,
                                    Kind = ChangeKind.Method,
                                    Name = "NewMethod"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("MyClass");
        output.Should().Contain("NewMethod");
    }

    [Fact]
    public void Format_WithCompactOption_ShouldNotShowModificationDetails()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "Method",
                            OldContent = "old",
                            NewContent = "new"
                        }
                    ]
                }
            ]
        };
        var options = new OutputOptions { Compact = true };

        // Act
        var output = _formatter.FormatResult(result, options);

        // Assert
        output.Should().NotContain("Body modified");
    }

    [Fact]
    public void Format_ShouldNotContainAnsiEscapeCodes()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "old.cs",
            NewPath = "new.cs",
            Stats = new DiffStats { TotalChanges = 1, Additions = 1 },
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
                            Kind = ChangeKind.Class,
                            Name = "Test"
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().NotContain("\x1b["); // ANSI escape sequence start
        output.Should().NotContain("\u001b["); // Unicode escape sequence start
    }

    [Fact]
    public async Task FormatAsync_ShouldWriteToWriter()
    {
        // Arrange
        var result = new DiffResult();
        using var writer = new StringWriter();

        // Act
        await _formatter.FormatResultAsync(result, writer);

        // Assert
        writer.ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Format_WithNullPaths_ShouldShowNone()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = null,
            NewPath = null
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("(none)");
    }

    [Fact]
    public void Format_WithMovedChange_ShouldShowGreaterThanMarker()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Moved,
                            Kind = ChangeKind.Method,
                            Name = "MovedMethod"
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[>]");
    }

    [Fact]
    public void Format_WithRenamedChange_ShouldShowAtMarker()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Renamed,
                            Kind = ChangeKind.Class,
                            Name = "RenamedClass"
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("[@]");
    }
}
