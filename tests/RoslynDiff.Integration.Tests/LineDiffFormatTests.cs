namespace RoslynDiff.Integration.Tests;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

/// <summary>
/// Golden snapshot comparison tests for LineDiffer output format.
/// Compares roslyn-diff line-mode output against diff -u and git diff.
/// </summary>
public partial class LineDiffFormatTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _goldenSnapshotsPath;
    private readonly string _roslynDiffPath;
    private readonly string _tempDir;

    public LineDiffFormatTests(ITestOutputHelper output)
    {
        _output = output;
        _goldenSnapshotsPath = Path.Combine(AppContext.BaseDirectory, "GoldenSnapshots", "LineDiff");
        _tempDir = Path.Combine(Path.GetTempPath(), $"roslyn-diff-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        // Find the roslyn-diff executable
        _roslynDiffPath = FindRoslynDiffExecutable();
    }

    public void Dispose()
    {
        // Cleanup temp directory
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
        GC.SuppressFinalize(this);
    }

    #region Test Data

    /// <summary>
    /// Gets test scenarios from the GoldenSnapshots/LineDiff directory.
    /// Each scenario has _old.txt, _new.txt, _diff_u.txt, and _git_diff.txt files.
    /// </summary>
    public static IEnumerable<object[]> GetScenarios()
    {
        var snapshotsPath = Path.Combine(AppContext.BaseDirectory, "GoldenSnapshots", "LineDiff");

        if (!Directory.Exists(snapshotsPath))
        {
            yield break;
        }

        var oldFiles = Directory.GetFiles(snapshotsPath, "*_old.txt");
        foreach (var oldFile in oldFiles)
        {
            var scenarioName = Path.GetFileName(oldFile).Replace("_old.txt", "");
            var newFile = Path.Combine(snapshotsPath, $"{scenarioName}_new.txt");

            if (File.Exists(newFile))
            {
                yield return new object[] { scenarioName };
            }
        }
    }

    #endregion

    #region diff -u Comparison Tests

    [Theory]
    [MemberData(nameof(GetScenarios))]
    public async Task CompareAgainstDiffU(string scenario)
    {
        // Arrange
        var (oldFile, newFile) = GetScenarioPaths(scenario);
        var expectedSnapshot = LoadGoldenSnapshot(scenario, "diff_u");

        if (string.IsNullOrEmpty(expectedSnapshot))
        {
            _output.WriteLine($"Skipping {scenario}: No diff_u golden snapshot found");
            return;
        }

        // Act
        var actualOutput = await RunRoslynDiff(oldFile, newFile);

        // Assert
        var normalizedActual = NormalizeUnifiedDiffOutput(actualOutput, "roslyn-diff");
        var normalizedExpected = NormalizeUnifiedDiffOutput(expectedSnapshot, "diff -u");

        var comparison = CompareUnifiedDiff(normalizedActual, normalizedExpected);

        if (!comparison.AreEquivalent)
        {
            ReportDifferences(scenario, "diff -u", actualOutput, expectedSnapshot, comparison);
        }

        comparison.AreEquivalent.Should().BeTrue(
            $"roslyn-diff output should match diff -u for scenario '{scenario}'. " +
            $"Differences: {comparison.Summary}");
    }

    #endregion

    #region git diff Comparison Tests

    [Theory]
    [MemberData(nameof(GetScenarios))]
    public async Task CompareAgainstGitDiff(string scenario)
    {
        // Arrange
        var (oldFile, newFile) = GetScenarioPaths(scenario);
        var expectedSnapshot = LoadGoldenSnapshot(scenario, "git_diff");

        if (string.IsNullOrEmpty(expectedSnapshot))
        {
            _output.WriteLine($"Skipping {scenario}: No git_diff golden snapshot found");
            return;
        }

        // Act
        var actualOutput = await RunRoslynDiff(oldFile, newFile);

        // Assert
        var normalizedActual = NormalizeUnifiedDiffOutput(actualOutput, "roslyn-diff");
        var normalizedExpected = NormalizeUnifiedDiffOutput(expectedSnapshot, "git diff");

        var comparison = CompareUnifiedDiff(normalizedActual, normalizedExpected);

        if (!comparison.AreEquivalent)
        {
            ReportDifferences(scenario, "git diff", actualOutput, expectedSnapshot, comparison);
        }

        comparison.AreEquivalent.Should().BeTrue(
            $"roslyn-diff output should match git diff for scenario '{scenario}'. " +
            $"Differences: {comparison.Summary}");
    }

    #endregion

    #region Helper Methods - CLI Execution

    /// <summary>
    /// Runs roslyn-diff with --mode line -o text on the given files.
    /// </summary>
    /// <param name="oldFile">Path to the old file.</param>
    /// <param name="newFile">Path to the new file.</param>
    /// <returns>The CLI output.</returns>
    public async Task<string> RunRoslynDiff(string oldFile, string newFile)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _roslynDiffPath,
            Arguments = $"diff --mode line -o text \"{oldFile}\" \"{newFile}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _tempDir
        };

        _output.WriteLine($"Running: {psi.FileName} {psi.Arguments}");

        using var process = new Process { StartInfo = psi };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var completed = await Task.Run(() => process.WaitForExit(30_000));

        if (!completed)
        {
            process.Kill();
            throw new TimeoutException("roslyn-diff process timed out after 30 seconds");
        }

        // Important: WaitForExit() with timeout may return before all async output is read.
        // Call WaitForExit() again without timeout to ensure all output handlers have completed.
        process.WaitForExit();

        var error = errorBuilder.ToString();
        if (!string.IsNullOrWhiteSpace(error) && process.ExitCode != 0)
        {
            _output.WriteLine($"Process stderr: {error}");
        }

        return outputBuilder.ToString();
    }

    #endregion

    #region Helper Methods - Golden Snapshot Loading

    /// <summary>
    /// Loads a golden snapshot file for the given scenario and tool.
    /// </summary>
    /// <param name="scenario">The scenario name (e.g., "SimpleAddition").</param>
    /// <param name="tool">The tool identifier ("diff_u" or "git_diff").</param>
    /// <returns>The contents of the snapshot file, or null if not found.</returns>
    public string? LoadGoldenSnapshot(string scenario, string tool)
    {
        var snapshotPath = Path.Combine(_goldenSnapshotsPath, $"{scenario}_{tool}.txt");

        if (!File.Exists(snapshotPath))
        {
            return null;
        }

        return File.ReadAllText(snapshotPath);
    }

    private (string OldFile, string NewFile) GetScenarioPaths(string scenario)
    {
        var oldFile = Path.Combine(_goldenSnapshotsPath, $"{scenario}_old.txt");
        var newFile = Path.Combine(_goldenSnapshotsPath, $"{scenario}_new.txt");

        if (!File.Exists(oldFile))
        {
            throw new FileNotFoundException($"Old file not found for scenario '{scenario}'", oldFile);
        }

        if (!File.Exists(newFile))
        {
            throw new FileNotFoundException($"New file not found for scenario '{scenario}'", newFile);
        }

        return (oldFile, newFile);
    }

    #endregion

    #region Helper Methods - Output Normalization

    /// <summary>
    /// Normalizes unified diff output for comparison by:
    /// - Normalizing line endings
    /// - Removing/normalizing timestamps from diff -u headers
    /// - Normalizing file paths in headers
    /// - Removing git diff index/mode lines
    /// </summary>
    /// <param name="output">The diff output to normalize.</param>
    /// <param name="source">Source identifier for logging (e.g., "roslyn-diff", "diff -u").</param>
    /// <returns>Normalized output suitable for structural comparison.</returns>
    public string NormalizeUnifiedDiffOutput(string output, string source)
    {
        if (string.IsNullOrEmpty(output))
        {
            return string.Empty;
        }

        var lines = output
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n');

        var normalizedLines = new List<string>();

        foreach (var line in lines)
        {
            var normalized = NormalizeDiffLine(line);
            if (normalized is not null)
            {
                normalizedLines.Add(normalized);
            }
        }

        return string.Join("\n", normalizedLines).TrimEnd();
    }

    private string? NormalizeDiffLine(string line)
    {
        // Skip git diff header lines (index, mode, etc.)
        if (line.StartsWith("diff --git", StringComparison.Ordinal) ||
            line.StartsWith("index ", StringComparison.Ordinal) ||
            line.StartsWith("new file mode", StringComparison.Ordinal) ||
            line.StartsWith("deleted file mode", StringComparison.Ordinal))
        {
            return null;
        }

        // Normalize --- and +++ header lines (remove timestamps and normalize paths)
        if (line.StartsWith("---", StringComparison.Ordinal) ||
            line.StartsWith("+++", StringComparison.Ordinal))
        {
            return NormalizeFileHeaderLine(line);
        }

        // Keep hunk headers and content lines as-is
        return line;
    }

    private string NormalizeFileHeaderLine(string line)
    {
        // Pattern to match "--- path<tab>timestamp" or "+++ path<tab>timestamp"
        // or "--- a/path" (git format) or "+++ b/path" (git format)
        var prefix = line.StartsWith("---") ? "---" : "+++";

        // Remove the prefix and any leading/trailing whitespace from the path portion
        var pathPortion = line[3..].Trim();

        // Remove timestamp if present (diff -u format: "path\t2026-01-15 09:47:44")
        var tabIndex = pathPortion.IndexOf('\t');
        if (tabIndex >= 0)
        {
            pathPortion = pathPortion[..tabIndex];
        }

        // Remove git's a/ or b/ prefix
        if (pathPortion.StartsWith("a/") || pathPortion.StartsWith("b/"))
        {
            pathPortion = pathPortion[2..];
        }

        // Extract just the filename (normalize away full paths)
        var fileName = Path.GetFileName(pathPortion);

        return $"{prefix} {fileName}";
    }

    #endregion

    #region Helper Methods - Diff Comparison

    /// <summary>
    /// Compares two normalized unified diff outputs for structural equivalence.
    /// Focuses on: same hunks, same line changes, same content.
    /// </summary>
    /// <param name="actual">The actual (roslyn-diff) output.</param>
    /// <param name="expected">The expected (diff -u or git diff) output.</param>
    /// <returns>A comparison result with details about differences.</returns>
    public DiffComparisonResult CompareUnifiedDiff(string actual, string expected)
    {
        var result = new DiffComparisonResult();

        var actualHunks = ParseHunks(actual);
        var expectedHunks = ParseHunks(expected);

        // Compare hunk counts
        if (actualHunks.Count != expectedHunks.Count)
        {
            result.AddDifference($"Hunk count mismatch: actual={actualHunks.Count}, expected={expectedHunks.Count}");
        }

        // Compare hunks
        var hunkCount = Math.Min(actualHunks.Count, expectedHunks.Count);
        for (var i = 0; i < hunkCount; i++)
        {
            CompareHunks(result, actualHunks[i], expectedHunks[i], i);
        }

        return result;
    }

    private List<DiffHunk> ParseHunks(string diff)
    {
        var hunks = new List<DiffHunk>();
        var lines = diff.Split('\n');
        DiffHunk? currentHunk = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("@@"))
            {
                currentHunk = new DiffHunk { Header = line };
                hunks.Add(currentHunk);
            }
            else if (currentHunk is not null)
            {
                // Content lines: +, -, or space (context)
                if (line.StartsWith('+') || line.StartsWith('-') || line.StartsWith(' ') ||
                    (line.Length > 0 && !line.StartsWith("---") && !line.StartsWith("+++")))
                {
                    currentHunk.Lines.Add(line);
                }
            }
        }

        return hunks;
    }

    private void CompareHunks(DiffComparisonResult result, DiffHunk actual, DiffHunk expected, int index)
    {
        // Compare headers (line numbers might differ slightly, focus on count)
        var actualRange = ParseHunkRange(actual.Header);
        var expectedRange = ParseHunkRange(expected.Header);

        if (actualRange.OldCount != expectedRange.OldCount ||
            actualRange.NewCount != expectedRange.NewCount)
        {
            result.AddDifference(
                $"Hunk {index} range mismatch: " +
                $"actual=(-{actualRange.OldStart},{actualRange.OldCount} +{actualRange.NewStart},{actualRange.NewCount}), " +
                $"expected=(-{expectedRange.OldStart},{expectedRange.OldCount} +{expectedRange.NewStart},{expectedRange.NewCount})");
        }

        // Compare line content
        var actualChanges = GetSignificantLines(actual.Lines);
        var expectedChanges = GetSignificantLines(expected.Lines);

        if (!actualChanges.SequenceEqual(expectedChanges))
        {
            result.AddDifference($"Hunk {index} content mismatch");
            result.AddDetail($"  Actual changes: [{string.Join(", ", actualChanges.Take(5))}...]");
            result.AddDetail($"  Expected changes: [{string.Join(", ", expectedChanges.Take(5))}...]");
        }
    }

    private (int OldStart, int OldCount, int NewStart, int NewCount) ParseHunkRange(string header)
    {
        // Parse @@ -oldStart,oldCount +newStart,newCount @@
        var match = HunkHeaderRegex().Match(header);
        if (match.Success)
        {
            return (
                int.Parse(match.Groups[1].Value),
                match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1,
                int.Parse(match.Groups[3].Value),
                match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 1
            );
        }

        return (0, 0, 0, 0);
    }

    private List<string> GetSignificantLines(List<string> lines)
    {
        // Return lines that represent actual changes (+ or -)
        // Normalize whitespace for comparison
        return lines
            .Where(l => l.StartsWith('+') || l.StartsWith('-'))
            .Select(l => l.Trim())
            .ToList();
    }

    [GeneratedRegex(@"^@@\s*-(\d+)(?:,(\d+))?\s*\+(\d+)(?:,(\d+))?\s*@@")]
    private static partial Regex HunkHeaderRegex();

    #endregion

    #region Helper Methods - Reporting

    private void ReportDifferences(string scenario, string tool, string actual, string expected, DiffComparisonResult comparison)
    {
        _output.WriteLine($"=== Difference Report for '{scenario}' vs {tool} ===");
        _output.WriteLine(string.Empty);

        _output.WriteLine("--- Summary ---");
        _output.WriteLine(comparison.Summary);
        _output.WriteLine(string.Empty);

        foreach (var detail in comparison.Details)
        {
            _output.WriteLine(detail);
        }
        _output.WriteLine(string.Empty);

        _output.WriteLine("--- Actual (roslyn-diff) ---");
        _output.WriteLine(actual);
        _output.WriteLine(string.Empty);

        _output.WriteLine($"--- Expected ({tool}) ---");
        _output.WriteLine(expected);
        _output.WriteLine(string.Empty);

        _output.WriteLine("--- Normalized Actual ---");
        _output.WriteLine(NormalizeUnifiedDiffOutput(actual, "roslyn-diff"));
        _output.WriteLine(string.Empty);

        _output.WriteLine("--- Normalized Expected ---");
        _output.WriteLine(NormalizeUnifiedDiffOutput(expected, tool));
        _output.WriteLine(string.Empty);
    }

    #endregion

    #region Helper Methods - Path Resolution

    private string FindRoslynDiffExecutable()
    {
        // Try to find the roslyn-diff executable in common locations
        var possiblePaths = new[]
        {
            // Built executable in the CLI project
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RoslynDiff.Cli", "bin", "Debug", "net10.0", "roslyn-diff"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RoslynDiff.Cli", "bin", "Release", "net10.0", "roslyn-diff"),
            // On Windows
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RoslynDiff.Cli", "bin", "Debug", "net10.0", "roslyn-diff.exe"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RoslynDiff.Cli", "bin", "Release", "net10.0", "roslyn-diff.exe"),
            // Dotnet tool
            "roslyn-diff",
            "dotnet-roslyn-diff"
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                _output.WriteLine($"Found roslyn-diff at: {fullPath}");
                return fullPath;
            }
        }

        // Fall back to using dotnet run
        var cliProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "RoslynDiff.Cli"));

        if (Directory.Exists(cliProjectPath))
        {
            _output.WriteLine($"Using dotnet run from: {cliProjectPath}");
            return $"dotnet run --project \"{cliProjectPath}\" --";
        }

        throw new InvalidOperationException(
            "Could not find roslyn-diff executable. Please build the project first.");
    }

    #endregion

    #region Supporting Types

    /// <summary>
    /// Represents the result of comparing two unified diff outputs.
    /// </summary>
    public sealed class DiffComparisonResult
    {
        private readonly List<string> _differences = [];
        private readonly List<string> _details = [];

        /// <summary>
        /// Gets a value indicating whether the two diffs are structurally equivalent.
        /// </summary>
        public bool AreEquivalent => _differences.Count == 0;

        /// <summary>
        /// Gets a summary of the differences found.
        /// </summary>
        public string Summary => _differences.Count == 0
            ? "No differences"
            : string.Join("; ", _differences);

        /// <summary>
        /// Gets detailed information about differences.
        /// </summary>
        public IReadOnlyList<string> Details => _details.AsReadOnly();

        /// <summary>
        /// Adds a difference description.
        /// </summary>
        public void AddDifference(string difference)
        {
            _differences.Add(difference);
        }

        /// <summary>
        /// Adds a detail line for the report.
        /// </summary>
        public void AddDetail(string detail)
        {
            _details.Add(detail);
        }
    }

    /// <summary>
    /// Represents a hunk in a unified diff.
    /// </summary>
    public sealed class DiffHunk
    {
        /// <summary>
        /// Gets or sets the hunk header line (e.g., "@@ -1,3 +1,5 @@").
        /// </summary>
        public string Header { get; set; } = string.Empty;

        /// <summary>
        /// Gets the content lines within this hunk.
        /// </summary>
        public List<string> Lines { get; } = [];
    }

    #endregion
}
