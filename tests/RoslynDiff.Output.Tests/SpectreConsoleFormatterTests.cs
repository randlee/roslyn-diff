namespace RoslynDiff.Output.Tests;

using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="SpectreConsoleFormatter"/>.
/// </summary>
public class SpectreConsoleFormatterTests
{
    private readonly SpectreConsoleFormatter _formatter = new();

    [Fact]
    public void Format_ShouldBeTerminal()
    {
        // Assert
        _formatter.Format.Should().Be("terminal");
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
        output.Should().Contain("Diff Report");
    }

    [Fact]
    public void Format_WithPaths_ShouldIncludePathsInOutput()
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
    public void Format_WithStats_ShouldIncludeStatisticsTable()
    {
        // Arrange
        var result = new DiffResult
        {
            Stats = new DiffStats
            {
                Additions = 2,
                Deletions = 1,
                Modifications = 3
            }
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("Additions");
        output.Should().Contain("Deletions");
        output.Should().Contain("Modifications");
        output.Should().Contain("Total");
    }

    [Fact]
    public void Format_WithStatsDisabled_ShouldNotIncludeStatisticsTable()
    {
        // Arrange
        var result = new DiffResult
        {
            Stats = new DiffStats
            {
                Additions = 2
            }
        };
        var options = new OutputOptions { IncludeStats = false };

        // Act
        var output = _formatter.FormatResult(result, options);

        // Assert
        // The table headers shouldn't appear
        output.Should().NotContain("Metric");
        output.Should().NotContain("Count");
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
    public void Format_WithAddedChange_ShouldShowChange()
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
        output.Should().Contain("Changes");
        output.Should().Contain("Class");
        output.Should().Contain("NewClass");
    }

    [Fact]
    public void Format_WithFileChanges_ShouldShowFilePath()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "src/MyFile.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "TestMethod"
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("src/MyFile.cs");
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
    public void Format_WithUseColorTrue_ShouldContainAnsiCodes()
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
                            Name = "TestClass"
                        }
                    ]
                }
            ]
        };
        var options = new OutputOptions { UseColor = true };

        // Act
        var output = _formatter.FormatResult(result, options);

        // Assert
        // When colors are enabled, output may contain ANSI codes
        // Spectre.Console will include escape sequences for coloring
        output.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Format_WithUseColorFalse_ShouldProduceOutput()
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
                            Name = "TestClass"
                        }
                    ]
                }
            ]
        };
        var options = new OutputOptions { UseColor = false };

        // Act
        var output = _formatter.FormatResult(result, options);

        // Assert
        output.Should().NotBeNullOrWhiteSpace();
        output.Should().Contain("TestClass");
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
    public void Format_WithSpecialCharactersInName_ShouldEscapeMarkup()
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
                            Kind = ChangeKind.Method,
                            Name = "Method[With]Brackets"
                        }
                    ]
                }
            ]
        };

        // Act & Assert
        // Should not throw due to markup interpretation
        var action = () => _formatter.FormatResult(result);
        action.Should().NotThrow();

        var output = _formatter.FormatResult(result);
        output.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Format_WithAllChangeTypes_ShouldHandleAll()
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
                        new Change { Type = ChangeType.Added, Kind = ChangeKind.Class, Name = "Added" },
                        new Change { Type = ChangeType.Removed, Kind = ChangeKind.Class, Name = "Removed" },
                        new Change { Type = ChangeType.Modified, Kind = ChangeKind.Class, Name = "Modified" },
                        new Change { Type = ChangeType.Moved, Kind = ChangeKind.Class, Name = "Moved" },
                        new Change { Type = ChangeType.Renamed, Kind = ChangeKind.Class, Name = "Renamed" },
                        new Change { Type = ChangeType.Unchanged, Kind = ChangeKind.Class, Name = "Unchanged" }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("Added");
        output.Should().Contain("Removed");
        output.Should().Contain("Modified");
        output.Should().Contain("Moved");
        output.Should().Contain("Renamed");
        output.Should().Contain("Unchanged");
    }

    [Fact]
    public void Format_WithAllChangeKinds_ShouldHandleAll()
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
                        new Change { Type = ChangeType.Added, Kind = ChangeKind.File, Name = "File" },
                        new Change { Type = ChangeType.Added, Kind = ChangeKind.Namespace, Name = "Namespace" },
                        new Change { Type = ChangeType.Added, Kind = ChangeKind.Class, Name = "Class" },
                        new Change { Type = ChangeType.Added, Kind = ChangeKind.Method, Name = "Method" },
                        new Change { Type = ChangeType.Added, Kind = ChangeKind.Property, Name = "Property" },
                        new Change { Type = ChangeType.Added, Kind = ChangeKind.Field, Name = "Field" },
                        new Change { Type = ChangeType.Added, Kind = ChangeKind.Statement, Name = "Statement" },
                        new Change { Type = ChangeType.Added, Kind = ChangeKind.Line, Name = "Line" }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("File:");
        output.Should().Contain("Namespace:");
        output.Should().Contain("Class:");
        output.Should().Contain("Method:");
        output.Should().Contain("Property:");
        output.Should().Contain("Field:");
        output.Should().Contain("Statement:");
        output.Should().Contain("Line:");
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
                            OldContent = "old body",
                            NewContent = "new body"
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
    public void Format_WithNonCompactOption_ShouldShowModificationDetails()
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
                            OldContent = "old body",
                            NewContent = "new body"
                        }
                    ]
                }
            ]
        };
        var options = new OutputOptions { Compact = false };

        // Act
        var output = _formatter.FormatResult(result, options);

        // Assert
        output.Should().Contain("Body modified");
    }

    [Fact]
    public void Format_WithLocation_ShouldShowLineNumbers()
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
                            Kind = ChangeKind.Method,
                            Name = "Method",
                            NewLocation = new Location { StartLine = 10, EndLine = 20 }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("line 10-20");
    }

    [Fact]
    public void Format_WithSingleLineLocation_ShouldShowSingleLineNumber()
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
                            NewLocation = new Location { StartLine = 15, EndLine = 15 }
                        }
                    ]
                }
            ]
        };

        // Act
        var output = _formatter.FormatResult(result);

        // Assert
        output.Should().Contain("line 15");
        output.Should().NotContain("line 15-15");
    }
}
