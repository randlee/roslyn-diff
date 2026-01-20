namespace RoslynDiff.Core.Tfm;

using RoslynDiff.Core.Models;

/// <summary>
/// Merges diff results from multiple Target Framework Moniker (TFM) analyses into a single unified result.
/// </summary>
/// <remarks>
/// This class handles the logic for combining results from analyzing the same code across different
/// target frameworks (e.g., net8.0, net10.0). Changes that appear in all TFMs are marked as universal
/// (empty ApplicableToTfms list), while changes that appear only in specific TFMs are tagged accordingly.
/// </remarks>
public static class TfmResultMerger
{
    /// <summary>
    /// Merges diff results from multiple TFM analyses into a single unified result.
    /// </summary>
    /// <param name="tfmResults">List of tuples containing TFM name and its corresponding diff result.</param>
    /// <param name="options">The diff options used for the analysis.</param>
    /// <returns>A merged DiffResult containing all changes with appropriate TFM tagging.</returns>
    /// <remarks>
    /// <para>
    /// The merging logic works as follows:
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description>Collect all changes from all TFM results.</description>
    /// </item>
    /// <item>
    /// <description>
    /// Group identical changes (same Type, Kind, Name, and location):
    /// <list type="bullet">
    /// <item><description>Changes appearing in ALL TFMs: Set ApplicableToTfms = empty list</description></item>
    /// <item><description>Changes appearing in SOME TFMs: Set ApplicableToTfms = list of specific TFMs</description></item>
    /// </list>
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Merge statistics: Sum additions, deletions, modifications, moves, and renames across all TFMs,
    /// counting each unique change only once.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var tfmResults = new List&lt;(string, DiffResult)&gt;
    /// {
    ///     ("net8.0", result1),
    ///     ("net10.0", result2)
    /// };
    /// var merged = TfmResultMerger.Merge(tfmResults, options);
    /// </code>
    /// </example>
    public static DiffResult Merge(
        List<(string Tfm, DiffResult Result)> tfmResults,
        DiffOptions options)
    {
        if (tfmResults == null || tfmResults.Count == 0)
        {
            return new DiffResult
            {
                Mode = DiffMode.Roslyn,
                AnalyzedTfms = Array.Empty<string>()
            };
        }

        // Single TFM case - no merging needed, but set AnalyzedTfms
        if (tfmResults.Count == 1)
        {
            var (tfm, result) = tfmResults[0];
            return result with
            {
                AnalyzedTfms = new[] { tfm }
            };
        }

        // Extract all TFMs
        var allTfms = tfmResults.Select(r => r.Tfm).ToList();

        // Collect all changes from all TFMs, grouped by file path
        var fileChangesMap = new Dictionary<string, List<Change>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (tfm, result) in tfmResults)
        {
            foreach (var fileChange in result.FileChanges)
            {
                var filePath = fileChange.Path ?? string.Empty;
                if (!fileChangesMap.ContainsKey(filePath))
                {
                    fileChangesMap[filePath] = new List<Change>();
                }

                // Tag each change with its source TFM before adding
                foreach (var change in fileChange.Changes)
                {
                    fileChangesMap[filePath].Add(TagChangeWithTfm(change, tfm));
                }
            }
        }

        // Merge changes for each file
        var mergedFileChanges = new List<FileChange>();
        foreach (var (path, changes) in fileChangesMap)
        {
            var mergedChanges = MergeChanges(changes, allTfms);
            if (mergedChanges.Any())
            {
                mergedFileChanges.Add(new FileChange
                {
                    Path = string.IsNullOrEmpty(path) ? null : path,
                    Changes = mergedChanges
                });
            }
        }

        // Calculate merged statistics
        var stats = CalculateStats(mergedFileChanges);

        // Get paths from first result
        var firstResult = tfmResults[0].Result;

        return new DiffResult
        {
            OldPath = firstResult.OldPath,
            NewPath = firstResult.NewPath,
            Mode = DiffMode.Roslyn,
            AnalyzedTfms = allTfms,
            FileChanges = mergedFileChanges,
            Stats = stats
        };
    }

    /// <summary>
    /// Tags a change and all its children with the source TFM.
    /// </summary>
    private static Change TagChangeWithTfm(Change change, string tfm)
    {
        var taggedChildren = change.Children?
            .Select(c => TagChangeWithTfm(c, tfm))
            .ToList();

        return change with
        {
            ApplicableToTfms = new[] { tfm },
            Children = taggedChildren
        };
    }

    /// <summary>
    /// Merges a list of changes, grouping identical changes and setting appropriate TFM tags.
    /// </summary>
    private static IReadOnlyList<Change> MergeChanges(List<Change> changes, List<string> allTfms)
    {
        // Group changes by their identity
        var groupedChanges = new Dictionary<ChangeIdentity, ChangeGroup>();

        foreach (var change in changes)
        {
            var identity = new ChangeIdentity(change);

            if (!groupedChanges.ContainsKey(identity))
            {
                groupedChanges[identity] = new ChangeGroup(change);
            }
            else
            {
                groupedChanges[identity].AddInstance(change);
            }

            groupedChanges[identity].AddTfms(change.ApplicableToTfms ?? Array.Empty<string>());
        }

        // Create merged changes with appropriate TFM tags
        var mergedChanges = new List<Change>();
        foreach (var group in groupedChanges.Values)
        {
            var mergedChange = CreateMergedChange(group, allTfms);
            mergedChanges.Add(mergedChange);
        }

        return mergedChanges;
    }

    /// <summary>
    /// Creates a merged change from a group of identical changes across TFMs.
    /// </summary>
    private static Change CreateMergedChange(ChangeGroup group, List<string> allTfms)
    {
        var baseChange = group.BaseChange;

        // Determine if change applies to all TFMs
        var applicableToTfms = DetermineApplicableTfms(group.Tfms, allTfms);

        // Merge children recursively
        IReadOnlyList<Change>? mergedChildren = null;
        var allChildren = new List<Change>();
        foreach (var instance in group.AllInstances)
        {
            if (instance.Children != null && instance.Children.Any())
            {
                allChildren.AddRange(instance.Children);
            }
        }

        if (allChildren.Any())
        {
            mergedChildren = MergeChanges(allChildren, allTfms);
        }

        return baseChange with
        {
            ApplicableToTfms = applicableToTfms,
            Children = mergedChildren ?? baseChange.Children
        };
    }

    /// <summary>
    /// Determines the appropriate ApplicableToTfms list based on which TFMs the change appears in.
    /// </summary>
    private static IReadOnlyList<string>? DetermineApplicableTfms(HashSet<string> changeTfms, List<string> allTfms)
    {
        // If change appears in all TFMs, return empty list (universal)
        if (changeTfms.Count == allTfms.Count && allTfms.All(tfm => changeTfms.Contains(tfm)))
        {
            return Array.Empty<string>();
        }

        // Otherwise, return the specific list of TFMs
        return changeTfms.OrderBy(t => t).ToList();
    }

    /// <summary>
    /// Calculates aggregate statistics from merged file changes, counting each unique change only once.
    /// </summary>
    private static DiffStats CalculateStats(List<FileChange> fileChanges)
    {
        int additions = 0;
        int deletions = 0;
        int modifications = 0;
        int moves = 0;
        int renames = 0;
        int breakingPublicApi = 0;
        int breakingInternalApi = 0;
        int nonBreaking = 0;
        int formattingOnly = 0;

        foreach (var fileChange in fileChanges)
        {
            foreach (var change in fileChange.Changes)
            {
                CountChange(change, ref additions, ref deletions, ref modifications,
                    ref moves, ref renames, ref breakingPublicApi, ref breakingInternalApi,
                    ref nonBreaking, ref formattingOnly);
            }
        }

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

    /// <summary>
    /// Recursively counts changes and their children for statistics.
    /// </summary>
    private static void CountChange(
        Change change,
        ref int additions,
        ref int deletions,
        ref int modifications,
        ref int moves,
        ref int renames,
        ref int breakingPublicApi,
        ref int breakingInternalApi,
        ref int nonBreaking,
        ref int formattingOnly)
    {
        // Count the change type
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

        // Count the impact
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

        // Recursively count children
        if (change.Children != null)
        {
            foreach (var child in change.Children)
            {
                CountChange(child, ref additions, ref deletions, ref modifications,
                    ref moves, ref renames, ref breakingPublicApi, ref breakingInternalApi,
                    ref nonBreaking, ref formattingOnly);
            }
        }
    }

    /// <summary>
    /// Represents the identity of a change for grouping purposes.
    /// </summary>
    private readonly struct ChangeIdentity : IEquatable<ChangeIdentity>
    {
        private readonly ChangeType _type;
        private readonly ChangeKind _kind;
        private readonly string? _name;
        private readonly Location? _oldLocation;
        private readonly Location? _newLocation;

        public ChangeIdentity(Change change)
        {
            _type = change.Type;
            _kind = change.Kind;
            _name = change.Name;
            _oldLocation = change.OldLocation;
            _newLocation = change.NewLocation;
        }

        public bool Equals(ChangeIdentity other)
        {
            return _type == other._type &&
                   _kind == other._kind &&
                   _name == other._name &&
                   LocationEquals(_oldLocation, other._oldLocation) &&
                   LocationEquals(_newLocation, other._newLocation);
        }

        public override bool Equals(object? obj)
        {
            return obj is ChangeIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 23 + _type.GetHashCode();
                hash = hash * 23 + _kind.GetHashCode();
                hash = hash * 23 + (_name?.GetHashCode() ?? 0);
                hash = hash * 23 + (LocationHashCode(_oldLocation));
                hash = hash * 23 + (LocationHashCode(_newLocation));
                return hash;
            }
        }

        private static bool LocationEquals(Location? loc1, Location? loc2)
        {
            if (loc1 == null && loc2 == null) return true;
            if (loc1 == null || loc2 == null) return false;

            return loc1.StartLine == loc2.StartLine &&
                   loc1.EndLine == loc2.EndLine &&
                   loc1.StartColumn == loc2.StartColumn &&
                   loc1.EndColumn == loc2.EndColumn &&
                   string.Equals(loc1.File, loc2.File, StringComparison.OrdinalIgnoreCase);
        }

        private static int LocationHashCode(Location? location)
        {
            if (location == null) return 0;

            unchecked
            {
                var hash = 17;
                hash = hash * 23 + location.StartLine;
                hash = hash * 23 + location.EndLine;
                hash = hash * 23 + location.StartColumn;
                hash = hash * 23 + location.EndColumn;
                hash = hash * 23 + (location.File?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }

    /// <summary>
    /// Represents a group of identical changes from different TFMs.
    /// </summary>
    private class ChangeGroup
    {
        public Change BaseChange { get; }
        public List<Change> AllInstances { get; }
        public HashSet<string> Tfms { get; }

        public ChangeGroup(Change baseChange)
        {
            BaseChange = baseChange;
            AllInstances = new List<Change> { baseChange };
            Tfms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public void AddInstance(Change change)
        {
            AllInstances.Add(change);
        }

        public void AddTfms(IEnumerable<string> tfms)
        {
            foreach (var tfm in tfms)
            {
                Tfms.Add(tfm);
            }
        }
    }
}
