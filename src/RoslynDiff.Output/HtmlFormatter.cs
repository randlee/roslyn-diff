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
        AppendSummarySection(sb, result.Stats, options);
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
            margin: 0 0 16px 0;
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

        .diff-mode {
            margin-top: 8px;
            font-size: 12px;
            color: #57606a;
        }

        main {
            display: flex;
            flex-direction: column;
            gap: 24px;
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

        .file-header .toggle-btn {
            background: none;
            border: none;
            cursor: pointer;
            padding: 4px 8px;
            font-size: 12px;
            color: #0969da;
        }

        .file-header .toggle-btn:hover {
            text-decoration: underline;
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
            padding: 8px 16px;
            background-color: var(--color-header-bg);
            border-bottom: 1px solid var(--color-border);
            font-size: 12px;
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
            line-height: 20px;
            white-space: pre;
            overflow-x: auto;
        }

        .diff-line {
            display: flex;
            min-height: 20px;
        }

        .line-number {
            flex-shrink: 0;
            width: 50px;
            padding: 0 8px;
            text-align: right;
            color: var(--color-line-number);
            background-color: var(--color-code-bg);
            border-right: 1px solid var(--color-border);
            user-select: none;
        }

        .line-content {
            flex: 1;
            padding: 0 8px;
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
            margin-bottom: 16px;
            border: 1px solid var(--color-border);
            border-radius: 6px;
            overflow: hidden;
        }

        .change-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 8px 16px;
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

    private static void AppendSummarySection(StringBuilder sb, DiffStats stats, OutputOptions options)
    {
        sb.AppendLine("    <header>");
        sb.AppendLine("        <h1>Diff Report</h1>");

        if (options.IncludeStats)
        {
            sb.AppendLine("        <div class=\"summary\">");
            sb.AppendLine($"            <span class=\"stat stat-total\">{stats.TotalChanges} changes</span>");

            if (stats.Additions > 0)
            {
                sb.AppendLine($"            <span class=\"stat stat-additions\">+{stats.Additions} added</span>");
            }

            if (stats.Deletions > 0)
            {
                sb.AppendLine($"            <span class=\"stat stat-deletions\">-{stats.Deletions} deleted</span>");
            }

            if (stats.Modifications > 0)
            {
                sb.AppendLine($"            <span class=\"stat stat-modifications\">~{stats.Modifications} modified</span>");
            }

            if (stats.Moves > 0)
            {
                sb.AppendLine($"            <span class=\"stat stat-moves\">\u21c4{stats.Moves} moved</span>");
            }

            if (stats.Renames > 0)
            {
                sb.AppendLine($"            <span class=\"stat stat-renames\">\u270e{stats.Renames} renamed</span>");
            }

            sb.AppendLine("        </div>");
        }

        sb.AppendLine("    </header>");
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

        sb.AppendLine($"        <section class=\"file-diff\" id=\"{fileId}\">");
        sb.AppendLine("            <div class=\"file-header\">");
        sb.AppendLine($"                <h2>{HtmlEncode(displayName)}</h2>");
        sb.AppendLine($"                <button class=\"toggle-btn\" onclick=\"toggleFile('{fileId}')\">Collapse</button>");
        sb.AppendLine("            </div>");
        sb.AppendLine($"            <div class=\"diff-container\" id=\"{fileId}-content\">");

        // Side-by-side view
        AppendSideBySideView(sb, fileChange, result, options);

        sb.AppendLine("            </div>");

        // Individual changes
        if (fileChange.Changes.Count > 0)
        {
            sb.AppendLine("            <div class=\"changes-list\">");
            foreach (var change in fileChange.Changes)
            {
                AppendChange(sb, change, 0);
            }
            sb.AppendLine("            </div>");
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
        var relevantChanges = fileChange.Changes.Where(c =>
            (isOld && c.OldContent != null) || (!isOld && c.NewContent != null)).ToList();

        if (relevantChanges.Count == 0)
        {
            sb.AppendLine("                        <div class=\"diff-line\">");
            sb.AppendLine("                            <span class=\"line-number\">-</span>");
            sb.AppendLine("                            <span class=\"line-content\">(no content)</span>");
            sb.AppendLine("                        </div>");
            return;
        }

        foreach (var change in fileChange.Changes)
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

        sb.AppendLine($"                <div class=\"change-section\" style=\"margin-left: {depth * 20}px\">");
        sb.AppendLine($"                    <div class=\"change-header\" onclick=\"toggleChange('{changeId}')\">");
        sb.AppendLine("                        <div class=\"change-title\">");
        sb.AppendLine($"                            <span class=\"expand-icon\">\u25bc</span>");
        sb.AppendLine($"                            <span class=\"change-badge {badgeClass}\">{badgeText}</span>");
        sb.AppendLine($"                            <span class=\"change-kind\">{kindText}</span>");
        sb.AppendLine($"                            <span class=\"change-name\">{HtmlEncode(nameText)}</span>");

        if (!string.IsNullOrEmpty(locationText))
        {
            sb.AppendLine($"                            <span class=\"change-location\">{locationText}</span>");
        }

        sb.AppendLine("                        </div>");
        sb.AppendLine("                    </div>");
        sb.AppendLine($"                    <div class=\"change-body\" id=\"{changeId}\">");

        // Show old and new content if available
        if (change.OldContent != null || change.NewContent != null)
        {
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
        if (change.Children != null)
        {
            foreach (var child in change.Children)
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
        sb.AppendLine(@"        let changeIndex = -1;
        const changes = document.querySelectorAll('.change-section');

        function toggleFile(fileId) {
            const content = document.getElementById(fileId + '-content');
            const btn = content.previousElementSibling.querySelector('.toggle-btn');
            if (content.style.display === 'none') {
                content.style.display = 'flex';
                btn.textContent = 'Collapse';
            } else {
                content.style.display = 'none';
                btn.textContent = 'Expand';
            }
        }

        function toggleChange(changeId) {
            const body = document.getElementById(changeId);
            const header = body.previousElementSibling;
            body.classList.toggle('collapsed');
            header.classList.toggle('collapsed');
        }

        function scrollToTop() {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }

        function prevChange() {
            if (changes.length === 0) return;
            changeIndex = Math.max(0, changeIndex - 1);
            changes[changeIndex].scrollIntoView({ behavior: 'smooth', block: 'center' });
            highlightChange(changeIndex);
        }

        function nextChange() {
            if (changes.length === 0) return;
            changeIndex = Math.min(changes.length - 1, changeIndex + 1);
            changes[changeIndex].scrollIntoView({ behavior: 'smooth', block: 'center' });
            highlightChange(changeIndex);
        }

        function highlightChange(index) {
            changes.forEach((c, i) => {
                c.style.outline = i === index ? '2px solid #0969da' : 'none';
            });
        }

        function expandAll() {
            document.querySelectorAll('.change-body').forEach(el => el.classList.remove('collapsed'));
            document.querySelectorAll('.change-header').forEach(el => el.classList.remove('collapsed'));
        }

        function collapseAll() {
            document.querySelectorAll('.change-body').forEach(el => el.classList.add('collapsed'));
            document.querySelectorAll('.change-header').forEach(el => el.classList.add('collapsed'));
        }

        // Keyboard navigation
        document.addEventListener('keydown', function(e) {
            if (e.key === 'j' || e.key === 'ArrowDown') {
                if (e.ctrlKey || e.metaKey) {
                    nextChange();
                    e.preventDefault();
                }
            } else if (e.key === 'k' || e.key === 'ArrowUp') {
                if (e.ctrlKey || e.metaKey) {
                    prevChange();
                    e.preventDefault();
                }
            }
        });");
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
