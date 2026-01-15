namespace RoslynDiff.Integration.Tests;

using System.Text.Json;
using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;
using Xunit;

/// <summary>
/// End-to-end integration tests for line-based diffing of non-.NET files.
/// Tests the full pipeline with .txt, .json, .xml, and other text files.
/// </summary>
public class LineDiffIntegrationTests
{
    private readonly DifferFactory _factory = new();
    private readonly OutputFormatterFactory _formatterFactory = new();

    #region Helper Methods

    /// <summary>
    /// Gets the path to a local test fixture file.
    /// </summary>
    private static string GetLocalFixturePath(string relativePath)
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, "TestFixtures", relativePath);
    }

    /// <summary>
    /// Creates diff options with file paths set.
    /// </summary>
    private static DiffOptions CreateOptions(string oldPath, string newPath) => new()
    {
        OldPath = oldPath,
        NewPath = newPath
    };

    #endregion

    #region Text File Tests

    [Fact]
    public async Task DiffTextFiles_WithChanges_DetectsLineDifferences()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("Text/Sample_Old.txt");
        var newPath = GetLocalFixturePath("Text/Sample_New.txt");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Mode.Should().Be(DiffMode.Line);
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
        result.FileChanges.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task DiffTextFiles_WithChanges_ProducesValidJsonOutput()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("Text/Sample_Old.txt");
        var newPath = GetLocalFixturePath("Text/Sample_New.txt");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        var formatter = _formatterFactory.GetFormatter("json");
        var json = formatter.FormatResult(result, new OutputOptions { PrettyPrint = true, IncludeContent = true });

        // Assert
        var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("metadata").GetProperty("mode").GetString().Should().Be("line");
        doc.RootElement.GetProperty("summary").GetProperty("totalChanges").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DiffTextFiles_WhenIdentical_ReturnsNoChanges()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("Text/Identical_Old.txt");
        var newPath = GetLocalFixturePath("Text/Identical_New.txt");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Mode.Should().Be(DiffMode.Line);
        result.Stats.TotalChanges.Should().Be(0);
    }

    #endregion

    #region JSON File Tests

    [Fact]
    public async Task DiffJsonFiles_WithChanges_DetectsLineDifferences()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("Json/Config_Old.json");
        var newPath = GetLocalFixturePath("Json/Config_New.json");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Mode.Should().Be(DiffMode.Line);
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DiffJsonFiles_ProducesValidHtmlOutput()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("Json/Config_Old.json");
        var newPath = GetLocalFixturePath("Json/Config_New.json");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        var formatter = _formatterFactory.GetFormatter("html");
        var html = formatter.FormatResult(result, new OutputOptions { IncludeContent = true, IncludeStats = true });

        // Assert
        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("Diff Report");
    }

    #endregion

    #region XML File Tests

    [Fact]
    public async Task DiffXmlFiles_WithChanges_DetectsLineDifferences()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("Xml/Project_Old.xml");
        var newPath = GetLocalFixturePath("Xml/Project_New.xml");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Mode.Should().Be(DiffMode.Line);
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region DifferFactory Line Mode Tests

    [Fact]
    public void DifferFactory_ForTextFile_ReturnsLineDiffer()
    {
        // Arrange
        var options = new DiffOptions();

        // Act
        var differ = _factory.GetDiffer("file.txt", options);

        // Assert
        differ.Should().BeOfType<LineDiffer>();
    }

    [Fact]
    public void DifferFactory_ForJsonFile_ReturnsLineDiffer()
    {
        // Arrange
        var options = new DiffOptions();

        // Act
        var differ = _factory.GetDiffer("config.json", options);

        // Assert
        differ.Should().BeOfType<LineDiffer>();
    }

    [Fact]
    public void DifferFactory_ForXmlFile_ReturnsLineDiffer()
    {
        // Arrange
        var options = new DiffOptions();

        // Act
        var differ = _factory.GetDiffer("project.xml", options);

        // Assert
        differ.Should().BeOfType<LineDiffer>();
    }

    [Fact]
    public void DifferFactory_ForCsFileWithLineMode_ReturnsLineDiffer()
    {
        // Arrange - Force line mode even for .cs files
        var options = new DiffOptions { Mode = DiffMode.Line };

        // Act
        var differ = _factory.GetDiffer("file.cs", options);

        // Assert
        differ.Should().BeOfType<LineDiffer>();
    }

    #endregion

    #region Output Format Tests

    [Fact]
    public async Task DiffTextFiles_AllFormats_ProduceNonEmptyOutput()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("Text/Sample_Old.txt");
        var newPath = GetLocalFixturePath("Text/Sample_New.txt");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);
        var result = differ.Compare(oldContent, newContent, options);

        // Act & Assert
        foreach (var format in _formatterFactory.SupportedFormats)
        {
            var formatter = _formatterFactory.GetFormatter(format);
            var output = formatter.FormatResult(result, new OutputOptions
            {
                IncludeContent = true,
                PrettyPrint = true
            });

            output.Should().NotBeNullOrWhiteSpace($"Format '{format}' should produce non-empty output");
        }
    }

    [Fact]
    public async Task DiffTextFiles_UnifiedFormat_ProducesUnifiedDiff()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("Text/Sample_Old.txt");
        var newPath = GetLocalFixturePath("Text/Sample_New.txt");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);
        var result = differ.Compare(oldContent, newContent, options);

        // Act
        var formatter = _formatterFactory.GetFormatter("text");
        var output = formatter.FormatResult(result, new OutputOptions { IncludeContent = true });

        // Assert
        output.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Location Information Tests

    [Fact]
    public async Task DiffTextFiles_Changes_HaveLineNumbers()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("Text/Sample_Old.txt");
        var newPath = GetLocalFixturePath("Text/Sample_New.txt");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        var allChanges = result.FileChanges.SelectMany(fc => fc.Changes).ToList();
        allChanges.Should().NotBeEmpty();

        // Line-based diff changes should have location information
        foreach (var change in allChanges.Where(c => c.Type != ChangeType.Unchanged))
        {
            var location = change.NewLocation ?? change.OldLocation;
            location.Should().NotBeNull();
            location!.StartLine.Should().BeGreaterThan(0);
        }
    }

    #endregion

    #region Empty Content Tests

    [Fact]
    public void DiffTextFiles_BothEmpty_ReturnsNoChanges()
    {
        // Arrange
        var differ = _factory.GetDiffer("file.txt", new DiffOptions());
        var options = new DiffOptions { OldPath = "old.txt", NewPath = "new.txt" };

        // Act
        var result = differ.Compare("", "", options);

        // Assert
        result.Mode.Should().Be(DiffMode.Line);
        result.Stats.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void DiffTextFiles_OldEmpty_DetectsAllAsAdditions()
    {
        // Arrange
        var differ = _factory.GetDiffer("file.txt", new DiffOptions());
        var options = new DiffOptions { OldPath = "old.txt", NewPath = "new.txt" };
        var newContent = "Line 1\nLine 2\nLine 3";

        // Act
        var result = differ.Compare("", newContent, options);

        // Assert
        result.Mode.Should().Be(DiffMode.Line);
        result.Stats.Additions.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DiffTextFiles_NewEmpty_DetectsAllAsDeletions()
    {
        // Arrange
        var differ = _factory.GetDiffer("file.txt", new DiffOptions());
        var options = new DiffOptions { OldPath = "old.txt", NewPath = "new.txt" };
        var oldContent = "Line 1\nLine 2\nLine 3";

        // Act
        var result = differ.Compare(oldContent, "", options);

        // Assert
        result.Mode.Should().Be(DiffMode.Line);
        result.Stats.Deletions.Should().BeGreaterThan(0);
    }

    #endregion
}
