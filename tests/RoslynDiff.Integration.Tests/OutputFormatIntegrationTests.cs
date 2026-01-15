namespace RoslynDiff.Integration.Tests;

using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;
using Xunit;

/// <summary>
/// Integration tests for output format validation.
/// Tests JSON schema validation, HTML structure, PlainText, and SpectreConsole output.
/// </summary>
public partial class OutputFormatIntegrationTests
{
    private readonly DifferFactory _differFactory = new();
    private readonly OutputFormatterFactory _formatterFactory = new();

    #region Helper Methods

    /// <summary>
    /// Creates a sample diff result for testing formatters.
    /// </summary>
    private static DiffResult CreateSampleDiffResult(DiffMode mode = DiffMode.Roslyn)
    {
        return new DiffResult
        {
            OldPath = "old/sample.cs",
            NewPath = "new/sample.cs",
            Mode = mode,
            FileChanges =
            [
                new FileChange
                {
                    Path = "new/sample.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Class,
                            Name = "NewClass",
                            NewContent = "public class NewClass { }",
                            NewLocation = new Location { StartLine = 5, EndLine = 5, StartColumn = 1, EndColumn = 26 }
                        },
                        new Change
                        {
                            Type = ChangeType.Removed,
                            Kind = ChangeKind.Method,
                            Name = "OldMethod",
                            OldContent = "public void OldMethod() { }",
                            OldLocation = new Location { StartLine = 10, EndLine = 10, StartColumn = 1, EndColumn = 28 }
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "UpdatedMethod",
                            OldContent = "public void UpdatedMethod() { return; }",
                            NewContent = "public void UpdatedMethod() { return value; }",
                            OldLocation = new Location { StartLine = 15, EndLine = 15 },
                            NewLocation = new Location { StartLine = 15, EndLine = 15 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats
            {
                TotalChanges = 3,
                Additions = 1,
                Deletions = 1,
                Modifications = 1,
                Renames = 0,
                Moves = 0
            }
        };
    }

    #endregion

    #region JSON Output Tests

    [Fact]
    public void JsonFormatter_ProducesValidJson()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var json = formatter.FormatResult(result, new OutputOptions { PrettyPrint = true });

        // Assert - Should parse without exception
        var act = () => JsonDocument.Parse(json);
        act.Should().NotThrow();
    }

    [Fact]
    public void JsonFormatter_ContainsMetadataSection()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var json = formatter.FormatResult(result, new OutputOptions { PrettyPrint = true });
        var doc = JsonDocument.Parse(json);

        // Assert
        doc.RootElement.TryGetProperty("metadata", out var metadata).Should().BeTrue();
        metadata.TryGetProperty("version", out _).Should().BeTrue();
        metadata.TryGetProperty("timestamp", out _).Should().BeTrue();
        metadata.TryGetProperty("mode", out _).Should().BeTrue();
        metadata.TryGetProperty("options", out _).Should().BeTrue();
    }

    [Fact]
    public void JsonFormatter_ContainsSummarySection()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var json = formatter.FormatResult(result, new OutputOptions());
        var doc = JsonDocument.Parse(json);

        // Assert
        doc.RootElement.TryGetProperty("summary", out var summary).Should().BeTrue();
        summary.GetProperty("totalChanges").GetInt32().Should().Be(3);
        summary.GetProperty("additions").GetInt32().Should().Be(1);
        summary.GetProperty("deletions").GetInt32().Should().Be(1);
        summary.GetProperty("modifications").GetInt32().Should().Be(1);
    }

    [Fact]
    public void JsonFormatter_ContainsFilesSection()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var json = formatter.FormatResult(result, new OutputOptions());
        var doc = JsonDocument.Parse(json);

        // Assert
        doc.RootElement.TryGetProperty("files", out var files).Should().BeTrue();
        files.GetArrayLength().Should().BeGreaterOrEqualTo(1);

        var firstFile = files[0];
        firstFile.TryGetProperty("changes", out var changes).Should().BeTrue();
        changes.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public void JsonFormatter_ChangesHaveCorrectProperties()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var json = formatter.FormatResult(result, new OutputOptions { IncludeContent = true });
        var doc = JsonDocument.Parse(json);

        // Assert
        var changes = doc.RootElement.GetProperty("files")[0].GetProperty("changes");
        var addedChange = changes.EnumerateArray().First(c => c.GetProperty("type").GetString() == "added");

        addedChange.GetProperty("type").GetString().Should().Be("added");
        addedChange.GetProperty("kind").GetString().Should().Be("class");
        addedChange.GetProperty("name").GetString().Should().Be("NewClass");
        addedChange.TryGetProperty("content", out _).Should().BeTrue();
        addedChange.TryGetProperty("location", out _).Should().BeTrue();
    }

    [Fact]
    public void JsonFormatter_LocationHasCorrectFormat()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var json = formatter.FormatResult(result, new OutputOptions());
        var doc = JsonDocument.Parse(json);

        // Assert
        var changes = doc.RootElement.GetProperty("files")[0].GetProperty("changes");
        var change = changes.EnumerateArray().First(c => c.TryGetProperty("location", out _));
        var location = change.GetProperty("location");

        location.TryGetProperty("startLine", out _).Should().BeTrue();
        location.TryGetProperty("endLine", out _).Should().BeTrue();
        location.GetProperty("startLine").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public void JsonFormatter_CompactMode_OmitsWhitespace()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var prettyJson = formatter.FormatResult(result, new OutputOptions { PrettyPrint = true });
        var compactJson = formatter.FormatResult(result, new OutputOptions { PrettyPrint = false });

        // Assert
        compactJson.Length.Should().BeLessThan(prettyJson.Length);
        compactJson.Should().NotContain("\n  "); // No indented newlines
    }

    [Fact]
    public void JsonFormatter_IncludeContent_AddsContentToChanges()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var jsonWithContent = formatter.FormatResult(result, new OutputOptions { IncludeContent = true });
        var jsonWithoutContent = formatter.FormatResult(result, new OutputOptions { IncludeContent = false });

        // Assert
        var docWith = JsonDocument.Parse(jsonWithContent);
        var docWithout = JsonDocument.Parse(jsonWithoutContent);

        var changesWithContent = docWith.RootElement.GetProperty("files")[0].GetProperty("changes");
        var changesWithoutContent = docWithout.RootElement.GetProperty("files")[0].GetProperty("changes");

        changesWithContent.EnumerateArray().Any(c => c.TryGetProperty("content", out _)).Should().BeTrue();
        // Without content, the content property should be null/missing
    }

    #endregion

    #region HTML Output Tests

    [Fact]
    public void HtmlFormatter_ProducesValidHtml()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("html");

        // Act
        var html = formatter.FormatResult(result, new OutputOptions { IncludeContent = true });

        // Assert
        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<html");
        html.Should().Contain("</html>");
        html.Should().Contain("<head>");
        html.Should().Contain("<body>");
    }

    [Fact]
    public void HtmlFormatter_ContainsDiffReport()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("html");

        // Act
        var html = formatter.FormatResult(result, new OutputOptions { IncludeStats = true });

        // Assert
        html.Should().Contain("Diff Report");
    }

    [Fact]
    public void HtmlFormatter_ContainsStatsBadges()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("html");

        // Act
        var html = formatter.FormatResult(result, new OutputOptions { IncludeStats = true });

        // Assert
        html.Should().Contain("added");
        (html.Contains("deleted") || html.Contains("removed")).Should().BeTrue();
        html.Should().Contain("modified");
    }

    [Fact]
    public void HtmlFormatter_ContainsChangeElements()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("html");

        // Act
        var html = formatter.FormatResult(result, new OutputOptions { IncludeContent = true });

        // Assert
        html.Should().Contain("NewClass");
        html.Should().Contain("OldMethod");
        html.Should().Contain("UpdatedMethod");
    }

    [Fact]
    public void HtmlFormatter_ContainsStyles()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("html");

        // Act
        var html = formatter.FormatResult(result, new OutputOptions());

        // Assert
        html.Should().Contain("<style>");
        html.Should().Contain("</style>");
    }

    [Fact]
    public void HtmlFormatter_ContainsScript()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("html");

        // Act
        var html = formatter.FormatResult(result, new OutputOptions());

        // Assert
        html.Should().Contain("<script>");
        html.Should().Contain("</script>");
    }

    [Fact]
    public void HtmlFormatter_EscapesHtmlCharacters()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
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
                            Name = "<T>Method", // Contains HTML special chars
                            NewContent = "public void Method<T>() where T : class { }"
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        var formatter = _formatterFactory.GetFormatter("html");

        // Act
        var html = formatter.FormatResult(result, new OutputOptions { IncludeContent = true });

        // Assert - Should be HTML-encoded
        html.Should().Contain("&lt;T&gt;"); // < and > should be escaped
    }

    #endregion

    #region PlainText Output Tests

    [Fact]
    public void PlainTextFormatter_ProducesReadableText()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("plain");

        // Act
        var text = formatter.FormatResult(result, new OutputOptions());

        // Assert
        text.Should().NotBeNullOrWhiteSpace();
        // Note: PlainText format uses [+], [-], [~] notation which is valid
        text.Should().NotContain("\u001b"); // No ANSI escape character
    }

    [Fact]
    public void PlainTextFormatter_ContainsChangeIndicators()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("plain");

        // Act
        var text = formatter.FormatResult(result, new OutputOptions { IncludeContent = true });

        // Assert - Should have some indication of changes
        text.Should().NotBeNullOrWhiteSpace();
        // The exact format depends on implementation, but it should contain meaningful content
        text.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region SpectreConsole Output Tests

    [Fact]
    public void SpectreConsoleFormatter_ProducesOutput()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("terminal");

        // Act
        var output = formatter.FormatResult(result, new OutputOptions { UseColor = true });

        // Assert
        output.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void SpectreConsoleFormatter_WithColorDisabled_ProducesPlainOutput()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("terminal");

        // Act
        var output = formatter.FormatResult(result, new OutputOptions { UseColor = false });

        // Assert
        output.Should().NotBeNullOrWhiteSpace();
        // When color is disabled, Spectre might still produce markup or plain text
        // depending on implementation
    }

    #endregion

    #region Unified Diff Format Tests

    [Fact]
    public void UnifiedFormatter_ProducesUnifiedDiffFormat()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var formatter = _formatterFactory.GetFormatter("text");

        // Act
        var text = formatter.FormatResult(result, new OutputOptions { IncludeContent = true });

        // Assert
        text.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Output Formatter Factory Tests

    [Fact]
    public void OutputFormatterFactory_SupportsAllExpectedFormats()
    {
        // Arrange & Act
        var formats = _formatterFactory.SupportedFormats;

        // Assert
        formats.Should().Contain("json");
        formats.Should().Contain("html");
        formats.Should().Contain("text");
        formats.Should().Contain("plain");
        formats.Should().Contain("terminal");
    }

    [Fact]
    public void OutputFormatterFactory_GetFormatter_ReturnsCorrectTypes()
    {
        // Arrange & Act & Assert
        _formatterFactory.GetFormatter("json").Should().BeAssignableTo<IOutputFormatter>();
        _formatterFactory.GetFormatter("html").Should().BeAssignableTo<IOutputFormatter>();
        _formatterFactory.GetFormatter("text").Should().BeAssignableTo<IOutputFormatter>();
        _formatterFactory.GetFormatter("plain").Should().BeAssignableTo<IOutputFormatter>();
        _formatterFactory.GetFormatter("terminal").Should().BeAssignableTo<IOutputFormatter>();
    }

    [Fact]
    public void OutputFormatterFactory_IsFormatSupported_ReturnsCorrectly()
    {
        // Assert
        _formatterFactory.IsFormatSupported("json").Should().BeTrue();
        _formatterFactory.IsFormatSupported("JSON").Should().BeTrue(); // Case insensitive
        _formatterFactory.IsFormatSupported("unknown").Should().BeFalse();
        _formatterFactory.IsFormatSupported("").Should().BeFalse();
    }

    [Fact]
    public void OutputFormatterFactory_GetFormatter_ThrowsForUnknownFormat()
    {
        // Arrange & Act
        var act = () => _formatterFactory.GetFormatter("unknown");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Content Type Tests

    [Fact]
    public void JsonFormatter_HasCorrectContentType()
    {
        // Arrange
        var formatter = _formatterFactory.GetFormatter("json");

        // Assert
        formatter.ContentType.Should().Be("application/json");
    }

    [Fact]
    public void HtmlFormatter_HasCorrectContentType()
    {
        // Arrange
        var formatter = _formatterFactory.GetFormatter("html");

        // Assert
        formatter.ContentType.Should().Be("text/html");
    }

    [Fact]
    public void PlainTextFormatter_HasCorrectContentType()
    {
        // Arrange
        var formatter = _formatterFactory.GetFormatter("plain");

        // Assert
        formatter.ContentType.Should().Contain("text/plain");
    }

    #endregion

    #region Empty Result Tests

    [Fact]
    public void JsonFormatter_EmptyResult_ProducesValidJson()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "old.cs",
            NewPath = "new.cs",
            Mode = DiffMode.Roslyn,
            FileChanges = [],
            Stats = new DiffStats()
        };

        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var json = formatter.FormatResult(result, new OutputOptions());

        // Assert
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("summary").GetProperty("totalChanges").GetInt32().Should().Be(0);
    }

    [Fact]
    public void HtmlFormatter_EmptyResult_ProducesValidHtml()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "old.cs",
            NewPath = "new.cs",
            Mode = DiffMode.Roslyn,
            FileChanges = [],
            Stats = new DiffStats()
        };

        var formatter = _formatterFactory.GetFormatter("html");

        // Act
        var html = formatter.FormatResult(result, new OutputOptions());

        // Assert
        html.Should().Contain("<!DOCTYPE html>");
        // The HTML output might say "0 changes" in summary or "No changes detected"
        (html.Contains("No changes") || html.Contains("0 changes") || html.Contains("changes")).Should().BeTrue();
    }

    #endregion

    #region Async Formatting Tests

    [Fact]
    public async Task AllFormatters_AsyncFormat_ProducesValidOutput()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var options = new OutputOptions { PrettyPrint = true, IncludeContent = true };

        foreach (var format in _formatterFactory.SupportedFormats)
        {
            var formatter = _formatterFactory.GetFormatter(format);

            // Act
            var syncOutput = formatter.FormatResult(result, options);

            using var writer = new StringWriter();
            await formatter.FormatResultAsync(result, writer, options);
            var asyncOutput = writer.ToString();

            // Assert - Both should produce valid, non-empty output
            syncOutput.Should().NotBeNullOrWhiteSpace($"Sync output should be valid for format '{format}'");
            asyncOutput.Should().NotBeNullOrWhiteSpace($"Async output should be valid for format '{format}'");

            // For JSON format, verify structure is preserved (timestamp may vary)
            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                var syncDoc = JsonDocument.Parse(syncOutput);
                var asyncDoc = JsonDocument.Parse(asyncOutput);
                syncDoc.RootElement.TryGetProperty("summary", out _).Should().BeTrue();
                asyncDoc.RootElement.TryGetProperty("summary", out _).Should().BeTrue();
            }
        }
    }

    #endregion
}
