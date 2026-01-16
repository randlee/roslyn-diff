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

    /// <summary>
    /// Initializes a new instance of the <see cref="DiffCommand"/> class.
    /// </summary>
    public DiffCommand()
    {
        _differFactory = new DifferFactory();
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
    /// <item>roslyn-diff diff old.cs new.cs --output json</item>
    /// <item>roslyn-diff diff old.cs new.cs -w -c --mode roslyn</item>
    /// <item>roslyn-diff diff old.txt new.txt --mode line</item>
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
        [Description("Diff mode: auto, roslyn, or line [default: auto]")]
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
        /// Gets or sets a value indicating whether to ignore comments.
        /// </summary>
        /// <remarks>
        /// This option is only effective when using Roslyn semantic diff mode.
        /// </remarks>
        [CommandOption("-c|--ignore-comments")]
        [Description("Ignore comment differences (Roslyn mode only)")]
        [DefaultValue(false)]
        public bool IgnoreComments { get; set; }

        /// <summary>
        /// Gets or sets the number of context lines.
        /// </summary>
        [CommandOption("-C|--context <lines>")]
        [Description("Lines of context to show [default: 3]")]
        [DefaultValue(3)]
        public int ContextLines { get; set; } = 3;

        /// <summary>
        /// Gets or sets the output format.
        /// </summary>
        /// <remarks>
        /// Supported formats: json, html, text, terminal.
        /// </remarks>
        [CommandOption("-o|--output <format>")]
        [Description("Output format: json, html, text, terminal [default: text]")]
        [DefaultValue("text")]
        public string OutputFormat { get; set; } = "text";

        /// <summary>
        /// Gets or sets the output file path.
        /// </summary>
        /// <remarks>
        /// If not specified, output is written to stdout.
        /// </remarks>
        [CommandOption("--out-file <path>")]
        [Description("Write output to file instead of stdout")]
        public string? OutputFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use rich terminal output.
        /// </summary>
        /// <remarks>
        /// Uses Spectre.Console for enhanced formatting with colors and styling.
        /// </remarks>
        [CommandOption("-r|--rich")]
        [Description("Use rich terminal output with colors and formatting")]
        [DefaultValue(false)]
        public bool RichOutput { get; set; }
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        // Validate input files
        if (!File.Exists(settings.OldPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: Old file not found: {settings.OldPath}[/]");
            return 1;
        }

        if (!File.Exists(settings.NewPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: New file not found: {settings.NewPath}[/]");
            return 1;
        }

        // Parse and validate mode
        DiffMode? diffMode = settings.Mode.ToLowerInvariant() switch
        {
            "auto" => null,
            "roslyn" => DiffMode.Roslyn,
            "line" => DiffMode.Line,
            _ => throw new ArgumentException($"Invalid mode: '{settings.Mode}'. Valid modes: auto, roslyn, line")
        };

        try
        {
            // Read file contents
            var oldContent = await File.ReadAllTextAsync(settings.OldPath);
            var newContent = await File.ReadAllTextAsync(settings.NewPath);

            // Create diff options
            var options = new DiffOptions
            {
                Mode = diffMode,
                IgnoreWhitespace = settings.IgnoreWhitespace,
                IgnoreComments = settings.IgnoreComments,
                ContextLines = settings.ContextLines,
                OldPath = Path.GetFullPath(settings.OldPath),
                NewPath = Path.GetFullPath(settings.NewPath)
            };

            // Get the appropriate differ
            var differ = _differFactory.GetDiffer(settings.NewPath, options);

            // Perform diff
            var result = differ.Compare(oldContent, newContent, options);

            // Determine output format (use terminal format if rich output is requested)
            var formatName = settings.RichOutput ? "terminal" : settings.OutputFormat;

            // Get the formatter
            var formatterFactory = new OutputFormatterFactory();
            var formatter = formatterFactory.IsFormatSupported(formatName)
                ? formatterFactory.GetFormatter(formatName)
                : GetLegacyFormatter(formatName);

            // Format output
            var outputOptions = new OutputOptions
            {
                UseColor = settings.RichOutput,
                PrettyPrint = true,
                AvailableEditors = EditorDetector.DetectAvailableEditors()
            };

            var output = formatter.FormatResult(result, outputOptions);

            // Write output
            if (!string.IsNullOrEmpty(settings.OutputFile))
            {
                await File.WriteAllTextAsync(settings.OutputFile, output);
                AnsiConsole.MarkupLine($"[green]Output written to: {settings.OutputFile}[/]");
            }
            else
            {
                Console.WriteLine(output);
            }

            return 0;
        }
        catch (ArgumentException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private static IOutputFormatter GetLegacyFormatter(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "unified" => new UnifiedFormatter(),
            "html" => new HtmlFormatter(),
            "plain" => new PlainTextFormatter(),
            "terminal" => new SpectreConsoleFormatter(),
            _ => throw new ArgumentException($"Unknown format: {format}. Supported formats: json, text, unified, html, plain, terminal")
        };
    }
}
