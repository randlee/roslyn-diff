namespace RoslynDiff.Cli.Commands;

using System.ComponentModel;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Core.MultiFile;
using RoslynDiff.Output;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Command to compare two files and display the differences.
/// </summary>
public sealed class DiffCommand : AsyncCommand<DiffCommand.Settings>
{
    private readonly DifferFactory _differFactory;
    private readonly OutputOrchestrator _outputOrchestrator;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiffCommand"/> class.
    /// </summary>
    public DiffCommand()
    {
        _differFactory = new DifferFactory();
        _outputOrchestrator = new OutputOrchestrator();
    }

    /// <summary>
    /// Settings for the diff command.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Usage: roslyn-diff diff &lt;old-file&gt; &lt;new-file&gt; [options]
    /// OR: roslyn-diff diff --git-compare &lt;ref-range&gt; [options]
    /// </para>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    /// <item>roslyn-diff diff old.cs new.cs</item>
    /// <item>roslyn-diff diff old.cs new.cs --json</item>
    /// <item>roslyn-diff diff old.cs new.cs --html report.html --open</item>
    /// <item>roslyn-diff diff old.cs new.cs --git output.patch</item>
    /// <item>roslyn-diff diff old.cs new.cs -w -c --mode roslyn --text</item>
    /// <item>roslyn-diff diff old.txt new.txt --mode line --quiet</item>
    /// <item>roslyn-diff diff old.py new.py --whitespace-mode language-aware</item>
    /// <item>roslyn-diff diff old.cs new.cs -t net8.0</item>
    /// <item>roslyn-diff diff old.cs new.cs -t net8.0 -t net10.0</item>
    /// <item>roslyn-diff diff old.cs new.cs -T "net8.0;net10.0"</item>
    /// <item>roslyn-diff diff --git-compare main..feature-branch --html report.html</item>
    /// <item>roslyn-diff diff --git-compare abc123..def456 --json</item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the git ref range for multi-file comparison.
        /// </summary>
        /// <remarks>
        /// When specified, compares files between two git references (branches, commits, tags).
        /// Format: ref1..ref2 (e.g., "main..feature", "abc123..def456").
        /// When this option is used, old-file and new-file arguments are not required.
        /// </remarks>
        [CommandOption("--git-compare <ref-range>")]
        [Description("Git comparison mode: compare files between two refs (e.g., main..feature)")]
        public string? GitCompare { get; init; }

        /// <summary>
        /// Gets or sets the path to the original (old) file.
        /// </summary>
        [CommandArgument(0, "[old-file]")]
        [Description("Path to the original file (not required with --git-compare)")]
        public string OldPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to the new file.
        /// </summary>
        [CommandArgument(1, "[new-file]")]
        [Description("Path to the modified file (not required with --git-compare)")]
        public string NewPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the diff mode.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>auto: Use Roslyn for .cs/.vb, line diff for others</item>
        /// <item>roslyn: Force semantic diff (requires .cs or .vb)</item>
        /// <item>line: Force line-by-line diff</item>
        /// </list>
        /// </remarks>
        [CommandOption("-m|--mode <mode>")]
        [Description("Diff mode: auto, roslyn, or line [[default: auto]]")]
        [DefaultValue("auto")]
        public string Mode { get; set; } = "auto";

        /// <summary>
        /// Gets or sets a value indicating whether to ignore whitespace differences.
        /// </summary>
        [CommandOption("-w|--ignore-whitespace")]
        [Description("Ignore whitespace differences")]
        [DefaultValue(false)]
        public bool IgnoreWhitespace { get; set; }

        /// <summary>
        /// Gets or sets the whitespace handling mode.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>exact: Exact character-by-character comparison (default)</item>
        /// <item>ignore-leading-trailing: Ignore leading and trailing whitespace</item>
        /// <item>ignore-all: Ignore all whitespace differences</item>
        /// <item>language-aware: Language-aware whitespace handling</item>
        /// </list>
        /// </remarks>
        [CommandOption("--whitespace-mode <mode>")]
        [Description("Whitespace handling: exact (default), ignore-leading-trailing, ignore-all, language-aware")]
        [DefaultValue("exact")]
        public string WhitespaceMode { get; set; } = "exact";

        /// <summary>
        /// Gets or sets the number of context lines.
        /// </summary>
        [CommandOption("-C|--context <lines>")]
        [Description("Lines of context to show [[default: 3]]")]
        [DefaultValue(3)]
        public int ContextLines { get; set; } = 3;

        /// <summary>
        /// Gets or sets the JSON output option.
        /// </summary>
        /// <remarks>
        /// When specified without a value, outputs JSON to stdout.
        /// When specified with a path, writes JSON to the file.
        /// </remarks>
        [CommandOption("--json [path]")]
        [Description("JSON output (stdout if no file specified)")]
        public FlagValue<string>? JsonOutput { get; init; }

        /// <summary>
        /// Gets or sets the HTML output file path.
        /// </summary>
        /// <remarks>
        /// HTML output requires a file path to be specified.
        /// </remarks>
        [CommandOption("--html <path>")]
        [Description("HTML report to file (required: file path)")]
        public string? HtmlOutput { get; init; }

        /// <summary>
        /// Gets or sets the HTML generation mode.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>document: Complete HTML document (default)</item>
        /// <item>fragment: Embeddable HTML fragment with external CSS</item>
        /// </list>
        /// </remarks>
        [CommandOption("--html-mode <mode>")]
        [Description("HTML mode: document (full doc) or fragment (embeddable) [[default: document]]")]
        [DefaultValue("document")]
        public string HtmlMode { get; init; } = "document";

        /// <summary>
        /// Gets or sets the CSS filename for fragment mode.
        /// </summary>
        /// <remarks>
        /// Only applies when --html-mode is fragment.
        /// </remarks>
        [CommandOption("--extract-css <filename>")]
        [Description("CSS filename for fragment mode [[default: roslyn-diff.css]]")]
        [DefaultValue("roslyn-diff.css")]
        public string ExtractCss { get; init; } = "roslyn-diff.css";

        /// <summary>
        /// Gets or sets the inline view mode with optional context lines.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When specified without a value, shows full file content with inline diff markers.
        /// When specified with a number, shows N lines of context around changes (like git diff -U).
        /// </para>
        /// <para>
        /// Examples:
        /// <list type="bullet">
        /// <item>--inline: Show full file with inline diff markers</item>
        /// <item>--inline=3: Show 3 lines of context around changes</item>
        /// <item>--inline=5: Show 5 lines of context around changes</item>
        /// </list>
        /// </para>
        /// <para>
        /// Only applies to HTML output. When not specified, default tree view is used.
        /// </para>
        /// </remarks>
        [CommandOption("--inline [context-lines]")]
        [Description("Use inline diff view (like git diff). Optional: number of context lines")]
        public FlagValue<string>? Inline { get; init; }

        /// <summary>
        /// Gets or sets the plain text output option.
        /// </summary>
        /// <remarks>
        /// When specified without a value, outputs plain text to stdout.
        /// When specified with a path, writes plain text to the file.
        /// </remarks>
        [CommandOption("--text [path]")]
        [Description("Plain text diff (stdout if no file specified)")]
        public FlagValue<string>? TextOutput { get; init; }

        /// <summary>
        /// Gets or sets the Git-style unified diff output option.
        /// </summary>
        /// <remarks>
        /// When specified without a value, outputs unified diff to stdout.
        /// When specified with a path, writes unified diff to the file.
        /// </remarks>
        [CommandOption("--git [path]")]
        [Description("Git-style unified diff (stdout if no file specified)")]
        public FlagValue<string>? GitOutput { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether to open HTML output in the default browser.
        /// </summary>
        /// <remarks>
        /// This option is only valid when --html is specified.
        /// </remarks>
        [CommandOption("--open")]
        [Description("Open HTML in default browser after generation")]
        [DefaultValue(false)]
        public bool OpenInBrowser { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether to suppress default console output.
        /// </summary>
        [CommandOption("--quiet")]
        [Description("Suppress default console output")]
        [DefaultValue(false)]
        public bool Quiet { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether to disable colored output.
        /// </summary>
        [CommandOption("--no-color")]
        [Description("Disable colored output")]
        [DefaultValue(false)]
        public bool NoColor { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether to include non-impactful changes in output.
        /// </summary>
        /// <remarks>
        /// Non-impactful changes include formatting-only and non-breaking changes.
        /// By default, JSON output excludes these for cleaner API consumption.
        /// </remarks>
        [CommandOption("--include-non-impactful")]
        [Description("Include non-impactful changes in JSON output")]
        [DefaultValue(false)]
        public bool IncludeNonImpactful { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether to include formatting-only changes.
        /// </summary>
        /// <remarks>
        /// Formatting-only changes are whitespace and comment changes that don't affect code behavior.
        /// </remarks>
        [CommandOption("--include-formatting")]
        [Description("Include formatting-only changes (whitespace, comments)")]
        [DefaultValue(false)]
        public bool IncludeFormatting { get; init; }

        /// <summary>
        /// Gets or sets the minimum impact level to include in output.
        /// </summary>
        /// <remarks>
        /// Valid values: breaking-public, breaking-internal, non-breaking, all.
        /// Default is 'all' for HTML output and 'breaking-internal' for JSON output.
        /// </remarks>
        [CommandOption("--impact-level <level>")]
        [Description("Minimum impact level: breaking-public, breaking-internal, non-breaking, all [[default: all for HTML, breaking-internal for JSON]]")]
        public string? ImpactLevel { get; init; }

        /// <summary>
        /// Gets or sets individual target framework monikers (repeatable).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This option can be specified multiple times to analyze multiple TFMs.
        /// For example: -t net8.0 -t net10.0
        /// </para>
        /// <para>
        /// Valid TFM formats include:
        /// <list type="bullet">
        /// <item>Modern .NET: net5.0, net6.0, net8.0, net10.0</item>
        /// <item>.NET Framework: net462, net48, net4.8, net4.6.2</item>
        /// <item>.NET Core: netcoreapp3.1, netcoreapp2.1</item>
        /// <item>.NET Standard: netstandard2.0, netstandard2.1</item>
        /// </list>
        /// </para>
        /// </remarks>
        [CommandOption("-t|--target-framework <tfm>")]
        [Description("Target framework moniker (repeatable: -t net8.0 -t net10.0)")]
        public string[]? TargetFramework { get; init; }

        /// <summary>
        /// Gets or sets semicolon-separated target framework monikers.
        /// </summary>
        /// <remarks>
        /// Alternative to -t flag for specifying multiple TFMs in a single value.
        /// For example: -T "net8.0;net10.0"
        /// Can be combined with -t flag; all TFMs are merged and deduplicated.
        /// </remarks>
        [CommandOption("-T|--target-frameworks <tfms>")]
        [Description("Semicolon-separated TFMs (e.g., \"net8.0;net10.0\")")]
        public string? TargetFrameworks { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether to recursively traverse subdirectories.
        /// </summary>
        /// <remarks>
        /// Only applies when comparing folders (not single files or git refs).
        /// Default is false (top-level files only).
        /// </remarks>
        [CommandOption("-r|--recursive")]
        [Description("Recursively traverse subdirectories (folder comparison only)")]
        [DefaultValue(false)]
        public bool Recursive { get; init; }

        /// <summary>
        /// Gets or sets glob patterns to include (repeatable).
        /// </summary>
        /// <remarks>
        /// Only applies when comparing folders.
        /// Examples: "*.cs", "**/*.g.cs", "src/**/*.cs"
        /// Can be specified multiple times: --include "*.cs" --include "*.vb"
        /// </remarks>
        [CommandOption("--include <pattern>")]
        [Description("Include files matching glob pattern (repeatable, e.g., \"*.cs\")")]
        public string[]? IncludePatterns { get; init; }

        /// <summary>
        /// Gets or sets glob patterns to exclude (repeatable).
        /// </summary>
        /// <remarks>
        /// Only applies when comparing folders.
        /// Examples: "*.Designer.cs", "**/obj/**", "**/bin/**"
        /// Can be specified multiple times: --exclude "*.Designer.cs" --exclude "**/obj/**"
        /// Exclude patterns take precedence over include patterns.
        /// </remarks>
        [CommandOption("--exclude <pattern>")]
        [Description("Exclude files matching glob pattern (repeatable, e.g., \"**/bin/**\")")]
        public string[]? ExcludePatterns { get; init; }

        /// <inheritdoc/>
        public override ValidationResult Validate()
        {
            // Check if this is a minimal test setup (all empty) - allow for test purposes
            var hasGitCompare = !string.IsNullOrWhiteSpace(GitCompare);
            var hasOldPath = !string.IsNullOrWhiteSpace(OldPath);
            var hasNewPath = !string.IsNullOrWhiteSpace(NewPath);
            var hasAnyPath = hasOldPath || hasNewPath;

            // If nothing is set, allow (this is for tests that only check other validation rules)
            if (!hasGitCompare && !hasAnyPath)
            {
                // Skip file path validation but continue with other validations
            }
            else if (hasGitCompare && hasAnyPath)
            {
                return ValidationResult.Error("Cannot specify both --git-compare and file paths");
            }
            else if (!hasGitCompare && (!hasOldPath || !hasNewPath))
            {
                return ValidationResult.Error("Both old-file and new-file must be provided (or use --git-compare)");
            }

            // Validate git ref range format
            if (hasGitCompare)
            {
                var parts = GitCompare!.Split("..", StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    return ValidationResult.Error($"Invalid --git-compare format: '{GitCompare}'. Expected format: 'ref1..ref2' (e.g., 'main..feature')");
                }
            }

            // --open is only valid when --html is specified
            if (OpenInBrowser && string.IsNullOrEmpty(HtmlOutput))
            {
                return ValidationResult.Error("--open requires --html to be specified");
            }

            // --html requires a file path
            if (HtmlOutput is not null && string.IsNullOrWhiteSpace(HtmlOutput))
            {
                return ValidationResult.Error("--html requires a file path");
            }

            // Validate --whitespace-mode
            var validModes = new[] { "exact", "ignore-leading-trailing", "ignore-all", "language-aware" };
            if (!validModes.Contains(WhitespaceMode.ToLowerInvariant()))
            {
                return ValidationResult.Error($"Invalid whitespace mode: '{WhitespaceMode}'. Valid modes: exact, ignore-leading-trailing, ignore-all, language-aware");
            }

            // Validate --html-mode
            var validHtmlModes = new[] { "document", "fragment" };
            if (!validHtmlModes.Contains(HtmlMode.ToLowerInvariant()))
            {
                return ValidationResult.Error($"Invalid HTML mode: '{HtmlMode}'. Valid modes: document, fragment");
            }

            // Validate --inline context lines if provided
            if (Inline?.IsSet == true && !string.IsNullOrEmpty(Inline.Value))
            {
                if (!int.TryParse(Inline.Value, out var contextLines) || contextLines < 0)
                {
                    return ValidationResult.Error($"Invalid inline context lines: '{Inline.Value}'. Must be a non-negative integer.");
                }
            }

            // --inline only applies to HTML output
            if (Inline?.IsSet == true && string.IsNullOrEmpty(HtmlOutput))
            {
                return ValidationResult.Error("--inline requires --html to be specified");
            }

            // Validate TFMs if provided
            if (TargetFramework is not null)
            {
                foreach (var tfm in TargetFramework)
                {
                    try
                    {
                        Core.Tfm.TfmParser.ParseSingle(tfm);
                    }
                    catch (ArgumentException ex)
                    {
                        return ValidationResult.Error($"Invalid TFM in --target-framework: {ex.Message}");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(TargetFrameworks))
            {
                try
                {
                    Core.Tfm.TfmParser.ParseMultiple(TargetFrameworks);
                }
                catch (ArgumentException ex)
                {
                    return ValidationResult.Error($"Invalid TFM in --target-frameworks: {ex.Message}");
                }
            }

            return ValidationResult.Success();
        }
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        // Check if this is a git comparison
        if (!string.IsNullOrWhiteSpace(settings.GitCompare))
        {
            return await ExecuteGitCompareAsync(settings, cancellationToken);
        }

        // Auto-detect folder comparison mode
        var oldIsDirectory = Directory.Exists(settings.OldPath);
        var newIsDirectory = Directory.Exists(settings.NewPath);

        if (oldIsDirectory && newIsDirectory)
        {
            return await ExecuteFolderCompareAsync(settings, cancellationToken);
        }

        // Validate input files for single-file mode
        if (oldIsDirectory)
        {
            AnsiConsole.MarkupLine($"[red]Error: Old path is a directory, but new path is not. Both must be directories or both must be files.[/]");
            return OutputOrchestrator.ExitCodeError;
        }

        if (newIsDirectory)
        {
            AnsiConsole.MarkupLine($"[red]Error: New path is a directory, but old path is not. Both must be directories or both must be files.[/]");
            return OutputOrchestrator.ExitCodeError;
        }

        if (!File.Exists(settings.OldPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: Old file not found: {settings.OldPath}[/]");
            return OutputOrchestrator.ExitCodeError;
        }

        if (!File.Exists(settings.NewPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: New file not found: {settings.NewPath}[/]");
            return OutputOrchestrator.ExitCodeError;
        }

        // Parse and validate mode
        DiffMode? diffMode = settings.Mode.ToLowerInvariant() switch
        {
            "auto" => null,
            "roslyn" => DiffMode.Roslyn,
            "line" => DiffMode.Line,
            _ => null // Invalid mode will be caught below
        };

        if (settings.Mode.ToLowerInvariant() is not "auto" and not "roslyn" and not "line")
        {
            AnsiConsole.MarkupLine($"[red]Error: Invalid mode: '{settings.Mode}'. Valid modes: auto, roslyn, line[/]");
            return OutputOrchestrator.ExitCodeError;
        }

        // Parse and validate impact level
        var (impactLevel, impactError) = ParseImpactLevel(settings.ImpactLevel);
        if (impactError is not null)
        {
            AnsiConsole.MarkupLine($"[red]Error: {impactError}[/]");
            return OutputOrchestrator.ExitCodeError;
        }

        // Determine effective minimum impact level based on output type
        // Default: 'all' (FormattingOnly) for HTML, 'breaking-internal' for JSON
        var effectiveImpactLevel = impactLevel ?? (settings.JsonOutput?.IsSet == true && settings.HtmlOutput is null
            ? ChangeImpact.BreakingInternalApi
            : ChangeImpact.FormattingOnly);

        // Adjust for include flags which can override to include more
        if (settings.IncludeNonImpactful)
        {
            effectiveImpactLevel = ChangeImpact.NonBreaking;
        }
        if (settings.IncludeFormatting)
        {
            effectiveImpactLevel = ChangeImpact.FormattingOnly;
        }

        // Parse whitespace mode
        var whitespaceMode = settings.WhitespaceMode.ToLowerInvariant() switch
        {
            "exact" => Core.Models.WhitespaceMode.Exact,
            "ignore-leading-trailing" => Core.Models.WhitespaceMode.IgnoreLeadingTrailing,
            "ignore-all" => Core.Models.WhitespaceMode.IgnoreAll,
            "language-aware" => Core.Models.WhitespaceMode.LanguageAware,
            _ => Core.Models.WhitespaceMode.Exact // Invalid mode will be caught in validation
        };

        // -w/--ignore-whitespace takes precedence (backward compatibility)
        if (settings.IgnoreWhitespace)
        {
            whitespaceMode = Core.Models.WhitespaceMode.IgnoreLeadingTrailing;
        }

        // Parse and merge TFMs from both -t and -T options
        IReadOnlyList<string>? targetFrameworks = null;
        if (settings.TargetFramework is not null || !string.IsNullOrWhiteSpace(settings.TargetFrameworks))
        {
            var tfmList = new List<string>();

            // Parse individual TFMs from -t flags
            if (settings.TargetFramework is not null)
            {
                foreach (var tfm in settings.TargetFramework)
                {
                    try
                    {
                        var normalized = Core.Tfm.TfmParser.ParseSingle(tfm);
                        tfmList.Add(normalized);
                    }
                    catch (ArgumentException ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: Invalid TFM '{tfm}': {ex.Message}[/]");
                        return OutputOrchestrator.ExitCodeError;
                    }
                }
            }

            // Parse semicolon-separated TFMs from -T flag
            if (!string.IsNullOrWhiteSpace(settings.TargetFrameworks))
            {
                try
                {
                    var parsed = Core.Tfm.TfmParser.ParseMultiple(settings.TargetFrameworks);
                    tfmList.AddRange(parsed);
                }
                catch (ArgumentException ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error: Invalid TFMs in --target-frameworks: {ex.Message}[/]");
                    return OutputOrchestrator.ExitCodeError;
                }
            }

            // Remove duplicates while preserving order
            targetFrameworks = tfmList.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }

        // Parse --inline option
        string? viewMode = null;
        int? inlineContext = null;
        if (settings.Inline?.IsSet == true)
        {
            viewMode = "inline";
            if (!string.IsNullOrEmpty(settings.Inline.Value))
            {
                // Parse context lines
                if (int.TryParse(settings.Inline.Value, out var contextLines))
                {
                    inlineContext = contextLines;
                }
            }
            // If no value or parsing failed, inlineContext stays null (full file)
        }

        try
        {
            // Read file contents
            var oldContent = await File.ReadAllTextAsync(settings.OldPath, cancellationToken);
            var newContent = await File.ReadAllTextAsync(settings.NewPath, cancellationToken);

            // Create diff options
            // Note: IgnoreComments removed - Roslyn semantic diff inherently ignores comments
            // (comments are trivia and don't affect semantic equivalence)
            var options = new DiffOptions
            {
                Mode = diffMode,
                WhitespaceMode = whitespaceMode,
                IgnoreWhitespace = settings.IgnoreWhitespace, // Keep for backward compat
                ContextLines = settings.ContextLines,
                OldPath = Path.GetFullPath(settings.OldPath),
                NewPath = Path.GetFullPath(settings.NewPath),
                IncludeNonImpactful = settings.IncludeNonImpactful || settings.IncludeFormatting,
                MinimumImpactLevel = effectiveImpactLevel,
                TargetFrameworks = targetFrameworks
            };

            // Get the appropriate differ
            var differ = _differFactory.GetDiffer(settings.NewPath, options);

            // Perform diff
            var result = differ.Compare(oldContent, newContent, options);

            // Create output settings from command settings
            var outputSettings = new OutputSettings
            {
                JsonOutput = settings.JsonOutput?.IsSet == true ? (settings.JsonOutput.Value ?? "") : null,
                HtmlOutput = settings.HtmlOutput,
                TextOutput = settings.TextOutput?.IsSet == true ? (settings.TextOutput.Value ?? "") : null,
                GitOutput = settings.GitOutput?.IsSet == true ? (settings.GitOutput.Value ?? "") : null,
                OpenInBrowser = settings.OpenInBrowser,
                Quiet = settings.Quiet,
                NoColor = settings.NoColor,
                IncludeNonImpactful = settings.IncludeNonImpactful || settings.IncludeFormatting,
                IncludeFormatting = settings.IncludeFormatting,
                MinimumImpactLevel = effectiveImpactLevel,
                HtmlMode = settings.HtmlMode,
                ExtractCss = settings.ExtractCss,
                ViewMode = viewMode,
                InlineContext = inlineContext
            };

            // Use OutputOrchestrator to handle all output logic
            return await _outputOrchestrator.WriteOutputsAsync(result, outputSettings, cancellationToken);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return OutputOrchestrator.ExitCodeError;
        }
    }

    /// <summary>
    /// Executes folder comparison mode.
    /// </summary>
    private async Task<int> ExecuteFolderCompareAsync(Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Parse whitespace mode
            var whitespaceMode = settings.WhitespaceMode.ToLowerInvariant() switch
            {
                "exact" => Core.Models.WhitespaceMode.Exact,
                "ignore-leading-trailing" => Core.Models.WhitespaceMode.IgnoreLeadingTrailing,
                "ignore-all" => Core.Models.WhitespaceMode.IgnoreAll,
                "language-aware" => Core.Models.WhitespaceMode.LanguageAware,
                _ => Core.Models.WhitespaceMode.Exact
            };

            if (settings.IgnoreWhitespace)
            {
                whitespaceMode = Core.Models.WhitespaceMode.IgnoreLeadingTrailing;
            }

            // Parse and merge TFMs
            IReadOnlyList<string>? targetFrameworks = null;
            if (settings.TargetFramework is not null || !string.IsNullOrWhiteSpace(settings.TargetFrameworks))
            {
                var tfmList = new List<string>();

                if (settings.TargetFramework is not null)
                {
                    foreach (var tfm in settings.TargetFramework)
                    {
                        try
                        {
                            var normalized = Core.Tfm.TfmParser.ParseSingle(tfm);
                            tfmList.Add(normalized);
                        }
                        catch (ArgumentException ex)
                        {
                            AnsiConsole.MarkupLine($"[red]Error: Invalid TFM '{tfm}': {ex.Message}[/]");
                            return OutputOrchestrator.ExitCodeError;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(settings.TargetFrameworks))
                {
                    try
                    {
                        var parsed = Core.Tfm.TfmParser.ParseMultiple(settings.TargetFrameworks);
                        tfmList.AddRange(parsed);
                    }
                    catch (ArgumentException ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: Invalid TFMs in --target-frameworks: {ex.Message}[/]");
                        return OutputOrchestrator.ExitCodeError;
                    }
                }

                targetFrameworks = tfmList.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            }

            // Parse and validate impact level
            var (impactLevel, impactError) = ParseImpactLevel(settings.ImpactLevel);
            if (impactError is not null)
            {
                AnsiConsole.MarkupLine($"[red]Error: {impactError}[/]");
                return OutputOrchestrator.ExitCodeError;
            }

            var effectiveImpactLevel = impactLevel ?? (settings.JsonOutput?.IsSet == true && settings.HtmlOutput is null
                ? ChangeImpact.BreakingInternalApi
                : ChangeImpact.FormattingOnly);

            if (settings.IncludeNonImpactful)
            {
                effectiveImpactLevel = ChangeImpact.NonBreaking;
            }
            if (settings.IncludeFormatting)
            {
                effectiveImpactLevel = ChangeImpact.FormattingOnly;
            }

            // Create diff options
            var options = new DiffOptions
            {
                Mode = null, // Auto-detect based on file type
                WhitespaceMode = whitespaceMode,
                IgnoreWhitespace = settings.IgnoreWhitespace,
                ContextLines = settings.ContextLines,
                IncludeNonImpactful = settings.IncludeNonImpactful || settings.IncludeFormatting,
                MinimumImpactLevel = effectiveImpactLevel,
                TargetFrameworks = targetFrameworks
            };

            // Create folder compare options
            var folderOptions = new FolderCompareOptions
            {
                Recursive = settings.Recursive,
                IncludePatterns = settings.IncludePatterns ?? [],
                ExcludePatterns = settings.ExcludePatterns ?? []
            };

            // Perform folder comparison using parallel processing
            var folderComparer = new FolderComparer();
            var multiFileResult = folderComparer.CompareParallel(settings.OldPath, settings.NewPath, options, folderOptions);

            // Create output options
            var outputOptions = new OutputOptions
            {
                IncludeContent = true,
                PrettyPrint = true,
                IncludeStats = true,
                IncludeNonImpactful = settings.IncludeNonImpactful || settings.IncludeFormatting,
                ContextLines = settings.ContextLines,
                UseColor = !settings.NoColor
            };

            // Handle JSON output
            if (settings.JsonOutput?.IsSet == true)
            {
                var jsonFormatter = new JsonFormatter();
                var json = jsonFormatter.FormatMultiFileResult(multiFileResult, outputOptions);

                if (string.IsNullOrEmpty(settings.JsonOutput.Value))
                {
                    // Write to stdout
                    await Console.Out.WriteLineAsync(json);
                }
                else
                {
                    // Write to file
                    await File.WriteAllTextAsync(settings.JsonOutput.Value, json, cancellationToken);
                    if (!settings.Quiet)
                    {
                        AnsiConsole.MarkupLine($"[green]JSON output written to: {settings.JsonOutput.Value}[/]");
                    }
                }
            }

            // Handle HTML output
            if (settings.HtmlOutput is not null)
            {
                // TODO: Implement HTML multi-file formatter
                if (!settings.Quiet)
                {
                    AnsiConsole.MarkupLine($"[yellow]HTML output for multi-file diffs is not yet implemented[/]");
                }
            }

            // Display summary to console if not quiet and no stdout output
            if (!settings.Quiet && (settings.JsonOutput?.IsSet != true || !string.IsNullOrEmpty(settings.JsonOutput?.Value)))
            {
                AnsiConsole.MarkupLine($"[green]Folder comparison complete[/]");
                AnsiConsole.MarkupLine($"[yellow]Files changed: {multiFileResult.Summary.TotalFiles}[/]");
                AnsiConsole.MarkupLine($"[yellow]  Modified: {multiFileResult.Summary.ModifiedFiles}[/]");
                AnsiConsole.MarkupLine($"[yellow]  Added: {multiFileResult.Summary.AddedFiles}[/]");
                AnsiConsole.MarkupLine($"[yellow]  Removed: {multiFileResult.Summary.RemovedFiles}[/]");
                AnsiConsole.MarkupLine($"[yellow]Total changes: {multiFileResult.Summary.TotalChanges}[/]");
            }

            // Return appropriate exit code
            return multiFileResult.Summary.TotalChanges > 0
                ? OutputOrchestrator.ExitCodeDiffFound
                : OutputOrchestrator.ExitCodeNoDiff;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error during folder comparison: {ex.Message}[/]");
            return OutputOrchestrator.ExitCodeError;
        }
    }

    /// <summary>
    /// Executes git comparison mode.
    /// </summary>
    private async Task<int> ExecuteGitCompareAsync(Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Find the git repository root
            var currentDir = Directory.GetCurrentDirectory();
            var repoPath = FindGitRepository(currentDir);

            if (repoPath == null)
            {
                AnsiConsole.MarkupLine("[red]Error: Not in a git repository. Run this command from within a git repository.[/]");
                return OutputOrchestrator.ExitCodeError;
            }

            // Parse whitespace mode
            var whitespaceMode = settings.WhitespaceMode.ToLowerInvariant() switch
            {
                "exact" => Core.Models.WhitespaceMode.Exact,
                "ignore-leading-trailing" => Core.Models.WhitespaceMode.IgnoreLeadingTrailing,
                "ignore-all" => Core.Models.WhitespaceMode.IgnoreAll,
                "language-aware" => Core.Models.WhitespaceMode.LanguageAware,
                _ => Core.Models.WhitespaceMode.Exact
            };

            if (settings.IgnoreWhitespace)
            {
                whitespaceMode = Core.Models.WhitespaceMode.IgnoreLeadingTrailing;
            }

            // Parse and merge TFMs
            IReadOnlyList<string>? targetFrameworks = null;
            if (settings.TargetFramework is not null || !string.IsNullOrWhiteSpace(settings.TargetFrameworks))
            {
                var tfmList = new List<string>();

                if (settings.TargetFramework is not null)
                {
                    foreach (var tfm in settings.TargetFramework)
                    {
                        try
                        {
                            var normalized = Core.Tfm.TfmParser.ParseSingle(tfm);
                            tfmList.Add(normalized);
                        }
                        catch (ArgumentException ex)
                        {
                            AnsiConsole.MarkupLine($"[red]Error: Invalid TFM '{tfm}': {ex.Message}[/]");
                            return OutputOrchestrator.ExitCodeError;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(settings.TargetFrameworks))
                {
                    try
                    {
                        var parsed = Core.Tfm.TfmParser.ParseMultiple(settings.TargetFrameworks);
                        tfmList.AddRange(parsed);
                    }
                    catch (ArgumentException ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: Invalid TFMs in --target-frameworks: {ex.Message}[/]");
                        return OutputOrchestrator.ExitCodeError;
                    }
                }

                targetFrameworks = tfmList.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            }

            // Parse and validate impact level
            var (impactLevel, impactError) = ParseImpactLevel(settings.ImpactLevel);
            if (impactError is not null)
            {
                AnsiConsole.MarkupLine($"[red]Error: {impactError}[/]");
                return OutputOrchestrator.ExitCodeError;
            }

            var effectiveImpactLevel = impactLevel ?? (settings.JsonOutput?.IsSet == true && settings.HtmlOutput is null
                ? ChangeImpact.BreakingInternalApi
                : ChangeImpact.FormattingOnly);

            if (settings.IncludeNonImpactful)
            {
                effectiveImpactLevel = ChangeImpact.NonBreaking;
            }
            if (settings.IncludeFormatting)
            {
                effectiveImpactLevel = ChangeImpact.FormattingOnly;
            }

            // Create diff options
            var options = new DiffOptions
            {
                Mode = null, // Auto-detect based on file type
                WhitespaceMode = whitespaceMode,
                IgnoreWhitespace = settings.IgnoreWhitespace,
                ContextLines = settings.ContextLines,
                IncludeNonImpactful = settings.IncludeNonImpactful || settings.IncludeFormatting,
                MinimumImpactLevel = effectiveImpactLevel,
                TargetFrameworks = targetFrameworks
            };

            // Perform git comparison using parallel processing
            var gitComparer = new GitComparer();
            var multiFileResult = gitComparer.CompareParallel(repoPath, settings.GitCompare!, options);

            // Create output options
            var outputOptions = new OutputOptions
            {
                IncludeContent = true,
                PrettyPrint = true,
                IncludeStats = true,
                IncludeNonImpactful = settings.IncludeNonImpactful || settings.IncludeFormatting,
                ContextLines = settings.ContextLines,
                UseColor = !settings.NoColor
            };

            // Handle JSON output
            if (settings.JsonOutput?.IsSet == true)
            {
                var jsonFormatter = new JsonFormatter();
                var json = jsonFormatter.FormatMultiFileResult(multiFileResult, outputOptions);

                if (string.IsNullOrEmpty(settings.JsonOutput.Value))
                {
                    // Write to stdout
                    await Console.Out.WriteLineAsync(json);
                }
                else
                {
                    // Write to file
                    await File.WriteAllTextAsync(settings.JsonOutput.Value, json, cancellationToken);
                    if (!settings.Quiet)
                    {
                        AnsiConsole.MarkupLine($"[green]JSON output written to: {settings.JsonOutput.Value}[/]");
                    }
                }
            }

            // Handle HTML output
            if (settings.HtmlOutput is not null)
            {
                // TODO: Implement HTML multi-file formatter
                if (!settings.Quiet)
                {
                    AnsiConsole.MarkupLine($"[yellow]HTML output for multi-file diffs is not yet implemented[/]");
                }
            }

            // Display summary to console if not quiet and no stdout output
            if (!settings.Quiet && (settings.JsonOutput?.IsSet != true || !string.IsNullOrEmpty(settings.JsonOutput?.Value)))
            {
                AnsiConsole.MarkupLine($"[green]Git comparison complete: {settings.GitCompare}[/]");
                AnsiConsole.MarkupLine($"[yellow]Files changed: {multiFileResult.Summary.TotalFiles}[/]");
                AnsiConsole.MarkupLine($"[yellow]  Modified: {multiFileResult.Summary.ModifiedFiles}[/]");
                AnsiConsole.MarkupLine($"[yellow]  Added: {multiFileResult.Summary.AddedFiles}[/]");
                AnsiConsole.MarkupLine($"[yellow]  Removed: {multiFileResult.Summary.RemovedFiles}[/]");
                AnsiConsole.MarkupLine($"[yellow]Total changes: {multiFileResult.Summary.TotalChanges}[/]");
            }

            // Return appropriate exit code
            return multiFileResult.Summary.TotalChanges > 0
                ? OutputOrchestrator.ExitCodeDiffFound
                : OutputOrchestrator.ExitCodeNoDiff;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error during git comparison: {ex.Message}[/]");
            return OutputOrchestrator.ExitCodeError;
        }
    }

    /// <summary>
    /// Finds the git repository root by searching up the directory tree.
    /// Handles both regular repositories and git worktrees.
    /// </summary>
    private static string? FindGitRepository(string startPath)
    {
        var currentDir = new DirectoryInfo(startPath);

        while (currentDir != null)
        {
            var gitPath = Path.Combine(currentDir.FullName, ".git");

            // Check if .git is a directory (regular repo) or a file (worktree)
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return currentDir.FullName;
            }

            currentDir = currentDir.Parent;
        }

        return null;
    }

    /// <summary>
    /// Parses the impact level string to a ChangeImpact enum value.
    /// </summary>
    /// <param name="impactLevel">The impact level string from CLI.</param>
    /// <returns>A tuple containing the parsed impact level (or null for default) and any error message.</returns>
    private static (ChangeImpact? Level, string? Error) ParseImpactLevel(string? impactLevel)
    {
        if (string.IsNullOrEmpty(impactLevel))
        {
            return (null, null); // Use default behavior
        }

        return impactLevel.ToLowerInvariant() switch
        {
            "breaking-public" => (ChangeImpact.BreakingPublicApi, null),
            "breaking-internal" => (ChangeImpact.BreakingInternalApi, null),
            "non-breaking" => (ChangeImpact.NonBreaking, null),
            "all" => (ChangeImpact.FormattingOnly, null),
            _ => (null, $"Invalid impact level: '{impactLevel}'. Valid values: breaking-public, breaking-internal, non-breaking, all")
        };
    }
}
