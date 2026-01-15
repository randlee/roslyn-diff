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
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the path to the original (old) file.
        /// </summary>
        [CommandArgument(0, "<OLD>")]
        [Description("Path to the original (old) file")]
        public string OldPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path to the new file.
        /// </summary>
        [CommandArgument(1, "<NEW>")]
        [Description("Path to the new file")]
        public string NewPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the output format.
        /// </summary>
        [CommandOption("-f|--format <FORMAT>")]
        [Description("Output format: unified, json, or html")]
        [DefaultValue("unified")]
        public string Format { get; set; } = "unified";

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
        [CommandOption("--ignore-comments")]
        [Description("Ignore comment differences (semantic mode only)")]
        [DefaultValue(false)]
        public bool IgnoreComments { get; set; }

        /// <summary>
        /// Gets or sets the number of context lines.
        /// </summary>
        [CommandOption("-c|--context <LINES>")]
        [Description("Number of context lines to show")]
        [DefaultValue(3)]
        public int ContextLines { get; set; } = 3;

        /// <summary>
        /// Gets or sets a value indicating whether to force line-based diff mode.
        /// </summary>
        [CommandOption("--line-mode")]
        [Description("Force line-based diff mode instead of semantic")]
        [DefaultValue(false)]
        public bool LineMode { get; set; }

        /// <summary>
        /// Gets or sets the output file path.
        /// </summary>
        [CommandOption("-o|--output <FILE>")]
        [Description("Write output to a file instead of stdout")]
        public string? OutputFile { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use colored output.
        /// </summary>
        [CommandOption("--color")]
        [Description("Use colored output")]
        [DefaultValue(false)]
        public bool UseColor { get; set; }
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
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

        try
        {
            // Read file contents
            var oldContent = await File.ReadAllTextAsync(settings.OldPath);
            var newContent = await File.ReadAllTextAsync(settings.NewPath);

            // Create diff options
            var options = new DiffOptions
            {
                Mode = settings.LineMode ? DiffMode.Line : null,
                IgnoreWhitespace = settings.IgnoreWhitespace,
                IgnoreComments = settings.IgnoreComments,
                ContextLines = settings.ContextLines,
                OldPath = settings.OldPath,
                NewPath = settings.NewPath
            };

            // Get the appropriate differ
            var differ = _differFactory.GetDiffer(settings.NewPath, options);

            // Perform diff
            var result = differ.Compare(oldContent, newContent, options);

            // Get the formatter
            var formatter = GetFormatter(settings.Format);

            // Format output
            var outputOptions = new OutputOptions
            {
                UseColor = settings.UseColor,
                IndentJson = true
            };

            var output = formatter.Format(result, outputOptions);

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
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    private static IOutputFormatter GetFormatter(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "json" => new JsonOutputFormatter(),
            "unified" or "text" or "plain" => new UnifiedFormatter(),
            // "html" => new HtmlFormatter(), // Future implementation
            _ => throw new ArgumentException($"Unknown format: {format}. Supported formats: unified, json")
        };
    }
}
