namespace RoslynDiff.Output;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using RoslynDiff.Core.Models;

/// <summary>
/// Formats diff results as AI-friendly JSON output with metadata and structured data.
/// </summary>
/// <remarks>
/// This formatter produces JSON with a clear structure designed for AI consumption:
/// <code>
/// {
///   "metadata": { version, timestamp, mode, options },
///   "summary": { totalChanges, additions, deletions, modifications, renames, moves },
///   "files": [ { oldPath, newPath, changes: [...] } ]
/// }
/// </code>
/// </remarks>
public class JsonFormatter : IOutputFormatter
{
    private static readonly string Version = Assembly.GetExecutingAssembly()
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? "1.0.0";

    /// <inheritdoc/>
    public string Format => "json";

    /// <inheritdoc/>
    public string ContentType => "application/json";

    /// <inheritdoc/>
    public string FormatResult(DiffResult result, OutputOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        options ??= new OutputOptions();

        var output = CreateOutputModel(result, options);
        var serializerOptions = CreateSerializerOptions(options);

        return JsonSerializer.Serialize(output, serializerOptions);
    }

    /// <inheritdoc/>
    public async Task FormatResultAsync(DiffResult result, TextWriter writer, OutputOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(writer);

        var json = FormatResult(result, options);
        await writer.WriteAsync(json);
    }

    private static JsonSerializerOptions CreateSerializerOptions(OutputOptions options)
    {
        return new JsonSerializerOptions
        {
            WriteIndented = options.PrettyPrint,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    private static JsonOutputModel CreateOutputModel(DiffResult result, OutputOptions options)
    {
        return new JsonOutputModel
        {
            Metadata = CreateMetadata(result, options),
            Summary = CreateSummary(result),
            Files = CreateFiles(result, options)
        };
    }

    private static JsonMetadata CreateMetadata(DiffResult result, OutputOptions options)
    {
        return new JsonMetadata
        {
            Version = Version,
            Timestamp = DateTimeOffset.UtcNow,
            Mode = result.Mode.ToString().ToLowerInvariant(),
            Options = new JsonMetadataOptions
            {
                IncludeContent = options.IncludeContent,
                ContextLines = options.ContextLines,
                IncludeNonImpactful = options.IncludeNonImpactful
            },
            TargetFrameworks = result.AnalyzedTfms
        };
    }

    private static JsonSummary CreateSummary(DiffResult result)
    {
        var stats = result.Stats;
        var impactBreakdown = ComputeImpactBreakdown(result);

        return new JsonSummary
        {
            TotalChanges = stats.TotalChanges,
            Additions = stats.Additions,
            Deletions = stats.Deletions,
            Modifications = stats.Modifications,
            Renames = stats.Renames,
            Moves = stats.Moves,
            ImpactBreakdown = impactBreakdown
        };
    }

    private static JsonImpactBreakdown ComputeImpactBreakdown(DiffResult result)
    {
        var allChanges = result.FileChanges.SelectMany(fc => GetAllChangesRecursive(fc.Changes)).ToList();

        return new JsonImpactBreakdown
        {
            BreakingPublicApi = allChanges.Count(c => c.Impact == ChangeImpact.BreakingPublicApi),
            BreakingInternalApi = allChanges.Count(c => c.Impact == ChangeImpact.BreakingInternalApi),
            NonBreaking = allChanges.Count(c => c.Impact == ChangeImpact.NonBreaking),
            FormattingOnly = allChanges.Count(c => c.Impact == ChangeImpact.FormattingOnly)
        };
    }

    private static IEnumerable<Change> GetAllChangesRecursive(IEnumerable<Change> changes)
    {
        foreach (var change in changes)
        {
            yield return change;
            if (change.Children != null)
            {
                foreach (var child in GetAllChangesRecursive(change.Children))
                {
                    yield return child;
                }
            }
        }
    }

    private static List<JsonFileChange> CreateFiles(DiffResult result, OutputOptions options)
    {
        var files = new List<JsonFileChange>();

        // If there's a single file comparison (OldPath/NewPath set)
        if (!string.IsNullOrEmpty(result.OldPath) || !string.IsNullOrEmpty(result.NewPath))
        {
            var allChanges = result.FileChanges.SelectMany(fc => fc.Changes).ToList();
            var filteredChanges = FilterChanges(allChanges, options);
            files.Add(new JsonFileChange
            {
                OldPath = result.OldPath,
                NewPath = result.NewPath,
                Changes = filteredChanges.Select(c => CreateChange(c, options)).ToList()
            });
        }
        else
        {
            // Multiple file changes
            foreach (var fileChange in result.FileChanges)
            {
                var filteredChanges = FilterChanges(fileChange.Changes, options);
                files.Add(new JsonFileChange
                {
                    OldPath = fileChange.Path,
                    NewPath = fileChange.Path,
                    Changes = filteredChanges.Select(c => CreateChange(c, options)).ToList()
                });
            }
        }

        return files;
    }

    private static IEnumerable<Change> FilterChanges(IEnumerable<Change> changes, OutputOptions options)
    {
        if (options.IncludeNonImpactful)
        {
            return changes;
        }

        return changes.Where(IsImpactful);
    }

    private static JsonChange CreateChange(Change change, OutputOptions options)
    {
        // Filter children based on IncludeNonImpactful option
        var filteredChildren = change.Children?.Count > 0
            ? change.Children
                .Where(c => options.IncludeNonImpactful || IsImpactful(c))
                .Select(c => CreateChange(c, options))
                .ToList()
            : null;

        // Convert whitespace issues to list of names
        List<string>? whitespaceIssueNames = null;
        if (change.WhitespaceIssues != WhitespaceIssue.None)
        {
            whitespaceIssueNames = GetWhitespaceIssueNames(change.WhitespaceIssues).ToList();
        }

        return new JsonChange
        {
            Type = change.Type.ToString().ToLowerInvariant(),
            Kind = change.Kind.ToString().ToLowerInvariant(),
            Name = change.Name,
            Impact = ConvertImpactToCamelCase(change.Impact),
            Visibility = change.Visibility?.ToString().ToLowerInvariant(),
            Caveats = change.Caveats?.Count > 0 ? change.Caveats.ToList() : null,
            WhitespaceIssues = whitespaceIssueNames,
            Location = (change.NewLocation ?? change.OldLocation) is not null
                ? CreateLocation((change.NewLocation ?? change.OldLocation)!)
                : null,
            OldLocation = change.OldLocation is not null && change.NewLocation is not null
                ? CreateLocation(change.OldLocation)
                : null,
            Content = options.IncludeContent ? (change.NewContent ?? change.OldContent) : null,
            OldContent = options.IncludeContent && change.Type == ChangeType.Modified ? change.OldContent : null,
            Children = filteredChildren?.Count > 0 ? filteredChildren : null,
            ApplicableToTfms = change.ApplicableToTfms?.Count > 0 ? change.ApplicableToTfms.ToList() : null
        };
    }

    private static string ConvertImpactToCamelCase(ChangeImpact impact)
    {
        return impact switch
        {
            ChangeImpact.BreakingPublicApi => "breakingPublicApi",
            ChangeImpact.BreakingInternalApi => "breakingInternalApi",
            ChangeImpact.NonBreaking => "nonBreaking",
            ChangeImpact.FormattingOnly => "formattingOnly",
            _ => impact.ToString().ToLowerInvariant()
        };
    }

    private static bool IsImpactful(Change change)
    {
        return change.Impact != ChangeImpact.NonBreaking && change.Impact != ChangeImpact.FormattingOnly;
    }

    private static JsonLocation CreateLocation(Location location)
    {
        return new JsonLocation
        {
            StartLine = location.StartLine,
            EndLine = location.EndLine,
            StartColumn = location.StartColumn,
            EndColumn = location.EndColumn
        };
    }

    /// <summary>
    /// Converts WhitespaceIssue flags to a list of issue names.
    /// </summary>
    private static IEnumerable<string> GetWhitespaceIssueNames(WhitespaceIssue issues)
    {
        if (issues.HasFlag(WhitespaceIssue.IndentationChanged))
            yield return "IndentationChanged";
        if (issues.HasFlag(WhitespaceIssue.MixedTabsSpaces))
            yield return "MixedTabsSpaces";
        if (issues.HasFlag(WhitespaceIssue.TrailingWhitespace))
            yield return "TrailingWhitespace";
        if (issues.HasFlag(WhitespaceIssue.LineEndingChanged))
            yield return "LineEndingChanged";
        if (issues.HasFlag(WhitespaceIssue.AmbiguousTabWidth))
            yield return "AmbiguousTabWidth";
    }

    #region JSON Output Models

    /// <summary>
    /// Root output model for JSON serialization.
    /// </summary>
    internal sealed class JsonOutputModel
    {
        [JsonPropertyName("$schema")]
        public string Schema { get; init; } = "roslyn-diff-output-v2";

        public required JsonMetadata Metadata { get; init; }
        public required JsonSummary Summary { get; init; }
        public required List<JsonFileChange> Files { get; init; }
    }

    /// <summary>
    /// Metadata section of the JSON output.
    /// </summary>
    internal sealed class JsonMetadata
    {
        public required string Version { get; init; }
        public required DateTimeOffset Timestamp { get; init; }
        public required string Mode { get; init; }
        public required JsonMetadataOptions Options { get; init; }

        /// <summary>
        /// Gets the Target Framework Monikers (TFMs) that were analyzed during this diff operation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property indicates which target frameworks were analyzed when comparing multi-targeted projects.
        /// The value interpretation is as follows:
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// <c>null</c> - No TFM analysis was performed. This field will be omitted from the JSON output
        /// for backward compatibility. This is the default for projects that are not multi-targeted or
        /// when TFM analysis is not requested.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// List with values - The specified TFMs were analyzed during the diff operation. For example,
        /// <c>["net8.0", "net10.0"]</c> indicates both .NET 8.0 and .NET 10.0 were analyzed.
        /// Individual changes may specify which TFMs they apply to via
        /// <see cref="JsonChange.ApplicableToTfms"/>.
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<string>? TargetFrameworks { get; init; }
    }

    /// <summary>
    /// Options metadata in the JSON output.
    /// </summary>
    internal sealed class JsonMetadataOptions
    {
        public bool IncludeContent { get; init; }
        public int ContextLines { get; init; }
        public bool IncludeNonImpactful { get; init; }
    }

    /// <summary>
    /// Summary section of the JSON output.
    /// </summary>
    internal sealed class JsonSummary
    {
        public int TotalChanges { get; init; }
        public int Additions { get; init; }
        public int Deletions { get; init; }
        public int Modifications { get; init; }
        public int Renames { get; init; }
        public int Moves { get; init; }
        public JsonImpactBreakdown? ImpactBreakdown { get; init; }
    }

    /// <summary>
    /// Impact breakdown in the JSON output summary.
    /// </summary>
    internal sealed class JsonImpactBreakdown
    {
        public int BreakingPublicApi { get; init; }
        public int BreakingInternalApi { get; init; }
        public int NonBreaking { get; init; }
        public int FormattingOnly { get; init; }
    }

    /// <summary>
    /// File change in the JSON output.
    /// </summary>
    internal sealed class JsonFileChange
    {
        public string? OldPath { get; init; }
        public string? NewPath { get; init; }
        public required List<JsonChange> Changes { get; init; }
    }

    /// <summary>
    /// Individual change in the JSON output.
    /// </summary>
    internal sealed class JsonChange
    {
        public required string Type { get; init; }
        public required string Kind { get; init; }
        public string? Name { get; init; }
        public required string Impact { get; init; }
        public string? Visibility { get; init; }
        public List<string>? Caveats { get; init; }
        public List<string>? WhitespaceIssues { get; init; }
        public JsonLocation? Location { get; init; }
        public JsonLocation? OldLocation { get; init; }
        public string? Content { get; init; }
        public string? OldContent { get; init; }
        public List<JsonChange>? Children { get; init; }

        /// <summary>
        /// Gets the Target Framework Monikers (TFMs) to which this change applies.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property indicates the specific TFMs where this change is applicable when analyzing
        /// multi-targeted projects. The value interpretation is as follows:
        /// </para>
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// <c>null</c> - No TFM analysis was performed. This field will be omitted from the JSON output
        /// for backward compatibility. This is the default for projects that are not multi-targeted or
        /// when TFM analysis is not requested.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// Empty list - The change applies to all analyzed TFMs. This indicates the change is
        /// common across all target frameworks. This field will be omitted from the JSON output
        /// when the list is empty.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// List with values - The change applies only to the specified TFMs. For example,
        /// <c>["net8.0", "net10.0"]</c> indicates the change exists only in those specific frameworks.
        /// This occurs when conditional compilation or framework-specific APIs cause changes to appear
        /// in some TFMs but not others.
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<string>? ApplicableToTfms { get; init; }
    }

    /// <summary>
    /// Location information in the JSON output.
    /// </summary>
    internal sealed class JsonLocation
    {
        public int StartLine { get; init; }
        public int EndLine { get; init; }
        public int StartColumn { get; init; }
        public int EndColumn { get; init; }
    }

    #endregion
}
