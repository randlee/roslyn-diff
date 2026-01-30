namespace RoslynDiff.Core.Differ;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Models;
using RoslynDiff.Core.Tfm;

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

        // Pre-scan optimization: Check if preprocessor directives exist
        var hasPreprocessorDirectives = PreprocessorDirectiveDetector.HasPreprocessorDirectives(oldContent) ||
                                       PreprocessorDirectiveDetector.HasPreprocessorDirectives(newContent);

        // Determine if we need multi-TFM analysis
        var shouldPerformMultiTfmAnalysis = hasPreprocessorDirectives &&
                                           options.TargetFrameworks is { Count: > 0 };

        // Single TFM handling (no TFMs specified OR no preprocessor directives)
        if (!shouldPerformMultiTfmAnalysis)
        {
            return CompareSingleTfm(oldContent, newContent, options);
        }

        // Multi-TFM parallel processing
        return CompareMultiTfm(oldContent, newContent, options);
    }

    private DiffResult CompareSingleTfm(string oldContent, string newContent, DiffOptions options)
    {
        // Use default symbols (NET10_0)
        var symbols = TfmSymbolResolver.GetDefaultSymbols();
        var parseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.Latest,
            documentationMode: DocumentationMode.Parse,
            preprocessorSymbols: symbols);

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
            AnalyzedTfms = null, // No TFM analysis performed
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

    private DiffResult CompareMultiTfm(string oldContent, string newContent, DiffOptions options)
    {
        var tfms = options.TargetFrameworks!;
        var tfmCount = tfms.Count;

        // Use parallel processing if 2+ TFMs
        if (tfmCount >= 2)
        {
            var results = new List<(string Tfm, IReadOnlyList<Change> Changes)>(tfmCount);
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            // Use Parallel.ForEach for better performance with large TFM lists
            var syncResults = new object();
            Parallel.ForEach(tfms, parallelOptions, tfm =>
            {
                var changes = AnalyzeTfm(oldContent, newContent, options, tfm);
                lock (syncResults)
                {
                    results.Add((tfm, changes));
                }
            });

            // Merge results using placeholder merger (Agent 3 will implement real merger)
            return MergeTfmResults(results, options, tfms.ToList());
        }
        else
        {
            // Single TFM in the list
            var tfm = tfms[0];
            var changes = AnalyzeTfm(oldContent, newContent, options, tfm);
            var results = new List<(string Tfm, IReadOnlyList<Change> Changes)>
            {
                (tfm, changes)
            };

            return MergeTfmResults(results, options, tfms.ToList());
        }
    }

    private IReadOnlyList<Change> AnalyzeTfm(string oldContent, string newContent, DiffOptions options, string tfm)
    {
        // Get symbols for this TFM
        var symbols = TfmSymbolResolver.GetPreprocessorSymbols(tfm);
        var parseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.Latest,
            documentationMode: DocumentationMode.Parse,
            preprocessorSymbols: symbols);

        var oldTree = CSharpSyntaxTree.ParseText(oldContent, parseOptions, options.OldPath ?? string.Empty);
        var newTree = CSharpSyntaxTree.ParseText(newContent, parseOptions, options.NewPath ?? string.Empty);

        // Check for parse errors
        var oldErrors = oldTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        var newErrors = newTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        if (oldErrors.Count > 0 || newErrors.Count > 0)
        {
            // For multi-TFM, we still want to continue with other TFMs even if one fails
            // Return an empty change list for this TFM
            return Array.Empty<Change>();
        }

        // Compare using SyntaxComparer
        return _comparer.Compare(oldTree, newTree, options);
    }

    private DiffResult MergeTfmResults(
        List<(string Tfm, IReadOnlyList<Change> Changes)> results,
        DiffOptions options,
        List<string> analyzedTfms)
    {
        // Convert to DiffResult format for merger
        var tfmResults = results.Select(r => (
            r.Tfm,
            Result: new DiffResult
            {
                OldPath = options.OldPath,
                NewPath = options.NewPath,
                Mode = DiffMode.Roslyn,
                FileChanges =
                [
                    new FileChange
                    {
                        Path = options.NewPath ?? options.OldPath,
                        Changes = r.Changes
                    }
                ],
                Stats = CalculateStats(r.Changes)
            }
        )).ToList();

        // Use TfmResultMerger to merge results
        return TfmResultMerger.Merge(tfmResults, options);
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
        var breakingPublicApi = 0;
        var breakingInternalApi = 0;
        var nonBreaking = 0;
        var formattingOnly = 0;

        CountChanges(changes, ref additions, ref deletions, ref modifications, ref moves, ref renames,
            ref breakingPublicApi, ref breakingInternalApi, ref nonBreaking, ref formattingOnly);

        return new DiffStats
        {
            Additions = additions,
            Deletions = deletions,
            Modifications = modifications,
            Moves = moves,
            Renames = renames,
            BreakingPublicApiCount = breakingPublicApi,
            BreakingInternalApiCount = breakingInternalApi,
            NonBreakingCount = nonBreaking,
            FormattingOnlyCount = formattingOnly
        };
    }

    private static void CountChanges(IReadOnlyList<Change> changes, ref int additions, ref int deletions, ref int modifications, ref int moves, ref int renames,
        ref int breakingPublicApi, ref int breakingInternalApi, ref int nonBreaking, ref int formattingOnly)
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

            // Count impact breakdown
            switch (change.Impact)
            {
                case ChangeImpact.BreakingPublicApi:
                    breakingPublicApi++;
                    break;
                case ChangeImpact.BreakingInternalApi:
                    breakingInternalApi++;
                    break;
                case ChangeImpact.NonBreaking:
                    nonBreaking++;
                    break;
                case ChangeImpact.FormattingOnly:
                    formattingOnly++;
                    break;
            }

            if (change.Children is { Count: > 0 })
            {
                CountChanges(change.Children, ref additions, ref deletions, ref modifications, ref moves, ref renames,
                    ref breakingPublicApi, ref breakingInternalApi, ref nonBreaking, ref formattingOnly);
            }
        }
    }
}
