namespace RoslynDiff.Integration.Tests;

using System.Text.Json;
using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;
using Xunit;

/// <summary>
/// End-to-end integration tests for C# file diffing.
/// Tests the full pipeline from file reading through diff generation to output formatting.
/// </summary>
public class CSharpDiffIntegrationTests
{
    private readonly DifferFactory _factory = new();
    private readonly OutputFormatterFactory _formatterFactory = new();

    #region Helper Methods

    /// <summary>
    /// Gets the path to a test fixture file relative to the Core.Tests TestFixtures directory.
    /// </summary>
    private static string GetFixturePath(string relativePath)
    {
        var baseDir = AppContext.BaseDirectory;
        // The Core.Tests test fixtures are in the Core.Tests project
        // We need to navigate to them from the Integration.Tests output directory
        var coreTestsFixtures = Path.GetFullPath(
            Path.Combine(baseDir, "..", "..", "..", "..", "RoslynDiff.Core.Tests", "TestFixtures", relativePath));

        if (File.Exists(coreTestsFixtures))
        {
            return coreTestsFixtures;
        }

        // Try bin/Debug path
        coreTestsFixtures = Path.GetFullPath(
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "tests", "RoslynDiff.Core.Tests", "TestFixtures", relativePath));

        return coreTestsFixtures;
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

    #region Class Addition Tests

    [Fact]
    public async Task DiffCSharpFiles_WithClassAdded_ProducesCorrectJsonOutput()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_Old.cs");
        var newPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_New.cs");

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

        // Verify metadata section exists
        doc.RootElement.GetProperty("metadata").Should().NotBeNull();
        doc.RootElement.GetProperty("metadata").GetProperty("mode").GetString().Should().Be("roslyn");

        // Verify summary section
        var summary = doc.RootElement.GetProperty("summary");
        summary.GetProperty("additions").GetInt32().Should().BeGreaterThan(0);

        // Verify files section
        var files = doc.RootElement.GetProperty("files");
        files.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        // Verify Calculator class was detected as added
        var changes = files[0].GetProperty("changes");
        changes.EnumerateArray()
            .Should()
            .Contain(c =>
                c.GetProperty("type").GetString() == "added" &&
                c.GetProperty("kind").GetString() == "class" &&
                c.GetProperty("name").GetString() == "Calculator");
    }

    [Fact]
    public async Task DiffCSharpFiles_WithClassAdded_ProducesValidHtmlOutput()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_Old.cs");
        var newPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_New.cs");

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
        html.Should().Contain("<html");
        html.Should().Contain("</html>");
        html.Should().Contain("Diff Report");
        html.Should().Contain("added"); // Badge for added changes
        html.Should().Contain("Calculator"); // The added class name
    }

    #endregion

    #region Class Removal Tests

    [Fact]
    public async Task DiffCSharpFiles_WithClassRemoved_DetectsRemoval()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/ClassChanges/ClassRemoved_Old.cs");
        var newPath = GetFixturePath("CSharp/ClassChanges/ClassRemoved_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.Deletions.Should().BeGreaterThan(0);
        result.FileChanges.Should().HaveCountGreaterThanOrEqualTo(1);

        var changes = result.FileChanges[0].Changes;
        changes.Should().Contain(c =>
            c.Type == ChangeType.Removed &&
            c.Kind == ChangeKind.Class);
    }

    #endregion

    #region Class Modification Tests

    [Fact]
    public async Task DiffCSharpFiles_WithClassModified_DetectsModification()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/ClassChanges/ClassModified_Old.cs");
        var newPath = GetFixturePath("CSharp/ClassChanges/ClassModified_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
        result.Mode.Should().Be(DiffMode.Roslyn);
    }

    #endregion

    #region Method Change Tests

    [Fact]
    public async Task DiffCSharpFiles_WithMethodAdded_DetectsMethodAddition()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/MethodChanges/MethodAdded_Old.cs");
        var newPath = GetFixturePath("CSharp/MethodChanges/MethodAdded_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
        result.FileChanges.SelectMany(fc => fc.Changes)
            .Should().Contain(c =>
                c.Type == ChangeType.Added &&
                c.Kind == ChangeKind.Method);
    }

    [Fact]
    public async Task DiffCSharpFiles_WithMethodRemoved_DetectsMethodRemoval()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/MethodChanges/MethodRemoved_Old.cs");
        var newPath = GetFixturePath("CSharp/MethodChanges/MethodRemoved_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.Deletions.Should().BeGreaterThan(0);
        result.FileChanges.SelectMany(fc => fc.Changes)
            .Should().Contain(c =>
                c.Type == ChangeType.Removed &&
                c.Kind == ChangeKind.Method);
    }

    [Fact]
    public async Task DiffCSharpFiles_WithMethodModified_DetectsMethodModification()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/MethodChanges/MethodModified_Old.cs");
        var newPath = GetFixturePath("CSharp/MethodChanges/MethodModified_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Property Change Tests

    [Fact]
    public async Task DiffCSharpFiles_WithPropertyAdded_DetectsPropertyAddition()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/PropertyChanges/PropertyAdded_Old.cs");
        var newPath = GetFixturePath("CSharp/PropertyChanges/PropertyAdded_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
        result.FileChanges.SelectMany(fc => fc.Changes)
            .Should().Contain(c =>
                c.Type == ChangeType.Added &&
                c.Kind == ChangeKind.Property);
    }

    #endregion

    #region Complex Change Tests

    [Fact]
    public async Task DiffCSharpFiles_WithMultipleChanges_DetectsAllChanges()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/ComplexChanges/MultipleChanges_Old.cs");
        var newPath = GetFixturePath("CSharp/ComplexChanges/MultipleChanges_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);

        // Verify JSON output contains multiple changes
        var formatter = _formatterFactory.GetFormatter("json");
        var json = formatter.FormatResult(result, new OutputOptions { PrettyPrint = true });
        var doc = JsonDocument.Parse(json);

        var summary = doc.RootElement.GetProperty("summary");
        summary.GetProperty("totalChanges").GetInt32().Should().BeGreaterThan(0);
    }

    #endregion

    #region Identical File Tests

    [Fact]
    public async Task DiffCSharpFiles_WhenIdentical_ReturnsNoChanges()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/IdenticalFiles/Identical_Old.cs");
        var newPath = GetFixturePath("CSharp/IdenticalFiles/Identical_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().Be(0);
        result.Mode.Should().Be(DiffMode.Roslyn);
    }

    #endregion

    #region Nested Class Tests

    [Fact]
    public async Task DiffCSharpFiles_WithNestedClassChanges_DetectsNestedChanges()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/NestedClasses/NestedClass_Old.cs");
        var newPath = GetFixturePath("CSharp/NestedClasses/NestedClass_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
        result.Mode.Should().Be(DiffMode.Roslyn);
    }

    #endregion

    #region Output Format Tests

    [Fact]
    public async Task DiffCSharpFiles_PlainTextOutput_ProducesReadableText()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_Old.cs");
        var newPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        var formatter = _formatterFactory.GetFormatter("plain");
        var text = formatter.FormatResult(result, new OutputOptions());

        // Assert
        text.Should().NotBeNullOrWhiteSpace();
        // Plain text should contain change indicators
        text.Should().ContainAny("+", "-", "Added", "Removed", "Modified", "added", "removed", "modified");
    }

    [Fact]
    public async Task DiffCSharpFiles_UnifiedTextOutput_ProducesUnifiedDiffFormat()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_Old.cs");
        var newPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        var formatter = _formatterFactory.GetFormatter("text");
        var text = formatter.FormatResult(result, new OutputOptions { IncludeContent = true });

        // Assert
        text.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DiffCSharpFiles_TerminalOutput_ProducesSpectreConsoleOutput()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_Old.cs");
        var newPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        var formatter = _formatterFactory.GetFormatter("terminal");
        var output = formatter.FormatResult(result, new OutputOptions { UseColor = true });

        // Assert
        output.Should().NotBeNullOrWhiteSpace();
        // Terminal output might contain ANSI escape codes when colors are enabled
        // The output should at least contain some meaningful content
        output.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region Location Information Tests

    [Fact]
    public async Task DiffCSharpFiles_Changes_HaveLocationInformation()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_Old.cs");
        var newPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        var addedChanges = result.FileChanges
            .SelectMany(fc => fc.Changes)
            .Where(c => c.Type == ChangeType.Added)
            .ToList();

        addedChanges.Should().NotBeEmpty();

        // Added items should have new location
        foreach (var change in addedChanges)
        {
            change.NewLocation.Should().NotBeNull();
            change.NewLocation!.StartLine.Should().BeGreaterThan(0);
        }
    }

    #endregion

    #region Async Formatting Tests

    [Fact]
    public async Task DiffCSharpFiles_AsyncJsonFormatting_ProducesSameStructure()
    {
        // Arrange
        var oldPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_Old.cs");
        var newPath = GetFixturePath("CSharp/ClassChanges/ClassAdded_New.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        var result = differ.Compare(oldContent, newContent, options);
        var formatter = _formatterFactory.GetFormatter("json");
        var outputOptions = new OutputOptions { PrettyPrint = true };

        // Act
        var syncOutput = formatter.FormatResult(result, outputOptions);

        using var writer = new StringWriter();
        await formatter.FormatResultAsync(result, writer, outputOptions);
        var asyncOutput = writer.ToString();

        // Assert - Both should be valid JSON with same structure
        var syncDoc = JsonDocument.Parse(syncOutput);
        var asyncDoc = JsonDocument.Parse(asyncOutput);

        // Compare structure (metadata, summary, files sections exist)
        syncDoc.RootElement.TryGetProperty("metadata", out _).Should().BeTrue();
        asyncDoc.RootElement.TryGetProperty("metadata", out _).Should().BeTrue();

        syncDoc.RootElement.TryGetProperty("summary", out _).Should().BeTrue();
        asyncDoc.RootElement.TryGetProperty("summary", out _).Should().BeTrue();

        syncDoc.RootElement.TryGetProperty("files", out _).Should().BeTrue();
        asyncDoc.RootElement.TryGetProperty("files", out _).Should().BeTrue();

        // Summary values should match (not timestamp-dependent)
        var syncSummary = syncDoc.RootElement.GetProperty("summary");
        var asyncSummary = asyncDoc.RootElement.GetProperty("summary");
        syncSummary.GetProperty("totalChanges").GetInt32().Should().Be(asyncSummary.GetProperty("totalChanges").GetInt32());
    }

    #endregion
}
