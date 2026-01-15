namespace RoslynDiff.Integration.Tests;

using System.Text.Json;
using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;
using Xunit;

/// <summary>
/// End-to-end integration tests for VB.NET file diffing.
/// Tests the full pipeline from file reading through diff generation to output formatting.
/// </summary>
public class VisualBasicDiffIntegrationTests
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

    #region Class Modification Tests

    [Fact]
    public async Task DiffVbFiles_WithClassModified_DetectsModification()
    {
        // Arrange
        var oldPath = GetFixturePath("VisualBasic/ClassChanges/ClassModified_Old.vb");
        var newPath = GetFixturePath("VisualBasic/ClassChanges/ClassModified_New.vb");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Mode.Should().Be(DiffMode.Roslyn);
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DiffVbFiles_WithClassModified_ProducesValidJsonOutput()
    {
        // Arrange
        var oldPath = GetFixturePath("VisualBasic/ClassChanges/ClassModified_Old.vb");
        var newPath = GetFixturePath("VisualBasic/ClassChanges/ClassModified_New.vb");

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

        doc.RootElement.GetProperty("metadata").Should().NotBeNull();
        doc.RootElement.GetProperty("metadata").GetProperty("mode").GetString().Should().Be("roslyn");

        var summary = doc.RootElement.GetProperty("summary");
        summary.GetProperty("totalChanges").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DiffVbFiles_WithClassModified_ProducesValidHtmlOutput()
    {
        // Arrange
        var oldPath = GetFixturePath("VisualBasic/ClassChanges/ClassModified_Old.vb");
        var newPath = GetFixturePath("VisualBasic/ClassChanges/ClassModified_New.vb");

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
    }

    #endregion

    #region Module Tests

    [Fact]
    public async Task DiffVbFiles_WithModuleAdded_DetectsAddition()
    {
        // Arrange
        var oldPath = GetFixturePath("VisualBasic/ModuleChanges/ModuleAdded_Old.vb");
        var newPath = GetFixturePath("VisualBasic/ModuleChanges/ModuleAdded_New.vb");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
        result.Mode.Should().Be(DiffMode.Roslyn);
    }

    #endregion

    #region Sub/Function Tests

    [Fact]
    public async Task DiffVbFiles_WithSubAdded_DetectsSubAddition()
    {
        // Arrange
        var oldPath = GetFixturePath("VisualBasic/SubFunctionChanges/SubAdded_Old.vb");
        var newPath = GetFixturePath("VisualBasic/SubFunctionChanges/SubAdded_New.vb");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.Additions.Should().BeGreaterThan(0);
        result.Mode.Should().Be(DiffMode.Roslyn);
    }

    [Fact]
    public async Task DiffVbFiles_WithFunctionRemoved_DetectsFunctionRemoval()
    {
        // Arrange
        var oldPath = GetFixturePath("VisualBasic/SubFunctionChanges/FunctionRemoved_Old.vb");
        var newPath = GetFixturePath("VisualBasic/SubFunctionChanges/FunctionRemoved_New.vb");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Stats.Deletions.Should().BeGreaterThan(0);
        result.Mode.Should().Be(DiffMode.Roslyn);
    }

    #endregion

    #region Property Tests

    [Fact]
    public async Task DiffVbFiles_WithPropertyChanged_DetectsPropertyChange()
    {
        // Arrange
        var oldPath = GetFixturePath("VisualBasic/PropertyChanges/PropertyChanged_Old.vb");
        var newPath = GetFixturePath("VisualBasic/PropertyChanges/PropertyChanged_New.vb");

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

    #region Identical File Tests

    [Fact]
    public async Task DiffVbFiles_WhenIdentical_ReturnsNoChanges()
    {
        // Arrange
        var oldPath = GetFixturePath("VisualBasic/IdenticalFiles/Identical_Old.vb");
        var newPath = GetFixturePath("VisualBasic/IdenticalFiles/Identical_New.vb");

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

    #region DifferFactory Tests

    [Fact]
    public void DifferFactory_ForVbFile_ReturnsVisualBasicDiffer()
    {
        // Arrange
        var options = new DiffOptions();

        // Act
        var differ = _factory.GetDiffer("file.vb", options);

        // Assert
        differ.Should().BeOfType<VisualBasicDiffer>();
    }

    [Fact]
    public void DifferFactory_ForVbFileWithLineMode_ReturnsLineDiffer()
    {
        // Arrange
        var options = new DiffOptions { Mode = DiffMode.Line };

        // Act
        var differ = _factory.GetDiffer("file.vb", options);

        // Assert
        differ.Should().BeOfType<LineDiffer>();
    }

    #endregion

    #region Output Format Tests

    [Fact]
    public async Task DiffVbFiles_AllFormats_ProduceNonEmptyOutput()
    {
        // Arrange
        var oldPath = GetFixturePath("VisualBasic/ClassChanges/ClassModified_Old.vb");
        var newPath = GetFixturePath("VisualBasic/ClassChanges/ClassModified_New.vb");

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

    #endregion

    #region Empty File Tests

    [Fact]
    public async Task DiffVbFiles_EmptyFiles_HandlesGracefully()
    {
        // Arrange
        var oldPath = GetFixturePath("VisualBasic/EmptyFiles/Empty_Old.vb");
        var newPath = GetFixturePath("VisualBasic/EmptyFiles/Empty_New.vb");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _factory.GetDiffer(newPath, new DiffOptions());
        var options = CreateOptions(oldPath, newPath);

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - Empty files should be handled without throwing
        result.Mode.Should().Be(DiffMode.Roslyn);
        result.Should().NotBeNull();
    }

    #endregion
}
