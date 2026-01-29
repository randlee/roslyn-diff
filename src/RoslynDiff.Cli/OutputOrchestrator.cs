namespace RoslynDiff.Cli;

using System.Diagnostics;
using System.Runtime.InteropServices;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;

/// <summary>
/// Orchestrates output generation for multiple formats, handling TTY detection,
/// stdout vs file routing, and exit code determination.
/// </summary>
public class OutputOrchestrator
{
    private readonly OutputFormatterFactory _formatterFactory;

    /// <summary>
    /// Exit code indicating no differences found.
    /// </summary>
    public const int ExitCodeNoDiff = 0;

    /// <summary>
    /// Exit code indicating differences were found (success with diff).
    /// </summary>
    public const int ExitCodeDiffFound = 1;

    /// <summary>
    /// Exit code indicating an error occurred (for use at command level).
    /// </summary>
    public const int ExitCodeError = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputOrchestrator"/> class.
    /// </summary>
    public OutputOrchestrator()
    {
        _formatterFactory = new OutputFormatterFactory();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OutputOrchestrator"/> class
    /// with a custom formatter factory.
    /// </summary>
    /// <param name="formatterFactory">The formatter factory to use.</param>
    public OutputOrchestrator(OutputFormatterFactory formatterFactory)
    {
        _formatterFactory = formatterFactory ?? throw new ArgumentNullException(nameof(formatterFactory));
    }

    /// <summary>
    /// Writes outputs in all specified formats and returns the appropriate exit code.
    /// </summary>
    /// <param name="result">The diff result to output.</param>
    /// <param name="settings">The output settings controlling format and destination.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// Exit code: 0 if no differences, 1 if differences found.
    /// </returns>
    public async Task<int> WriteOutputsAsync(
        DiffResult result,
        OutputSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(settings);

        // Track if any format writes to stdout using a wrapper class (since async methods can't use ref)
        var stdoutState = new StdoutState();

        // Create output options
        var outputOptions = CreateOutputOptions(settings);

        // Process each output format
        // Order: JSON, Text, Git, HTML (HTML last since we might open in browser)

        // JSON output
        if (settings.JsonOutput is not null)
        {
            await WriteFormatOutputAsync(
                result,
                "json",
                settings.JsonOutput,
                outputOptions,
                stdoutState,
                settings.Quiet,
                cancellationToken);
        }

        // Text output (or default console output if no format flags)
        if (settings.TextOutput is not null)
        {
            // For text output, use appropriate formatter based on TTY detection
            var formatName = DetermineTextFormat(settings);
            await WriteFormatOutputAsync(
                result,
                formatName,
                settings.TextOutput,
                outputOptions,
                stdoutState,
                settings.Quiet,
                cancellationToken);
        }

        // Git output (unified diff format)
        if (settings.GitOutput is not null)
        {
            await WriteFormatOutputAsync(
                result,
                "text", // UnifiedFormatter uses "text" as its format name
                settings.GitOutput,
                outputOptions,
                stdoutState,
                settings.Quiet,
                cancellationToken);
        }

        // HTML output (always to file, never stdout)
        if (settings.HtmlOutput is not null)
        {
            // HTML requires a file path
            if (string.IsNullOrEmpty(settings.HtmlOutput))
            {
                throw new InvalidOperationException("HTML output requires a file path (cannot write to stdout).");
            }

            // Create HTML-specific options with mode, CSS path, and view mode
            var htmlOptions = outputOptions with
            {
                HtmlMode = settings.HtmlMode.ToLowerInvariant() == "fragment"
                    ? Output.HtmlMode.Fragment
                    : Output.HtmlMode.Document,
                ExtractCssPath = settings.ExtractCss,
                HtmlOutputPath = settings.HtmlOutput,
                ViewMode = settings.ViewMode?.ToLowerInvariant() == "inline"
                    ? Output.ViewMode.Inline
                    : Output.ViewMode.Tree,
                InlineContext = settings.InlineContext
            };

            await WriteFormatOutputAsync(
                result,
                "html",
                settings.HtmlOutput,
                htmlOptions,
                stdoutState,
                settings.Quiet,
                cancellationToken);

            // Open in browser if requested
            if (settings.OpenInBrowser && !string.IsNullOrEmpty(settings.HtmlOutput))
            {
                OpenInBrowser(settings.HtmlOutput);
            }
        }

        // Default output if no format flags specified and not in quiet mode
        if (!settings.Quiet &&
            settings.JsonOutput is null &&
            settings.TextOutput is null &&
            settings.GitOutput is null &&
            settings.HtmlOutput is null)
        {
            await WriteDefaultOutputAsync(result, settings, outputOptions, cancellationToken);
        }

        // Return exit code based on diff result
        return result.Stats.TotalChanges == 0 ? ExitCodeNoDiff : ExitCodeDiffFound;
    }

    /// <summary>
    /// Determines if the current environment is an interactive terminal.
    /// </summary>
    /// <returns>True if running in an interactive terminal; otherwise, false.</returns>
    public static bool IsInteractiveTerminal() =>
        !Console.IsOutputRedirected &&
        !Console.IsErrorRedirected &&
        Environment.UserInteractive;

    /// <summary>
    /// Helper class to track stdout usage state in async methods.
    /// </summary>
    private sealed class StdoutState
    {
        public bool IsClaimed { get; set; }
    }

    private async Task WriteFormatOutputAsync(
        DiffResult result,
        string formatName,
        string outputPath,
        OutputOptions outputOptions,
        StdoutState stdoutState,
        bool quiet,
        CancellationToken cancellationToken)
    {
        var formatter = _formatterFactory.GetFormatter(formatName);

        if (string.IsNullOrEmpty(outputPath))
        {
            // Write to stdout
            if (stdoutState.IsClaimed)
            {
                // Skip this output - stdout already used by another format
                // In the future, we could throw or warn here
                return;
            }

            stdoutState.IsClaimed = true;
            await formatter.FormatResultAsync(result, Console.Out, outputOptions);
        }
        else
        {
            // Write to file
            await using var fileStream = new FileStream(
                outputPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);
            await using var writer = new StreamWriter(fileStream);

            await formatter.FormatResultAsync(result, writer, outputOptions);

            if (!quiet)
            {
                await Console.Error.WriteLineAsync($"Output written to: {outputPath}");
            }
        }
    }

    private async Task WriteDefaultOutputAsync(
        DiffResult result,
        OutputSettings settings,
        OutputOptions outputOptions,
        CancellationToken cancellationToken)
    {
        // Default behavior: TTY gets colored terminal output, piped gets plain text
        var formatName = IsInteractiveTerminal() && !settings.NoColor
            ? "terminal"
            : "plain";

        var formatter = _formatterFactory.GetFormatter(formatName);
        await formatter.FormatResultAsync(result, Console.Out, outputOptions);
    }

    private static OutputOptions CreateOutputOptions(OutputSettings settings)
    {
        var useColor = !settings.NoColor && IsInteractiveTerminal();

        return new OutputOptions
        {
            UseColor = useColor,
            PrettyPrint = true,
            IncludeStats = true,
            Compact = false,
            IncludeNonImpactful = settings.IncludeNonImpactful || settings.IncludeFormatting,
            AvailableEditors = EditorDetector.DetectAvailableEditors()
        };
    }

    private string DetermineTextFormat(OutputSettings settings)
    {
        // If writing to a file, use plain text
        if (!string.IsNullOrEmpty(settings.TextOutput))
        {
            return "plain";
        }

        // If writing to stdout, check TTY status
        if (IsInteractiveTerminal() && !settings.NoColor)
        {
            return "terminal"; // Rich colored output
        }

        return "plain"; // Plain text for piped/redirected output
    }

    /// <summary>
    /// Opens the specified file in the default browser.
    /// </summary>
    /// <param name="filePath">The path to the file to open.</param>
    private static void OpenInBrowser(string filePath)
    {
        // Skip browser opening if running in test mode
        var disableBrowserOpen = Environment.GetEnvironmentVariable("ROSLYN_DIFF_DISABLE_BROWSER_OPEN");
        if (!string.IsNullOrEmpty(disableBrowserOpen) && disableBrowserOpen.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Info: Browser opening disabled (ROSLYN_DIFF_DISABLE_BROWSER_OPEN=true)");
            return;
        }

        // Validate that we have a valid file path
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.Error.WriteLine("Warning: Could not open browser: File path is empty or null.");
            return;
        }

        // Get absolute path with exception handling
        string absolutePath;
        try
        {
            absolutePath = Path.GetFullPath(filePath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not open browser: Invalid file path: {ex.Message}");
            return;
        }

        // Check if the file actually exists before attempting to open
        if (!File.Exists(absolutePath))
        {
            Console.Error.WriteLine($"Warning: Could not open browser: File does not exist: {absolutePath}");
            return;
        }

        var url = new Uri(absolutePath).AbsoluteUri;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: use start command
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c start \"\" \"{url}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: use open command
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = url,
                    UseShellExecute = false
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: use xdg-open
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = url,
                    UseShellExecute = false
                });
            }
        }
        catch (Exception ex)
        {
            // Log warning but don't fail the command
            Console.Error.WriteLine($"Warning: Could not open browser: {ex.Message}");
        }
    }
}

/// <summary>
/// Settings controlling output generation behavior.
/// </summary>
public class OutputSettings
{
    /// <summary>
    /// Gets or sets the JSON output destination.
    /// null = skip, "" = stdout, "path" = write to file.
    /// </summary>
    public string? JsonOutput { get; init; }

    /// <summary>
    /// Gets or sets the HTML output file path.
    /// null = skip, "path" = write to file.
    /// HTML output always requires a file path (cannot write to stdout).
    /// </summary>
    public string? HtmlOutput { get; init; }

    /// <summary>
    /// Gets or sets the text output destination.
    /// null = skip, "" = stdout, "path" = write to file.
    /// </summary>
    public string? TextOutput { get; init; }

    /// <summary>
    /// Gets or sets the git-style unified diff output destination.
    /// null = skip, "" = stdout, "path" = write to file.
    /// </summary>
    public string? GitOutput { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to open HTML output in the browser.
    /// </summary>
    public bool OpenInBrowser { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to suppress informational messages.
    /// When true, only the requested output is written (no "Output written to:" messages).
    /// </summary>
    public bool Quiet { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to disable colored output.
    /// When true, plain text output is used even in interactive terminals.
    /// </summary>
    public bool NoColor { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to include non-impactful changes in output.
    /// </summary>
    /// <remarks>
    /// Non-impactful changes include formatting-only and non-breaking changes.
    /// </remarks>
    public bool IncludeNonImpactful { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to include formatting-only changes.
    /// </summary>
    public bool IncludeFormatting { get; init; }

    /// <summary>
    /// Gets or sets the minimum impact level to include in output.
    /// </summary>
    /// <remarks>
    /// Only changes at or above this impact level are included.
    /// </remarks>
    public ChangeImpact MinimumImpactLevel { get; init; } = ChangeImpact.FormattingOnly;

    /// <summary>
    /// Gets or sets the HTML generation mode.
    /// </summary>
    public string HtmlMode { get; init; } = "document";

    /// <summary>
    /// Gets or sets the CSS filename for fragment mode.
    /// </summary>
    public string ExtractCss { get; init; } = "roslyn-diff.css";

    /// <summary>
    /// Gets or sets the view mode for HTML output.
    /// null = default (tree), "inline" = inline view.
    /// </summary>
    public string? ViewMode { get; init; }

    /// <summary>
    /// Gets or sets the inline context lines.
    /// null = full file, N = N lines of context.
    /// </summary>
    public int? InlineContext { get; init; }
}
