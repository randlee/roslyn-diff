namespace RoslynDiff.Core.MultiFile;

using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

/// <summary>
/// Compares files between two directory trees with support for glob patterns and recursive traversal.
/// </summary>
public sealed class FolderComparer
{
    private readonly DifferFactory _differFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="FolderComparer"/> class.
    /// </summary>
    public FolderComparer()
    {
        _differFactory = new DifferFactory();
    }

    /// <summary>
    /// Compares files between two folders.
    /// </summary>
    /// <param name="oldPath">Path to the old folder.</param>
    /// <param name="newPath">Path to the new folder.</param>
    /// <param name="options">Options for diff comparison.</param>
    /// <param name="folderOptions">Options for folder comparison (filtering, recursion).</param>
    /// <returns>A multi-file diff result containing all file changes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when either directory does not exist.</exception>
    public MultiFileDiffResult Compare(
        string oldPath,
        string newPath,
        DiffOptions options,
        FolderCompareOptions folderOptions)
    {
        ArgumentNullException.ThrowIfNull(oldPath);
        ArgumentNullException.ThrowIfNull(newPath);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(folderOptions);

        if (!Directory.Exists(oldPath))
        {
            throw new DirectoryNotFoundException($"Old directory not found: {oldPath}");
        }

        if (!Directory.Exists(newPath))
        {
            throw new DirectoryNotFoundException($"New directory not found: {newPath}");
        }

        // Normalize paths
        oldPath = Path.GetFullPath(oldPath);
        newPath = Path.GetFullPath(newPath);

        // Collect all files from both directories
        var oldFiles = CollectFiles(oldPath, folderOptions);
        var newFiles = CollectFiles(newPath, folderOptions);

        // Match files by relative path
        var filePairs = MatchFiles(oldFiles, newFiles, oldPath, newPath);

        // Process all file changes
        var fileDiffs = ProcessChanges(filePairs, oldPath, newPath, options);

        // Calculate summary statistics
        var summary = CalculateSummary(fileDiffs);

        var metadata = new MultiFileMetadata
        {
            Mode = "folder",
            OldRoot = oldPath,
            NewRoot = newPath,
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
    /// Compares files between two folders in parallel for better performance.
    /// </summary>
    /// <param name="oldPath">Path to the old folder.</param>
    /// <param name="newPath">Path to the new folder.</param>
    /// <param name="options">Options for diff comparison.</param>
    /// <param name="folderOptions">Options for folder comparison (filtering, recursion).</param>
    /// <returns>A multi-file diff result containing all file changes.</returns>
    public MultiFileDiffResult CompareParallel(
        string oldPath,
        string newPath,
        DiffOptions options,
        FolderCompareOptions folderOptions)
    {
        ArgumentNullException.ThrowIfNull(oldPath);
        ArgumentNullException.ThrowIfNull(newPath);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(folderOptions);

        if (!Directory.Exists(oldPath))
        {
            throw new DirectoryNotFoundException($"Old directory not found: {oldPath}");
        }

        if (!Directory.Exists(newPath))
        {
            throw new DirectoryNotFoundException($"New directory not found: {newPath}");
        }

        // Normalize paths
        oldPath = Path.GetFullPath(oldPath);
        newPath = Path.GetFullPath(newPath);

        // Collect all files from both directories
        var oldFiles = CollectFiles(oldPath, folderOptions);
        var newFiles = CollectFiles(newPath, folderOptions);

        // Match files by relative path
        var filePairs = MatchFiles(oldFiles, newFiles, oldPath, newPath);

        // Process all file changes in parallel
        var fileDiffs = ProcessChangesParallel(filePairs, oldPath, newPath, options);

        // Calculate summary statistics
        var summary = CalculateSummary(fileDiffs);

        var metadata = new MultiFileMetadata
        {
            Mode = "folder",
            OldRoot = oldPath,
            NewRoot = newPath,
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
    /// Collects all files from a directory that match the filter criteria.
    /// </summary>
    private HashSet<string> CollectFiles(string rootPath, FolderCompareOptions options)
    {
        var matchedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var searchOption = options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var allFiles = Directory.GetFiles(rootPath, "*", searchOption);

            foreach (var filePath in allFiles)
            {
                var relativePath = GetRelativePath(rootPath, filePath);

                // Apply include/exclude filters
                if (ShouldIncludeFile(relativePath, options))
                {
                    matchedFiles.Add(relativePath);
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            // Handle permission denied gracefully - log and continue with files we could access
            Console.Error.WriteLine($"Warning: Access denied to directory '{rootPath}': {ex.Message}");
        }
        catch (DirectoryNotFoundException)
        {
            // Directory was deleted or doesn't exist - return empty set
        }
        catch (IOException ex)
        {
            // Other I/O errors (e.g., network issues) - log and return what we have
            Console.Error.WriteLine($"Warning: I/O error accessing directory '{rootPath}': {ex.Message}");
        }

        return matchedFiles;
    }

    /// <summary>
    /// Determines if a file should be included based on include/exclude patterns.
    /// </summary>
    private bool ShouldIncludeFile(string relativePath, FolderCompareOptions options)
    {
        // If there are exclude patterns, check them first
        if (options.ExcludePatterns.Count > 0)
        {
            foreach (var pattern in options.ExcludePatterns)
            {
                if (MatchesGlobPattern(relativePath, pattern))
                {
                    return false;
                }
            }
        }

        // If there are include patterns, file must match at least one
        if (options.IncludePatterns.Count > 0)
        {
            foreach (var pattern in options.IncludePatterns)
            {
                if (MatchesGlobPattern(relativePath, pattern))
                {
                    return true;
                }
            }
            return false;
        }

        // No include patterns means include all (that aren't excluded)
        return true;
    }

    /// <summary>
    /// Matches a file path against a glob pattern.
    /// Supports * (single directory), ** (recursive), and ? (single character).
    /// </summary>
    private bool MatchesGlobPattern(string path, string pattern)
    {
        // Normalize path separators
        path = path.Replace('\\', '/');
        pattern = pattern.Replace('\\', '/');

        // Convert glob pattern to regex
        var regexPattern = ConvertGlobToRegex(pattern);

        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Converts a glob pattern to a regular expression.
    /// </summary>
    private string ConvertGlobToRegex(string pattern)
    {
        var regex = new System.Text.StringBuilder("^");
        var i = 0;

        while (i < pattern.Length)
        {
            var c = pattern[i];

            switch (c)
            {
                case '*':
                    // Check for **
                    if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                    {
                        // ** matches any number of directories
                        if (i + 2 < pattern.Length && pattern[i + 2] == '/')
                        {
                            regex.Append("(?:.*/)?");
                            i += 3;
                        }
                        else
                        {
                            regex.Append(".*");
                            i += 2;
                        }
                    }
                    else
                    {
                        // * matches anything except directory separator
                        regex.Append("[^/]*");
                        i++;
                    }
                    break;

                case '?':
                    // ? matches any single character except directory separator
                    regex.Append("[^/]");
                    i++;
                    break;

                case '[':
                    // Character class - find the closing bracket
                    var bracketEnd = pattern.IndexOf(']', i + 1);
                    if (bracketEnd == -1)
                    {
                        // No closing bracket, treat as literal
                        regex.Append("\\[");
                        i++;
                    }
                    else
                    {
                        // Extract the bracket content
                        var bracketContent = pattern.Substring(i + 1, bracketEnd - i - 1);
                        regex.Append('[');

                        // Handle negation: [!abc] -> [^abc]
                        if (bracketContent.StartsWith("!"))
                        {
                            regex.Append('^');
                            bracketContent = bracketContent.Substring(1);
                        }

                        regex.Append(bracketContent);
                        regex.Append(']');
                        i = bracketEnd + 1;
                    }
                    break;

                case '{':
                    // Brace expansion: {src,tests} -> (src|tests)
                    var braceEnd = pattern.IndexOf('}', i + 1);
                    if (braceEnd == -1)
                    {
                        // No closing brace, treat as literal
                        regex.Append("\\{");
                        i++;
                    }
                    else
                    {
                        var braceContent = pattern.Substring(i + 1, braceEnd - i - 1);
                        var alternatives = braceContent.Split(',');
                        regex.Append("(?:");
                        for (var j = 0; j < alternatives.Length; j++)
                        {
                            if (j > 0) regex.Append('|');
                            // Recursively convert each alternative (escape special chars)
                            foreach (var ch in alternatives[j])
                            {
                                if (ch == '.' || ch == '(' || ch == ')' || ch == '+' ||
                                    ch == '|' || ch == '^' || ch == '$' || ch == '\\')
                                {
                                    regex.Append('\\');
                                }
                                regex.Append(ch);
                            }
                        }
                        regex.Append(')');
                        i = braceEnd + 1;
                    }
                    break;

                case '.':
                case '(':
                case ')':
                case '+':
                case '|':
                case '^':
                case '$':
                case '@':
                case '%':
                case ']':
                case '}':
                case '\\':
                    // Escape regex special characters
                    regex.Append('\\');
                    regex.Append(c);
                    i++;
                    break;

                default:
                    regex.Append(c);
                    i++;
                    break;
            }
        }

        regex.Append('$');
        return regex.ToString();
    }

    /// <summary>
    /// Gets the relative path from a root directory to a file.
    /// </summary>
    private string GetRelativePath(string rootPath, string filePath)
    {
        var rootUri = new Uri(EnsureTrailingSlash(rootPath));
        var fileUri = new Uri(filePath);
        var relativeUri = rootUri.MakeRelativeUri(fileUri);
        return Uri.UnescapeDataString(relativeUri.ToString()).Replace('\\', '/');
    }

    /// <summary>
    /// Ensures a directory path has a trailing slash.
    /// </summary>
    private string EnsureTrailingSlash(string path)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar) && !path.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return path + Path.DirectorySeparatorChar;
        }
        return path;
    }

    /// <summary>
    /// Matches files from old and new directories by relative path.
    /// </summary>
    private List<FilePair> MatchFiles(
        HashSet<string> oldFiles,
        HashSet<string> newFiles,
        string oldRoot,
        string newRoot)
    {
        var pairs = new List<FilePair>();
        var processedNewFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Process files that exist in old directory
        foreach (var oldRelativePath in oldFiles)
        {
            // Find the matching file in newFiles with correct casing
            var newRelativePath = newFiles.FirstOrDefault(f =>
                string.Equals(f, oldRelativePath, StringComparison.OrdinalIgnoreCase));

            if (newRelativePath != null)
            {
                // File exists in both - potential modification
                // Use the actual filenames from each directory (preserve case)
                pairs.Add(new FilePair
                {
                    RelativePath = oldRelativePath,
                    OldFullPath = Path.Combine(oldRoot, oldRelativePath),
                    NewFullPath = Path.Combine(newRoot, newRelativePath),
                    Status = FileChangeStatus.Modified
                });
                processedNewFiles.Add(newRelativePath);
            }
            else
            {
                // File only in old - removed
                pairs.Add(new FilePair
                {
                    RelativePath = oldRelativePath,
                    OldFullPath = Path.Combine(oldRoot, oldRelativePath),
                    NewFullPath = null,
                    Status = FileChangeStatus.Removed
                });
            }
        }

        // Process files that only exist in new directory
        foreach (var newRelativePath in newFiles)
        {
            if (!processedNewFiles.Contains(newRelativePath))
            {
                // File only in new - added
                pairs.Add(new FilePair
                {
                    RelativePath = newRelativePath,
                    OldFullPath = null,
                    NewFullPath = Path.Combine(newRoot, newRelativePath),
                    Status = FileChangeStatus.Added
                });
            }
        }

        return pairs.OrderBy(p => p.RelativePath, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Processes all file changes sequentially.
    /// </summary>
    private IReadOnlyList<FileDiffResult> ProcessChanges(
        List<FilePair> filePairs,
        string oldRoot,
        string newRoot,
        DiffOptions options)
    {
        var results = new List<FileDiffResult>();

        foreach (var pair in filePairs)
        {
            var fileDiff = ProcessSingleFile(pair, options);
            if (fileDiff != null)
            {
                results.Add(fileDiff);
            }
        }

        return results;
    }

    /// <summary>
    /// Processes all file changes in parallel.
    /// </summary>
    private IReadOnlyList<FileDiffResult> ProcessChangesParallel(
        List<FilePair> filePairs,
        string oldRoot,
        string newRoot,
        DiffOptions options)
    {
        var results = new ConcurrentBag<FileDiffResult>();

        Parallel.ForEach(filePairs, pair =>
        {
            var fileDiff = ProcessSingleFile(pair, options);
            if (fileDiff != null)
            {
                results.Add(fileDiff);
            }
        });

        return results.OrderBy(f => f.NewPath ?? f.OldPath, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// Processes a single file pair.
    /// </summary>
    private FileDiffResult? ProcessSingleFile(FilePair pair, DiffOptions options)
    {
        try
        {
            string oldContent = string.Empty;
            string newContent = string.Empty;

            // Read file contents
            if (pair.Status != FileChangeStatus.Added && pair.OldFullPath != null)
            {
                oldContent = File.ReadAllText(pair.OldFullPath);
            }

            if (pair.Status != FileChangeStatus.Removed && pair.NewFullPath != null)
            {
                newContent = File.ReadAllText(pair.NewFullPath);
            }

            // For modified files, check if content actually changed
            if (pair.Status == FileChangeStatus.Modified && oldContent == newContent)
            {
                // No actual changes, skip this file
                return null;
            }

            // Get appropriate differ
            var filePath = pair.NewFullPath ?? pair.OldFullPath ?? pair.RelativePath;
            var differ = _differFactory.GetDiffer(filePath, options);

            // Create diff options with file paths
            var diffOptions = options with
            {
                OldPath = pair.OldFullPath,
                NewPath = pair.NewFullPath
            };

            // Perform diff
            var diffResult = differ.Compare(oldContent, newContent, diffOptions);

            return new FileDiffResult
            {
                Result = diffResult,
                Status = pair.Status,
                OldPath = pair.OldFullPath,
                NewPath = pair.NewFullPath
            };
        }
        catch (Exception ex)
        {
            // Log error and return null to skip this file
            Console.Error.WriteLine($"Error processing file '{pair.RelativePath}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Calculates summary statistics for all files.
    /// </summary>
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

    /// <summary>
    /// Represents a pair of old and new file paths.
    /// </summary>
    private class FilePair
    {
        public required string RelativePath { get; init; }
        public string? OldFullPath { get; init; }
        public string? NewFullPath { get; init; }
        public FileChangeStatus Status { get; init; }
    }
}

/// <summary>
/// Options for folder comparison.
/// </summary>
public record FolderCompareOptions
{
    /// <summary>
    /// Gets a value indicating whether to recursively traverse subdirectories.
    /// </summary>
    public bool Recursive { get; init; }

    /// <summary>
    /// Gets the list of glob patterns to include.
    /// If empty, all files are included (except those matching exclude patterns).
    /// </summary>
    /// <remarks>
    /// Examples: "*.cs", "**/*.g.cs", "src/**/*.cs"
    /// </remarks>
    public IReadOnlyList<string> IncludePatterns { get; init; } = [];

    /// <summary>
    /// Gets the list of glob patterns to exclude.
    /// Exclude patterns take precedence over include patterns.
    /// </summary>
    /// <remarks>
    /// Examples: "*.Designer.cs", "**/obj/**", "**/bin/**"
    /// </remarks>
    public IReadOnlyList<string> ExcludePatterns { get; init; } = [];
}
