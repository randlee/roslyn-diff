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
        /// Gets or sets the output format.
        /// </summary>
        [CommandOption("-o|--output <FORMAT>")]
        [Description("Output format: json, html, text, plain, or terminal")]
        [DefaultValue("text")]
        public string Output { get; set; } = "text";

        /// <summary>
        /// Gets or sets the output file path.
        /// </summary>
        [CommandOption("-f|--out-file <PATH>")]
        [Description("Output to file instead of stdout")]
        public string? OutFile { get; set; }
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Parse class specifications
            var (oldFilePath, oldClassName) = ClassSpecParser.Parse(settings.OldSpec);
            var (newFilePath, newClassName) = ClassSpecParser.Parse(settings.NewSpec);

            // Validate files exist
            if (!File.Exists(oldFilePath))
            {
                AnsiConsole.MarkupLine($"[red]Error: Old file not found: {oldFilePath}[/]");
                return 1;
            }

            if (!File.Exists(newFilePath))
            {
                AnsiConsole.MarkupLine($"[red]Error: New file not found: {newFilePath}[/]");
                return 1;
            }

            // Parse and validate match strategy
            var matchStrategy = ParseMatchStrategy(settings.MatchBy);

            // Validate similarity threshold
            if (settings.Similarity < 0.0 || settings.Similarity > 1.0)
            {
                AnsiConsole.MarkupLine("[red]Error: Similarity threshold must be between 0.0 and 1.0[/]");
                return 1;
            }

            // Validate interface matching
            if (matchStrategy == ClassMatchStrategy.Interface && string.IsNullOrWhiteSpace(settings.InterfaceName))
            {
                AnsiConsole.MarkupLine("[red]Error: Interface name is required when using interface matching strategy[/]");
                return 1;
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
                return 1;
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
                        return 1;
                    }

                    matchedNewClass = matchResult.NewClass;
                }
                else
                {
                    AnsiConsole.MarkupLine("[red]Error: Class not specified in new file specification[/]");
                    return 1;
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

            // Format output
            var formatterFactory = new OutputFormatterFactory();
            if (!formatterFactory.IsFormatSupported(settings.Output))
            {
                AnsiConsole.MarkupLine($"[red]Error: Unknown output format: {settings.Output}. Supported formats: json, html, text, plain, terminal[/]");
                return 1;
            }

            var formatter = formatterFactory.GetFormatter(settings.Output);

            var outputOptions = new OutputOptions
            {
                UseColor = string.IsNullOrEmpty(settings.OutFile), // Color for stdout only
                PrettyPrint = true
            };

            var formattedOutput = formatter.FormatResult(result, outputOptions);

            // Write output
            if (!string.IsNullOrEmpty(settings.OutFile))
            {
                await File.WriteAllTextAsync(settings.OutFile, formattedOutput);
                AnsiConsole.MarkupLine($"[green]Output written to: {settings.OutFile}[/]");
            }
            else
            {
                Console.WriteLine(formattedOutput);
            }

            return 0;
        }
        catch (ArgumentException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
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
            // Entire class is new
            changes.Add(new Change
            {
                Type = ChangeType.Added,
                Kind = ChangeKind.Class,
                Name = newClass.Identifier.Text,
                NewContent = newClass.ToFullString(),
                NewLocation = NodeMatcher.CreateLocation(newClass, newFilePath)
            });
        }
        else if (oldClass is not null && newClass is null)
        {
            // Entire class was removed
            changes.Add(new Change
            {
                Type = ChangeType.Removed,
                Kind = ChangeKind.Class,
                Name = oldClass.Identifier.Text,
                OldContent = oldClass.ToFullString(),
                OldLocation = NodeMatcher.CreateLocation(oldClass, oldFilePath)
            });
        }
        else if (oldClass is not null && newClass is not null)
        {
            // Compare class contents
            var oldNodes = nodeMatcher.ExtractStructuralNodes(oldClass);
            var newNodes = nodeMatcher.ExtractStructuralNodes(newClass);

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
            foreach (var (oldNode, newNode) in matchResult.MatchedPairs)
            {
                var oldText = oldNode.ToFullString();
                var newText = newNode.ToFullString();

                if (oldText != newText)
                {
                    var name = NodeMatcher.GetNodeName(oldNode) ?? "unknown";
                    changes.Add(new Change
                    {
                        Type = ChangeType.Modified,
                        Kind = NodeMatcher.GetChangeKind(oldNode),
                        Name = name,
                        OldContent = oldText,
                        NewContent = newText,
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
                    OldContent = removed.ToFullString(),
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
                    NewContent = added.ToFullString(),
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
