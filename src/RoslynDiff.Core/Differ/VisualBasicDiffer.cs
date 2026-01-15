namespace RoslynDiff.Core.Differ;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Models;

/// <summary>
/// Performs semantic diff comparison for VB.NET source code using Roslyn.
/// </summary>
/// <remarks>
/// <para>
/// This differ parses VB.NET source code into Roslyn syntax trees and compares them
/// at the semantic level, understanding code structure such as modules, classes, subs,
/// functions, properties, and other declarations.
/// </para>
/// <para>
/// VB.NET is case-insensitive by default, so this differ performs case-insensitive
/// matching when comparing identifiers.
/// </para>
/// </remarks>
public sealed class VisualBasicDiffer : IDiffer
{
    private readonly VisualBasicSyntaxComparer _comparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualBasicDiffer"/> class.
    /// </summary>
    public VisualBasicDiffer()
    {
        _comparer = new VisualBasicSyntaxComparer();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualBasicDiffer"/> class with a custom comparer.
    /// </summary>
    /// <param name="comparer">The syntax comparer to use.</param>
    public VisualBasicDiffer(VisualBasicSyntaxComparer comparer)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
    }

    /// <inheritdoc/>
    public bool CanHandle(string filePath, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(options);

        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        if (extension != ".vb")
        {
            return false;
        }

        // Handle if mode is Roslyn or auto (null), but not Line
        return options.Mode != DiffMode.Line;
    }

    /// <inheritdoc/>
    public DiffResult Compare(string oldContent, string newContent, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(oldContent);
        ArgumentNullException.ThrowIfNull(newContent);
        ArgumentNullException.ThrowIfNull(options);

        // Parse both sources
        var parseOptions = new VisualBasicParseOptions(
            languageVersion: LanguageVersion.Latest,
            documentationMode: DocumentationMode.Parse);

        var oldTree = VisualBasicSyntaxTree.ParseText(oldContent, parseOptions, options.OldPath ?? string.Empty);
        var newTree = VisualBasicSyntaxTree.ParseText(newContent, parseOptions, options.NewPath ?? string.Empty);

        // Check for parse errors
        var oldErrors = oldTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        var newErrors = newTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        if (oldErrors.Count > 0 || newErrors.Count > 0)
        {
            var errorMsg = string.Join("; ", oldErrors.Concat(newErrors).Take(5).Select(d => d.GetMessage()));
            return CreateParseErrorResult(options, $"Parse errors: {errorMsg}");
        }

        // Compare using VisualBasicSyntaxComparer
        var changes = _comparer.Compare(oldTree, newTree, options);

        // Calculate stats
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
                            Type = ChangeType.Modified,
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

    private static DiffStats CalculateStats(IReadOnlyList<Change> changes)
    {
        var additions = 0;
        var deletions = 0;
        var modifications = 0;
        var moves = 0;
        var renames = 0;

        CountChanges(changes, ref additions, ref deletions, ref modifications, ref moves, ref renames);

        return new DiffStats
        {
            TotalChanges = additions + deletions + modifications + moves + renames,
            Additions = additions,
            Deletions = deletions,
            Modifications = modifications,
            Moves = moves,
            Renames = renames
        };
    }

    private static void CountChanges(IReadOnlyList<Change> changes, ref int additions, ref int deletions, ref int modifications, ref int moves, ref int renames)
    {
        foreach (var change in changes)
        {
            switch (change.Type)
            {
                case ChangeType.Added:
                    additions++;
                    break;
                case ChangeType.Removed:
                    deletions++;
                    break;
                case ChangeType.Modified:
                    modifications++;
                    break;
                case ChangeType.Moved:
                    moves++;
                    break;
                case ChangeType.Renamed:
                    renames++;
                    break;
            }

            if (change.Children is { Count: > 0 })
            {
                CountChanges(change.Children, ref additions, ref deletions, ref modifications, ref moves, ref renames);
            }
        }
    }
}
