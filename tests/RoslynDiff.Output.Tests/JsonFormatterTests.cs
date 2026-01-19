namespace RoslynDiff.Output.Tests;

using System.Text.Json;
using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="JsonFormatter"/>.
/// </summary>
public class JsonFormatterTests
{
    private readonly JsonFormatter _formatter = new();

    #region Format and ContentType Properties

    [Fact]
    public void Format_ShouldBeJson()
    {
        _formatter.Format.Should().Be("json");
    }

    [Fact]
    public void ContentType_ShouldBeApplicationJson()
    {
        _formatter.ContentType.Should().Be("application/json");
    }

    #endregion

    #region Basic Formatting

    [Fact]
    public void FormatResult_EmptyResult_ShouldProduceValidJson()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var json = _formatter.FormatResult(result);

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        var action = () => JsonDocument.Parse(json);
        action.Should().NotThrow();
    }

    [Fact]
    public void FormatResult_EmptyResult_ShouldContainMetadataSection()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        doc.RootElement.TryGetProperty("metadata", out var metadata).Should().BeTrue();
        metadata.TryGetProperty("version", out _).Should().BeTrue();
        metadata.TryGetProperty("timestamp", out _).Should().BeTrue();
        metadata.TryGetProperty("mode", out _).Should().BeTrue();
        metadata.TryGetProperty("options", out _).Should().BeTrue();
    }

    [Fact]
    public void FormatResult_EmptyResult_ShouldContainSummarySection()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        doc.RootElement.TryGetProperty("summary", out var summary).Should().BeTrue();
        summary.TryGetProperty("totalChanges", out _).Should().BeTrue();
        summary.TryGetProperty("additions", out _).Should().BeTrue();
        summary.TryGetProperty("deletions", out _).Should().BeTrue();
        summary.TryGetProperty("modifications", out _).Should().BeTrue();
        summary.TryGetProperty("renames", out _).Should().BeTrue();
        summary.TryGetProperty("moves", out _).Should().BeTrue();
    }

    [Fact]
    public void FormatResult_EmptyResult_ShouldContainFilesSection()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        doc.RootElement.TryGetProperty("files", out var files).Should().BeTrue();
        files.ValueKind.Should().Be(JsonValueKind.Array);
    }

    #endregion

    #region With Changes

    [Fact]
    public void FormatResult_WithChanges_ShouldIncludeChangeDetails()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "old.cs",
            NewPath = "new.cs",
            Mode = DiffMode.Roslyn,
            Stats = new DiffStats
            {
                Additions = 1
            },
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
                            NewContent = "public class NewClass { }",
                            NewLocation = new Location
                            {
                                StartLine = 10,
                                EndLine = 12,
                                StartColumn = 1,
                                EndColumn = 1
                            }
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var files = doc.RootElement.GetProperty("files");
        files.GetArrayLength().Should().Be(1);

        var file = files[0];
        file.GetProperty("oldPath").GetString().Should().Be("old.cs");
        file.GetProperty("newPath").GetString().Should().Be("new.cs");

        var changes = file.GetProperty("changes");
        changes.GetArrayLength().Should().Be(1);

        var change = changes[0];
        change.GetProperty("type").GetString().Should().Be("added");
        change.GetProperty("kind").GetString().Should().Be("class");
        change.GetProperty("name").GetString().Should().Be("NewClass");
        change.GetProperty("content").GetString().Should().Be("public class NewClass { }");
    }

    [Fact]
    public void FormatResult_WithChanges_ShouldIncludeLocation()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            NewLocation = new Location
                            {
                                StartLine = 10,
                                EndLine = 20,
                                StartColumn = 5,
                                EndColumn = 6
                            }
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        var location = change.GetProperty("location");

        location.GetProperty("startLine").GetInt32().Should().Be(10);
        location.GetProperty("endLine").GetInt32().Should().Be(20);
        location.GetProperty("startColumn").GetInt32().Should().Be(5);
        location.GetProperty("endColumn").GetInt32().Should().Be(6);
    }

    [Fact]
    public void FormatResult_WithModification_ShouldIncludeOldContent()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            OldContent = "void OldMethod() { }",
                            NewContent = "void NewMethod() { }",
                            OldLocation = new Location { StartLine = 5, EndLine = 5 },
                            NewLocation = new Location { StartLine = 5, EndLine = 5 }
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        change.GetProperty("content").GetString().Should().Be("void NewMethod() { }");
        change.GetProperty("oldContent").GetString().Should().Be("void OldMethod() { }");
    }

    [Fact]
    public void FormatResult_WithChildren_ShouldIncludeNestedChanges()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Class,
                            Name = "ParentClass",
                            Children =
                            [
                                new Change
                                {
                                    Type = ChangeType.Added,
                                    Kind = ChangeKind.Method,
                                    Name = "ChildMethod"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        var children = change.GetProperty("children");

        children.GetArrayLength().Should().Be(1);
        children[0].GetProperty("type").GetString().Should().Be("added");
        children[0].GetProperty("kind").GetString().Should().Be("method");
        children[0].GetProperty("name").GetString().Should().Be("ChildMethod");
    }

    #endregion

    #region Include Content Option

    [Fact]
    public void FormatResult_WithIncludeContentFalse_ShouldNotIncludeContent()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Class,
                            Name = "TestClass",
                            NewContent = "public class TestClass { }"
                        }
                    ]
                }
            ]
        };
        var options = new OutputOptions { IncludeContent = false };

        // Act
        var json = _formatter.FormatResult(result, options);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        change.TryGetProperty("content", out _).Should().BeFalse();
    }

    [Fact]
    public void FormatResult_WithIncludeContentTrue_ShouldIncludeContent()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Class,
                            NewContent = "public class TestClass { }"
                        }
                    ]
                }
            ]
        };
        var options = new OutputOptions { IncludeContent = true };

        // Act
        var json = _formatter.FormatResult(result, options);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        change.GetProperty("content").GetString().Should().Be("public class TestClass { }");
    }

    #endregion

    #region Pretty Print Option

    [Fact]
    public void FormatResult_WithPrettyPrintTrue_ShouldBeIndented()
    {
        // Arrange
        var result = new DiffResult();
        var options = new OutputOptions { PrettyPrint = true };

        // Act
        var json = _formatter.FormatResult(result, options);

        // Assert
        json.Should().Contain("\n  "); // Contains indentation
    }

    [Fact]
    public void FormatResult_WithPrettyPrintFalse_ShouldBeCompact()
    {
        // Arrange
        var result = new DiffResult();
        var options = new OutputOptions { PrettyPrint = false };

        // Act
        var json = _formatter.FormatResult(result, options);

        // Assert
        json.Should().NotContain("\n  "); // No indentation
    }

    #endregion

    #region Async Formatting

    [Fact]
    public async Task FormatResultAsync_ShouldWriteToWriter()
    {
        // Arrange
        var result = new DiffResult();
        using var writer = new StringWriter();

        // Act
        await _formatter.FormatResultAsync(result, writer);

        // Assert
        var json = writer.ToString();
        json.Should().NotBeNullOrWhiteSpace();

        var action = () => JsonDocument.Parse(json);
        action.Should().NotThrow();
    }

    [Fact]
    public async Task FormatResultAsync_ShouldProduceSameOutputAsFormatResult()
    {
        // Arrange
        var result = new DiffResult
        {
            Stats = new DiffStats { Additions = 1 }
        };
        using var writer = new StringWriter();

        // Act
        var syncJson = _formatter.FormatResult(result);
        await _formatter.FormatResultAsync(result, writer);
        var asyncJson = writer.ToString();

        // Assert - Compare structure (timestamps will differ)
        using var syncDoc = JsonDocument.Parse(syncJson);
        using var asyncDoc = JsonDocument.Parse(asyncJson);

        syncDoc.RootElement.GetProperty("summary").GetProperty("totalChanges").GetInt32()
            .Should().Be(asyncDoc.RootElement.GetProperty("summary").GetProperty("totalChanges").GetInt32());
    }

    #endregion

    #region Metadata Validation

    [Fact]
    public void FormatResult_Metadata_ShouldIncludeCorrectMode()
    {
        // Arrange
        var roslynResult = new DiffResult { Mode = DiffMode.Roslyn };
        var lineResult = new DiffResult { Mode = DiffMode.Line };

        // Act
        var roslynJson = _formatter.FormatResult(roslynResult);
        var lineJson = _formatter.FormatResult(lineResult);

        using var roslynDoc = JsonDocument.Parse(roslynJson);
        using var lineDoc = JsonDocument.Parse(lineJson);

        // Assert
        roslynDoc.RootElement.GetProperty("metadata").GetProperty("mode").GetString().Should().Be("roslyn");
        lineDoc.RootElement.GetProperty("metadata").GetProperty("mode").GetString().Should().Be("line");
    }

    [Fact]
    public void FormatResult_Metadata_ShouldIncludeOptions()
    {
        // Arrange
        var result = new DiffResult();
        var options = new OutputOptions { IncludeContent = false, ContextLines = 5 };

        // Act
        var json = _formatter.FormatResult(result, options);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var metaOptions = doc.RootElement.GetProperty("metadata").GetProperty("options");
        metaOptions.GetProperty("includeContent").GetBoolean().Should().BeFalse();
        metaOptions.GetProperty("contextLines").GetInt32().Should().Be(5);
    }

    [Fact]
    public void FormatResult_Metadata_ShouldIncludeVersion()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var version = doc.RootElement.GetProperty("metadata").GetProperty("version").GetString();
        version.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void FormatResult_Metadata_ShouldIncludeTimestamp()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var timestamp = doc.RootElement.GetProperty("metadata").GetProperty("timestamp").GetDateTimeOffset();
        timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Summary Validation

    [Fact]
    public void FormatResult_Summary_ShouldMatchStats()
    {
        // Arrange
        var result = new DiffResult
        {
            Stats = new DiffStats
            {
                Additions = 3,
                Deletions = 2,
                Modifications = 4,
                Renames = 1,
                Moves = 0
            }
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var summary = doc.RootElement.GetProperty("summary");
        summary.GetProperty("totalChanges").GetInt32().Should().Be(10);
        summary.GetProperty("additions").GetInt32().Should().Be(3);
        summary.GetProperty("deletions").GetInt32().Should().Be(2);
        summary.GetProperty("modifications").GetInt32().Should().Be(4);
        summary.GetProperty("renames").GetInt32().Should().Be(1);
        summary.GetProperty("moves").GetInt32().Should().Be(0);
    }

    #endregion

    #region Null Handling

    [Fact]
    public void FormatResult_WithNullResult_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => _formatter.FormatResult(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task FormatResultAsync_WithNullResult_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var writer = new StringWriter();

        // Act & Assert
        var action = async () => await _formatter.FormatResultAsync(null!, writer);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task FormatResultAsync_WithNullWriter_ShouldThrowArgumentNullException()
    {
        // Arrange
        var result = new DiffResult();

        // Act & Assert
        var action = async () => await _formatter.FormatResultAsync(result, null!);
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region camelCase Property Names

    [Fact]
    public void FormatResult_ShouldUseCamelCasePropertyNames()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "old.cs",
            NewPath = "new.cs",
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
                            NewLocation = new Location { StartLine = 1, EndLine = 2, StartColumn = 1, EndColumn = 1 }
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);

        // Assert - Check that property names are camelCase
        json.Should().Contain("\"totalChanges\"");
        json.Should().Contain("\"startLine\"");
        json.Should().Contain("\"endLine\"");
        json.Should().Contain("\"startColumn\"");
        json.Should().Contain("\"endColumn\"");
        json.Should().Contain("\"oldPath\"");
        json.Should().Contain("\"newPath\"");

        // Should NOT contain PascalCase
        json.Should().NotContain("\"TotalChanges\"");
        json.Should().NotContain("\"StartLine\"");
    }


    #endregion

    #region Impact Classification (P0 Critical Tests)

    [Fact]
    public void FormatResult_WithImpactProperty_IncludesImpactField()
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
                            Name = "PublicMethod",
                            Impact = ChangeImpact.BreakingPublicApi,
                            OldContent = "public void PublicMethod() { }",
                            OldLocation = new Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        change.TryGetProperty("impact", out var impact).Should().BeTrue("JSON output should include 'impact' field");
        impact.GetString().Should().Be("breakingPublicApi", "impact should be serialized in camelCase");
    }

    [Fact]
    public void FormatResult_WithVisibility_IncludesVisibilityField()
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
                            Name = "InternalMethod",
                            Impact = ChangeImpact.BreakingInternalApi,
                            Visibility = Visibility.Internal,
                            OldContent = "internal void InternalMethod() { }",
                            NewContent = "internal void InternalMethod(int x) { }",
                            OldLocation = new Location { StartLine = 5, EndLine = 5 },
                            NewLocation = new Location { StartLine = 5, EndLine = 5 }
                        }
                    ]
                }
            ]
        };

        // Act
        var json = _formatter.FormatResult(result);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        change.TryGetProperty("visibility", out var visibility).Should().BeTrue("JSON output should include 'visibility' field");
        visibility.GetString().Should().Be("internal", "visibility should be serialized in lowercase");
    }

    [Fact]
    public void FormatResult_WithCaveats_IncludesCaveatsArray()
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
                            Name = "MethodWithCaveats",
                            Impact = ChangeImpact.NonBreaking,
                            Caveats = new List<string>
                            {
                                "Method is virtual - derived classes may be affected",
                                "Method is used in reflection scenarios"
                            },
                            OldContent = "public virtual void MethodWithCaveats() { }",
                            NewContent = "public virtual void MethodWithCaveats() { /* comment */ }",
                            OldLocation = new Location { StartLine = 15, EndLine = 15 },
                            NewLocation = new Location { StartLine = 15, EndLine = 15 }
                        }
                    ]
                }
            ]
        };

        // We must explicitly include non-impactful changes since the default is IncludeNonImpactful=false
        var options = new OutputOptions { IncludeNonImpactful = true };

        // Act
        var json = _formatter.FormatResult(result, options);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var change = doc.RootElement.GetProperty("files")[0].GetProperty("changes")[0];
        change.TryGetProperty("caveats", out var caveats).Should().BeTrue("JSON output should include 'caveats' array when present");
        caveats.ValueKind.Should().Be(JsonValueKind.Array, "caveats should be an array");
        caveats.GetArrayLength().Should().Be(2, "caveats array should contain 2 items");
        caveats[0].GetString().Should().Contain("virtual", "first caveat should mention virtual");
        caveats[1].GetString().Should().Contain("reflection", "second caveat should mention reflection");
    }

    [Fact]
    public void FormatResult_WithIncludeNonImpactfulFalse_FiltersNonBreaking()
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
                            Name = "BreakingChange",
                            Impact = ChangeImpact.BreakingPublicApi,
                            OldContent = "public void BreakingChange() { }",
                            OldLocation = new Location { StartLine = 10, EndLine = 10 }
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "NonBreakingChange",
                            Impact = ChangeImpact.NonBreaking,
                            OldContent = "private void NonBreakingChange() { }",
                            NewContent = "private void NonBreakingChange() { /* comment */ }",
                            OldLocation = new Location { StartLine = 20, EndLine = 20 },
                            NewLocation = new Location { StartLine = 20, EndLine = 20 }
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "FormattingChange",
                            Impact = ChangeImpact.FormattingOnly,
                            OldContent = "public void FormattingChange() {}",
                            NewContent = "public void FormattingChange() { }",
                            OldLocation = new Location { StartLine = 30, EndLine = 30 },
                            NewLocation = new Location { StartLine = 30, EndLine = 30 }
                        }
                    ]
                }
            ]
        };
        var options = new OutputOptions { IncludeNonImpactful = false };

        // Act
        var json = _formatter.FormatResult(result, options);
        using var doc = JsonDocument.Parse(json);

        // Assert
        var changes = doc.RootElement.GetProperty("files")[0].GetProperty("changes");
        changes.GetArrayLength().Should().Be(1, "only breaking changes should be included when IncludeNonImpactful=false");
        changes[0].GetProperty("name").GetString().Should().Be("BreakingChange", "the breaking change should be included");
    }

    #endregion
}
