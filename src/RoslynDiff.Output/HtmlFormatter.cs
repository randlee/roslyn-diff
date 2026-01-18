namespace RoslynDiff.Output;

using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using RoslynDiff.Core.Models;

/// <summary>
/// Formats diff results as self-contained HTML with side-by-side diff view.
/// </summary>
public partial class HtmlFormatter : IOutputFormatter
{
    /// <inheritdoc/>
    public string Format => "html";

    /// <inheritdoc/>
    public string ContentType => "text/html";

    /// <inheritdoc/>
    public string FormatResult(DiffResult result, OutputOptions? options = null)
    {
        options ??= new OutputOptions();
        var sb = new StringBuilder();

        AppendHtmlHeader(sb, result);
        AppendSummarySection(sb, result, options);
        AppendDiffContent(sb, result, options);
        AppendHtmlFooter(sb);

        return sb.ToString();
    }

    /// <inheritdoc/>
    public async Task FormatResultAsync(DiffResult result, TextWriter writer, OutputOptions? options = null)
    {
        var html = FormatResult(result, options);
        await writer.WriteAsync(html);
    }

    private static void AppendHtmlHeader(StringBuilder sb, DiffResult result)
    {
        var title = FormatTitle(result);

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"    <title>{HtmlEncode(title)}</title>");
        AppendStyles(sb);
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
    }

    private static string FormatTitle(DiffResult result)
    {
        if (result.OldPath is not null && result.NewPath is not null)
        {
            return $"Diff: {Path.GetFileName(result.OldPath)} \u2192 {Path.GetFileName(result.NewPath)}";
        }

        if (result.OldPath is not null)
        {
            return $"Diff: {Path.GetFileName(result.OldPath)}";
        }

        if (result.NewPath is not null)
        {
            return $"Diff: {Path.GetFileName(result.NewPath)}";
        }

        return "Diff Report";
    }

    private static void AppendStyles(StringBuilder sb)
    {
        sb.AppendLine("    <style>");
        sb.AppendLine(@"        :root {
            --color-added-bg: #e6ffec;
            --color-added-border: #2da44e;
            --color-removed-bg: #ffebe9;
            --color-removed-border: #cf222e;
            --color-modified-bg: #fff3cd;
            --color-modified-border: #bf8700;
            --color-moved-bg: #ddf4ff;
            --color-moved-border: #218bff;
            --color-renamed-bg: #f5e8ff;
            --color-renamed-border: #8250df;
            --color-line-number: #57606a;
            --color-border: #d0d7de;
            --color-header-bg: #f6f8fa;
            --color-code-bg: #f6f8fa;
            --font-mono: ui-monospace, SFMono-Regular, SF Mono, Menlo, Consolas, Liberation Mono, monospace;
        }

        * {
            box-sizing: border-box;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif;
            line-height: 1.5;
            color: #24292f;
            margin: 0;
            padding: 20px;
            background-color: #ffffff;
        }

        header {
            margin-bottom: 24px;
            padding-bottom: 16px;
            border-bottom: 1px solid var(--color-border);
        }

        h1 {
            font-size: 24px;
            font-weight: 600;
            margin: 0;
        }

        .header-title-row {
            display: flex;
            align-items: baseline;
            gap: 16px;
            margin-bottom: 16px;
            flex-wrap: wrap;
            justify-content: space-between;
        }

        .header-title-left {
            display: flex;
            align-items: baseline;
            gap: 16px;
            flex-wrap: wrap;
        }

        .header-timestamp {
            font-size: 13px;
            color: #57606a;
            white-space: nowrap;
        }

        .header-stats {
            display: flex;
            align-items: center;
            gap: 8px;
            font-size: 13px;
            color: #57606a;
        }

        .header-stats .stat-inline {
            font-weight: 400;
        }

        .header-stats .stat-separator {
            color: #d0d7de;
        }

        h2 {
            font-size: 16px;
            font-weight: 600;
            margin: 0;
            padding: 12px 16px;
            background-color: var(--color-header-bg);
            border: 1px solid var(--color-border);
            border-radius: 6px 6px 0 0;
        }

        .summary {
            display: flex;
            gap: 16px;
            flex-wrap: wrap;
        }

        .stat {
            display: inline-flex;
            align-items: center;
            padding: 4px 12px;
            border-radius: 20px;
            font-size: 14px;
            font-weight: 500;
        }

        .stat-additions {
            background-color: var(--color-added-bg);
            color: var(--color-added-border);
        }

        .stat-deletions {
            background-color: var(--color-removed-bg);
            color: var(--color-removed-border);
        }

        .stat-modifications {
            background-color: var(--color-modified-bg);
            color: var(--color-modified-border);
        }

        .stat-moves {
            background-color: var(--color-moved-bg);
            color: var(--color-moved-border);
        }

        .stat-renames {
            background-color: var(--color-renamed-bg);
            color: var(--color-renamed-border);
        }

        .stat-total {
            background-color: var(--color-header-bg);
            color: #57606a;
        }

        .stat-mode {
            background-color: var(--color-header-bg);
            color: #57606a;
            border: 1px solid var(--color-border);
        }

        .diff-mode {
            margin-top: 8px;
            font-size: 12px;
            color: #57606a;
        }

        main {
            display: flex;
            flex-direction: column;
            gap: 16px;
        }

        .file-diff {
            border-radius: 6px;
            overflow: hidden;
        }

        .file-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
        }

        .diff-container {
            display: flex;
            border: 1px solid var(--color-border);
            border-top: none;
            border-radius: 0 0 6px 6px;
            overflow: hidden;
        }

        .diff-side {
            flex: 1;
            min-width: 0;
            overflow-x: auto;
        }

        .diff-old {
            border-right: 1px solid var(--color-border);
        }

        .diff-side-header {
            padding: 4px 12px;
            background-color: var(--color-header-bg);
            border-bottom: 1px solid var(--color-border);
            font-size: 11px;
            font-weight: 500;
            color: #57606a;
        }

        .diff-old .diff-side-header {
            background-color: rgba(255, 235, 233, 0.5);
        }

        .diff-new .diff-side-header {
            background-color: rgba(230, 255, 236, 0.5);
        }

        .diff-content {
            font-family: var(--font-mono);
            font-size: 12px;
            overflow-x: auto;
        }

        .diff-line {
            display: flex;
            line-height: 1.2;
        }

        .line-number {
            flex-shrink: 0;
            width: 40px;
            padding: 0 6px;
            text-align: right;
            color: var(--color-line-number);
            background-color: var(--color-code-bg);
            border-right: 1px solid var(--color-border);
            user-select: none;
            white-space: pre;
        }

        .line-content {
            flex: 1;
            padding: 0 6px;
            white-space: pre;
            overflow-x: auto;
        }

        .line-added {
            background-color: var(--color-added-bg);
        }

        .line-added .line-number {
            background-color: rgba(46, 160, 67, 0.2);
        }

        .line-removed {
            background-color: var(--color-removed-bg);
        }

        .line-removed .line-number {
            background-color: rgba(207, 34, 46, 0.2);
        }

        .line-modified {
            background-color: var(--color-modified-bg);
        }

        .line-modified .line-number {
            background-color: rgba(191, 135, 0, 0.2);
        }

        .change-section {
            margin-bottom: 8px;
            border: 1px solid var(--color-border);
            border-radius: 6px;
            overflow: hidden;
        }

        .change-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 6px 12px;
            background-color: var(--color-header-bg);
            border-bottom: 1px solid var(--color-border);
            cursor: pointer;
        }

        .change-header:hover {
            background-color: #f3f4f6;
        }

        .change-title {
            display: flex;
            align-items: center;
            gap: 8px;
        }

        .change-badge {
            padding: 2px 8px;
            border-radius: 12px;
            font-size: 11px;
            font-weight: 500;
            text-transform: uppercase;
        }

        .badge-added {
            background-color: var(--color-added-bg);
            color: var(--color-added-border);
        }

        .badge-removed {
            background-color: var(--color-removed-bg);
            color: var(--color-removed-border);
        }

        .badge-modified {
            background-color: var(--color-modified-bg);
            color: var(--color-modified-border);
        }

        .badge-moved {
            background-color: var(--color-moved-bg);
            color: var(--color-moved-border);
        }

        .badge-renamed {
            background-color: var(--color-renamed-bg);
            color: var(--color-renamed-border);
        }

        .change-kind {
            font-size: 12px;
            color: #57606a;
        }

        .change-name {
            font-family: var(--font-mono);
            font-size: 13px;
        }

        .change-location {
            font-size: 11px;
            color: #57606a;
        }

        .change-body {
            display: block;
        }

        .change-body.collapsed {
            display: none;
        }

        .expand-icon {
            font-size: 10px;
            color: #57606a;
            transition: transform 0.2s;
        }

        .change-header.collapsed .expand-icon {
            transform: rotate(-90deg);
        }

        /* Top diff section header styling */
        .top-diff-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 8px 12px;
            background-color: var(--color-header-bg);
            border: 1px solid var(--color-border);
            border-bottom: none;
            cursor: pointer;
        }

        .top-diff-header:hover {
            background-color: #f3f4f6;
        }

        .top-diff-header .expand-icon {
            font-size: 12px;
            margin-right: 8px;
        }

        .top-diff-header.collapsed .expand-icon {
            transform: rotate(-90deg);
        }

        .top-diff-header-left {
            display: flex;
            align-items: center;
            gap: 12px;
        }

        .top-diff-header-stats {
            display: flex;
            align-items: center;
            gap: 8px;
            font-size: 12px;
        }

        .top-diff-header-stats .stat-inline {
            color: #57606a;
        }

        .top-diff-header-stats .stat-added {
            color: var(--color-added-border);
            font-weight: 500;
        }

        .top-diff-header-stats .stat-removed {
            color: var(--color-removed-border);
            font-weight: 500;
        }

        .top-diff-header-stats .stat-mode {
            color: #57606a;
            padding-left: 8px;
            border-left: 1px solid var(--color-border);
        }

        .top-diff-header-buttons {
            display: flex;
            gap: 4px;
        }

        .top-diff-content.collapsed {
            display: none;
        }

        .no-changes {
            padding: 40px;
            text-align: center;
            color: #57606a;
            background-color: var(--color-header-bg);
            border-radius: 6px;
        }

        /* Syntax highlighting */
        .keyword { color: #cf222e; font-weight: 500; }
        .string { color: #0a3069; }
        .comment { color: #6e7781; font-style: italic; }
        .type { color: #8250df; }
        .number { color: #0550ae; }
        .preprocessor { color: #6e7781; }

        /* Copy buttons */
        .copy-buttons {
            display: flex;
            gap: 4px;
            margin-left: auto;
        }

        .copy-btn {
            background: none;
            border: 1px solid var(--color-border);
            border-radius: 4px;
            padding: 2px 8px;
            font-size: 11px;
            color: #57606a;
            cursor: pointer;
            transition: all 0.15s;
        }

        .copy-btn:hover {
            background-color: var(--color-header-bg);
            color: #24292f;
        }

        .copy-btn.copied {
            background-color: var(--color-added-bg);
            border-color: var(--color-added-border);
            color: var(--color-added-border);
        }

        /* File info section */
        .file-info {
            margin: 0;
            padding: 0;
        }

        .file-row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
            padding: 8px 12px;
            background-color: var(--color-header-bg);
            border: 1px solid var(--color-border);
            border-radius: 6px;
            margin-bottom: 6px;
        }

        .file-row:last-child {
            margin-bottom: 0;
        }

        .file-row-left {
            display: flex;
            align-items: center;
            gap: 8px;
            min-width: 0;
            flex: 1;
        }

        .file-path-label {
            font-size: 12px;
            font-weight: 500;
            color: #57606a;
            white-space: nowrap;
        }

        .file-path {
            font-family: var(--font-mono);
            font-size: 12px;
            color: #24292f;
            word-break: break-all;
            min-width: 0;
        }

        .file-actions {
            display: flex;
            gap: 4px;
            flex-shrink: 0;
        }

        .action-btn {
            position: relative;
            display: inline-flex;
            align-items: center;
            gap: 4px;
            padding: 4px 10px;
            font-size: 11px;
            border: 1px solid var(--color-border);
            border-radius: 4px;
            background: white;
            color: #24292f;
            cursor: pointer;
            text-decoration: none;
            transition: all 0.15s;
        }

        .action-btn:hover {
            background-color: var(--color-header-bg);
            border-color: #57606a;
        }

        .action-btn svg {
            width: 14px;
            height: 14px;
        }

        .action-btn.icon-only {
            padding: 6px;
            gap: 0;
        }

        .action-btn.icon-only svg {
            width: 16px;
            height: 16px;
        }

        /* Notification popup */
        .notification {
            position: absolute;
            top: calc(100% + 4px);
            left: 0;
            padding: 6px 10px;
            font-size: 11px;
            background: #1f2328;
            color: white;
            border-radius: 4px;
            white-space: nowrap;
            z-index: 1000;
            opacity: 0;
            transform: translateY(-4px);
            transition: opacity 0.15s, transform 0.15s;
            pointer-events: none;
        }

        .notification.show {
            opacity: 1;
            transform: translateY(0);
        }

        /* Navigation */
        .nav-container {
            position: fixed;
            bottom: 20px;
            right: 20px;
            display: flex;
            flex-direction: column;
            gap: 8px;
            z-index: 1000;
        }

        .nav-btn {
            width: 40px;
            height: 40px;
            border-radius: 50%;
            border: 1px solid var(--color-border);
            background-color: white;
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: center;
            box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
            font-size: 14px;
        }

        .nav-btn:hover {
            background-color: var(--color-header-bg);
        }

        /* Custom CSS tooltips for buttons - more reliable than native title attribute */
        .action-btn[title],
        .copy-btn[title],
        .nav-btn[title] {
            position: relative;
        }

        .action-btn[title]::after,
        .copy-btn[title]::after,
        .nav-btn[title]::after {
            content: attr(title);
            position: absolute;
            bottom: calc(100% + 8px);
            left: 50%;
            transform: translateX(-50%);
            padding: 6px 10px;
            background: #1f2328;
            color: white;
            font-size: 11px;
            font-weight: normal;
            white-space: nowrap;
            border-radius: 4px;
            z-index: 10000;
            pointer-events: none;
            opacity: 0;
            visibility: hidden;
            transition: opacity 0.15s ease-out, visibility 0.15s ease-out;
        }

        .action-btn[title]::before,
        .copy-btn[title]::before,
        .nav-btn[title]::before {
            content: '';
            position: absolute;
            bottom: calc(100% + 4px);
            left: 50%;
            transform: translateX(-50%);
            border: 5px solid transparent;
            border-top-color: #1f2328;
            z-index: 10000;
            pointer-events: none;
            opacity: 0;
            visibility: hidden;
            transition: opacity 0.15s ease-out, visibility 0.15s ease-out;
        }

        .action-btn[title]:hover::after,
        .copy-btn[title]:hover::after,
        .nav-btn[title]:hover::after,
        .action-btn[title]:hover::before,
        .copy-btn[title]:hover::before,
        .nav-btn[title]:hover::before {
            opacity: 1;
            visibility: visible;
        }

        /* Position nav button tooltips to the left (since they're at the right edge) */
        .nav-btn[title]::after {
            bottom: 50%;
            top: auto;
            right: calc(100% + 8px);
            left: auto;
            transform: translateY(50%);
        }

        .nav-btn[title]::before {
            bottom: 50%;
            top: auto;
            right: calc(100% + 3px);
            left: auto;
            transform: translateY(50%);
            border-top-color: transparent;
            border-left-color: #1f2328;
        }

        /* Impact indicator badges */
        .impact-indicator {
            font-size: 10px;
            padding: 1px 6px;
            border-radius: 3px;
            margin-left: 8px;
        }

        .impact-breaking-public {
            background-color: #fee2e2;
            color: #dc2626;
        }

        .impact-breaking-internal {
            background-color: #fef3c7;
            color: #d97706;
        }

        .impact-nonbreaking {
            background-color: #f3f4f6;
            color: #6b7280;
        }

        .impact-formatting {
            background-color: #f9fafb;
            color: #9ca3af;
        }

        /* Caveat warnings */
        .change-caveats {
            font-size: 11px;
            color: #b45309;
            background-color: #fffbeb;
            padding: 4px 8px;
            border-radius: 4px;
            margin-top: 4px;
        }

        /* Muted styling for non-breaking changes */
        .change-nonbreaking {
            opacity: 0.7;
        }

        /* Print styles */
        @media print {
            body {
                padding: 0;
                font-size: 10px;
            }

            .nav-container {
                display: none;
            }

            .file-diff {
                page-break-inside: avoid;
            }

            .change-section {
                page-break-inside: avoid;
            }

            .diff-container {
                border: 1px solid #000;
            }

            .change-body.collapsed {
                display: block !important;
            }
        }

        /* Responsive */
        @media (max-width: 768px) {
            body {
                padding: 10px;
            }

            .diff-container {
                flex-direction: column;
            }

            .diff-old {
                border-right: none;
                border-bottom: 1px solid var(--color-border);
            }

            h1 {
                font-size: 18px;
            }

            .summary {
                gap: 8px;
            }

            .stat {
                font-size: 12px;
                padding: 2px 8px;
            }
        }");
        sb.AppendLine("    </style>");
    }

    private static void AppendSummarySection(StringBuilder sb, DiffResult result, OutputOptions options)
    {
        var stats = result.Stats;

        sb.AppendLine("    <header>");

        // Title row with inline stats (left) and timestamp (right)
        sb.AppendLine("        <div class=\"header-title-row\">");
        sb.AppendLine("            <div class=\"header-title-left\">");
        sb.AppendLine("                <h1>Diff Report</h1>");

        if (options.IncludeStats)
        {
            sb.AppendLine("                <div class=\"header-stats\">");

            // Total changes
            sb.AppendLine($"                    <span class=\"stat-inline\">{stats.TotalChanges} changes</span>");

            // Additions
            if (stats.Additions > 0)
            {
                sb.AppendLine("                    <span class=\"stat-separator\">|</span>");
                sb.AppendLine($"                    <span class=\"stat-inline\" style=\"color: var(--color-added-border);\">+{stats.Additions} added</span>");
            }

            // Deletions
            if (stats.Deletions > 0)
            {
                sb.AppendLine("                    <span class=\"stat-separator\">|</span>");
                sb.AppendLine($"                    <span class=\"stat-inline\" style=\"color: var(--color-removed-border);\">-{stats.Deletions} deleted</span>");
            }

            // Modifications
            if (stats.Modifications > 0)
            {
                sb.AppendLine("                    <span class=\"stat-separator\">|</span>");
                sb.AppendLine($"                    <span class=\"stat-inline\" style=\"color: var(--color-modified-border);\">~{stats.Modifications} modified</span>");
            }

            // Moves
            if (stats.Moves > 0)
            {
                sb.AppendLine("                    <span class=\"stat-separator\">|</span>");
                sb.AppendLine($"                    <span class=\"stat-inline\" style=\"color: var(--color-moved-border);\">\u21c4{stats.Moves} moved</span>");
            }

            // Renames
            if (stats.Renames > 0)
            {
                sb.AppendLine("                    <span class=\"stat-separator\">|</span>");
                sb.AppendLine($"                    <span class=\"stat-inline\" style=\"color: var(--color-renamed-border);\">\u270e{stats.Renames} renamed</span>");
            }

            // Mode indicator
            var modeText = result.Mode == DiffMode.Roslyn ? "Roslyn Semantic" : "Line-by-Line";
            sb.AppendLine("                    <span class=\"stat-separator\">|</span>");
            sb.AppendLine($"                    <span class=\"stat-inline\">Mode: {modeText}</span>");

            sb.AppendLine("                </div>");
        }

        sb.AppendLine("            </div>");

        // Timestamp (right-aligned)
        var timestamp = DateTime.Now.ToString("MMM d, yyyy h:mm tt");
        sb.AppendLine($"            <span class=\"header-timestamp\">{HtmlEncode(timestamp)}</span>");

        sb.AppendLine("        </div>");

        // Add file paths section
        AppendFilePaths(sb, result, options);

        sb.AppendLine("    </header>");
    }

    private static void AppendFilePaths(StringBuilder sb, DiffResult result, OutputOptions options)
    {
        // Show file paths if available
        if (result.OldPath == null && result.NewPath == null)
        {
            return;
        }

        sb.AppendLine("        <div class=\"file-info\">");

        if (result.OldPath != null)
        {
            sb.AppendLine("            <div class=\"file-row\">");
            sb.AppendLine("                <div class=\"file-row-left\">");
            sb.AppendLine("                    <span class=\"file-path-label\">Old file:</span>");
            sb.AppendLine($"                    <span class=\"file-path\" id=\"old-path\" data-path=\"{HtmlEncode(result.OldPath)}\">{HtmlEncode(result.OldPath)}</span>");
            sb.AppendLine("                </div>");
            AppendFileActions(sb, result.OldPath, "old", options.AvailableEditors);
            sb.AppendLine("            </div>");
        }

        if (result.NewPath != null)
        {
            sb.AppendLine("            <div class=\"file-row\">");
            sb.AppendLine("                <div class=\"file-row-left\">");
            sb.AppendLine("                    <span class=\"file-path-label\">New file:</span>");
            sb.AppendLine($"                    <span class=\"file-path\" id=\"new-path\" data-path=\"{HtmlEncode(result.NewPath)}\">{HtmlEncode(result.NewPath)}</span>");
            sb.AppendLine("                </div>");
            AppendFileActions(sb, result.NewPath, "new", options.AvailableEditors);
            sb.AppendLine("            </div>");
        }

        sb.AppendLine("        </div>");
    }

    private static void AppendFileActions(StringBuilder sb, string filePath, string prefix, IReadOnlyList<string> availableEditors)
    {
        var directory = Path.GetDirectoryName(filePath) ?? "";
        var extension = Path.GetExtension(filePath)?.ToLowerInvariant() ?? "";

        // Check if this is a code file that editors can open
        var isCodeFile = extension is ".cs" or ".vb" or ".fs" or ".js" or ".ts" or ".jsx" or ".tsx"
            or ".py" or ".rb" or ".go" or ".rs" or ".java" or ".kt" or ".swift" or ".c" or ".cpp"
            or ".h" or ".hpp" or ".json" or ".xml" or ".yaml" or ".yml" or ".md" or ".txt" or ".html"
            or ".css" or ".scss" or ".less" or ".sql" or ".sh" or ".ps1" or ".bat" or ".cmd";

        sb.AppendLine("                <div class=\"file-actions\">");

        // Copy file path button (clipboard icon)
        sb.AppendLine($"                    <button class=\"action-btn icon-only\" onclick=\"copyPathWithNotification(this, '{HtmlEncodeJs(filePath)}')\" title=\"Copy full path to clipboard\">");
        sb.AppendLine("                        <svg viewBox=\"0 0 16 16\" fill=\"currentColor\"><path d=\"M0 6.75C0 5.784.784 5 1.75 5h1.5a.75.75 0 010 1.5h-1.5a.25.25 0 00-.25.25v7.5c0 .138.112.25.25.25h7.5a.25.25 0 00.25-.25v-1.5a.75.75 0 011.5 0v1.5A1.75 1.75 0 019.25 16h-7.5A1.75 1.75 0 010 14.25v-7.5z\"/><path d=\"M5 1.75C5 .784 5.784 0 6.75 0h7.5C15.216 0 16 .784 16 1.75v7.5A1.75 1.75 0 0114.25 11h-7.5A1.75 1.75 0 015 9.25v-7.5zm1.75-.25a.25.25 0 00-.25.25v7.5c0 .138.112.25.25.25h7.5a.25.25 0 00.25-.25v-7.5a.25.25 0 00-.25-.25h-7.5z\"/></svg>");
        sb.AppendLine("                    </button>");

        // Copy folder path button (folder icon)
        sb.AppendLine($"                    <button class=\"action-btn icon-only\" onclick=\"copyPathWithNotification(this, '{HtmlEncodeJs(directory)}')\" title=\"Copy folder path to clipboard\">");
        sb.AppendLine("                        <svg viewBox=\"0 0 16 16\" fill=\"currentColor\"><path d=\"M1.75 2h5.5c.551 0 1.064.278 1.365.737l.43.645A.25.25 0 009.25 3.5h5A1.75 1.75 0 0116 5.25v8A1.75 1.75 0 0114.25 15H1.75A1.75 1.75 0 010 13.25V3.75C0 2.784.784 2 1.75 2zm0 1.5a.25.25 0 00-.25.25v9.5c0 .138.112.25.25.25h12.5a.25.25 0 00.25-.25v-8a.25.25 0 00-.25-.25H9.25a1.75 1.75 0 01-1.458-.79l-.429-.644a.25.25 0 00-.208-.11z\"/></svg>");
        sb.AppendLine("                    </button>");

        if (isCodeFile)
        {
            // VS Code button (only if available)
            if (availableEditors.Contains("vscode"))
            {
                sb.AppendLine($"                    <button class=\"action-btn editor-btn icon-only\" data-editor=\"vscode\" onclick=\"openInEditor('{HtmlEncodeJs(filePath)}', 'vscode')\" title=\"Open file in VS Code\">");
                sb.AppendLine("                        <svg viewBox=\"0 0 16 16\" fill=\"currentColor\"><path d=\"M14.25 1a.74.74 0 00-.218.033L10.7 2.252l-4.5 4-3.5-2.8a.75.75 0 00-.946.086l-.75.75a.75.75 0 00.033 1.09L4.5 8l-3.463 2.622a.75.75 0 00-.033 1.09l.75.75a.75.75 0 00.946.086l3.5-2.8 4.5 4 3.332 1.22A.74.74 0 0015 14.25V1.75a.75.75 0 00-.75-.75zM11 12.12L7.28 8 11 3.88v8.24z\"/></svg>");
                sb.AppendLine("                    </button>");
            }

            // Rider button (only if available)
            if (availableEditors.Contains("rider"))
            {
                sb.AppendLine($"                    <button class=\"action-btn editor-btn icon-only\" data-editor=\"rider\" onclick=\"openInEditor('{HtmlEncodeJs(filePath)}', 'rider')\" title=\"Open file in Rider\">");
                sb.AppendLine("                        <svg viewBox=\"0 0 16 16\" fill=\"currentColor\"><path d=\"M0 0v16h16V0H0zm13.2 2.8c.4.5.6 1 .6 1.7 0 .5-.1.9-.3 1.3-.2.4-.6.7-1 .9l1.6 2.5h-2.2l-1.4-2.2H8.8v2.2H7V2.1h3.7c.6 0 1.1.1 1.5.3.4.2.8.3 1 .4zM7 12h6v1.2H7V12z\"/><path d=\"M8.8 3.6v2h1.3c.3 0 .6-.1.8-.3.2-.2.3-.4.3-.7s-.1-.5-.3-.7c-.2-.2-.5-.3-.8-.3H8.8z\"/></svg>");
                sb.AppendLine("                    </button>");
            }

            // PyCharm button (only if available)
            if (availableEditors.Contains("pycharm"))
            {
                sb.AppendLine($"                    <button class=\"action-btn editor-btn icon-only\" data-editor=\"pycharm\" onclick=\"openInEditor('{HtmlEncodeJs(filePath)}', 'pycharm')\" title=\"Open file in PyCharm\">");
                sb.AppendLine("                        <svg viewBox=\"0 0 16 16\" fill=\"currentColor\"><path d=\"M0 0v16h16V0H0zm2 13h5v1H2v-1zm6.3-5.5c-.1.4-.4.7-.8.9l1.2 1.8h-1.5l-1-1.5H5v1.5H3.5V4.5h2.8c.5 0 .9.1 1.2.3.3.2.5.4.7.7.1.3.2.6.2 1 0 .4-.1.7-.1 1zm-.9-1c0-.2-.1-.4-.2-.5-.1-.2-.3-.2-.5-.2H5v1.5h1.7c.2 0 .4-.1.5-.2.1-.2.2-.4.2-.6z\"/></svg>");
                sb.AppendLine("                    </button>");
            }

            // Zed button (only if available)
            if (availableEditors.Contains("zed"))
            {
                sb.AppendLine($"                    <button class=\"action-btn editor-btn icon-only\" data-editor=\"zed\" onclick=\"openInEditor('{HtmlEncodeJs(filePath)}', 'zed')\" title=\"Open file in Zed\">");
                sb.AppendLine("                        <svg viewBox=\"0 0 16 16\" fill=\"currentColor\"><path d=\"M2.5 1L13 1L15 3.5L15 14.5L13 16L3 16L1 13.5L1 2.5L2.5 1ZM3.5 3L3 3.5L3 12.5L4 14L12 14L13 12.5L13 4L12 3L3.5 3ZM5 5L11 5L11 6.5L7.5 11L11 11L11 13L5 13L5 11.5L8.5 7L5 7L5 5Z\"/></svg>");
                sb.AppendLine("                    </button>");
            }
        }

        sb.AppendLine("                </div>");
    }

    private static string HtmlEncodeJs(string text)
    {
        // Encode for use in JavaScript strings within HTML attributes
        return HtmlEncode(text.Replace("\\", "\\\\").Replace("'", "\\'"));
    }

    private static void AppendDiffContent(StringBuilder sb, DiffResult result, OutputOptions options)
    {
        sb.AppendLine("    <main>");

        if (result.FileChanges.Count == 0)
        {
            sb.AppendLine("        <div class=\"no-changes\">No changes detected.</div>");
        }
        else
        {
            foreach (var fileChange in result.FileChanges)
            {
                AppendFileChange(sb, fileChange, result, options);
            }
        }

        sb.AppendLine("    </main>");

        // Navigation buttons
        AppendNavigation(sb);
    }

    private static void AppendFileChange(StringBuilder sb, FileChange fileChange, DiffResult result, OutputOptions options)
    {
        var fileName = fileChange.Path ?? "Unknown file";
        var displayName = Path.GetFileName(fileName);
        var fileId = GenerateFileId(fileName);

        // Prepare file-level JSON and diff data
        var fileJson = GenerateFileJson(fileChange);
        var fileDiff = GenerateFileDiff(fileChange, result);

        sb.AppendLine($"        <section class=\"file-diff\" id=\"{fileId}\">");
        sb.AppendLine($"            <div class=\"file-header\">");
        sb.AppendLine($"                <h2>{HtmlEncode(displayName)}</h2>");
        sb.AppendLine("            </div>");

        // Top diff header with stats and copy buttons
        var topDiffId = $"{fileId}-top-diff";
        var stats = result.Stats;
        var modeText = result.Mode == DiffMode.Roslyn ? "Roslyn Semantic" : "Line-by-Line";

        sb.AppendLine($"            <div class=\"top-diff-header\" onclick=\"toggleTopDiff('{topDiffId}')\" data-file-json=\"{HtmlEncode(fileJson)}\" data-file-diff=\"{HtmlEncode(fileDiff)}\">");
        sb.AppendLine("                <div class=\"top-diff-header-left\">");
        sb.AppendLine("                    <span class=\"expand-icon\">\u25bc</span>");
        sb.AppendLine("                    <div class=\"top-diff-header-stats\">");
        sb.AppendLine($"                        <span class=\"stat-inline\">{stats.TotalChanges} changes</span>");
        if (stats.Additions > 0)
        {
            sb.AppendLine($"                        <span class=\"stat-added\">+{stats.Additions} added</span>");
        }
        if (stats.Deletions > 0)
        {
            sb.AppendLine($"                        <span class=\"stat-removed\">-{stats.Deletions} deleted</span>");
        }
        sb.AppendLine($"                        <span class=\"stat-mode\">Mode: {modeText}</span>");
        sb.AppendLine("                    </div>");
        sb.AppendLine("                </div>");
        sb.AppendLine("                <div class=\"top-diff-header-buttons\" onclick=\"event.stopPropagation()\">");
        sb.AppendLine($"                    <button class=\"copy-btn\" onclick=\"copyTopDiffAsJson(this)\" title=\"Copy entire diff as JSON\">JSON</button>");
        sb.AppendLine($"                    <button class=\"copy-btn\" onclick=\"copyTopDiffAsDiff(this)\" title=\"Copy entire diff in unified format\">Diff</button>");
        sb.AppendLine("                </div>");
        sb.AppendLine("            </div>");

        sb.AppendLine($"            <div class=\"top-diff-content\" id=\"{topDiffId}\">");
        sb.AppendLine($"                <div class=\"diff-container\" id=\"{fileId}-content\">");

        // Side-by-side view
        AppendSideBySideView(sb, fileChange, result, options);

        sb.AppendLine("                </div>");
        sb.AppendLine("            </div>");

        // Individual changes - only render root-level changes (those not nested as children)
        if (fileChange.Changes.Count > 0)
        {
            // Build a set of all child changes to identify root-level changes
            var childChanges = new HashSet<Change>();
            foreach (var change in fileChange.Changes)
            {
                if (change.Children != null)
                {
                    foreach (var child in change.Children)
                    {
                        childChanges.Add(child);
                    }
                }
            }

            // Only render changes that are not children of another change
            var rootChanges = fileChange.Changes.Where(c => !childChanges.Contains(c)).ToList();

            if (rootChanges.Count > 0)
            {
                sb.AppendLine("            <div class=\"changes-list\">");
                foreach (var change in rootChanges)
                {
                    AppendChange(sb, change, 0);
                }
                sb.AppendLine("            </div>");
            }
        }

        sb.AppendLine("        </section>");
    }

    private static void AppendSideBySideView(StringBuilder sb, FileChange fileChange, DiffResult result, OutputOptions options)
    {
        // Old side
        sb.AppendLine("                <div class=\"diff-side diff-old\">");
        sb.AppendLine("                    <div class=\"diff-side-header\">Old Version</div>");
        sb.AppendLine("                    <div class=\"diff-content\">");
        AppendSideContent(sb, fileChange, isOld: true, result.Mode);
        sb.AppendLine("                    </div>");
        sb.AppendLine("                </div>");

        // New side
        sb.AppendLine("                <div class=\"diff-side diff-new\">");
        sb.AppendLine("                    <div class=\"diff-side-header\">New Version</div>");
        sb.AppendLine("                    <div class=\"diff-content\">");
        AppendSideContent(sb, fileChange, isOld: false, result.Mode);
        sb.AppendLine("                    </div>");
        sb.AppendLine("                </div>");
    }

    private static void AppendSideContent(StringBuilder sb, FileChange fileChange, bool isOld, DiffMode mode)
    {
        var lineNumber = 1;

        // Build a set of all child changes to identify root-level changes
        var childChanges = new HashSet<Change>();
        foreach (var change in fileChange.Changes)
        {
            if (change.Children != null)
            {
                foreach (var child in change.Children)
                {
                    childChanges.Add(child);
                }
            }
        }

        // Only process root-level changes (those not nested as children)
        var rootChanges = fileChange.Changes.Where(c => !childChanges.Contains(c)).ToList();

        var relevantChanges = rootChanges.Where(c =>
            (isOld && c.OldContent != null) || (!isOld && c.NewContent != null)).ToList();

        if (relevantChanges.Count == 0)
        {
            sb.AppendLine("                        <div class=\"diff-line\">");
            sb.AppendLine("                            <span class=\"line-number\">-</span>");
            sb.AppendLine("                            <span class=\"line-content\">(no content)</span>");
            sb.AppendLine("                        </div>");
            return;
        }

        foreach (var change in rootChanges)
        {
            var content = isOld ? change.OldContent : change.NewContent;
            var location = isOld ? change.OldLocation : change.NewLocation;

            if (content == null)
            {
                // For added (in old view) or removed (in new view), show empty placeholder
                if ((isOld && change.Type == ChangeType.Added) ||
                    (!isOld && change.Type == ChangeType.Removed))
                {
                    // Show placeholder lines to keep sides aligned
                    var otherContent = isOld ? change.NewContent : change.OldContent;
                    if (otherContent != null)
                    {
                        var lines = otherContent.Split('\n');
                        foreach (var _ in lines)
                        {
                            sb.AppendLine("                        <div class=\"diff-line\">");
                            sb.AppendLine("                            <span class=\"line-number\"></span>");
                            sb.AppendLine("                            <span class=\"line-content\"></span>");
                            sb.AppendLine("                        </div>");
                        }
                    }
                }
                continue;
            }

            var cssClass = GetLineCssClass(change.Type, isOld);
            var contentLines = content.Split('\n');
            var startLine = location?.StartLine ?? lineNumber;

            for (var i = 0; i < contentLines.Length; i++)
            {
                var line = contentLines[i].TrimEnd('\r');
                var currentLineNumber = startLine + i;
                var highlightedLine = HighlightSyntax(line, GetFileExtension(fileChange.Path));

                sb.AppendLine($"                        <div class=\"diff-line {cssClass}\">");
                sb.AppendLine($"                            <span class=\"line-number\">{currentLineNumber}</span>");
                sb.AppendLine($"                            <span class=\"line-content\">{highlightedLine}</span>");
                sb.AppendLine("                        </div>");
            }

            lineNumber = startLine + contentLines.Length;
        }
    }

    private static string GetLineCssClass(ChangeType type, bool isOld)
    {
        return type switch
        {
            ChangeType.Added => isOld ? "" : "line-added",
            ChangeType.Removed => isOld ? "line-removed" : "",
            ChangeType.Modified => "line-modified",
            _ => ""
        };
    }

    private static void AppendChange(StringBuilder sb, Change change, int depth)
    {
        if (change.Type == ChangeType.Unchanged)
        {
            return;
        }

        var changeId = GenerateChangeId(change);
        var badgeClass = GetBadgeClass(change.Type);
        var badgeText = change.Type.ToString();
        var kindText = change.Kind.ToString();
        var nameText = change.Name ?? "(unnamed)";
        var locationText = FormatLocation(change);

        // Determine impact styling
        var (impactBadgeClass, impactBadgeText) = GetImpactBadge(change.Impact);
        var sectionClass = IsNonBreakingOrFormattingOnly(change.Impact) ? "change-section change-nonbreaking" : "change-section";

        sb.AppendLine($"                <div class=\"{sectionClass}\" style=\"margin-left: {depth * 20}px\">");
        sb.AppendLine($"                    <div class=\"change-header\">");
        sb.AppendLine($"                        <div class=\"change-title\" onclick=\"toggleChange('{changeId}')\">");
        sb.AppendLine($"                            <span class=\"expand-icon\">\u25bc</span>");
        sb.AppendLine($"                            <span class=\"change-badge {badgeClass}\">{badgeText}</span>");
        sb.AppendLine($"                            <span class=\"change-kind\">{kindText}</span>");
        sb.AppendLine($"                            <span class=\"change-name\">{HtmlEncode(nameText)}</span>");

        if (!string.IsNullOrEmpty(locationText))
        {
            sb.AppendLine($"                            <span class=\"change-location\">{locationText}</span>");
        }

        // Add impact indicator badge
        sb.AppendLine($"                            <span class=\"impact-indicator {impactBadgeClass}\">{impactBadgeText}</span>");

        sb.AppendLine("                        </div>");
        sb.AppendLine("                        <div class=\"copy-buttons\" onclick=\"event.stopPropagation()\">");
        sb.AppendLine($"                            <button class=\"copy-btn\" onclick=\"copyAsJson('{changeId}')\" title=\"Copy as JSON\">JSON</button>");
        sb.AppendLine($"                            <button class=\"copy-btn\" onclick=\"copyAsDiff('{changeId}')\" title=\"Copy as diff\">Diff</button>");
        sb.AppendLine("                        </div>");
        sb.AppendLine("                    </div>");

        // Display caveats if present
        if (change.Caveats != null && change.Caveats.Count > 0)
        {
            sb.AppendLine("                    <div class=\"change-caveats\">");
            foreach (var caveat in change.Caveats)
            {
                sb.AppendLine($"                        <div>\u26a0\ufe0f {HtmlEncode(caveat)}</div>");
            }
            sb.AppendLine("                    </div>");
        }

        // Store change data for copy buttons (JSON-encoded in data attributes)
        var oldContentEscaped = HtmlEncode(change.OldContent ?? "");
        var newContentEscaped = HtmlEncode(change.NewContent ?? "");
        var changeMetadata = $"data-type=\"{change.Type}\" data-kind=\"{change.Kind}\" data-name=\"{HtmlEncode(change.Name ?? "")}\" data-old-line=\"{change.OldLocation?.StartLine}\" data-new-line=\"{change.NewLocation?.StartLine}\" data-old-content=\"{oldContentEscaped}\" data-new-content=\"{newContentEscaped}\"";

        sb.AppendLine($"                    <div class=\"change-body\" id=\"{changeId}\" {changeMetadata}>");

        // If this change has children, show a summary instead of full content to avoid duplication
        // The children will show their own detailed content
        var hasChildren = change.Children != null && change.Children.Count > 0;

        if (hasChildren)
        {
            // Show a summary placeholder for parent elements with children
            var childCount = change.Children!.Count;
            var childSummary = childCount == 1 ? "1 nested change" : $"{childCount} nested changes";
            sb.AppendLine($"                        <div class=\"diff-line\" style=\"padding: 8px 12px; color: #57606a; font-style: italic;\">");
            sb.AppendLine($"                            Contains {childSummary} shown below.");
            sb.AppendLine("                        </div>");
        }
        else if (change.OldContent != null || change.NewContent != null)
        {
            // Show old and new content only for leaf changes (no children)
            sb.AppendLine("                        <div class=\"diff-container\">");

            sb.AppendLine("                            <div class=\"diff-side diff-old\">");
            sb.AppendLine("                                <div class=\"diff-side-header\">Before</div>");
            sb.AppendLine("                                <div class=\"diff-content\">");
            AppendContentLines(sb, change.OldContent, change.OldLocation, "line-removed", null);
            sb.AppendLine("                                </div>");
            sb.AppendLine("                            </div>");

            sb.AppendLine("                            <div class=\"diff-side diff-new\">");
            sb.AppendLine("                                <div class=\"diff-side-header\">After</div>");
            sb.AppendLine("                                <div class=\"diff-content\">");
            AppendContentLines(sb, change.NewContent, change.NewLocation, "line-added", null);
            sb.AppendLine("                                </div>");
            sb.AppendLine("                            </div>");

            sb.AppendLine("                        </div>");
        }

        sb.AppendLine("                    </div>");

        // Render children recursively
        if (hasChildren)
        {
            foreach (var child in change.Children!)
            {
                AppendChange(sb, child, depth + 1);
            }
        }

        sb.AppendLine("                </div>");
    }

    private static void AppendContentLines(StringBuilder sb, string? content, Location? location, string cssClass, string? fileExtension)
    {
        if (content == null)
        {
            sb.AppendLine("                            <div class=\"diff-line\">");
            sb.AppendLine("                                <span class=\"line-number\">-</span>");
            sb.AppendLine("                                <span class=\"line-content\">(none)</span>");
            sb.AppendLine("                            </div>");
            return;
        }

        var lines = content.Split('\n');
        var startLine = location?.StartLine ?? 1;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var lineNumber = startLine + i;
            var highlightedLine = HighlightSyntax(line, fileExtension ?? ".cs");

            sb.AppendLine($"                            <div class=\"diff-line {cssClass}\">");
            sb.AppendLine($"                                <span class=\"line-number\">{lineNumber}</span>");
            sb.AppendLine($"                                <span class=\"line-content\">{highlightedLine}</span>");
            sb.AppendLine("                            </div>");
        }
    }

    private static string GetBadgeClass(ChangeType type)
    {
        return type switch
        {
            ChangeType.Added => "badge-added",
            ChangeType.Removed => "badge-removed",
            ChangeType.Modified => "badge-modified",
            ChangeType.Moved => "badge-moved",
            ChangeType.Renamed => "badge-renamed",
            _ => ""
        };
    }

    private static (string cssClass, string text) GetImpactBadge(ChangeImpact impact)
    {
        return impact switch
        {
            ChangeImpact.BreakingPublicApi => ("impact-breaking-public", "BREAKING PUBLIC API"),
            ChangeImpact.BreakingInternalApi => ("impact-breaking-internal", "BREAKING INTERNAL"),
            ChangeImpact.NonBreaking => ("impact-nonbreaking", "NON-BREAKING"),
            ChangeImpact.FormattingOnly => ("impact-formatting", "FORMATTING"),
            _ => ("impact-nonbreaking", "NON-BREAKING")
        };
    }

    private static bool IsNonBreakingOrFormattingOnly(ChangeImpact impact)
    {
        return impact is ChangeImpact.NonBreaking or ChangeImpact.FormattingOnly;
    }

    private static string FormatLocation(Change change)
    {
        var oldLoc = change.OldLocation;
        var newLoc = change.NewLocation;

        if (oldLoc != null && newLoc != null)
        {
            if (oldLoc.StartLine == newLoc.StartLine)
            {
                return $"line {oldLoc.StartLine}";
            }
            return $"line {oldLoc.StartLine} \u2192 {newLoc.StartLine}";
        }

        if (oldLoc != null)
        {
            return $"line {oldLoc.StartLine}-{oldLoc.EndLine}";
        }

        if (newLoc != null)
        {
            return $"line {newLoc.StartLine}-{newLoc.EndLine}";
        }

        return string.Empty;
    }

    private static void AppendNavigation(StringBuilder sb)
    {
        sb.AppendLine("    <div class=\"nav-container\">");
        sb.AppendLine("        <button class=\"nav-btn\" onclick=\"scrollToTop()\" title=\"Go to top\">\u2191</button>");
        sb.AppendLine("        <button class=\"nav-btn\" onclick=\"prevChange()\" title=\"Previous change\">\u25b2</button>");
        sb.AppendLine("        <button class=\"nav-btn\" onclick=\"nextChange()\" title=\"Next change\">\u25bc</button>");
        sb.AppendLine("        <button class=\"nav-btn\" onclick=\"expandAll()\" title=\"Expand all\">+</button>");
        sb.AppendLine("        <button class=\"nav-btn\" onclick=\"collapseAll()\" title=\"Collapse all\">-</button>");
        sb.AppendLine("    </div>");
    }

    private static void AppendHtmlFooter(StringBuilder sb)
    {
        sb.AppendLine("    <script>");
        sb.AppendLine("        let changeIndex = -1;");
        sb.AppendLine("        const changes = document.querySelectorAll('.change-section');");
        sb.AppendLine("");
        sb.AppendLine("        function toggleChange(changeId) {");
        sb.AppendLine("            const body = document.getElementById(changeId);");
        sb.AppendLine("            const header = body.previousElementSibling;");
        sb.AppendLine("            body.classList.toggle('collapsed');");
        sb.AppendLine("            header.classList.toggle('collapsed');");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Toggle top diff section visibility");
        sb.AppendLine("        function toggleTopDiff(topDiffId) {");
        sb.AppendLine("            const content = document.getElementById(topDiffId);");
        sb.AppendLine("            const header = content.previousElementSibling;");
        sb.AppendLine("            content.classList.toggle('collapsed');");
        sb.AppendLine("            header.classList.toggle('collapsed');");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Copy top diff as JSON");
        sb.AppendLine("        function copyTopDiffAsJson(btn) {");
        sb.AppendLine("            const header = btn.closest('.top-diff-header');");
        sb.AppendLine("            const jsonData = decodeHtmlEntities(header.dataset.fileJson);");
        sb.AppendLine("            try {");
        sb.AppendLine("                const parsed = JSON.parse(jsonData);");
        sb.AppendLine("                copyToClipboard(JSON.stringify(parsed, null, 2), btn);");
        sb.AppendLine("            } catch (e) {");
        sb.AppendLine("                copyToClipboard(jsonData, btn);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Copy top diff in unified diff format");
        sb.AppendLine("        function copyTopDiffAsDiff(btn) {");
        sb.AppendLine("            const header = btn.closest('.top-diff-header');");
        sb.AppendLine("            const diffData = decodeHtmlEntities(header.dataset.fileDiff);");
        sb.AppendLine("            copyToClipboard(diffData, btn);");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        function scrollToTop() {");
        sb.AppendLine("            window.scrollTo({ top: 0, behavior: 'smooth' });");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        function prevChange() {");
        sb.AppendLine("            if (changes.length === 0) return;");
        sb.AppendLine("            changeIndex = Math.max(0, changeIndex - 1);");
        sb.AppendLine("            changes[changeIndex].scrollIntoView({ behavior: 'smooth', block: 'center' });");
        sb.AppendLine("            highlightChange(changeIndex);");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        function nextChange() {");
        sb.AppendLine("            if (changes.length === 0) return;");
        sb.AppendLine("            changeIndex = Math.min(changes.length - 1, changeIndex + 1);");
        sb.AppendLine("            changes[changeIndex].scrollIntoView({ behavior: 'smooth', block: 'center' });");
        sb.AppendLine("            highlightChange(changeIndex);");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        function highlightChange(index) {");
        sb.AppendLine("            changes.forEach((c, i) => {");
        sb.AppendLine("                c.style.outline = i === index ? '2px solid #0969da' : 'none';");
        sb.AppendLine("            });");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        function expandAll() {");
        sb.AppendLine("            document.querySelectorAll('.change-body').forEach(el => el.classList.remove('collapsed'));");
        sb.AppendLine("            document.querySelectorAll('.change-header').forEach(el => el.classList.remove('collapsed'));");
        sb.AppendLine("            document.querySelectorAll('.top-diff-content').forEach(el => el.classList.remove('collapsed'));");
        sb.AppendLine("            document.querySelectorAll('.top-diff-header').forEach(el => el.classList.remove('collapsed'));");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        function collapseAll() {");
        sb.AppendLine("            document.querySelectorAll('.change-body').forEach(el => el.classList.add('collapsed'));");
        sb.AppendLine("            document.querySelectorAll('.change-header').forEach(el => el.classList.add('collapsed'));");
        sb.AppendLine("            document.querySelectorAll('.top-diff-content').forEach(el => el.classList.add('collapsed'));");
        sb.AppendLine("            document.querySelectorAll('.top-diff-header').forEach(el => el.classList.add('collapsed'));");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Copy as JSON format (for AI/Claude)");
        sb.AppendLine("        function copyAsJson(changeId) {");
        sb.AppendLine("            const body = document.getElementById(changeId);");
        sb.AppendLine("            const data = {");
        sb.AppendLine("                type: body.dataset.type,");
        sb.AppendLine("                kind: body.dataset.kind,");
        sb.AppendLine("                name: body.dataset.name || null,");
        sb.AppendLine("                oldLine: body.dataset.oldLine ? parseInt(body.dataset.oldLine) : null,");
        sb.AppendLine("                newLine: body.dataset.newLine ? parseInt(body.dataset.newLine) : null,");
        sb.AppendLine("                oldContent: decodeHtmlEntities(body.dataset.oldContent) || null,");
        sb.AppendLine("                newContent: decodeHtmlEntities(body.dataset.newContent) || null");
        sb.AppendLine("            };");
        sb.AppendLine("            copyToClipboard(JSON.stringify(data, null, 2), event.target);");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Copy as diff format");
        sb.AppendLine("        function copyAsDiff(changeId) {");
        sb.AppendLine("            const body = document.getElementById(changeId);");
        sb.AppendLine("            const oldContent = decodeHtmlEntities(body.dataset.oldContent);");
        sb.AppendLine("            const newContent = decodeHtmlEntities(body.dataset.newContent);");
        sb.AppendLine("            const name = body.dataset.name;");
        sb.AppendLine("            let diff = '--- ' + (name || 'a') + '\\n+++ ' + (name || 'b') + '\\n';");
        sb.AppendLine("            if (oldContent) {");
        sb.AppendLine("                oldContent.split('\\n').forEach(line => { diff += '- ' + line + '\\n'; });");
        sb.AppendLine("            }");
        sb.AppendLine("            if (newContent) {");
        sb.AppendLine("                newContent.split('\\n').forEach(line => { diff += '+ ' + line + '\\n'; });");
        sb.AppendLine("            }");
        sb.AppendLine("            copyToClipboard(diff, event.target);");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        function decodeHtmlEntities(text) {");
        sb.AppendLine("            if (!text) return '';");
        sb.AppendLine("            const textarea = document.createElement('textarea');");
        sb.AppendLine("            textarea.innerHTML = text;");
        sb.AppendLine("            return textarea.value;");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        async function copyToClipboard(text, btn) {");
        sb.AppendLine("            try {");
        sb.AppendLine("                await navigator.clipboard.writeText(text);");
        sb.AppendLine("                btn.classList.add('copied');");
        sb.AppendLine("                const original = btn.textContent;");
        sb.AppendLine("                btn.textContent = 'Copied!';");
        sb.AppendLine("                setTimeout(() => {");
        sb.AppendLine("                    btn.classList.remove('copied');");
        sb.AppendLine("                    btn.textContent = original;");
        sb.AppendLine("                }, 1500);");
        sb.AppendLine("            } catch (err) {");
        sb.AppendLine("                console.error('Failed to copy:', err);");
        sb.AppendLine("                alert('Failed to copy to clipboard');");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Check if path is absolute");
        sb.AppendLine("        function isAbsolutePath(path) {");
        sb.AppendLine("            return path.startsWith('/') || /^[A-Za-z]:[\\\\/]/.test(path);");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Copy path to clipboard");
        sb.AppendLine("        async function copyPath(path) {");
        sb.AppendLine("            try {");
        sb.AppendLine("                await navigator.clipboard.writeText(path);");
        sb.AppendLine("                return true;");
        sb.AppendLine("            } catch (e) { return false; }");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Copy path with notification popup");
        sb.AppendLine("        async function copyPathWithNotification(button, path) {");
        sb.AppendLine("            const success = await copyPath(path);");
        sb.AppendLine("            showNotification(button, success ? 'Copied!' : 'Failed to copy');");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Show notification below button");
        sb.AppendLine("        function showNotification(button, message) {");
        sb.AppendLine("            // Remove any existing notification");
        sb.AppendLine("            const existing = button.querySelector('.notification');");
        sb.AppendLine("            if (existing) existing.remove();");
        sb.AppendLine("            // Create notification element");
        sb.AppendLine("            const notif = document.createElement('div');");
        sb.AppendLine("            notif.className = 'notification';");
        sb.AppendLine("            notif.textContent = message;");
        sb.AppendLine("            button.appendChild(notif);");
        sb.AppendLine("            // Trigger animation");
        sb.AppendLine("            requestAnimationFrame(() => {");
        sb.AppendLine("                notif.classList.add('show');");
        sb.AppendLine("                setTimeout(() => {");
        sb.AppendLine("                    notif.classList.remove('show');");
        sb.AppendLine("                    setTimeout(() => notif.remove(), 150);");
        sb.AppendLine("                }, 1500);");
        sb.AppendLine("            });");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Editor integration");
        sb.AppendLine("        function openInEditor(path, editor) {");
        sb.AppendLine("            const cmds = { vscode: 'code', rider: 'rider', pycharm: 'pycharm', zed: 'zed' };");
        sb.AppendLine("            if (!isAbsolutePath(path)) {");
        sb.AppendLine("                copyPath(path);");
        sb.AppendLine("                alert('Path is relative. Copied to clipboard.\\n\\nTo open, run:\\n' + (cmds[editor] || editor) + ' \"' + path + '\"');");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine("            // VS Code: use vscode:// URL scheme");
        sb.AppendLine("            if (editor === 'vscode') {");
        sb.AppendLine("                window.location.href = 'vscode://file/' + path;");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine("            // Zed: use zed:// URL scheme");
        sb.AppendLine("            if (editor === 'zed') {");
        sb.AppendLine("                window.location.href = 'zed://file/' + path;");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine("            // Rider: use rider:// URL scheme");
        sb.AppendLine("            if (editor === 'rider') {");
        sb.AppendLine("                window.location.href = 'rider://open?file=' + encodeURIComponent(path);");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine("            // PyCharm: use pycharm:// URL scheme");
        sb.AppendLine("            if (editor === 'pycharm') {");
        sb.AppendLine("                window.location.href = 'pycharm://open?file=' + encodeURIComponent(path);");
        sb.AppendLine("                return;");
        sb.AppendLine("            }");
        sb.AppendLine("            // Fallback: copy path and show CLI command");
        sb.AppendLine("            copyPath(path);");
        sb.AppendLine("            alert('Path copied to clipboard.\\n\\nRun: ' + (cmds[editor] || editor) + ' \"' + path + '\"');");
        sb.AppendLine("        }");
        sb.AppendLine("");
        sb.AppendLine("        // Keyboard navigation");
        sb.AppendLine("        document.addEventListener('keydown', function(e) {");
        sb.AppendLine("            if (e.key === 'j' || e.key === 'ArrowDown') {");
        sb.AppendLine("                if (e.ctrlKey || e.metaKey) {");
        sb.AppendLine("                    nextChange();");
        sb.AppendLine("                    e.preventDefault();");
        sb.AppendLine("                }");
        sb.AppendLine("            } else if (e.key === 'k' || e.key === 'ArrowUp') {");
        sb.AppendLine("                if (e.ctrlKey || e.metaKey) {");
        sb.AppendLine("                    prevChange();");
        sb.AppendLine("                    e.preventDefault();");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        });");
        sb.AppendLine("    </script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
    }

    /// <summary>
    /// Applies basic syntax highlighting to code.
    /// </summary>
    /// <param name="code">The code to highlight.</param>
    /// <param name="fileExtension">The file extension to determine language.</param>
    /// <returns>HTML with syntax highlighting spans.</returns>
    public static string HighlightSyntax(string code, string? fileExtension)
    {
        if (string.IsNullOrEmpty(code))
        {
            return string.Empty;
        }

        // First, HTML encode the content
        var encoded = HtmlEncode(code);

        // Determine language from extension
        var isCSharp = fileExtension?.ToLowerInvariant() is ".cs" or null;
        var isVb = fileExtension?.ToLowerInvariant() is ".vb";

        if (isCSharp)
        {
            return HighlightCSharp(encoded);
        }

        if (isVb)
        {
            return HighlightVbNet(encoded);
        }

        return encoded;
    }

    private static string HighlightCSharp(string code)
    {
        // Order matters: apply keywords FIRST before adding HTML markup,
        // then wrap in spans using placeholders to avoid regex matching HTML attributes

        // Use placeholders to avoid nested regex matches
        const string keywordStart = "\u0001KW\u0002";
        const string keywordEnd = "\u0001/KW\u0002";
        const string typeStart = "\u0001TY\u0002";
        const string typeEnd = "\u0001/TY\u0002";
        const string numberStart = "\u0001NU\u0002";
        const string numberEnd = "\u0001/NU\u0002";
        const string stringStart = "\u0001ST\u0002";
        const string stringEnd = "\u0001/ST\u0002";
        const string commentStart = "\u0001CO\u0002";
        const string commentEnd = "\u0001/CO\u0002";
        const string preprocStart = "\u0001PP\u0002";
        const string preprocEnd = "\u0001/PP\u0002";

        // Apply all regex with placeholders
        code = CSharpKeywordRegex().Replace(code, $"{keywordStart}$1{keywordEnd}");
        code = CSharpTypeRegex().Replace(code, $"{typeStart}$1{typeEnd}");
        code = NumberLiteralRegex().Replace(code, $"{numberStart}$0{numberEnd}");
        code = StringLiteralRegex().Replace(code, $"{stringStart}$0{stringEnd}");
        code = SingleLineCommentRegex().Replace(code, $"{commentStart}$0{commentEnd}");
        code = PreprocessorRegex().Replace(code, $"{preprocStart}$0{preprocEnd}");

        // Replace placeholders with actual HTML
        code = code.Replace(keywordStart, "<span class=\"keyword\">").Replace(keywordEnd, "</span>");
        code = code.Replace(typeStart, "<span class=\"type\">").Replace(typeEnd, "</span>");
        code = code.Replace(numberStart, "<span class=\"number\">").Replace(numberEnd, "</span>");
        code = code.Replace(stringStart, "<span class=\"string\">").Replace(stringEnd, "</span>");
        code = code.Replace(commentStart, "<span class=\"comment\">").Replace(commentEnd, "</span>");
        code = code.Replace(preprocStart, "<span class=\"preprocessor\">").Replace(preprocEnd, "</span>");

        return code;
    }

    private static string HighlightVbNet(string code)
    {
        // Use placeholders to avoid nested regex matches
        const string keywordStart = "\u0001KW\u0002";
        const string keywordEnd = "\u0001/KW\u0002";
        const string typeStart = "\u0001TY\u0002";
        const string typeEnd = "\u0001/TY\u0002";
        const string numberStart = "\u0001NU\u0002";
        const string numberEnd = "\u0001/NU\u0002";
        const string stringStart = "\u0001ST\u0002";
        const string stringEnd = "\u0001/ST\u0002";
        const string commentStart = "\u0001CO\u0002";
        const string commentEnd = "\u0001/CO\u0002";

        // Apply all regex with placeholders
        code = VbKeywordRegex().Replace(code, $"{keywordStart}$1{keywordEnd}");
        code = VbTypeRegex().Replace(code, $"{typeStart}$1{typeEnd}");
        code = NumberLiteralRegex().Replace(code, $"{numberStart}$0{numberEnd}");
        code = StringLiteralRegex().Replace(code, $"{stringStart}$0{stringEnd}");
        code = VbCommentRegex().Replace(code, $"{commentStart}$0{commentEnd}");

        // Replace placeholders with actual HTML
        code = code.Replace(keywordStart, "<span class=\"keyword\">").Replace(keywordEnd, "</span>");
        code = code.Replace(typeStart, "<span class=\"type\">").Replace(typeEnd, "</span>");
        code = code.Replace(numberStart, "<span class=\"number\">").Replace(numberEnd, "</span>");
        code = code.Replace(stringStart, "<span class=\"string\">").Replace(stringEnd, "</span>");
        code = code.Replace(commentStart, "<span class=\"comment\">").Replace(commentEnd, "</span>");

        return code;
    }

    /// <summary>
    /// Encodes a string for safe inclusion in HTML content.
    /// </summary>
    /// <param name="text">The text to encode.</param>
    /// <returns>HTML-encoded string.</returns>
    public static string HtmlEncode(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return HttpUtility.HtmlEncode(text);
    }

    private static string GenerateFileId(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "file-unknown";
        }

        var safeName = Path.GetFileNameWithoutExtension(path)
            .Replace(" ", "-")
            .Replace(".", "-");
        return $"file-{safeName}-{path.GetHashCode():x8}";
    }

    private static string GenerateChangeId(Change change)
    {
        var hash = HashCode.Combine(
            change.Type,
            change.Kind,
            change.Name,
            change.OldLocation?.StartLine,
            change.NewLocation?.StartLine);
        return $"change-{hash:x8}";
    }

    private static string? GetFileExtension(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        return Path.GetExtension(path);
    }

    /// <summary>
    /// Generates JSON representation of all changes in a file for copy functionality.
    /// </summary>
    private static string GenerateFileJson(FileChange fileChange)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append($"\"path\":\"{EscapeJsonString(fileChange.Path ?? "")}\",");
        sb.Append("\"changes\":[");

        var first = true;
        foreach (var change in fileChange.Changes)
        {
            if (change.Type == ChangeType.Unchanged)
            {
                continue;
            }

            if (!first)
            {
                sb.Append(",");
            }
            first = false;

            sb.Append("{");
            sb.Append($"\"type\":\"{change.Type}\",");
            sb.Append($"\"kind\":\"{change.Kind}\",");
            sb.Append($"\"name\":{(change.Name != null ? $"\"{EscapeJsonString(change.Name)}\"" : "null")},");
            sb.Append($"\"oldLine\":{change.OldLocation?.StartLine.ToString() ?? "null"},");
            sb.Append($"\"newLine\":{change.NewLocation?.StartLine.ToString() ?? "null"},");
            sb.Append($"\"oldContent\":{(change.OldContent != null ? $"\"{EscapeJsonString(change.OldContent)}\"" : "null")},");
            sb.Append($"\"newContent\":{(change.NewContent != null ? $"\"{EscapeJsonString(change.NewContent)}\"" : "null")}");
            sb.Append("}");
        }

        sb.Append("]}");
        return sb.ToString();
    }

    /// <summary>
    /// Generates unified diff format representation of all changes in a file.
    /// </summary>
    private static string GenerateFileDiff(FileChange fileChange, DiffResult result)
    {
        var sb = new StringBuilder();
        var filePath = fileChange.Path ?? "file";
        var oldPath = result.OldPath ?? filePath;
        var newPath = result.NewPath ?? filePath;

        sb.AppendLine($"--- {oldPath}");
        sb.AppendLine($"+++ {newPath}");

        foreach (var change in fileChange.Changes)
        {
            if (change.Type == ChangeType.Unchanged)
            {
                continue;
            }

            // Add hunk header
            var oldStart = change.OldLocation?.StartLine ?? 0;
            var oldCount = change.OldContent?.Split('\n').Length ?? 0;
            var newStart = change.NewLocation?.StartLine ?? 0;
            var newCount = change.NewContent?.Split('\n').Length ?? 0;
            sb.AppendLine($"@@ -{oldStart},{oldCount} +{newStart},{newCount} @@ {change.Name ?? ""}");

            // Add removed lines
            if (change.OldContent != null)
            {
                foreach (var line in change.OldContent.Split('\n'))
                {
                    sb.AppendLine($"-{line.TrimEnd('\r')}");
                }
            }

            // Add added lines
            if (change.NewContent != null)
            {
                foreach (var line in change.NewContent.Split('\n'))
                {
                    sb.AppendLine($"+{line.TrimEnd('\r')}");
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a string for safe inclusion in JSON.
    /// </summary>
    private static string EscapeJsonString(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    // Regex patterns for syntax highlighting (using GeneratedRegex for performance)
    [GeneratedRegex(@"&quot;(?:[^&]|&(?!quot;))*?&quot;|&#39;(?:[^&]|&(?!#39;))*?&#39;")]
    private static partial Regex StringLiteralRegex();

    [GeneratedRegex(@"//.*$", RegexOptions.Multiline)]
    private static partial Regex SingleLineCommentRegex();

    [GeneratedRegex(@"\b(abstract|as|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|record|ref|return|sbyte|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|while|async|await|when|where|yield|init|required|get|set|add|remove|value|partial|global|nint|nuint|with|and|or|not)\b")]
    private static partial Regex CSharpKeywordRegex();

    [GeneratedRegex(@"\b(String|Int32|Int64|Boolean|Object|Double|Single|Decimal|Byte|Char|DateTime|TimeSpan|Guid|Task|List|Dictionary|IEnumerable|ICollection|IList|Action|Func|Exception|ArgumentException|InvalidOperationException|StringBuilder|Regex)\b")]
    private static partial Regex CSharpTypeRegex();

    [GeneratedRegex(@"\b\d+(\.\d+)?(f|d|m|L|UL|u)?\b")]
    private static partial Regex NumberLiteralRegex();

    [GeneratedRegex(@"^#\w+.*$", RegexOptions.Multiline)]
    private static partial Regex PreprocessorRegex();

    [GeneratedRegex(@"&#39;.*$", RegexOptions.Multiline)]
    private static partial Regex VbCommentRegex();

    [GeneratedRegex(@"\b(AddHandler|AddressOf|Alias|And|AndAlso|As|Boolean|ByRef|Byte|ByVal|Call|Case|Catch|CBool|CByte|CChar|CDate|CDbl|CDec|Char|CInt|Class|CLng|CObj|Const|Continue|CSByte|CShort|CSng|CStr|CType|CUInt|CULng|CUShort|Date|Decimal|Declare|Default|Delegate|Dim|DirectCast|Do|Double|Each|Else|ElseIf|End|EndIf|Enum|Erase|Error|Event|Exit|False|Finally|For|Friend|Function|Get|GetType|GetXMLNamespace|Global|GoSub|GoTo|Handles|If|Implements|Imports|In|Inherits|Integer|Interface|Is|IsNot|Let|Lib|Like|Long|Loop|Me|Mod|Module|MustInherit|MustOverride|MyBase|MyClass|Namespace|Narrowing|New|Next|Not|Nothing|NotInheritable|NotOverridable|Object|Of|On|Operator|Option|Optional|Or|OrElse|Overloads|Overridable|Overrides|ParamArray|Partial|Private|Property|Protected|Public|RaiseEvent|ReadOnly|ReDim|REM|RemoveHandler|Resume|Return|SByte|Select|Set|Shadows|Shared|Short|Single|Static|Step|Stop|String|Structure|Sub|SyncLock|Then|Throw|To|True|Try|TryCast|TypeOf|UInteger|ULong|UShort|Using|Variant|Wend|When|While|Widening|With|WithEvents|WriteOnly|Xor|Async|Await|Iterator|Yield)\b", RegexOptions.IgnoreCase)]
    private static partial Regex VbKeywordRegex();

    [GeneratedRegex(@"\b(String|Integer|Long|Boolean|Object|Double|Single|Decimal|Byte|Char|Date|TimeSpan|Guid|Task|List|Dictionary|IEnumerable|ICollection|IList|Action|Func|Exception|ArgumentException|InvalidOperationException|StringBuilder|Regex)\b")]
    private static partial Regex VbTypeRegex();
}
