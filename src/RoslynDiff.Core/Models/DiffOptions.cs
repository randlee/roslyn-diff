namespace RoslynDiff.Core.Models;

/// <summary>
/// Configuration options for controlling diff behavior.
/// </summary>
public record DiffOptions
{
    /// <summary>
    /// Gets the diff mode to use.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the differ will automatically select the appropriate mode
    /// based on the file type and content.
    /// </remarks>
    public DiffMode? Mode { get; init; }

    /// <summary>
    /// Gets a value indicating whether whitespace differences should be ignored.
    /// </summary>
    /// <remarks>
    /// This property is retained for backward compatibility with the CLI.
    /// For more fine-grained control, use <see cref="WhitespaceMode"/> instead.
    /// </remarks>
    public bool IgnoreWhitespace { get; init; }

    /// <summary>
    /// Gets the whitespace handling mode for diff comparison.
    /// </summary>
    /// <remarks>
    /// Controls how whitespace differences are treated during comparison:
    /// <list type="bullet">
    /// <item><description><see cref="Models.WhitespaceMode.Exact"/>: Character-by-character comparison (default)</description></item>
    /// <item><description><see cref="Models.WhitespaceMode.IgnoreLeadingTrailing"/>: Ignore leading/trailing whitespace</description></item>
    /// <item><description><see cref="Models.WhitespaceMode.IgnoreAll"/>: Collapse and ignore all whitespace</description></item>
    /// <item><description><see cref="Models.WhitespaceMode.LanguageAware"/>: Language-specific whitespace handling</description></item>
    /// </list>
    /// </remarks>
    public WhitespaceMode WhitespaceMode { get; init; } = WhitespaceMode.Exact;

    /// <summary>
    /// Gets a value indicating whether comment differences should be ignored.
    /// </summary>
    /// <remarks>
    /// This option is only applicable when using Roslyn semantic diff mode.
    /// </remarks>
    public bool IgnoreComments { get; init; }

    /// <summary>
    /// Gets the number of unchanged context lines to include around each change.
    /// </summary>
    public int ContextLines { get; init; } = 3;

    /// <summary>
    /// Gets the path to the old (original) file being compared.
    /// </summary>
    /// <remarks>
    /// This is used for display purposes and to determine the file type for automatic mode selection.
    /// </remarks>
    public string? OldPath { get; init; }

    /// <summary>
    /// Gets the path to the new file being compared.
    /// </summary>
    /// <remarks>
    /// This is used for display purposes and to determine the file type for automatic mode selection.
    /// </remarks>
    public string? NewPath { get; init; }

    /// <summary>
    /// Gets a value indicating whether non-impactful changes should be included in results.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, all changes are included regardless of impact level.
    /// When <c>false</c>, non-breaking and formatting-only changes are filtered out.
    /// Default is <c>true</c> for the core library; output formatters may apply their own filtering.
    /// </remarks>
    public bool IncludeNonImpactful { get; init; } = true;

    /// <summary>
    /// Gets the minimum impact level to include in results.
    /// </summary>
    /// <remarks>
    /// Only changes with an impact level at or above this threshold are included.
    /// The hierarchy from most to least impactful is:
    /// <see cref="ChangeImpact.BreakingPublicApi"/> > <see cref="ChangeImpact.BreakingInternalApi"/>
    /// > <see cref="ChangeImpact.NonBreaking"/> > <see cref="ChangeImpact.FormattingOnly"/>.
    /// Default includes all changes.
    /// </remarks>
    public ChangeImpact MinimumImpactLevel { get; init; } = ChangeImpact.FormattingOnly;

    /// <summary>
    /// Gets the Target Framework Monikers (TFMs) to analyze during the diff operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Specifies which target frameworks should be analyzed when comparing multi-targeted projects.
    /// The differ will compile and analyze the project for each specified TFM, identifying
    /// changes that are specific to certain frameworks.
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <c>null</c> - No TFM-specific analysis is performed. This is the default behavior and
    /// is appropriate for single-targeted projects or when TFM-specific differences are not relevant.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// List with values - Analyze the specified TFMs. For example, <c>["net8.0", "net10.0"]</c>
    /// will compile and analyze the project for both .NET 8.0 and .NET 10.0, detecting changes
    /// that only apply to specific frameworks (e.g., due to conditional compilation directives
    /// or framework-specific APIs).
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// The analyzed TFMs are captured in <see cref="DiffResult.AnalyzedTfms"/>, and individual
    /// changes indicate their applicable TFMs via <see cref="Change.ApplicableToTfms"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Analyze both .NET 8.0 and .NET 10.0 target frameworks
    /// var options = new DiffOptions
    /// {
    ///     TargetFrameworks = new[] { "net8.0", "net10.0" }
    /// };
    /// </code>
    /// </example>
    public IReadOnlyList<string>? TargetFrameworks { get; init; }
}
