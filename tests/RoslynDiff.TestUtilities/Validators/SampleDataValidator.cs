using System.Diagnostics;
using System.Text;
using RoslynDiff.TestUtilities.Comparers;
using RoslynDiff.TestUtilities.Models;
using RoslynDiff.TestUtilities.Parsers;

namespace RoslynDiff.TestUtilities.Validators;

/// <summary>
/// Provides comprehensive validation of roslyn-diff sample data outputs across multiple formats.
/// This is the main entry point class that orchestrates all validation using parsers and validators.
/// </summary>
public static class SampleDataValidator
{
    /// <summary>
    /// Validates all aspects of the diff output for the given files.
    /// Runs all validation methods and returns aggregated results.
    /// </summary>
    /// <param name="oldFile">Path to the old version of the file.</param>
    /// <param name="newFile">Path to the new version of the file.</param>
    /// <param name="options">Optional configuration options for validation.</param>
    /// <returns>A collection of test results from all validations.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="oldFile"/> or <paramref name="newFile"/> is <c>null</c>.</exception>
    /// <exception cref="FileNotFoundException">Thrown when either file does not exist.</exception>
    public static IEnumerable<TestResult> ValidateAll(
        string oldFile,
        string newFile,
        SampleDataValidatorOptions? options = null)
    {
        if (oldFile == null) throw new ArgumentNullException(nameof(oldFile));
        if (newFile == null) throw new ArgumentNullException(nameof(newFile));
        if (!File.Exists(oldFile)) throw new FileNotFoundException($"Old file not found: {oldFile}");
        if (!File.Exists(newFile)) throw new FileNotFoundException($"New file not found: {newFile}");

        options ??= new SampleDataValidatorOptions();

        var results = new List<TestResult>();

        try
        {
            // Run all validation methods
            results.AddRange(ValidateLineNumberIntegrity(oldFile, newFile, options));
            results.AddRange(ValidateJsonConsistency(oldFile, newFile, options));
            results.AddRange(ValidateHtmlConsistency(oldFile, newFile, options));
            results.AddRange(ValidateCrossFormatConsistency(oldFile, newFile, options));
        }
        catch (Exception ex)
        {
            results.Add(TestResult.Fail(
                "Validation Exception",
                $"An error occurred during validation: {ex.Message}",
                new[] { ex.ToString() }
            ));
        }

        return results;
    }

    /// <summary>
    /// Validates line number integrity for all output formats.
    /// Checks for overlaps, duplicates, and sequential ordering.
    /// </summary>
    /// <param name="oldFile">Path to the old version of the file.</param>
    /// <param name="newFile">Path to the new version of the file.</param>
    /// <param name="options">Configuration options for validation.</param>
    /// <returns>A collection of test results for line number validation.</returns>
    public static IEnumerable<TestResult> ValidateLineNumberIntegrity(
        string oldFile,
        string newFile,
        SampleDataValidatorOptions? options = null)
    {
        options ??= new SampleDataValidatorOptions();
        var results = new List<TestResult>();
        var tempFiles = new List<string>();

        try
        {
            // Generate outputs in all formats
            var jsonOutput = GenerateOutput(oldFile, newFile, "json", options);
            var htmlOutput = GenerateOutput(oldFile, newFile, "html", options);
            var textOutput = GenerateOutput(oldFile, newFile, "text", options);

            tempFiles.AddRange(new[] { jsonOutput, htmlOutput, textOutput });

            // Validate JSON format
            if (File.Exists(jsonOutput))
            {
                var jsonParser = new JsonOutputParser();
                var jsonContent = File.ReadAllText(jsonOutput);
                var jsonRanges = jsonParser.ExtractLineRanges(jsonContent).ToList();

                results.Add(LineNumberValidator.ValidateRanges(
                    jsonRanges,
                    "JSON Output Line Numbers",
                    requireSequential: false
                ));
            }

            // Validate HTML format
            if (File.Exists(htmlOutput))
            {
                var htmlParser = new HtmlOutputParser();
                var htmlContent = File.ReadAllText(htmlOutput);
                var htmlRanges = htmlParser.ExtractLineRanges(htmlContent).ToList();

                results.Add(LineNumberValidator.ValidateRanges(
                    htmlRanges,
                    "HTML Output Line Numbers",
                    requireSequential: false
                ));
            }

            // Validate Text format
            if (File.Exists(textOutput))
            {
                var textParser = new TextOutputParser();
                var textContent = File.ReadAllText(textOutput);
                var textRanges = textParser.ExtractLineRanges(textContent).ToList();

                results.Add(LineNumberValidator.ValidateRanges(
                    textRanges,
                    "Text Output Line Numbers",
                    requireSequential: false
                ));
            }

            // Try unified diff format if in line mode
            if (ShouldGenerateUnifiedDiff(oldFile, options))
            {
                var unifiedOutput = GenerateOutput(oldFile, newFile, "git", options);
                tempFiles.Add(unifiedOutput);

                if (File.Exists(unifiedOutput))
                {
                    var unifiedParser = new UnifiedDiffParser();
                    var unifiedContent = File.ReadAllText(unifiedOutput);
                    var unifiedRanges = unifiedParser.ExtractLineRanges(unifiedContent).ToList();

                    results.Add(LineNumberValidator.ValidateRanges(
                        unifiedRanges,
                        "Unified Diff Line Numbers",
                        requireSequential: false
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            results.Add(TestResult.Fail(
                "Line Number Integrity Validation",
                $"Error during validation: {ex.Message}",
                new[] { ex.ToString() }
            ));
        }
        finally
        {
            if (!options.PreserveTempFiles)
            {
                CleanupTempFiles(tempFiles);
            }
        }

        return results;
    }

    /// <summary>
    /// Validates JSON output consistency across different flag combinations.
    /// </summary>
    /// <param name="oldFile">Path to the old version of the file.</param>
    /// <param name="newFile">Path to the new version of the file.</param>
    /// <param name="options">Configuration options for validation.</param>
    /// <returns>A collection of test results for JSON validation.</returns>
    public static IEnumerable<TestResult> ValidateJsonConsistency(
        string oldFile,
        string newFile,
        SampleDataValidatorOptions? options = null)
    {
        options ??= new SampleDataValidatorOptions();
        var results = new List<TestResult>();
        var tempFiles = new List<string>();

        try
        {
            // Generate JSON with different flag combinations
            var defaultJson = GenerateOutput(oldFile, newFile, "json", options);
            tempFiles.Add(defaultJson);

            if (!File.Exists(defaultJson))
            {
                results.Add(TestResult.Fail(
                    "JSON Consistency Validation",
                    "Failed to generate JSON output"
                ));
                return results;
            }

            var jsonContent = File.ReadAllText(defaultJson);
            var parser = new JsonOutputParser();

            // Validate that JSON can be parsed
            if (!parser.CanParse(jsonContent))
            {
                results.Add(TestResult.Fail(
                    "JSON Format Validation",
                    "Generated JSON output is not valid or parseable"
                ));
                return results;
            }

            // Extract and validate line numbers
            var lineNumbers = parser.ExtractLineNumbers(jsonContent).ToList();
            var lineRanges = parser.ExtractLineRanges(jsonContent).ToList();

            results.Add(LineNumberValidator.ValidateNoDuplicates(
                lineNumbers,
                "JSON Line Numbers - Duplicates Check"
            ));

            results.Add(LineNumberValidator.ValidateNoOverlaps(
                lineRanges,
                "JSON Line Ranges - Overlaps Check"
            ));

            // Use JsonValidator from Workstream E
            results.Add(JsonValidator.ValidateLineNumberIntegrity(jsonContent));
        }
        catch (Exception ex)
        {
            results.Add(TestResult.Fail(
                "JSON Consistency Validation",
                $"Error during validation: {ex.Message}",
                new[] { ex.ToString() }
            ));
        }
        finally
        {
            if (!options.PreserveTempFiles)
            {
                CleanupTempFiles(tempFiles);
            }
        }

        return results;
    }

    /// <summary>
    /// Validates HTML output consistency across different flag combinations.
    /// </summary>
    /// <param name="oldFile">Path to the old version of the file.</param>
    /// <param name="newFile">Path to the new version of the file.</param>
    /// <param name="options">Configuration options for validation.</param>
    /// <returns>A collection of test results for HTML validation.</returns>
    public static IEnumerable<TestResult> ValidateHtmlConsistency(
        string oldFile,
        string newFile,
        SampleDataValidatorOptions? options = null)
    {
        options ??= new SampleDataValidatorOptions();
        var results = new List<TestResult>();
        var tempFiles = new List<string>();

        try
        {
            // Generate HTML output
            var htmlOutput = GenerateOutput(oldFile, newFile, "html", options);
            tempFiles.Add(htmlOutput);

            if (!File.Exists(htmlOutput))
            {
                results.Add(TestResult.Fail(
                    "HTML Consistency Validation",
                    "Failed to generate HTML output"
                ));
                return results;
            }

            var htmlContent = File.ReadAllText(htmlOutput);
            var parser = new HtmlOutputParser();

            // Validate that HTML can be parsed
            if (!parser.CanParse(htmlContent))
            {
                results.Add(TestResult.Fail(
                    "HTML Format Validation",
                    "Generated HTML output is not valid or parseable"
                ));
                return results;
            }

            // Extract and validate line numbers
            var lineNumbers = parser.ExtractLineNumbers(htmlContent).ToList();
            var lineRanges = parser.ExtractLineRanges(htmlContent).ToList();

            results.Add(LineNumberValidator.ValidateNoDuplicates(
                lineNumbers,
                "HTML Line Numbers - Duplicates Check"
            ));

            results.Add(LineNumberValidator.ValidateNoOverlaps(
                lineRanges,
                "HTML Line Ranges - Overlaps Check"
            ));

            // Use HtmlValidator from Workstream E
            results.AddRange(HtmlValidator.ValidateAll(htmlContent));
        }
        catch (Exception ex)
        {
            results.Add(TestResult.Fail(
                "HTML Consistency Validation",
                $"Error during validation: {ex.Message}",
                new[] { ex.ToString() }
            ));
        }
        finally
        {
            if (!options.PreserveTempFiles)
            {
                CleanupTempFiles(tempFiles);
            }
        }

        return results;
    }

    /// <summary>
    /// Validates consistency across all output formats.
    /// Ensures that JSON, HTML, Text, and Git formats all report the same changes.
    /// </summary>
    /// <param name="oldFile">Path to the old version of the file.</param>
    /// <param name="newFile">Path to the new version of the file.</param>
    /// <param name="options">Configuration options for validation.</param>
    /// <returns>A collection of test results for cross-format validation.</returns>
    public static IEnumerable<TestResult> ValidateCrossFormatConsistency(
        string oldFile,
        string newFile,
        SampleDataValidatorOptions? options = null)
    {
        options ??= new SampleDataValidatorOptions();
        var results = new List<TestResult>();
        var tempFiles = new List<string>();

        try
        {
            // Generate all formats
            var jsonOutput = GenerateOutput(oldFile, newFile, "json", options);
            var htmlOutput = GenerateOutput(oldFile, newFile, "html", options);
            var textOutput = GenerateOutput(oldFile, newFile, "text", options);

            tempFiles.AddRange(new[] { jsonOutput, htmlOutput, textOutput });

            // Parse each format
            var jsonParser = new JsonOutputParser();
            var htmlParser = new HtmlOutputParser();
            var textParser = new TextOutputParser();

            var jsonRanges = new List<LineRange>();
            var htmlRanges = new List<LineRange>();
            var textRanges = new List<LineRange>();

            if (File.Exists(jsonOutput))
            {
                jsonRanges = jsonParser.ExtractLineRanges(File.ReadAllText(jsonOutput)).ToList();
            }

            if (File.Exists(htmlOutput))
            {
                htmlRanges = htmlParser.ExtractLineRanges(File.ReadAllText(htmlOutput)).ToList();
            }

            if (File.Exists(textOutput))
            {
                textRanges = textParser.ExtractLineRanges(File.ReadAllText(textOutput)).ToList();
            }

            // Compare range counts
            var rangeCounts = new Dictionary<string, int>
            {
                ["JSON"] = jsonRanges.Count,
                ["HTML"] = htmlRanges.Count,
                ["Text"] = textRanges.Count
            };

            // Check if all formats have the same number of ranges
            var uniqueCounts = rangeCounts.Values.Distinct().Count();
            if (uniqueCounts > 1)
            {
                var issues = rangeCounts.Select(kvp => $"{kvp.Key}: {kvp.Value} ranges").ToList();
                results.Add(TestResult.Fail(
                    "Cross-Format Range Count Consistency",
                    "Different formats report different numbers of line ranges",
                    issues
                ));
            }
            else
            {
                results.Add(TestResult.Pass(
                    "Cross-Format Range Count Consistency",
                    $"All formats report {rangeCounts.First().Value} line ranges"
                ));
            }

            // Compare line number sets
            var jsonLines = new HashSet<int>(jsonRanges.SelectMany(r => Enumerable.Range(r.Start, r.LineCount)));
            var htmlLines = new HashSet<int>(htmlRanges.SelectMany(r => Enumerable.Range(r.Start, r.LineCount)));
            var textLines = new HashSet<int>(textRanges.SelectMany(r => Enumerable.Range(r.Start, r.LineCount)));

            var issues2 = new List<string>();

            if (!jsonLines.SetEquals(htmlLines))
            {
                issues2.Add("JSON and HTML formats have different line numbers");
            }

            if (!jsonLines.SetEquals(textLines))
            {
                issues2.Add("JSON and Text formats have different line numbers");
            }

            if (!htmlLines.SetEquals(textLines))
            {
                issues2.Add("HTML and Text formats have different line numbers");
            }

            if (issues2.Any())
            {
                results.Add(TestResult.Fail(
                    "Cross-Format Line Number Consistency",
                    "Line numbers differ across formats",
                    issues2
                ));
            }
            else
            {
                results.Add(TestResult.Pass(
                    "Cross-Format Line Number Consistency",
                    "All formats report consistent line numbers"
                ));
            }

            // Add unified diff comparison if in line mode
            if (ShouldGenerateUnifiedDiff(oldFile, options))
            {
                var unifiedOutput = GenerateOutput(oldFile, newFile, "git", options);
                tempFiles.Add(unifiedOutput);

                if (File.Exists(unifiedOutput))
                {
                    var unifiedParser = new UnifiedDiffParser();
                    var unifiedRanges = unifiedParser.ExtractLineRanges(File.ReadAllText(unifiedOutput)).ToList();

                    results.Add(TestResult.Pass(
                        "Unified Diff Format Generated",
                        $"Unified diff contains {unifiedRanges.Count} ranges"
                    ));
                }
            }
        }
        catch (Exception ex)
        {
            results.Add(TestResult.Fail(
                "Cross-Format Consistency Validation",
                $"Error during validation: {ex.Message}",
                new[] { ex.ToString() }
            ));
        }
        finally
        {
            if (!options.PreserveTempFiles)
            {
                CleanupTempFiles(tempFiles);
            }
        }

        return results;
    }

    /// <summary>
    /// Generates output by invoking the roslyn-diff CLI tool.
    /// </summary>
    /// <param name="oldFile">Path to the old version of the file.</param>
    /// <param name="newFile">Path to the new version of the file.</param>
    /// <param name="format">The output format (json, html, text, git).</param>
    /// <param name="options">Configuration options.</param>
    /// <returns>Path to the generated output file.</returns>
    private static string GenerateOutput(
        string oldFile,
        string newFile,
        string format,
        SampleDataValidatorOptions options)
    {
        var outputDir = options.TempOutputDirectory ?? Path.GetTempPath();
        var outputFile = Path.Combine(outputDir, $"roslyn-diff-{Guid.NewGuid()}.{format}");

        // Determine the CLI path
        var cliPath = options.RoslynDiffCliPath ?? FindRoslynDiffCli();

        // Build arguments
        var args = new StringBuilder();
        args.Append($"diff \"{oldFile}\" \"{newFile}\"");
        args.Append($" --{format.ToLowerInvariant()} \"{outputFile}\"");

        // Add mode flag if specified
        if (options.DiffMode != DiffMode.Auto)
        {
            args.Append($" --mode {options.DiffMode.ToString().ToLowerInvariant()}");
        }

        // Execute the CLI
        var startInfo = new ProcessStartInfo
        {
            FileName = cliPath,
            Arguments = args.ToString(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        if (!process.WaitForExit(options.CliTimeoutMs))
        {
            process.Kill();
            throw new TimeoutException($"CLI invocation timed out after {options.CliTimeoutMs}ms");
        }

        // Exit codes: 0 = no differences (files identical), 1 = differences found (both success!)
        // Exit code 2+ = actual error (file not found, invalid arguments, etc.)
        if (process.ExitCode > 1)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"CLI invocation failed with exit code {process.ExitCode}: {error}");
        }

        return outputFile;
    }

    /// <summary>
    /// Attempts to find the roslyn-diff CLI executable.
    /// </summary>
    /// <returns>Path to the CLI executable.</returns>
    private static string FindRoslynDiffCli()
    {
        // Try local build output
        var localBuildPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "RoslynDiff.Cli", "bin", "Debug", "net10.0", "roslyn-diff"
        );

        if (File.Exists(localBuildPath))
        {
            return localBuildPath;
        }

        // Assume it's available as a dotnet tool or in PATH
        return "roslyn-diff";
    }

    /// <summary>
    /// Determines whether unified diff format should be generated.
    /// </summary>
    /// <param name="oldFile">Path to the old file.</param>
    /// <param name="options">Configuration options.</param>
    /// <returns><c>true</c> if unified diff should be generated; otherwise, <c>false</c>.</returns>
    private static bool ShouldGenerateUnifiedDiff(string oldFile, SampleDataValidatorOptions options)
    {
        // Only generate unified diff for text files or when in line mode
        var extension = Path.GetExtension(oldFile).ToLowerInvariant();
        var isTextFile = extension is ".txt" or ".md" or ".log";
        var isLineMode = options.DiffMode == DiffMode.Line;

        return isTextFile || isLineMode;
    }

    /// <summary>
    /// Cleans up temporary files.
    /// </summary>
    /// <param name="files">Collection of file paths to delete.</param>
    private static void CleanupTempFiles(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Aggregates multiple test results into a summary.
    /// </summary>
    /// <param name="results">The test results to aggregate.</param>
    /// <returns>A single test result summarizing all results.</returns>
    public static TestResult AggregateResults(IEnumerable<TestResult> results)
    {
        var resultList = results.ToList();
        var passed = resultList.Count(r => r.Passed);
        var failed = resultList.Count(r => !r.Passed);
        var total = resultList.Count;

        if (failed == 0)
        {
            return TestResult.Pass(
                "Aggregate Validation Results",
                $"All {total} validations passed"
            );
        }

        var issues = resultList
            .Where(r => !r.Passed)
            .Select(r => $"{r.Context}: {r.Message}")
            .ToList();

        return TestResult.Fail(
            "Aggregate Validation Results",
            $"{failed} of {total} validations failed",
            issues
        );
    }
}
