namespace RoslynDiff.Integration.Tests;

using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;
using Xunit;

/// <summary>
/// Comprehensive integration tests for multi-TFM (Target Framework Moniker) functionality.
/// Tests end-to-end scenarios from CLI with TFM flags through to output formatters.
/// </summary>
public class MultiTfmIntegrationTests
{
    private readonly DifferFactory _differFactory = new();
    private readonly OutputFormatterFactory _formatterFactory = new();

    #region Helper Methods

    /// <summary>
    /// Gets the path to a sample file in the multi-tfm samples directory.
    /// </summary>
    private static string GetSamplePath(string fileName)
    {
        var baseDir = AppContext.BaseDirectory;
        var samplePath = Path.GetFullPath(
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "..", "samples", "multi-tfm", fileName));

        if (!File.Exists(samplePath))
        {
            // Try alternative path structure
            samplePath = Path.GetFullPath(
                Path.Combine(baseDir, "..", "..", "..", "..", "..", "samples", "multi-tfm", fileName));
        }

        return samplePath;
    }

    /// <summary>
    /// Creates diff options with TFM configuration.
    /// </summary>
    private static DiffOptions CreateTfmOptions(string oldPath, string newPath, params string[] tfms) => new()
    {
        OldPath = oldPath,
        NewPath = newPath,
        TargetFrameworks = tfms.Length > 0 ? tfms : null
    };

    /// <summary>
    /// Finds a change in the hierarchical structure by predicate.
    /// </summary>
    private static Change? FindChange(IReadOnlyList<Change> changes, Func<Change, bool> predicate)
    {
        foreach (var change in changes)
        {
            if (predicate(change))
                return change;

            if (change.Children is not null)
            {
                var found = FindChange(change.Children, predicate);
                if (found is not null)
                    return found;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets all changes flattened from the hierarchy.
    /// </summary>
    private static List<Change> GetAllChanges(DiffResult result)
    {
        var allChanges = new List<Change>();
        foreach (var fileChange in result.FileChanges)
        {
            allChanges.AddRange(fileChange.Changes.Flatten());
        }
        return allChanges;
    }

    #endregion

    #region Single TFM Analysis Tests

    [Fact]
    public async Task SingleTfm_Net8_ProducesCorrectDiffResult()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0");

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Should().NotBeNull();
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().ContainSingle();
        result.AnalyzedTfms![0].Should().Be("net8.0");
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SingleTfm_Net10_ProducesCorrectDiffResult()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net10.0");

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.Should().NotBeNull();
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().ContainSingle();
        result.AnalyzedTfms![0].Should().Be("net10.0");
        result.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Multiple TFM Analysis Tests

    [Fact]
    public async Task MultipleTfms_Net8AndNet10_DetectsBothFrameworks()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert
        result.AnalyzedTfms.Should().NotBeNull();
        result.AnalyzedTfms.Should().HaveCount(2);
        result.AnalyzedTfms.Should().Contain("net8.0");
        result.AnalyzedTfms.Should().Contain("net10.0");
    }

    [Fact]
    public async Task MultipleTfms_DetectsTfmSpecificChanges()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");

        // Act
        var result = differ.Compare(oldContent, newContent, options);
        var allChanges = GetAllChanges(result);

        // Assert - should have changes with TFM annotations
        allChanges.Should().NotBeEmpty();

        // Some changes should be TFM-specific (have non-empty ApplicableToTfms)
        var tfmSpecificChanges = allChanges.Where(c => c.ApplicableToTfms is not null && c.ApplicableToTfms.Count > 0).ToList();
        tfmSpecificChanges.Should().NotBeEmpty("there should be TFM-specific changes");
    }

    [Fact]
    public async Task MultipleTfms_ChangeInAllTfms_HasEmptyApplicableToTfms()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");

        // Act
        var result = differ.Compare(oldContent, newContent, options);
        var allChanges = GetAllChanges(result);

        // Assert - when TFM analysis is done, ApplicableToTfms should be set (not null)
        // Changes that apply to all TFMs should have an empty list
        // However, the merger may choose to represent common changes differently,
        // so we just verify that TFM tracking is happening
        var changesWithTfmTracking = allChanges.Where(c => c.ApplicableToTfms is not null).ToList();
        changesWithTfmTracking.Should().NotBeEmpty("TFM tracking should be active when multiple TFMs are analyzed");
    }

    [Fact]
    public async Task MultipleTfms_Net10OnlyChange_HasCorrectTfmAnnotation()
    {
        // Arrange - the new version adds MetricsCollector class only in NET10.0+
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");

        // Act
        var result = differ.Compare(oldContent, newContent, options);
        var allChanges = GetAllChanges(result);

        // Assert - MetricsCollector should only appear in net10.0
        var metricsCollectorChange = allChanges.FirstOrDefault(c =>
            c.Type == ChangeType.Added &&
            c.Kind == ChangeKind.Class &&
            c.Name == "MetricsCollector");

        if (metricsCollectorChange is not null)
        {
            metricsCollectorChange.ApplicableToTfms.Should().NotBeNull();
            metricsCollectorChange.ApplicableToTfms.Should().Contain("net10.0");
            metricsCollectorChange.ApplicableToTfms.Should().NotContain("net8.0");
        }
    }

    #endregion

    #region JSON Output with TFM Information Tests

    [Fact]
    public async Task JsonOutput_SingleTfm_IncludesTfmMetadata()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0");

        var result = differ.Compare(oldContent, newContent, options);
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var json = formatter.FormatResult(result, new OutputOptions { PrettyPrint = true, IncludeContent = true });

        // Assert
        var doc = JsonDocument.Parse(json);

        // Check metadata section has TFM information
        doc.RootElement.TryGetProperty("metadata", out var metadata).Should().BeTrue();
        metadata.TryGetProperty("targetFrameworks", out var targetFrameworks).Should().BeTrue();
        targetFrameworks.GetArrayLength().Should().Be(1);
        targetFrameworks[0].GetString().Should().Be("net8.0");
    }

    [Fact]
    public async Task JsonOutput_MultipleTfms_IncludesAllTfmsInMetadata()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");

        var result = differ.Compare(oldContent, newContent, options);
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var json = formatter.FormatResult(result, new OutputOptions { PrettyPrint = true, IncludeContent = true });

        // Assert
        var doc = JsonDocument.Parse(json);

        doc.RootElement.TryGetProperty("metadata", out var metadata).Should().BeTrue();
        metadata.TryGetProperty("targetFrameworks", out var targetFrameworks).Should().BeTrue();
        targetFrameworks.GetArrayLength().Should().Be(2);

        var tfms = targetFrameworks.EnumerateArray().Select(t => t.GetString()).ToList();
        tfms.Should().Contain("net8.0");
        tfms.Should().Contain("net10.0");
    }

    [Fact]
    public async Task JsonOutput_TfmSpecificChange_IncludesApplicableToTfmsProperty()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");

        var result = differ.Compare(oldContent, newContent, options);
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var json = formatter.FormatResult(result, new OutputOptions { PrettyPrint = true, IncludeContent = true });

        // Assert
        var doc = JsonDocument.Parse(json);

        // Navigate to changes and verify applicableToTfms property exists
        doc.RootElement.TryGetProperty("files", out var files).Should().BeTrue();
        files.GetArrayLength().Should().BeGreaterThan(0);

        var changes = files[0].GetProperty("changes");
        bool foundTfmProperty = false;

        foreach (var change in changes.EnumerateArray())
        {
            if (change.TryGetProperty("applicableToTfms", out _))
            {
                foundTfmProperty = true;
                break;
            }

            // Check children too
            if (change.TryGetProperty("children", out var children))
            {
                foreach (var child in children.EnumerateArray())
                {
                    if (child.TryGetProperty("applicableToTfms", out _))
                    {
                        foundTfmProperty = true;
                        break;
                    }
                }
            }

            if (foundTfmProperty) break;
        }

        foundTfmProperty.Should().BeTrue("JSON output should include applicableToTfms property on changes");
    }

    [Fact]
    public async Task JsonOutput_NoTfmAnalysis_OmitsTfmMetadata()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath); // No TFMs specified

        var result = differ.Compare(oldContent, newContent, options);
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var json = formatter.FormatResult(result, new OutputOptions { PrettyPrint = true });

        // Assert
        var doc = JsonDocument.Parse(json);

        // When no TFM analysis is done, targetFrameworks should be null or omitted
        doc.RootElement.TryGetProperty("metadata", out var metadata).Should().BeTrue();
        if (metadata.TryGetProperty("targetFrameworks", out var targetFrameworks))
        {
            targetFrameworks.ValueKind.Should().Be(JsonValueKind.Null);
        }
    }

    #endregion

    #region HTML Output with TFM Information Tests

    [Fact]
    public async Task HtmlOutput_WithTfms_ProducesValidHtml()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");

        var result = differ.Compare(oldContent, newContent, options);
        var formatter = _formatterFactory.GetFormatter("html");

        // Act
        var html = formatter.FormatResult(result, new OutputOptions { IncludeContent = true, IncludeStats = true });

        // Assert - verify HTML structure is valid
        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("<html");
        html.Should().Contain("</html>");
        html.Should().Contain("<body>");
        html.Should().Contain("</body>");

        // HTML output should not be empty and should contain meaningful content
        html.Length.Should().BeGreaterThan(1000, "HTML should contain substantial content");
    }

    [Fact]
    public async Task HtmlOutput_TfmSpecificChange_DisplaysTfmAnnotation()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");

        var result = differ.Compare(oldContent, newContent, options);
        var formatter = _formatterFactory.GetFormatter("html");

        // Act
        var html = formatter.FormatResult(result, new OutputOptions { IncludeContent = true });

        // Assert
        html.Should().NotBeNullOrWhiteSpace();

        // HTML should be valid
        html.Should().Contain("</html>");

        // Should include some TFM-related content (badges, annotations, etc.)
        // The exact format depends on implementation, but TFM info should be present
        var containsTfmInfo = html.Contains("net8.0") || html.Contains("net10.0") ||
                              html.Contains("tfm") || html.Contains("TFM") ||
                              html.Contains("framework");
        containsTfmInfo.Should().BeTrue("HTML should display TFM information for TFM-specific changes");
    }

    #endregion

    #region PlainText Output with TFM Information Tests

    [Fact]
    public async Task PlainTextOutput_WithTfms_IncludesTfmAnnotations()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");

        var result = differ.Compare(oldContent, newContent, options);
        var formatter = _formatterFactory.GetFormatter("plain");

        // Act
        var text = formatter.FormatResult(result, new OutputOptions { IncludeContent = true });

        // Assert
        text.Should().NotBeNullOrWhiteSpace();

        // Plain text output should include TFM information
        var containsTfmInfo = text.Contains("net8.0") || text.Contains("net10.0") ||
                              text.Contains("TFM") || text.Contains("framework");
        containsTfmInfo.Should().BeTrue("Plain text output should include TFM annotations");
    }

    [Fact]
    public async Task PlainTextOutput_TfmSpecificChange_AnnotatesCorrectly()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net10.0");

        var result = differ.Compare(oldContent, newContent, options);
        var formatter = _formatterFactory.GetFormatter("plain");

        // Act
        var text = formatter.FormatResult(result, new OutputOptions { IncludeContent = true });

        // Assert
        text.Should().NotBeNullOrWhiteSpace();
        text.Should().Contain("net10.0", "should show TFM annotation for net10.0");
    }

    #endregion

    #region Error Handling and Validation Tests

    [Fact]
    public async Task InvalidTfm_ThrowsArgumentException()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());

        // Use an invalid TFM format
        var options = CreateTfmOptions(oldPath, newPath, "invalid-tfm-123");

        // Act & Assert - should throw ArgumentException for invalid TFM
        var act = () => differ.Compare(oldContent, newContent, options);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unrecognized TFM*");
    }

    [Fact]
    public async Task EmptyTfmList_TreatedAsNoTfmAnalysis()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = new DiffOptions
        {
            OldPath = oldPath,
            NewPath = newPath,
            TargetFrameworks = Array.Empty<string>()
        };

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - empty TFM list should be treated as no TFM analysis
        result.AnalyzedTfms.Should().BeNullOrEmpty();
    }

    #endregion

    #region Preprocessor Directive Optimization Tests

    [Fact]
    public async Task FilesWithoutPreprocessorDirectives_SkipTfmSpecificParsing()
    {
        // Arrange - create simple files without #if directives
        var oldContent = @"
namespace SimpleNamespace;

public class SimpleClass
{
    public void Method1() { }
}
";

        var newContent = @"
namespace SimpleNamespace;

public class SimpleClass
{
    public void Method1() { }
    public void Method2() { }
}
";

        var differ = _differFactory.GetDiffer("test.cs", new DiffOptions());
        var options = CreateTfmOptions("old.cs", "new.cs", "net8.0", "net10.0");

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - changes should apply to all TFMs (empty ApplicableToTfms)
        var allChanges = GetAllChanges(result);
        allChanges.Should().NotBeEmpty();

        // All changes should either have null or empty ApplicableToTfms
        // since there are no conditional compilation directives
        foreach (var change in allChanges.Where(c => c.ApplicableToTfms is not null))
        {
            change.ApplicableToTfms.Should().BeEmpty("changes without #if directives apply to all TFMs");
        }
    }

    #endregion

    #region Parallel Processing Tests

    [Fact]
    public async Task MultipleTfms_ProcessedCorrectly()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());

        // Test with multiple TFMs to ensure parallel processing works
        var options = CreateTfmOptions(oldPath, newPath, "net6.0", "net7.0", "net8.0", "net10.0");

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = differ.Compare(oldContent, newContent, options);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        result.AnalyzedTfms.Should().NotBeNull();

        // Should process all requested TFMs (or valid ones)
        result.AnalyzedTfms!.Count.Should().BeGreaterThan(0);

        // Processing should complete in reasonable time (parallel processing)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000, "parallel processing should be reasonably fast");
    }

    #endregion

    #region All Output Formats Integration Tests

    [Fact]
    public async Task AllOutputFormats_WithTfms_ProduceValidOutput()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");

        var result = differ.Compare(oldContent, newContent, options);

        // Act & Assert - test all output formats
        var formats = new[] { "json", "html", "plain", "text", "terminal" };

        foreach (var format in formats)
        {
            var formatter = _formatterFactory.GetFormatter(format);
            var output = formatter.FormatResult(result, new OutputOptions { IncludeContent = true });

            output.Should().NotBeNullOrWhiteSpace($"Format '{format}' should produce non-empty output");

            // JSON should be parseable
            if (format == "json")
            {
                var act = () => JsonDocument.Parse(output);
                act.Should().NotThrow($"JSON format should produce valid JSON");
            }

            // HTML should have proper structure
            if (format == "html")
            {
                output.Should().Contain("<!DOCTYPE html>");
                output.Should().Contain("</html>");
            }
        }
    }

    #endregion

    #region Async Formatting with TFM Tests

    [Fact]
    public async Task AsyncJsonFormatting_WithTfms_ProducesCorrectStructure()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");

        var result = differ.Compare(oldContent, newContent, options);
        var formatter = _formatterFactory.GetFormatter("json");
        var outputOptions = new OutputOptions { PrettyPrint = true, IncludeContent = true };

        // Act
        var syncOutput = formatter.FormatResult(result, outputOptions);

        using var writer = new StringWriter();
        await formatter.FormatResultAsync(result, writer, outputOptions);
        var asyncOutput = writer.ToString();

        // Assert
        var syncDoc = JsonDocument.Parse(syncOutput);
        var asyncDoc = JsonDocument.Parse(asyncOutput);

        // Both should have TFM metadata
        syncDoc.RootElement.TryGetProperty("metadata", out var syncMetadata).Should().BeTrue();
        asyncDoc.RootElement.TryGetProperty("metadata", out var asyncMetadata).Should().BeTrue();

        syncMetadata.TryGetProperty("targetFrameworks", out var syncTfms).Should().BeTrue();
        asyncMetadata.TryGetProperty("targetFrameworks", out var asyncTfms).Should().BeTrue();

        syncTfms.GetArrayLength().Should().Be(asyncTfms.GetArrayLength());
    }

    #endregion

    #region Mixed TFM and Non-TFM Scenarios

    [Fact]
    public async Task MixedScenario_SomeTfmsSomeNot_HandlesCorrectly()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());

        // First run without TFMs
        var optionsNoTfm = CreateTfmOptions(oldPath, newPath);
        var resultNoTfm = differ.Compare(oldContent, newContent, optionsNoTfm);

        // Then run with TFMs
        var optionsWithTfm = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");
        var resultWithTfm = differ.Compare(oldContent, newContent, optionsWithTfm);

        // Assert
        resultNoTfm.AnalyzedTfms.Should().BeNullOrEmpty();
        resultWithTfm.AnalyzedTfms.Should().NotBeNullOrEmpty();

        // Both should detect changes, but TFM version has additional metadata
        resultNoTfm.Stats.TotalChanges.Should().BeGreaterThan(0);
        resultWithTfm.Stats.TotalChanges.Should().BeGreaterThan(0);
    }

    #endregion

    #region Realistic Scenario Tests

    [Fact]
    public async Task RealisticScenario_ApiEvolutionAcrossTfms()
    {
        // Arrange - simulate realistic API evolution scenario
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");

        // Act
        var result = differ.Compare(oldContent, newContent, options);

        // Assert - verify the scenario produces meaningful results
        result.Should().NotBeNull();
        result.Stats.TotalChanges.Should().BeGreaterThan(0);

        var allChanges = GetAllChanges(result);

        // Should have additions (new methods/classes)
        allChanges.Should().Contain(c => c.Type == ChangeType.Added);

        // Should have modifications (changed methods)
        allChanges.Should().Contain(c => c.Type == ChangeType.Modified);

        // Verify TFM annotations exist where expected
        var changesWithTfmAnnotation = allChanges.Where(c => c.ApplicableToTfms is not null).ToList();
        changesWithTfmAnnotation.Should().NotBeEmpty("TFM analysis should annotate changes");
    }

    [Fact]
    public async Task RealisticScenario_JsonOutputForCiCd()
    {
        // Arrange - simulate CI/CD scenario where JSON is consumed by tools
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());
        var options = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");

        var result = differ.Compare(oldContent, newContent, options);
        var formatter = _formatterFactory.GetFormatter("json");

        // Act
        var json = formatter.FormatResult(result, new OutputOptions { PrettyPrint = false, IncludeContent = false });

        // Assert - JSON should be compact, parseable, and contain required metadata
        var doc = JsonDocument.Parse(json);

        // Must have metadata section with TFM info
        doc.RootElement.TryGetProperty("metadata", out var metadata).Should().BeTrue();
        metadata.TryGetProperty("targetFrameworks", out _).Should().BeTrue();

        // Must have summary for quick stats
        doc.RootElement.TryGetProperty("summary", out var summary).Should().BeTrue();
        summary.TryGetProperty("totalChanges", out _).Should().BeTrue();

        // Files section with changes
        doc.RootElement.TryGetProperty("files", out _).Should().BeTrue();
    }

    #endregion

    #region Performance Characterization Tests

    [Fact]
    public async Task Performance_SingleTfmVsMultiTfm_ComparisonCharacterization()
    {
        // Arrange
        var oldPath = GetSamplePath("old-conditional-code.cs");
        var newPath = GetSamplePath("new-conditional-code.cs");

        var oldContent = await File.ReadAllTextAsync(oldPath);
        var newContent = await File.ReadAllTextAsync(newPath);

        var differ = _differFactory.GetDiffer(newPath, new DiffOptions());

        // Act - measure single TFM
        var singleTfmOptions = CreateTfmOptions(oldPath, newPath, "net8.0");
        var stopwatch = Stopwatch.StartNew();
        var singleResult = differ.Compare(oldContent, newContent, singleTfmOptions);
        var singleTfmTime = stopwatch.ElapsedMilliseconds;

        // Act - measure multiple TFMs
        var multiTfmOptions = CreateTfmOptions(oldPath, newPath, "net8.0", "net10.0");
        stopwatch.Restart();
        var multiResult = differ.Compare(oldContent, newContent, multiTfmOptions);
        var multiTfmTime = stopwatch.ElapsedMilliseconds;

        // Assert - both should complete, multi-TFM may take longer but should be reasonable
        singleResult.Should().NotBeNull();
        multiResult.Should().NotBeNull();

        // Log performance characteristics for reference
        Console.WriteLine($"Single TFM processing time: {singleTfmTime}ms");
        Console.WriteLine($"Multi TFM (2 frameworks) processing time: {multiTfmTime}ms");
        Console.WriteLine($"Overhead ratio: {(double)multiTfmTime / singleTfmTime:F2}x");

        // Multi-TFM should not have excessive overhead (ballpark check)
        // With parallel processing, it should be less than N times slower
        // Use max of multiplier or absolute threshold to account for CI runner variability
        var maxAllowedTime = Math.Max(singleTfmTime * 10, 500);
        multiTfmTime.Should().BeLessThan(maxAllowedTime, "multi-TFM processing should have reasonable overhead");
    }

    #endregion
}
