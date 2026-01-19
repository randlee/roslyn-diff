namespace RoslynDiff.Cli.Commands;

using System.ComponentModel;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
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
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the path to the original (old) file.
        /// </summary>
        [CommandArgument(0, "<old-file>")]
        [Description("Path to the original file")]
        public string OldPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to the new file.
        /// </summary>
        [CommandArgument(1, "<new-file>")]
        [Description("Path to the modified file")]
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

        /// <inheritdoc/>
        public override ValidationResult Validate()
        {
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

            return ValidationResult.Success();
        }
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        // Validate input files
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
                MinimumImpactLevel = effectiveImpactLevel
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
                MinimumImpactLevel = effectiveImpactLevel
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
