namespace RoslynDiff.Output;

using RoslynDiff.Core.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

/// <summary>
/// Formats diff results using Spectre.Console for rich terminal output with colors and formatting.
/// Intended for interactive terminal use with the --rich flag.
/// </summary>
public class SpectreConsoleFormatter : IOutputFormatter
{
    /// <inheritdoc/>
    public string Format => "terminal";

    /// <inheritdoc/>
    public string ContentType => "text/plain";

    /// <inheritdoc/>
    public string FormatResult(DiffResult result, OutputOptions? options = null)
    {
        options ??= new OutputOptions();

        using var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer),
            ColorSystem = options.UseColor ? ColorSystemSupport.Detect : ColorSystemSupport.NoColors
        });

        RenderToConsole(console, result, options);

        return writer.ToString();
    }

    /// <inheritdoc/>
    public async Task FormatResultAsync(DiffResult result, TextWriter writer, OutputOptions? options = null)
    {
        var text = FormatResult(result, options);
        await writer.WriteAsync(text);
    }

    private static void RenderToConsole(IAnsiConsole console, DiffResult result, OutputOptions options)
    {
        // Header Panel
        RenderHeader(console, result);

        // Statistics Table
        if (options.IncludeStats)
        {
            RenderStatsTable(console, result.Stats);
        }

        // Changes Tree
        RenderChangesTree(console, result, options);
    }

    private static void RenderHeader(IAnsiConsole console, DiffResult result)
    {
        var oldPath = result.OldPath ?? "(none)";
        var newPath = result.NewPath ?? "(none)";
        var modeText = result.Mode == DiffMode.Roslyn ? "Roslyn (semantic)" : "Line (text)";

        var panel = new Panel(new Markup($"[dim]{oldPath}[/] [blue]->[/] [dim]{newPath}[/]\n[grey]Mode: {modeText}[/]"))
        {
            Header = new PanelHeader("[bold blue]Diff Report[/]"),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0)
        };

        console.Write(panel);
        console.WriteLine();
    }

    private static void RenderStatsTable(IAnsiConsole console, DiffStats stats)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Metric[/]").Centered())
            .AddColumn(new TableColumn("[bold]Count[/]").Centered());

        if (stats.Additions > 0)
            table.AddRow("[green]Additions[/]", $"[green]+{stats.Additions}[/]");
        if (stats.Deletions > 0)
            table.AddRow("[red]Deletions[/]", $"[red]-{stats.Deletions}[/]");
        if (stats.Modifications > 0)
            table.AddRow("[yellow]Modifications[/]", $"[yellow]~{stats.Modifications}[/]");
        if (stats.Moves > 0)
            table.AddRow("[cyan]Moves[/]", $"[cyan]>{stats.Moves}[/]");
        if (stats.Renames > 0)
            table.AddRow("[magenta]Renames[/]", $"[magenta]@{stats.Renames}[/]");

        table.AddRow("[bold]Total[/]", $"[bold]{stats.TotalChanges}[/]");

        console.Write(table);
        console.WriteLine();
    }

    private static void RenderChangesTree(IAnsiConsole console, DiffResult result, OutputOptions options)
    {
        if (result.FileChanges.Count == 0)
        {
            console.MarkupLine("[grey]No changes detected.[/]");
            return;
        }

        var tree = new Tree("[bold yellow]Changes[/]");

        foreach (var fileChange in result.FileChanges)
        {
            var fileNode = tree.AddNode(GetFileNodeMarkup(fileChange));

            foreach (var change in fileChange.Changes)
            {
                AddChangeNode(fileNode, change, options);
            }
        }

        console.Write(tree);
    }

    private static string GetFileNodeMarkup(FileChange fileChange)
    {
        var path = fileChange.Path ?? "(unknown file)";
        return $"[bold]{EscapeMarkup(path)}[/]";
    }

    private static void AddChangeNode(TreeNode parent, Change change, OutputOptions options)
    {
        var markup = GetChangeMarkup(change);
        var node = parent.AddNode(markup);

        // Add modification details
        if (change.Type == ChangeType.Modified && !options.Compact)
        {
            AddModificationDetails(node, change);
        }

        // Add child changes recursively
        if (change.Children is { Count: > 0 })
        {
            foreach (var child in change.Children)
            {
                AddChangeNode(node, child, options);
            }
        }
    }

    private static string GetChangeMarkup(Change change)
    {
        var (color, marker) = GetChangeStyle(change.Type);
        var kindLabel = GetKindLabel(change.Kind);
        var name = EscapeMarkup(change.Name ?? "(unnamed)");
        var location = GetLocationMarkup(change);

        return $"[{color}][[{marker}]][/] [bold]{kindLabel}:[/] {name}{location}";
    }

    private static (string color, string marker) GetChangeStyle(ChangeType type)
    {
        return type switch
        {
            ChangeType.Added => ("green", "+"),
            ChangeType.Removed => ("red", "-"),
            ChangeType.Modified => ("yellow", "~"),
            ChangeType.Moved => ("cyan", ">"),
            ChangeType.Renamed => ("magenta", "@"),
            ChangeType.Unchanged => ("grey", "="),
            _ => ("white", "?")
        };
    }

    private static string GetKindLabel(ChangeKind kind)
    {
        return kind switch
        {
            ChangeKind.File => "File",
            ChangeKind.Namespace => "Namespace",
            ChangeKind.Class => "Class",
            ChangeKind.Method => "Method",
            ChangeKind.Property => "Property",
            ChangeKind.Field => "Field",
            ChangeKind.Statement => "Statement",
            ChangeKind.Line => "Line",
            _ => kind.ToString()
        };
    }

    private static string GetLocationMarkup(Change change)
    {
        var location = change.NewLocation ?? change.OldLocation;
        if (location is null)
            return string.Empty;

        var lineText = location.StartLine == location.EndLine
            ? $"line {location.StartLine}"
            : $"line {location.StartLine}-{location.EndLine}";

        return $" [dim]({lineText})[/]";
    }

    private static void AddModificationDetails(TreeNode node, Change change)
    {
        if (change.OldContent is not null && change.NewContent is not null)
        {
            node.AddNode("[dim italic]Body modified[/]");
        }
    }

    /// <summary>
    /// Escapes special Spectre.Console markup characters in the input text.
    /// </summary>
    private static string EscapeMarkup(string text)
    {
        return text
            .Replace("[", "[[")
            .Replace("]", "]]");
    }
}
