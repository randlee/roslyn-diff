namespace RoslynDiff.TestUtilities.Models;

/// <summary>
/// Represents the diff mode to use when validating sample data.
/// </summary>
public enum DiffMode
{
    /// <summary>
    /// Automatically detect the appropriate diff mode based on file extension.
    /// </summary>
    Auto,

    /// <summary>
    /// Use Roslyn-based semantic diff for C# and VB.NET files.
    /// </summary>
    Roslyn,

    /// <summary>
    /// Use line-by-line text diff.
    /// </summary>
    Line,

    /// <summary>
    /// Validate both Roslyn and Line modes and compare results.
    /// </summary>
    Both
}

/// <summary>
/// Provides configuration options for sample data validation.
/// </summary>
public class SampleDataValidatorOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to ignore timestamps in validation.
    /// Default is <c>true</c>.
    /// </summary>
    public bool IgnoreTimestamps { get; set; } = true;

    /// <summary>
    /// Gets or sets the diff mode to use when generating output.
    /// Default is <see cref="DiffMode.Auto"/>.
    /// </summary>
    public DiffMode DiffMode { get; set; } = DiffMode.Auto;

    /// <summary>
    /// Gets or sets a value indicating whether to include external tool comparisons (if available).
    /// Default is <c>false</c>.
    /// </summary>
    public bool IncludeExternalTools { get; set; } = false;

    /// <summary>
    /// Gets or sets the temporary output directory for generated files.
    /// If <c>null</c>, a system temporary directory will be used.
    /// </summary>
    public string? TempOutputDirectory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to preserve temporary files after validation.
    /// Default is <c>false</c> (files are cleaned up).
    /// </summary>
    public bool PreserveTempFiles { get; set; } = false;

    /// <summary>
    /// Gets or sets the timeout in milliseconds for CLI invocations.
    /// Default is 30000 (30 seconds).
    /// </summary>
    public int CliTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the path to the roslyn-diff CLI executable.
    /// If <c>null</c>, the tool will attempt to locate it automatically.
    /// </summary>
    public string? RoslynDiffCliPath { get; set; }
}
