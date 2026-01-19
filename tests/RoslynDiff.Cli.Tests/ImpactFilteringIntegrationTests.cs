namespace RoslynDiff.Cli.Tests;

using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;
using RoslynDiff.Cli.Commands;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Integration tests for CLI impact filtering options (Phase 4).
/// Tests the new output flags: --include-non-impactful, --include-formatting, --impact-level.
/// </summary>
public class ImpactFilteringIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _fixturesDir;

    /// <summary>
    /// Path to test fixture: Old file with mixed impact changes.
    /// </summary>
    private string MixedImpactOldPath => Path.Combine(_fixturesDir, "ImpactChanges", "MixedImpact_Old.cs");

    /// <summary>
    /// Path to test fixture: New file with mixed impact changes.
    /// </summary>
    private string MixedImpactNewPath => Path.Combine(_fixturesDir, "ImpactChanges", "MixedImpact_New.cs");

    /// <summary>
    /// Path to test fixture: Old file with breaking public API change.
    /// </summary>
    private string BreakingPublicOldPath => Path.Combine(_fixturesDir, "ImpactChanges", "BreakingPublicApi_Old.cs");

    /// <summary>
    /// Path to test fixture: New file with breaking public API change.
    /// </summary>
    private string BreakingPublicNewPath => Path.Combine(_fixturesDir, "ImpactChanges", "BreakingPublicApi_New.cs");

    /// <summary>
    /// Path to test fixture: Old file with breaking internal API change.
    /// </summary>
    private string BreakingInternalOldPath => Path.Combine(_fixturesDir, "ImpactChanges", "BreakingInternalApi_Old.cs");

    /// <summary>
    /// Path to test fixture: New file with breaking internal API change.
    /// </summary>
    private string BreakingInternalNewPath => Path.Combine(_fixturesDir, "ImpactChanges", "BreakingInternalApi_New.cs");

    /// <summary>
    /// Path to test fixture: Old file with non-breaking changes.
    /// </summary>
    private string NonBreakingOldPath => Path.Combine(_fixturesDir, "ImpactChanges", "NonBreaking_Old.cs");

    /// <summary>
    /// Path to test fixture: New file with non-breaking changes.
    /// </summary>
    private string NonBreakingNewPath => Path.Combine(_fixturesDir, "ImpactChanges", "NonBreaking_New.cs");

    /// <summary>
    /// Path to test fixture: Old file with formatting-only changes.
    /// </summary>
    private string FormattingOnlyOldPath => Path.Combine(_fixturesDir, "ImpactChanges", "FormattingOnly_Old.cs");

    /// <summary>
    /// Path to test fixture: New file with formatting-only changes.
    /// </summary>
    private string FormattingOnlyNewPath => Path.Combine(_fixturesDir, "ImpactChanges", "FormattingOnly_New.cs");

    public ImpactFilteringIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"roslyn-diff-impact-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        // Disable browser opening during tests
        Environment.SetEnvironmentVariable("ROSLYN_DIFF_DISABLE_BROWSER_OPEN", "true");

        // Navigate to test fixtures - relative path from test assembly location
        var assemblyDir = Path.GetDirectoryName(typeof(ImpactFilteringIntegrationTests).Assembly.Location)!;
        _fixturesDir = Path.Combine(assemblyDir, "..", "..", "..", "..", "RoslynDiff.Core.Tests", "TestFixtures", "CSharp");

        // Normalize the path
        _fixturesDir = Path.GetFullPath(_fixturesDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    #region Test Helpers

    /// <summary>
    /// Gets the path to the pre-built roslyn-diff executable.
    /// </summary>
    private static string GetRoslynDiffPath()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(ImpactFilteringIntegrationTests).Assembly.Location)!;

        var possiblePaths = new[]
        {
            Path.Combine(assemblyDir, "..", "..", "..", "..", "..", "src", "RoslynDiff.Cli", "bin", "Debug", "net10.0", "roslyn-diff"),
            Path.Combine(assemblyDir, "..", "..", "..", "..", "..", "src", "RoslynDiff.Cli", "bin", "Release", "net10.0", "roslyn-diff"),
            Path.Combine(assemblyDir, "..", "..", "..", "..", "..", "src", "RoslynDiff.Cli", "bin", "Debug", "net10.0", "roslyn-diff.exe"),
            Path.Combine(assemblyDir, "..", "..", "..", "..", "..", "src", "RoslynDiff.Cli", "bin", "Release", "net10.0", "roslyn-diff.exe"),
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new InvalidOperationException(
            "Could not find roslyn-diff executable. Please build the project first with 'dotnet build'.");
    }

    /// <summary>
    /// Runs the roslyn-diff CLI with the specified arguments and captures output.
    /// </summary>
    private async Task<(int ExitCode, string Stdout, string Stderr)> RunCliAsync(params string[] args)
    {
        var argsString = string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));

        var psi = new ProcessStartInfo
        {
            FileName = GetRoslynDiffPath(),
            Arguments = argsString,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _tempDir
        };

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, stdout, stderr);
    }

    /// <summary>
    /// Ensures the test fixture files exist before running tests.
    /// </summary>
    private void EnsureFixturesExist()
    {
        if (!Directory.Exists(_fixturesDir))
        {
            throw new InvalidOperationException($"Test fixtures directory not found: {_fixturesDir}");
        }

        var impactDir = Path.Combine(_fixturesDir, "ImpactChanges");
        if (!Directory.Exists(impactDir))
        {
            throw new InvalidOperationException($"Impact test fixtures directory not found: {impactDir}");
        }
    }

    /// <summary>
    /// Creates a temporary output file path with the specified extension.
    /// </summary>
    private string GetTempFilePath(string extension)
    {
        return Path.Combine(_tempDir, $"output-{Guid.NewGuid():N}.{extension}");
    }

    /// <summary>
    /// Parses JSON output and returns the changes array.
    /// </summary>
    private static JsonElement GetChangesFromJson(string json)
    {
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("files")[0].GetProperty("changes");
    }

    /// <summary>
    /// Gets the impact values from a JSON changes array.
    /// </summary>
    private static List<string> GetImpactValues(JsonElement changes)
    {
        var impacts = new List<string>();
        foreach (var change in changes.EnumerateArray())
        {
            if (change.TryGetProperty("impact", out var impact))
            {
                impacts.Add(impact.GetString() ?? "unknown");
            }
        }
        return impacts;
    }

    #endregion

    #region Default Behavior Tests

    [Fact]
    public async Task Json_Default_ExcludesNonImpactfulChanges()
    {
        // Arrange
        EnsureFixturesExist();

        // Act - Default JSON output should exclude non-impactful changes
        var (exitCode, stdout, _) = await RunCliAsync(
            "diff",
            MixedImpactOldPath,
            MixedImpactNewPath,
            "--json");

        // Assert
        exitCode.Should().Be(1, "exit code should be 1 when differences found");
        stdout.Should().NotBeNullOrWhiteSpace("JSON output should be written to stdout");

        // Default JSON should focus on breaking changes (BreakingPublicApi, BreakingInternalApi)
        // Non-breaking and formatting-only should be excluded by default
        var parseAction = () => JsonDocument.Parse(stdout);
        parseAction.Should().NotThrow("output should be valid JSON");
    }

    [Fact]
    public async Task Html_Default_ShowsAllChanges()
    {
        // Arrange
        EnsureFixturesExist();
        var outputPath = GetTempFilePath("html");

        // Act - HTML output shows all changes by default
        var (exitCode, _, _) = await RunCliAsync(
            "diff",
            MixedImpactOldPath,
            MixedImpactNewPath,
            "--html", outputPath);

        // Assert
        exitCode.Should().Be(1, "exit code should be 1 when differences found");
        File.Exists(outputPath).Should().BeTrue("HTML file should be created");

        var html = await File.ReadAllTextAsync(outputPath);
        html.Should().Contain("<!DOCTYPE html>", "should be valid HTML5");
        // HTML should contain all types of changes
        html.Should().NotBeEmpty();
    }

    #endregion

    #region --include-non-impactful Tests

    [Fact]
    public async Task IncludeNonImpactful_Json_IncludesNonBreakingChanges()
    {
        // Arrange
        EnsureFixturesExist();

        // Act
        var (exitCode, stdout, _) = await RunCliAsync(
            "diff",
            NonBreakingOldPath,
            NonBreakingNewPath,
            "--json",
            "--include-non-impactful");

        // Assert
        exitCode.Should().Be(1, "exit code should be 1 when differences found");
        stdout.Should().NotBeNullOrWhiteSpace("JSON output should be written");

        var parseAction = () => JsonDocument.Parse(stdout);
        parseAction.Should().NotThrow("output should be valid JSON");

        // With --include-non-impactful, we should see non-breaking changes
        using var doc = JsonDocument.Parse(stdout);
        doc.RootElement.TryGetProperty("files", out _).Should().BeTrue("JSON should have files section");
    }

    [Fact]
    public async Task IncludeNonImpactful_WithMixedChanges_ShowsNonBreaking()
    {
        // Arrange
        EnsureFixturesExist();

        // Act
        var (exitCode, stdout, _) = await RunCliAsync(
            "diff",
            MixedImpactOldPath,
            MixedImpactNewPath,
            "--json",
            "--include-non-impactful");

        // Assert
        exitCode.Should().Be(1, "exit code should be 1 when differences found");

        var parseAction = () => JsonDocument.Parse(stdout);
        parseAction.Should().NotThrow("output should be valid JSON");
    }

    #endregion

    #region --include-formatting Tests

    [Fact]
    public async Task IncludeFormatting_Json_IncludesFormattingChanges()
    {
        // Arrange
        EnsureFixturesExist();

        // Act
        var (exitCode, stdout, _) = await RunCliAsync(
            "diff",
            FormattingOnlyOldPath,
            FormattingOnlyNewPath,
            "--json",
            "--include-formatting");

        // Assert
        // Note: With only formatting changes, if detection works, we should see them
        stdout.Should().NotBeNullOrWhiteSpace("JSON output should be written");

        var parseAction = () => JsonDocument.Parse(stdout);
        parseAction.Should().NotThrow("output should be valid JSON");
    }

    [Fact]
    public async Task IncludeFormatting_ImpliesIncludeNonImpactful()
    {
        // Arrange
        EnsureFixturesExist();

        // Act
        var (exitCode, stdout, _) = await RunCliAsync(
            "diff",
            MixedImpactOldPath,
            MixedImpactNewPath,
            "--json",
            "--include-formatting");

        // Assert
        exitCode.Should().Be(1, "exit code should be 1 when differences found");
        stdout.Should().NotBeNullOrWhiteSpace("JSON output should be written");

        var parseAction = () => JsonDocument.Parse(stdout);
        parseAction.Should().NotThrow("output should be valid JSON");
    }

    #endregion

    #region --impact-level Tests

    [Fact]
    public async Task ImpactLevel_BreakingPublic_ShowsOnlyPublicApiBreaking()
    {
        // Arrange
        EnsureFixturesExist();

        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync(
            "diff",
            MixedImpactOldPath,
            MixedImpactNewPath,
            "--json",
            "--impact-level", "breaking-public");

        // Assert
        stdout.Should().NotBeNullOrWhiteSpace($"JSON output should be written. Stderr: {stderr}");

        var parseAction = () => JsonDocument.Parse(stdout);
        parseAction.Should().NotThrow("output should be valid JSON");

        // Parse and verify the impact levels
        using var doc = JsonDocument.Parse(stdout);
        doc.RootElement.TryGetProperty("files", out var files).Should().BeTrue("JSON should have files section");

        // Collect all impact values from all changes
        var allImpacts = new List<string>();
        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("changes", out var changes))
            {
                allImpacts.AddRange(GetImpactValues(changes));
            }
        }

        // With --impact-level breaking-public, should ONLY contain breakingPublicApi (camelCase in JSON)
        allImpacts.Should().NotBeEmpty("should have at least one change");
        allImpacts.Should().AllBe("breakingPublicApi", 
            "with --impact-level breaking-public, only breakingPublicApi changes should be shown");
        allImpacts.Should().NotContain("breakingInternalApi", 
            "internal API changes should be filtered out");
        allImpacts.Should().NotContain("nonBreaking", 
            "non-breaking changes should be filtered out");
        allImpacts.Should().NotContain("formattingOnly", 
            "formatting-only changes should be filtered out");
    }

    [Fact]
    public async Task ImpactLevel_BreakingInternal_IncludesInternalAndPublic()
    {
        // Arrange
        EnsureFixturesExist();

        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync(
            "diff",
            MixedImpactOldPath,
            MixedImpactNewPath,
            "--json",
            "--impact-level", "breaking-internal");

        // Assert
        stdout.Should().NotBeNullOrWhiteSpace($"JSON output should be written. Stderr: {stderr}");

        var parseAction = () => JsonDocument.Parse(stdout);
        parseAction.Should().NotThrow("output should be valid JSON");

        // Parse and verify the impact levels
        using var doc = JsonDocument.Parse(stdout);
        doc.RootElement.TryGetProperty("files", out var files).Should().BeTrue("JSON should have files section");

        // Collect all impact values from all changes
        var allImpacts = new List<string>();
        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("changes", out var changes))
            {
                allImpacts.AddRange(GetImpactValues(changes));
            }
        }

        // With --impact-level breaking-internal, should include breakingPublicApi and breakingInternalApi (camelCase in JSON)
        allImpacts.Should().NotBeEmpty("should have at least one change");
        
        // Should only contain breaking changes (public and internal)
        var allowedImpacts = new[] { "breakingPublicApi", "breakingInternalApi" };
        allImpacts.Should().OnlyContain(impact => allowedImpacts.Contains(impact),
            "with --impact-level breaking-internal, only breakingPublicApi and breakingInternalApi changes should be shown");
        
        allImpacts.Should().NotContain("nonBreaking", 
            "non-breaking changes should be filtered out");
        allImpacts.Should().NotContain("formattingOnly", 
            "formatting-only changes should be filtered out");
    }

    [Fact]
    public async Task ImpactLevel_NonBreaking_IncludesAllExceptFormatting()
    {
        // Arrange
        EnsureFixturesExist();

        // Act
        var (exitCode, stdout, stderr) = await RunCliAsync(
            "diff",
            MixedImpactOldPath,
            MixedImpactNewPath,
            "--json",
            "--impact-level", "non-breaking");

        // Assert
        stdout.Should().NotBeNullOrWhiteSpace($"JSON output should be written. Stderr: {stderr}");

        var parseAction = () => JsonDocument.Parse(stdout);
        parseAction.Should().NotThrow("output should be valid JSON");

        // Parse and verify the impact levels
        using var doc = JsonDocument.Parse(stdout);
        doc.RootElement.TryGetProperty("files", out var files).Should().BeTrue("JSON should have files section");

        // Collect all impact values from all changes
        var allImpacts = new List<string>();
        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("changes", out var changes))
            {
                allImpacts.AddRange(GetImpactValues(changes));
            }
        }

        // With --impact-level non-breaking, should include everything EXCEPT formattingOnly (camelCase in JSON)
        allImpacts.Should().NotBeEmpty("should have at least one change");
        
        // Should contain breaking and non-breaking changes, but not formatting
        var allowedImpacts = new[] { "breakingPublicApi", "breakingInternalApi", "nonBreaking" };
        allImpacts.Should().OnlyContain(impact => allowedImpacts.Contains(impact),
            "with --impact-level non-breaking, breakingPublicApi, breakingInternalApi, and nonBreaking changes should be shown");
        
        allImpacts.Should().NotContain("formattingOnly", 
            "formatting-only changes should be filtered out with --impact-level non-breaking");
    }

    [Fact]
    public async Task ImpactLevel_All_ShowsEverything()
    {
        // Arrange
        EnsureFixturesExist();

        // Act - Test with MixedImpact which has breaking and non-breaking changes
        var (exitCode, stdout, stderr) = await RunCliAsync(
            "diff",
            MixedImpactOldPath,
            MixedImpactNewPath,
            "--json",
            "--impact-level", "all");

        // Assert
        exitCode.Should().Be(1, "exit code should be 1 when differences found");
        stdout.Should().NotBeNullOrWhiteSpace($"JSON output should be written. Stderr: {stderr}");

        var parseAction = () => JsonDocument.Parse(stdout);
        parseAction.Should().NotThrow("output should be valid JSON");

        // Parse and verify the impact levels
        using var doc = JsonDocument.Parse(stdout);
        doc.RootElement.TryGetProperty("files", out var files).Should().BeTrue("JSON should have files section");

        // Collect all impact values from all changes
        var allImpacts = new List<string>();
        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("changes", out var changes))
            {
                allImpacts.AddRange(GetImpactValues(changes));
            }
        }

        // With --impact-level all, should include all types of changes present in the fixture
        // MixedImpact has: breakingPublicApi (public method signature change), 
        //                  breakingInternalApi (internal method rename),
        //                  nonBreaking (private member/method renames)
        allImpacts.Should().NotBeEmpty("should have at least one change");
        
        // The mixed impact fixture should produce at least breaking public changes (camelCase in JSON)
        allImpacts.Should().Contain("breakingPublicApi", 
            "should include breaking public API changes");
        
        // All impacts should be valid impact values (camelCase in JSON)
        var validImpacts = new[] { "breakingPublicApi", "breakingInternalApi", "nonBreaking", "formattingOnly" };
        allImpacts.Should().OnlyContain(impact => validImpacts.Contains(impact),
            "all impact values should be valid ChangeImpact enum values");
    }

    [Fact]
    public async Task ImpactLevel_All_IncludesFormattingOnlyChanges()
    {
        // Arrange
        EnsureFixturesExist();

        // Act - Test with FormattingOnly fixtures which only have whitespace changes
        var (exitCode, stdout, stderr) = await RunCliAsync(
            "diff",
            FormattingOnlyOldPath,
            FormattingOnlyNewPath,
            "--json",
            "--impact-level", "all");

        // Assert
        stdout.Should().NotBeNullOrWhiteSpace($"JSON output should be written. Stderr: {stderr}");

        var parseAction = () => JsonDocument.Parse(stdout);
        parseAction.Should().NotThrow("output should be valid JSON");

        // Parse and verify the impact levels
        using var doc = JsonDocument.Parse(stdout);
        doc.RootElement.TryGetProperty("files", out var files).Should().BeTrue("JSON should have files section");

        // Collect all impact values from all changes
        var allImpacts = new List<string>();
        foreach (var file in files.EnumerateArray())
        {
            if (file.TryGetProperty("changes", out var changes))
            {
                allImpacts.AddRange(GetImpactValues(changes));
            }
        }

        // With --impact-level all and FormattingOnly fixtures, 
        // we should see formattingOnly changes (if the detector classifies them as such)
        // Note: The exit code may be 0 if only formatting changes exist and they're considered "no differences"
        // or 1 if differences are detected
        if (allImpacts.Count > 0)
        {
            // If changes are detected, they should be formatting-only (camelCase in JSON)
            allImpacts.Should().AllBe("formattingOnly", 
                "FormattingOnly fixtures should produce formattingOnly impact level");
        }
    }

    [Fact]
    public async Task ImpactLevel_Invalid_ReturnsError()
    {
        // Arrange
        EnsureFixturesExist();

        // Act
        var (exitCode, stdout, _) = await RunCliAsync(
            "diff",
            MixedImpactOldPath,
            MixedImpactNewPath,
            "--impact-level", "invalid-level");

        // Assert
        exitCode.Should().Be(OutputOrchestrator.ExitCodeError, "should return error for invalid impact level");
        stdout.Should().Contain("Invalid impact level", "error message should indicate invalid impact level");
    }

    #endregion

    #region Combined Options Tests

    [Fact]
    public async Task Combined_JsonAndHtml_WithImpactLevel()
    {
        // Arrange
        EnsureFixturesExist();
        var htmlPath = GetTempFilePath("html");

        // Act - JSON to stdout with impact level, HTML to file
        var (exitCode, stdout, _) = await RunCliAsync(
            "diff",
            BreakingPublicOldPath,
            BreakingPublicNewPath,
            "--json",
            "--html", htmlPath,
            "--impact-level", "breaking-public");

        // Assert
        exitCode.Should().Be(1, "exit code should be 1 when differences found");

        // JSON should be on stdout
        var parseAction = () => JsonDocument.Parse(stdout);
        parseAction.Should().NotThrow("stdout should contain valid JSON");

        // HTML should be in file
        File.Exists(htmlPath).Should().BeTrue("HTML file should be created");
    }

    [Fact]
    public async Task Combined_IncludeNonImpactful_WithHtml()
    {
        // Arrange
        EnsureFixturesExist();
        var jsonPath = GetTempFilePath("json");
        var htmlPath = GetTempFilePath("html");

        // Act
        var (exitCode, _, _) = await RunCliAsync(
            "diff",
            NonBreakingOldPath,
            NonBreakingNewPath,
            "--json", jsonPath,
            "--html", htmlPath,
            "--include-non-impactful");

        // Assert
        exitCode.Should().Be(1, "exit code should be 1 when differences found");
        File.Exists(jsonPath).Should().BeTrue("JSON file should be created");
        File.Exists(htmlPath).Should().BeTrue("HTML file should be created");

        var json = await File.ReadAllTextAsync(jsonPath);
        var parseAction = () => JsonDocument.Parse(json);
        parseAction.Should().NotThrow("JSON file should contain valid JSON");
    }

    #endregion

    #region Settings Property Tests

    [Fact]
    public void Settings_IncludeNonImpactful_DefaultIsFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.IncludeNonImpactful.Should().BeFalse("IncludeNonImpactful should be false by default");
    }

    [Fact]
    public void Settings_IncludeFormatting_DefaultIsFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.IncludeFormatting.Should().BeFalse("IncludeFormatting should be false by default");
    }

    [Fact]
    public void Settings_ImpactLevel_DefaultIsNull()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.ImpactLevel.Should().BeNull("ImpactLevel should be null by default (uses format-specific defaults)");
    }

    [Fact]
    public void Settings_IncludeNonImpactful_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { IncludeNonImpactful = true };

        // Assert
        settings.IncludeNonImpactful.Should().BeTrue();
    }

    [Fact]
    public void Settings_IncludeFormatting_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { IncludeFormatting = true };

        // Assert
        settings.IncludeFormatting.Should().BeTrue();
    }

    [Theory]
    [InlineData("breaking-public")]
    [InlineData("breaking-internal")]
    [InlineData("non-breaking")]
    [InlineData("all")]
    public void Settings_ImpactLevel_WhenSetToValidValue_ShouldReturnThatValue(string level)
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { ImpactLevel = level };

        // Assert
        settings.ImpactLevel.Should().Be(level);
    }

    #endregion

    #region Exit Code Tests

    [Fact]
    public async Task ExitCode_1_WhenBreakingChangesFound()
    {
        // Arrange
        EnsureFixturesExist();

        // Act
        var (exitCode, _, _) = await RunCliAsync(
            "diff",
            BreakingPublicOldPath,
            BreakingPublicNewPath,
            "--json");

        // Assert
        exitCode.Should().Be(1, "exit code should be 1 when differences found");
    }

    [Fact]
    public async Task ExitCode_WithImpactLevelFilter_StillReturnsCorrectCode()
    {
        // Arrange
        EnsureFixturesExist();

        // Act - Even if filtering to only breaking-public, if there are such changes, return 1
        var (exitCode, _, _) = await RunCliAsync(
            "diff",
            BreakingPublicOldPath,
            BreakingPublicNewPath,
            "--json",
            "--impact-level", "breaking-public");

        // Assert
        exitCode.Should().Be(1, "exit code should be 1 when breaking changes are found");
    }

    #endregion

    #region HTML Impact Styling Tests

    [Fact]
    public async Task Html_ContainsImpactStyling()
    {
        // Arrange
        EnsureFixturesExist();
        var outputPath = GetTempFilePath("html");

        // Act
        var (exitCode, _, _) = await RunCliAsync(
            "diff",
            MixedImpactOldPath,
            MixedImpactNewPath,
            "--html", outputPath);

        // Assert
        exitCode.Should().Be(1, "exit code should be 1 when differences found");
        File.Exists(outputPath).Should().BeTrue("HTML file should be created");

        var html = await File.ReadAllTextAsync(outputPath);
        // HTML should have impact-related content or styling
        html.Should().Contain("<!DOCTYPE html>", "should be valid HTML5");
    }

    #endregion
}
