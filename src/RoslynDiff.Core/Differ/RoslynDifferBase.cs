namespace RoslynDiff.Core.Differ;

using Microsoft.CodeAnalysis;
using RoslynDiff.Core.Models;
using RoslynDiff.Core.Syntax;

// Alias to avoid ambiguity with Microsoft.CodeAnalysis.Location
using SourceLocation = RoslynDiff.Core.Models.Location;

/// <summary>
/// Abstract base class for Roslyn-based semantic differs.
/// </summary>
/// <remarks>
/// <para>
/// This class implements the Template Method pattern for comparing source code using Roslyn syntax trees.
/// Derived classes must implement language-specific parsing and node classification.
/// </para>
/// <para>
/// The comparison workflow is:
/// <list type="number">
/// <item>Parse both source strings into syntax trees using <see cref="ParseSource"/></item>
/// <item>Compare the trees using <see cref="ISyntaxComparer"/></item>
/// <item>Build a <see cref="DiffResult"/> from the comparison results</item>
/// </list>
/// </para>
/// </remarks>
public abstract class RoslynDifferBase : IDiffer
{
    private readonly ISyntaxComparer _syntaxComparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynDifferBase"/> class.
    /// </summary>
    /// <param name="syntaxComparer">The syntax comparer to use for tree comparison.</param>
    protected RoslynDifferBase(ISyntaxComparer syntaxComparer)
    {
        _syntaxComparer = syntaxComparer ?? throw new ArgumentNullException(nameof(syntaxComparer));
    }

    /// <summary>
    /// Gets the file extensions supported by this differ.
    /// </summary>
    /// <remarks>
    /// Extensions should include the leading dot (e.g., ".cs", ".vb").
    /// </remarks>
    protected abstract IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Parses the source content into a syntax tree.
    /// </summary>
    /// <param name="content">The source code content to parse.</param>
    /// <param name="path">Optional file path for diagnostic purposes.</param>
    /// <returns>A <see cref="SyntaxTree"/> representing the parsed source.</returns>
    protected abstract SyntaxTree ParseSource(string content, string? path = null);

    /// <summary>
    /// Determines whether the specified syntax node represents a structural code element.
    /// </summary>
    /// <param name="node">The syntax node to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the node represents a structural element such as a class, method, or property;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Structural nodes are the primary units of comparison in semantic diffs.
    /// They typically include type declarations, method declarations, property declarations, etc.
    /// </remarks>
    protected abstract bool IsStructuralNode(SyntaxNode node);

    /// <summary>
    /// Gets the <see cref="ChangeKind"/> for the specified syntax node.
    /// </summary>
    /// <param name="node">The syntax node to classify.</param>
    /// <returns>The <see cref="ChangeKind"/> representing the type of code element.</returns>
    protected abstract ChangeKind GetChangeKind(SyntaxNode node);

    /// <summary>
    /// Gets the name of a syntax node, if applicable.
    /// </summary>
    /// <param name="node">The syntax node to get the name from.</param>
    /// <returns>The name of the node, or <c>null</c> if the node has no name.</returns>
    protected abstract string? GetNodeName(SyntaxNode node);

    /// <inheritdoc/>
    public bool CanHandle(string filePath, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(options);

        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        // Check if the extension is supported
        var isSupported = SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        if (!isSupported)
        {
            return false;
        }

        // If mode is explicitly Line, don't handle
        if (options.Mode == DiffMode.Line)
        {
            return false;
        }

        // Handle if mode is Roslyn or auto (null)
        return options.Mode == DiffMode.Roslyn || options.Mode == null;
    }

    /// <inheritdoc/>
    public DiffResult Compare(string oldContent, string newContent, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(oldContent);
        ArgumentNullException.ThrowIfNull(newContent);
        ArgumentNullException.ThrowIfNull(options);

        // Parse both sources
        SyntaxTree oldTree;
        SyntaxTree newTree;
        IReadOnlyList<Diagnostic> parseErrors;

        try
        {
            oldTree = ParseSource(oldContent, options.OldPath);
            newTree = ParseSource(newContent, options.NewPath);

            // Collect parse diagnostics (errors and warnings)
            var diagnostics = new List<Diagnostic>();
            diagnostics.AddRange(oldTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
            diagnostics.AddRange(newTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
            parseErrors = diagnostics;
        }
        catch (Exception ex)
        {
            // If parsing fails completely, return an error result
            // This allows the DifferFactory to potentially fall back to LineDiffer
            return CreateParseErrorResult(options, ex.Message);
        }

        // If there are critical parse errors, return error result
        if (parseErrors.Count > 0)
        {
            var errorMessages = string.Join("; ", parseErrors.Take(5).Select(d => d.GetMessage()));
            return CreateParseErrorResult(options, $"Parse errors: {errorMessages}");
        }

        // Compare the syntax trees
        var comparisonOptions = new SyntaxCompareOptions
        {
            IgnoreWhitespace = options.IgnoreWhitespace,
            IgnoreComments = options.IgnoreComments
        };

        var syntaxChanges = _syntaxComparer.Compare(
            oldTree.GetRoot(),
            newTree.GetRoot(),
            comparisonOptions);

        // Build the diff result from syntax changes
        var changes = BuildChanges(syntaxChanges, oldTree, newTree);
        var stats = CalculateStats(changes);

        return new DiffResult
        {
            OldPath = options.OldPath,
            NewPath = options.NewPath,
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = options.NewPath ?? options.OldPath,
                    Changes = changes
                }
            ],
            Stats = stats
        };
    }

    /// <summary>
    /// Creates a <see cref="SourceLocation"/> from a Roslyn <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="node">The syntax node to get the location from.</param>
    /// <param name="filePath">The file path to include in the location.</param>
    /// <returns>A <see cref="SourceLocation"/> representing the node's position.</returns>
    protected static SourceLocation CreateLocation(SyntaxNode node, string? filePath)
    {
        var span = node.GetLocation().GetLineSpan();
        return new SourceLocation
        {
            File = filePath,
            // Convert from 0-based to 1-based line numbers
            StartLine = span.StartLinePosition.Line + 1,
            EndLine = span.EndLinePosition.Line + 1,
            StartColumn = span.StartLinePosition.Character + 1,
            EndColumn = span.EndLinePosition.Character + 1
        };
    }

    /// <summary>
    /// Creates an error result indicating a parse failure.
    /// </summary>
    /// <param name="options">The diff options.</param>
    /// <param name="errorMessage">The error message describing the parse failure.</param>
    /// <returns>A <see cref="DiffResult"/> indicating the parse error.</returns>
    private static DiffResult CreateParseErrorResult(DiffOptions options, string errorMessage)
    {
        return new DiffResult
        {
            OldPath = options.OldPath,
            NewPath = options.NewPath,
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = options.NewPath ?? options.OldPath,
                    Changes =
                    [
                        new Change
                        {
                            Type = Models.ChangeType.Modified,
                            Kind = ChangeKind.File,
                            Name = "Parse Error",
                            OldContent = errorMessage,
                            NewContent = errorMessage
                        }
                    ]
                }
            ],
            Stats = new DiffStats { TotalChanges = 1, Modifications = 1 }
        };
    }

    /// <summary>
    /// Builds the list of changes from syntax comparison results.
    /// </summary>
    /// <param name="syntaxChanges">The syntax changes from the comparer.</param>
    /// <param name="oldTree">The old syntax tree.</param>
    /// <param name="newTree">The new syntax tree.</param>
    /// <returns>A list of <see cref="Change"/> objects.</returns>
    private List<Change> BuildChanges(
        IReadOnlyList<SyntaxChange> syntaxChanges,
        SyntaxTree oldTree,
        SyntaxTree newTree)
    {
        var changes = new List<Change>();

        foreach (var syntaxChange in syntaxChanges)
        {
            var change = ConvertSyntaxChange(syntaxChange, oldTree, newTree);
            changes.Add(change);
        }

        return changes;
    }

    /// <summary>
    /// Converts a <see cref="SyntaxChange"/> to a <see cref="Change"/>.
    /// </summary>
    /// <param name="syntaxChange">The syntax change to convert.</param>
    /// <param name="oldTree">The old syntax tree.</param>
    /// <param name="newTree">The new syntax tree.</param>
    /// <returns>A <see cref="Change"/> representing the syntax change.</returns>
    private Change ConvertSyntaxChange(SyntaxChange syntaxChange, SyntaxTree oldTree, SyntaxTree newTree)
    {
        var changeType = syntaxChange.ChangeType switch
        {
            SyntaxChangeType.Added => Models.ChangeType.Added,
            SyntaxChangeType.Removed => Models.ChangeType.Removed,
            SyntaxChangeType.Modified => Models.ChangeType.Modified,
            SyntaxChangeType.Moved => Models.ChangeType.Moved,
            SyntaxChangeType.Renamed => Models.ChangeType.Renamed,
            _ => Models.ChangeType.Unchanged
        };

        var changeKind = syntaxChange.OldNode != null
            ? GetChangeKind(syntaxChange.OldNode)
            : syntaxChange.NewNode != null
                ? GetChangeKind(syntaxChange.NewNode)
                : ChangeKind.Statement;

        var name = syntaxChange.OldNode != null
            ? GetNodeName(syntaxChange.OldNode)
            : syntaxChange.NewNode != null
                ? GetNodeName(syntaxChange.NewNode)
                : null;

        // Build child changes recursively
        IReadOnlyList<Change>? children = null;
        if (syntaxChange.Children is { Count: > 0 })
        {
            var childList = new List<Change>();
            foreach (var child in syntaxChange.Children)
            {
                childList.Add(ConvertSyntaxChange(child, oldTree, newTree));
            }
            children = childList;
        }

        return new Change
        {
            Type = changeType,
            Kind = changeKind,
            Name = name,
            OldLocation = syntaxChange.OldNode != null
                ? CreateLocation(syntaxChange.OldNode, oldTree.FilePath)
                : null,
            NewLocation = syntaxChange.NewNode != null
                ? CreateLocation(syntaxChange.NewNode, newTree.FilePath)
                : null,
            OldContent = syntaxChange.OldNode?.ToFullString(),
            NewContent = syntaxChange.NewNode?.ToFullString(),
            Children = children
        };
    }

    /// <summary>
    /// Calculates statistics from the list of changes.
    /// </summary>
    /// <param name="changes">The changes to calculate statistics for.</param>
    /// <returns>A <see cref="DiffStats"/> containing the calculated statistics.</returns>
    private static DiffStats CalculateStats(IReadOnlyList<Change> changes)
    {
        var stats = new StatsAccumulator();
        AccumulateStats(changes, stats);

        return new DiffStats
        {
            TotalChanges = stats.Additions + stats.Deletions + stats.Modifications + stats.Moves + stats.Renames,
            Additions = stats.Additions,
            Deletions = stats.Deletions,
            Modifications = stats.Modifications,
            Moves = stats.Moves,
            Renames = stats.Renames
        };
    }

    /// <summary>
    /// Recursively accumulates statistics from changes and their children.
    /// </summary>
    private static void AccumulateStats(IReadOnlyList<Change> changes, StatsAccumulator stats)
    {
        foreach (var change in changes)
        {
            switch (change.Type)
            {
                case Models.ChangeType.Added:
                    stats.Additions++;
                    break;
                case Models.ChangeType.Removed:
                    stats.Deletions++;
                    break;
                case Models.ChangeType.Modified:
                    stats.Modifications++;
                    break;
                case Models.ChangeType.Moved:
                    stats.Moves++;
                    break;
                case Models.ChangeType.Renamed:
                    stats.Renames++;
                    break;
            }

            if (change.Children is { Count: > 0 })
            {
                AccumulateStats(change.Children, stats);
            }
        }
    }

    /// <summary>
    /// Helper class for accumulating statistics during tree traversal.
    /// </summary>
    private sealed class StatsAccumulator
    {
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public int Modifications { get; set; }
        public int Moves { get; set; }
        public int Renames { get; set; }
    }
}
