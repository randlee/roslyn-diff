namespace RoslynDiff.Core.MultiFile;

using LibGit2Sharp;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using System.Collections.Concurrent;

/// <summary>
/// Compares files between two git references (branches, commits, tags).
/// </summary>
public sealed class GitComparer
{
    private readonly DifferFactory _differFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitComparer"/> class.
    /// </summary>
    public GitComparer()
    {
        _differFactory = new DifferFactory();
    }

    /// <summary>
    /// Compares files between two git references.
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository.</param>
    /// <param name="refRange">Reference range (e.g., "main..feature", "abc123..def456").</param>
    /// <param name="options">Options for diff comparison.</param>
    /// <returns>A multi-file diff result containing all file changes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the repository or refs are invalid.</exception>
    public MultiFileDiffResult Compare(string repositoryPath, string refRange, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(repositoryPath);
        ArgumentNullException.ThrowIfNull(refRange);
        ArgumentNullException.ThrowIfNull(options);

        // Parse the ref range
        var parts = refRange.Split("..", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid ref range format: '{refRange}'. Expected format: 'ref1..ref2'", nameof(refRange));
        }

        var oldRef = parts[0];
        var newRef = parts[1];

        using var repo = new Repository(repositoryPath);

        // Resolve references to commits
        var oldCommit = ResolveCommit(repo, oldRef);
        var newCommit = ResolveCommit(repo, newRef);

        if (oldCommit == null)
        {
            throw new InvalidOperationException($"Could not resolve reference '{oldRef}' to a commit.");
        }

        if (newCommit == null)
        {
            throw new InvalidOperationException($"Could not resolve reference '{newRef}' to a commit.");
        }

        // Get the tree diff between commits
        var oldTree = oldCommit.Tree;
        var newTree = newCommit.Tree;

        var compareOptions = new CompareOptions
        {
            Similarity = SimilarityOptions.Renames
        };

        var treeDiff = repo.Diff.Compare<TreeChanges>(oldTree, newTree, compareOptions);

        // Process all file changes
        var fileDiffs = ProcessChanges(repo, treeDiff, oldTree, newTree, options);

        // Calculate summary statistics
        var summary = CalculateSummary(fileDiffs);

        var metadata = new MultiFileMetadata
        {
            Mode = "git",
            GitRefRange = refRange,
            Timestamp = DateTime.UtcNow
        };

        return new MultiFileDiffResult
        {
            Files = fileDiffs,
            Summary = summary,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Compares files between two git references in parallel for better performance.
    /// </summary>
    /// <param name="repositoryPath">Path to the git repository.</param>
    /// <param name="refRange">Reference range (e.g., "main..feature", "abc123..def456").</param>
    /// <param name="options">Options for diff comparison.</param>
    /// <returns>A multi-file diff result containing all file changes.</returns>
    public MultiFileDiffResult CompareParallel(string repositoryPath, string refRange, DiffOptions options)
    {
        ArgumentNullException.ThrowIfNull(repositoryPath);
        ArgumentNullException.ThrowIfNull(refRange);
        ArgumentNullException.ThrowIfNull(options);

        // Parse the ref range
        var parts = refRange.Split("..", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid ref range format: '{refRange}'. Expected format: 'ref1..ref2'", nameof(refRange));
        }

        var oldRef = parts[0];
        var newRef = parts[1];

        using var repo = new Repository(repositoryPath);

        // Resolve references to commits
        var oldCommit = ResolveCommit(repo, oldRef);
        var newCommit = ResolveCommit(repo, newRef);

        if (oldCommit == null)
        {
            throw new InvalidOperationException($"Could not resolve reference '{oldRef}' to a commit.");
        }

        if (newCommit == null)
        {
            throw new InvalidOperationException($"Could not resolve reference '{newRef}' to a commit.");
        }

        // Get the tree diff between commits
        var oldTree = oldCommit.Tree;
        var newTree = newCommit.Tree;

        var compareOptions = new CompareOptions
        {
            Similarity = SimilarityOptions.Renames
        };

        var treeDiff = repo.Diff.Compare<TreeChanges>(oldTree, newTree, compareOptions);

        // Process all file changes in parallel
        var fileDiffs = ProcessChangesParallel(repo, treeDiff, oldTree, newTree, options);

        // Calculate summary statistics
        var summary = CalculateSummary(fileDiffs);

        var metadata = new MultiFileMetadata
        {
            Mode = "git",
            GitRefRange = refRange,
            Timestamp = DateTime.UtcNow
        };

        return new MultiFileDiffResult
        {
            Files = fileDiffs,
            Summary = summary,
            Metadata = metadata
        };
    }

    private static Commit? ResolveCommit(Repository repo, string reference)
    {
        try
        {
            // Try to resolve as a direct commit SHA
            if (repo.Lookup<Commit>(reference) is Commit commit)
            {
                return commit;
            }

            // Try to resolve as a branch
            if (repo.Branches[reference] is Branch branch)
            {
                return branch.Tip;
            }

            // Try to resolve as a tag
            if (repo.Tags[reference] is Tag tag)
            {
                return tag.Target as Commit ?? (tag.Target as TagAnnotation)?.Target as Commit;
            }

            // Try to resolve as a reference
            if (repo.Refs[reference] is Reference gitRef)
            {
                return gitRef.ResolveToDirectReference()?.Target as Commit;
            }

            return null;
        }
        catch (LibGit2Sharp.InvalidSpecificationException)
        {
            // Invalid reference name - return null to indicate reference not found
            return null;
        }
    }

    private IReadOnlyList<FileDiffResult> ProcessChanges(
        Repository repo,
        TreeChanges changes,
        Tree oldTree,
        Tree newTree,
        DiffOptions options)
    {
        var results = new List<FileDiffResult>();

        foreach (var change in changes)
        {
            var fileDiff = ProcessSingleChange(repo, change, oldTree, newTree, options);
            if (fileDiff != null)
            {
                results.Add(fileDiff);
            }
        }

        return results;
    }

    private IReadOnlyList<FileDiffResult> ProcessChangesParallel(
        Repository repo,
        TreeChanges changes,
        Tree oldTree,
        Tree newTree,
        DiffOptions options)
    {
        var results = new ConcurrentBag<FileDiffResult>();

        Parallel.ForEach(changes, change =>
        {
            var fileDiff = ProcessSingleChange(repo, change, oldTree, newTree, options);
            if (fileDiff != null)
            {
                results.Add(fileDiff);
            }
        });

        return results.OrderBy(f => f.NewPath ?? f.OldPath).ToList();
    }

    private FileDiffResult? ProcessSingleChange(
        Repository repo,
        TreeEntryChanges change,
        Tree oldTree,
        Tree newTree,
        DiffOptions options)
    {
        try
        {
            var status = MapChangeStatus(change.Status);

            // Check if file is binary before trying to get content
            var isBinaryFile = IsBinaryFile(change, repo);

            if (isBinaryFile)
            {
                return CreateBinaryFileDiff(change, status);
            }

            // Get file content
            var oldContent = change.Status != LibGit2Sharp.ChangeKind.Added
                ? GetBlobContent(change.OldOid, repo)
                : string.Empty;

            var newContent = change.Status != LibGit2Sharp.ChangeKind.Deleted
                ? GetBlobContent(change.Oid, repo)
                : string.Empty;

            // Determine file path for differ selection
            var filePath = change.Path ?? change.OldPath;

            // Get appropriate differ
            var differ = _differFactory.GetDiffer(filePath, options);

            // Create diff options with file paths
            var diffOptions = options with
            {
                OldPath = change.OldPath,
                NewPath = change.Path
            };

            // Perform diff
            var diffResult = differ.Compare(oldContent, newContent, diffOptions);

            return new FileDiffResult
            {
                Result = diffResult,
                Status = status,
                OldPath = change.Status == LibGit2Sharp.ChangeKind.Added ? null : change.OldPath,
                NewPath = change.Status == LibGit2Sharp.ChangeKind.Deleted ? null : change.Path
            };
        }
        catch (Exception ex)
        {
            // Log error and return null to skip this file
            Console.Error.WriteLine($"Error processing file '{change.Path ?? change.OldPath}': {ex.Message}");
            return null;
        }
    }

    private static bool IsBinaryFile(TreeEntryChanges change, Repository repo)
    {
        // Check the new file if it exists
        if (change.Status != LibGit2Sharp.ChangeKind.Deleted && change.Oid != null && change.Oid.Sha != ObjectId.Zero.Sha)
        {
            var blob = repo.Lookup<Blob>(change.Oid);
            if (blob?.IsBinary == true)
            {
                return true;
            }
        }

        // Check the old file if it exists
        if (change.Status != LibGit2Sharp.ChangeKind.Added && change.OldOid != null && change.OldOid.Sha != ObjectId.Zero.Sha)
        {
            var blob = repo.Lookup<Blob>(change.OldOid);
            if (blob?.IsBinary == true)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetBlobContent(ObjectId oid, Repository repo)
    {
        if (oid == null || oid.Sha == ObjectId.Zero.Sha)
        {
            return string.Empty;
        }

        var blob = repo.Lookup<Blob>(oid);
        if (blob == null)
        {
            return string.Empty;
        }

        // Check if binary
        if (blob.IsBinary)
        {
            return string.Empty;
        }

        return blob.GetContentText();
    }

    private static FileChangeStatus MapChangeStatus(LibGit2Sharp.ChangeKind changeKind)
    {
        return changeKind switch
        {
            LibGit2Sharp.ChangeKind.Added => FileChangeStatus.Added,
            LibGit2Sharp.ChangeKind.Deleted => FileChangeStatus.Removed,
            LibGit2Sharp.ChangeKind.Modified => FileChangeStatus.Modified,
            LibGit2Sharp.ChangeKind.Renamed => FileChangeStatus.Renamed,
            LibGit2Sharp.ChangeKind.Unmodified => FileChangeStatus.Unchanged,
            _ => FileChangeStatus.Modified
        };
    }

    private static FileDiffResult CreateBinaryFileDiff(TreeEntryChanges change, FileChangeStatus status)
    {
        // Create a minimal diff result for binary files
        var diffResult = new DiffResult
        {
            OldPath = change.OldPath,
            NewPath = change.Path,
            Mode = DiffMode.Line,
            FileChanges = []
        };

        return new FileDiffResult
        {
            Result = diffResult,
            Status = status,
            OldPath = change.OldPath,
            NewPath = change.Path
        };
    }

    private static MultiFileSummary CalculateSummary(IReadOnlyList<FileDiffResult> files)
    {
        var modifiedCount = files.Count(f => f.Status == FileChangeStatus.Modified);
        var addedCount = files.Count(f => f.Status == FileChangeStatus.Added);
        var removedCount = files.Count(f => f.Status == FileChangeStatus.Removed);
        var renamedCount = files.Count(f => f.Status == FileChangeStatus.Renamed);

        var totalChanges = files.Sum(f => f.Result.Stats.TotalChanges);

        var impactBreakdown = new ImpactBreakdown
        {
            BreakingPublicApi = files.Sum(f => f.Result.Stats.BreakingPublicApiCount),
            BreakingInternalApi = files.Sum(f => f.Result.Stats.BreakingInternalApiCount),
            NonBreaking = files.Sum(f => f.Result.Stats.NonBreakingCount),
            FormattingOnly = files.Sum(f => f.Result.Stats.FormattingOnlyCount)
        };

        return new MultiFileSummary
        {
            TotalFiles = files.Count,
            ModifiedFiles = modifiedCount,
            AddedFiles = addedCount,
            RemovedFiles = removedCount,
            RenamedFiles = renamedCount,
            TotalChanges = totalChanges,
            ImpactBreakdown = impactBreakdown
        };
    }
}
