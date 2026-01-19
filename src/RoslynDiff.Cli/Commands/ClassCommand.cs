namespace RoslynDiff.Cli.Commands;

using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Matching;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;
using Spectre.Console;
using Spectre.Console.Cli;

/// <summary>
/// Command for comparing specific classes between two files.
/// </summary>
public sealed class ClassCommand : AsyncCommand<ClassCommand.Settings>
{
    private readonly OutputOrchestrator _outputOrchestrator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClassCommand"/> class.
    /// </summary>
    public ClassCommand()
    {
        _outputOrchestrator = new OutputOrchestrator();
    }

    /// <summary>
    /// Settings for the class command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the old file specification (file.cs:ClassName or file.cs).
        /// </summary>
        [CommandArgument(0, "<OLD-SPEC>")]
        [Description("Old file specification (file.cs:ClassName or file.cs)")]
        public string OldSpec { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the new file specification (file.cs:ClassName or file.cs).
        /// </summary>
        [CommandArgument(1, "<NEW-SPEC>")]
        [Description("New file specification (file.cs:ClassName or file.cs)")]
        public string NewSpec { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the matching strategy.
        /// </summary>
        [CommandOption("-m|--match-by <STRATEGY>")]
        [Description("Matching strategy: exact, interface, similarity, or auto")]
        [DefaultValue("auto")]
        public string MatchBy { get; set; } = "auto";

        /// <summary>
        /// Gets or sets the interface name for interface matching strategy.
        /// </summary>
        [CommandOption("-i|--interface <NAME>")]
        [Description("Interface name for interface matching strategy")]
        public string? InterfaceName { get; set; }

        /// <summary>
        /// Gets or sets the similarity threshold.
        /// </summary>
        [CommandOption("-s|--similarity <THRESHOLD>")]
        [Description("Similarity threshold for similarity matching (0.0-1.0)")]
        [DefaultValue(0.8)]
        public double Similarity { get; set; } = 0.8;

        /// <summary>
        /// Gets or sets the JSON output option.
        /// </summary>
        /// <remarks>
        /// When specified without a value, outputs JSON to stdout.
        /// When specified with a path, writes JSON to the file.
        /// </remarks>
        [CommandOption("--json [path]")]
        [Description("JSON output (stdout if no file specified)")]
        public FlagValue<string>? JsonOutput { get; init; }

        /// <summary>
        /// Gets or sets the HTML output file path.
        /// </summary>
        /// <remarks>
        /// HTML output requires a file path to be specified.
        /// </remarks>
        [CommandOption("--html <path>")]
        [Description("HTML report to file (required: file path)")]
        public string? HtmlOutput { get; init; }

        /// <summary>
        /// Gets or sets the plain text output option.
        /// </summary>
        /// <remarks>
        /// When specified without a value, outputs plain text to stdout.
        /// When specified with a path, writes plain text to the file.
        /// </remarks>
        [CommandOption("--text [path]")]
        [Description("Plain text diff (stdout if no file specified)")]
        public FlagValue<string>? TextOutput { get; init; }

        /// <summary>
        /// Gets or sets the Git-style unified diff output option.
        /// </summary>
        /// <remarks>
        /// When specified without a value, outputs unified diff to stdout.
        /// When specified with a path, writes unified diff to the file.
        /// </remarks>
        [CommandOption("--git [path]")]
        [Description("Git-style unified diff (stdout if no file specified)")]
        public FlagValue<string>? GitOutput { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether to open HTML output in the default browser.
        /// </summary>
        /// <remarks>
        /// This option is only valid when --html is specified.
        /// </remarks>
        [CommandOption("--open")]
        [Description("Open HTML in default browser after generation")]
        [DefaultValue(false)]
        public bool OpenInBrowser { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether to suppress default console output.
        /// </summary>
        [CommandOption("--quiet")]
        [Description("Suppress default console output")]
        [DefaultValue(false)]
        public bool Quiet { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether to disable colored output.
        /// </summary>
        [CommandOption("--no-color")]
        [Description("Disable colored output")]
        [DefaultValue(false)]
        public bool NoColor { get; init; }

        /// <inheritdoc/>
        public override ValidationResult Validate()
        {
            // --open is only valid when --html is specified
            if (OpenInBrowser && string.IsNullOrEmpty(HtmlOutput))
            {
                return ValidationResult.Error("--open requires --html to be specified");
            }

            // --html requires a file path
            if (HtmlOutput is not null && string.IsNullOrWhiteSpace(HtmlOutput))
            {
                return ValidationResult.Error("--html requires a file path");
            }

            return ValidationResult.Success();
        }
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Parse class specifications and convert to absolute paths
            var (oldFilePathRaw, oldClassName) = ClassSpecParser.Parse(settings.OldSpec);
            var (newFilePathRaw, newClassName) = ClassSpecParser.Parse(settings.NewSpec);
            var oldFilePath = Path.GetFullPath(oldFilePathRaw);
            var newFilePath = Path.GetFullPath(newFilePathRaw);

            // Validate files exist
            if (!File.Exists(oldFilePath))
            {
                AnsiConsole.MarkupLine($"[red]Error: Old file not found: {oldFilePath}[/]");
                return OutputOrchestrator.ExitCodeError;
            }

            if (!File.Exists(newFilePath))
            {
                AnsiConsole.MarkupLine($"[red]Error: New file not found: {newFilePath}[/]");
                return OutputOrchestrator.ExitCodeError;
            }

            // Parse and validate match strategy
            var matchStrategy = ParseMatchStrategy(settings.MatchBy);

            // Validate similarity threshold
            if (settings.Similarity < 0.0 || settings.Similarity > 1.0)
            {
                AnsiConsole.MarkupLine("[red]Error: Similarity threshold must be between 0.0 and 1.0[/]");
                return OutputOrchestrator.ExitCodeError;
            }

            // Validate interface matching
            if (matchStrategy == ClassMatchStrategy.Interface && string.IsNullOrWhiteSpace(settings.InterfaceName))
            {
                AnsiConsole.MarkupLine("[red]Error: Interface name is required when using interface matching strategy[/]");
                return OutputOrchestrator.ExitCodeError;
            }

            // Read file contents
            var oldContent = await File.ReadAllTextAsync(oldFilePath);
            var newContent = await File.ReadAllTextAsync(newFilePath);

            // Parse syntax trees
            var oldTree = CSharpSyntaxTree.ParseText(oldContent, path: oldFilePath);
            var newTree = CSharpSyntaxTree.ParseText(newContent, path: newFilePath);

            var oldRoot = await oldTree.GetRootAsync();
            var newRoot = await newTree.GetRootAsync();

            // Find classes
            var oldClass = FindClass(oldRoot, oldClassName);
            var newClass = FindClass(newRoot, newClassName);

            // Handle class matching based on strategy
            TypeDeclarationSyntax? matchedOldClass = oldClass;
            TypeDeclarationSyntax? matchedNewClass = newClass;

            if (oldClass is null && oldClassName is not null)
            {
                AnsiConsole.MarkupLine($"[red]Error: Class '{oldClassName}' not found in {oldFilePath}[/]");
                return OutputOrchestrator.ExitCodeError;
            }

            if (newClass is null && newClassName is not null)
            {
                // A specific class was requested in new spec but not found
                if (oldClass is not null)
                {
                    // Try to find a match using the specified strategy
                    var matcherOptions = new ClassMatchOptions
                    {
                        Strategy = matchStrategy,
                        InterfaceName = settings.InterfaceName,
                        SimilarityThreshold = settings.Similarity
                    };

                    var matcher = new ClassMatcher();
                    var matchResult = matcher.FindMatch(oldClass, newTree, matcherOptions);

                    if (matchResult is null)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: No matching class found in {newFilePath} using {settings.MatchBy} strategy[/]");
                        return OutputOrchestrator.ExitCodeError;
                    }

                    matchedNewClass = matchResult.NewClass;
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Error: Class not specified in new file specification[/]");
                    return OutputOrchestrator.ExitCodeError;
                }
            }
            else if (newClass is null && oldClass is not null && newClassName is null)
            {
                // No specific class name in new spec - find matching class using strategy
                var matcherOptions = new ClassMatchOptions
                {
                    Strategy = matchStrategy,
                    InterfaceName = settings.InterfaceName,
                    SimilarityThreshold = settings.Similarity
                };

                var matcher = new ClassMatcher();
                var matchResult = matcher.FindMatch(oldClass, newTree, matcherOptions);

                if (matchResult is not null)
                {
                    matchedNewClass = matchResult.NewClass;
                }
            }

            // Perform comparison
            var result = CompareClasses(matchedOldClass, matchedNewClass, oldFilePath, newFilePath);

            // Create output settings from command settings
            var outputSettings = new OutputSettings
            {
                JsonOutput = settings.JsonOutput?.IsSet == true ? (settings.JsonOutput.Value ?? "") : null,
                HtmlOutput = settings.HtmlOutput,
                TextOutput = settings.TextOutput?.IsSet == true ? (settings.TextOutput.Value ?? "") : null,
                GitOutput = settings.GitOutput?.IsSet == true ? (settings.GitOutput.Value ?? "") : null,
                OpenInBrowser = settings.OpenInBrowser,
                Quiet = settings.Quiet,
                NoColor = settings.NoColor
            };

            // Use OutputOrchestrator to handle all output logic
            return await _outputOrchestrator.WriteOutputsAsync(result, outputSettings, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return OutputOrchestrator.ExitCodeError;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return OutputOrchestrator.ExitCodeError;
        }
    }

    private static ClassMatchStrategy ParseMatchStrategy(string strategy)
    {
        return strategy.ToLowerInvariant() switch
        {
            "exact" => ClassMatchStrategy.ExactName,
            "interface" => ClassMatchStrategy.Interface,
            "similarity" => ClassMatchStrategy.Similarity,
            "auto" => ClassMatchStrategy.Auto,
            _ => throw new ArgumentException($"Unknown matching strategy: {strategy}. Valid options: exact, interface, similarity, auto")
        };
    }

    private static TypeDeclarationSyntax? FindClass(SyntaxNode root, string? className)
    {
        var classes = root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .ToList();

        if (string.IsNullOrWhiteSpace(className))
        {
            // Return the first class if no class name specified
            return classes.FirstOrDefault();
        }

        return classes.FirstOrDefault(c =>
            c.Identifier.Text.Equals(className, StringComparison.Ordinal));
    }

    private static DiffResult CompareClasses(
        TypeDeclarationSyntax? oldClass,
        TypeDeclarationSyntax? newClass,
        string oldFilePath,
        string newFilePath)
    {
        var changes = new List<Change>();
        var nodeMatcher = new NodeMatcher();

        if (oldClass is null && newClass is null)
        {
            // No classes to compare
            return new DiffResult
            {
                OldPath = oldFilePath,
                NewPath = newFilePath,
                Mode = DiffMode.Roslyn,
                FileChanges = [],
                Stats = new DiffStats()
            };
        }

        if (oldClass is null && newClass is not null)
        {
            // Entire class is new - use NormalizeWhitespace() for consistent formatting
            changes.Add(new Change
            {
                Type = ChangeType.Added,
                Kind = ChangeKind.Class,
                Name = newClass.Identifier.Text,
                NewContent = newClass.NormalizeWhitespace().ToString(),
                NewLocation = NodeMatcher.CreateLocation(newClass, newFilePath)
            });
        }
        else if (oldClass is not null && newClass is null)
        {
            // Entire class was removed - use NormalizeWhitespace() for consistent formatting
            changes.Add(new Change
            {
                Type = ChangeType.Removed,
                Kind = ChangeKind.Class,
                Name = oldClass.Identifier.Text,
                OldContent = oldClass.NormalizeWhitespace().ToString(),
                OldLocation = NodeMatcher.CreateLocation(oldClass, oldFilePath)
            });
        }
        else if (oldClass is not null && newClass is not null)
        {
            // Compare class contents - extract only MEMBER-level nodes (not the class itself)
            // to avoid duplicate reporting of both class and member changes.
            // When comparing two classes, we want to show individual member changes,
            // not the entire class as "Modified" alongside those member changes.
#pragma warning disable CS0618 // Type or member is obsolete
            var oldNodes = nodeMatcher.ExtractStructuralNodes(oldClass)
                .Where(n => n.Node != oldClass)  // Exclude the class itself
                .ToList();
            var newNodes = nodeMatcher.ExtractStructuralNodes(newClass)
                .Where(n => n.Node != newClass)  // Exclude the class itself
                .ToList();
#pragma warning restore CS0618 // Type or member is obsolete

            var matchResult = nodeMatcher.MatchNodes(oldNodes, newNodes);

            // Check if class itself was renamed
            if (oldClass.Identifier.Text != newClass.Identifier.Text)
            {
                changes.Add(new Change
                {
                    Type = ChangeType.Renamed,
                    Kind = ChangeKind.Class,
                    Name = newClass.Identifier.Text,
                    OldName = oldClass.Identifier.Text,
                    OldContent = oldClass.Identifier.Text,
                    NewContent = newClass.Identifier.Text,
                    OldLocation = NodeMatcher.CreateLocation(oldClass, oldFilePath),
                    NewLocation = NodeMatcher.CreateLocation(newClass, newFilePath)
                });
            }

            // Process matched pairs for modifications
            // Use NormalizeWhitespace() for consistent formatting in diff output
            foreach (var (oldNode, newNode) in matchResult.MatchedPairs)
            {
                // Compare normalized text to detect semantic changes (ignoring whitespace differences)
                var oldNormalized = oldNode.NormalizeWhitespace().ToString();
                var newNormalized = newNode.NormalizeWhitespace().ToString();

                if (oldNormalized != newNormalized)
                {
                    var name = NodeMatcher.GetNodeName(oldNode) ?? "unknown";
                    changes.Add(new Change
                    {
                        Type = ChangeType.Modified,
                        Kind = NodeMatcher.GetChangeKind(oldNode),
                        Name = name,
                        OldContent = oldNormalized,
                        NewContent = newNormalized,
                        OldLocation = NodeMatcher.CreateLocation(oldNode, oldFilePath),
                        NewLocation = NodeMatcher.CreateLocation(newNode, newFilePath)
                    });
                }
            }

            // Process unmatched old nodes (removals)
            foreach (var removed in matchResult.UnmatchedOld)
            {
                var name = NodeMatcher.GetNodeName(removed) ?? "unknown";
                changes.Add(new Change
                {
                    Type = ChangeType.Removed,
                    Kind = NodeMatcher.GetChangeKind(removed),
                    Name = name,
                    OldContent = removed.NormalizeWhitespace().ToString(),
                    OldLocation = NodeMatcher.CreateLocation(removed, oldFilePath)
                });
            }

            // Process unmatched new nodes (additions)
            foreach (var added in matchResult.UnmatchedNew)
            {
                var name = NodeMatcher.GetNodeName(added) ?? "unknown";
                changes.Add(new Change
                {
                    Type = ChangeType.Added,
                    Kind = NodeMatcher.GetChangeKind(added),
                    Name = name,
                    NewContent = added.NormalizeWhitespace().ToString(),
                    NewLocation = NodeMatcher.CreateLocation(added, newFilePath)
                });
            }
        }

        // Calculate statistics
        var stats = new DiffStats
        {
            Additions = changes.Count(c => c.Type == ChangeType.Added),
            Deletions = changes.Count(c => c.Type == ChangeType.Removed),
            Modifications = changes.Count(c => c.Type == ChangeType.Modified),
            Renames = changes.Count(c => c.Type == ChangeType.Renamed),
            Moves = changes.Count(c => c.Type == ChangeType.Moved)
        };

        var fileChange = new FileChange
        {
            Path = newFilePath,
            Changes = changes
        };

        return new DiffResult
        {
            OldPath = oldFilePath,
            NewPath = newFilePath,
            Mode = DiffMode.Roslyn,
            FileChanges = [fileChange],
            Stats = stats
        };
    }
}
