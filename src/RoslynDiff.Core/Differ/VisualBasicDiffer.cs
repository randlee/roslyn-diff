namespace RoslynDiff.Core.Differ;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Models;
using RoslynDiff.Core.Tfm;
using System.Collections.Concurrent;

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

        // Pre-scan: Check if source code contains preprocessor directives
        var hasPreprocessorDirectives =
            PreprocessorDirectiveDetector.HasPreprocessorDirectives(oldContent) ||
            PreprocessorDirectiveDetector.HasPreprocessorDirectives(newContent);

        // Determine TFMs to analyze
        var tfmsToAnalyze = DetermineTfmsToAnalyze(options, hasPreprocessorDirectives);

        // Single TFM path (optimized)
        if (tfmsToAnalyze.Count == 1)
        {
            return CompareSingleTfm(oldContent, newContent, options, tfmsToAnalyze[0]);
        }

        // Multi-TFM path (parallel processing)
        return CompareMultipleTfms(oldContent, newContent, options, tfmsToAnalyze);
    }

    /// <summary>
    /// Determines which TFMs should be analyzed based on options and content.
    /// </summary>
    private static List<string> DetermineTfmsToAnalyze(DiffOptions options, bool hasPreprocessorDirectives)
    {
        // If no TFMs specified in options, use default (NET10_0)
        if (options.TargetFrameworks == null || options.TargetFrameworks.Count == 0)
        {
            return new List<string> { "net10.0" };
        }

        // If no preprocessor directives detected, only analyze first TFM (optimization)
        if (!hasPreprocessorDirectives)
        {
            return new List<string> { options.TargetFrameworks[0] };
        }

        // Multiple TFMs with preprocessor directives - analyze all
        return options.TargetFrameworks.ToList();
    }

    /// <summary>
    /// Compares source code for a single TFM.
    /// </summary>
    private DiffResult CompareSingleTfm(string oldContent, string newContent, DiffOptions options, string tfm)
    {
        // Get preprocessor symbols for the TFM
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols(tfm);

        // Parse both sources with TFM-specific symbols
        var parseOptions = new VisualBasicParseOptions(
            languageVersion: LanguageVersion.Latest,
            documentationMode: DocumentationMode.Parse,
            preprocessorSymbols: symbols.Select(s => new KeyValuePair<string, object>(s, true)));

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
            AnalyzedTfms = new[] { tfm },
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
    /// Compares source code for multiple TFMs in parallel.
    /// </summary>
    private DiffResult CompareMultipleTfms(string oldContent, string newContent, DiffOptions options, List<string> tfms)
    {
        // Parse and compare for each TFM in parallel
        var tfmResults = new ConcurrentBag<(string Tfm, IReadOnlyList<Change> Changes)>();

        Parallel.ForEach(tfms, tfm =>
        {
            // Get preprocessor symbols for the TFM
            var symbols = TfmSymbolResolver.GetPreprocessorSymbols(tfm);

            // Parse both sources with TFM-specific symbols
            var parseOptions = new VisualBasicParseOptions(
                languageVersion: LanguageVersion.Latest,
                documentationMode: DocumentationMode.Parse,
                preprocessorSymbols: symbols.Select(s => new KeyValuePair<string, object>(s, true)));

            var oldTree = VisualBasicSyntaxTree.ParseText(oldContent, parseOptions, options.OldPath ?? string.Empty);
            var newTree = VisualBasicSyntaxTree.ParseText(newContent, parseOptions, options.NewPath ?? string.Empty);

            // Check for parse errors
            var oldErrors = oldTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            var newErrors = newTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

            if (oldErrors.Count > 0 || newErrors.Count > 0)
            {
                // Skip this TFM if there are parse errors
                return;
            }

            // Compare using VisualBasicSyntaxComparer
            var changes = _comparer.Compare(oldTree, newTree, options);

            tfmResults.Add((tfm, changes));
        });

        // Merge results from all TFMs
        var mergedChanges = MergeTfmResults(tfmResults.ToList());

        // Calculate stats
        var stats = CalculateStats(mergedChanges);

        return new DiffResult
        {
            OldPath = options.OldPath,
            NewPath = options.NewPath,
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = tfms,
            FileChanges =
            [
                new FileChange
                {
                    Path = options.NewPath ?? options.OldPath,
                    Changes = mergedChanges
                }
            ],
            Stats = stats
        };
    }

    /// <summary>
    /// Merges changes from multiple TFM analyses.
    /// This is a placeholder implementation until TfmResultMerger is available.
    /// </summary>
    private static List<Change> MergeTfmResults(List<(string Tfm, IReadOnlyList<Change> Changes)> tfmResults)
    {
        if (tfmResults.Count == 0)
        {
            return new List<Change>();
        }

        // Placeholder: For now, just return changes from the first TFM
        // TODO: Sprint 3 will implement proper TfmResultMerger to intelligently merge
        // changes and populate ApplicableToTfms for each change
        var firstResult = tfmResults[0];
        return firstResult.Changes.ToList();
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
