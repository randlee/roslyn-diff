namespace RoslynDiff.Core.Differ;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Models;

/// <summary>
/// Performs semantic diff comparison for C# source code using Roslyn.
/// </summary>
/// <remarks>
/// <para>
/// This differ parses C# source code into Roslyn syntax trees and compares them
/// at the semantic level, understanding code structure such as classes, methods,
/// properties, and other declarations.
/// </para>
/// </remarks>
public sealed class CSharpDiffer : IDiffer
{
    private readonly SyntaxComparer _comparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpDiffer"/> class.
    /// </summary>
    public CSharpDiffer()
    {
        _comparer = new SyntaxComparer();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CSharpDiffer"/> class with a custom comparer.
    /// </summary>
    /// <param name="comparer">The syntax comparer to use.</param>
    public CSharpDiffer(SyntaxComparer comparer)
    {
        _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
    }

    /// <inheritdoc/>
    public bool CanHandle(string filePath, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(options);

        var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
        if (extension != ".cs")
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
        var parseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.Latest,
            documentationMode: DocumentationMode.Parse);

        var oldTree = CSharpSyntaxTree.ParseText(oldContent, parseOptions, options.OldPath ?? string.Empty);
        var newTree = CSharpSyntaxTree.ParseText(newContent, parseOptions, options.NewPath ?? string.Empty);

        // Check for parse errors
        var oldErrors = oldTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        var newErrors = newTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        if (oldErrors.Count > 0 || newErrors.Count > 0)
        {
            var errorMsg = string.Join("; ", oldErrors.Concat(newErrors).Take(5).Select(d => d.GetMessage()));
            return CreateParseErrorResult(options, $"Parse errors: {errorMsg}");
        }

        // Compare using SyntaxComparer
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
            Stats = new DiffStats { Modifications = 1 }
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
