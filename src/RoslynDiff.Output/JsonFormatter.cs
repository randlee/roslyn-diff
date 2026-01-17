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
            Summary = CreateSummary(result.Stats),
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
                ContextLines = options.ContextLines
            }
        };
    }

    private static JsonSummary CreateSummary(DiffStats stats)
    {
        return new JsonSummary
        {
            TotalChanges = stats.TotalChanges,
            Additions = stats.Additions,
            Deletions = stats.Deletions,
            Modifications = stats.Modifications,
            Renames = stats.Renames,
            Moves = stats.Moves
        };
    }

    private static List<JsonFileChange> CreateFiles(DiffResult result, OutputOptions options)
    {
        var files = new List<JsonFileChange>();

        // If there's a single file comparison (OldPath/NewPath set)
        if (!string.IsNullOrEmpty(result.OldPath) || !string.IsNullOrEmpty(result.NewPath))
        {
            var allChanges = result.FileChanges.SelectMany(fc => fc.Changes).ToList();
            files.Add(new JsonFileChange
            {
                OldPath = result.OldPath,
                NewPath = result.NewPath,
                Changes = allChanges.Select(c => CreateChange(c, options)).ToList()
            });
        }
        else
        {
            // Multiple file changes
            foreach (var fileChange in result.FileChanges)
            {
                files.Add(new JsonFileChange
                {
                    OldPath = fileChange.Path,
                    NewPath = fileChange.Path,
                    Changes = fileChange.Changes.Select(c => CreateChange(c, options)).ToList()
                });
            }
        }

        return files;
    }

    private static JsonChange CreateChange(Change change, OutputOptions options)
    {
        return new JsonChange
        {
            Type = change.Type.ToString().ToLowerInvariant(),
            Kind = change.Kind.ToString().ToLowerInvariant(),
            Name = change.Name,
            Location = (change.NewLocation ?? change.OldLocation) is not null
                ? CreateLocation((change.NewLocation ?? change.OldLocation)!)
                : null,
            OldLocation = change.OldLocation is not null && change.NewLocation is not null
                ? CreateLocation(change.OldLocation)
                : null,
            Content = options.IncludeContent ? (change.NewContent ?? change.OldContent) : null,
            OldContent = options.IncludeContent && change.Type == ChangeType.Modified ? change.OldContent : null,
            Children = change.Children?.Count > 0
                ? change.Children.Select(c => CreateChange(c, options)).ToList()
                : null
        };
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

    #region JSON Output Models

    /// <summary>
    /// Root output model for JSON serialization.
    /// </summary>
    internal sealed class JsonOutputModel
    {
        [JsonPropertyName("$schema")]
        public string Schema { get; init; } = "roslyn-diff-output-v1";

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
    }

    /// <summary>
    /// Options metadata in the JSON output.
    /// </summary>
    internal sealed class JsonMetadataOptions
    {
        public bool IncludeContent { get; init; }
        public int ContextLines { get; init; }
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
        public JsonLocation? Location { get; init; }
        public JsonLocation? OldLocation { get; init; }
        public string? Content { get; init; }
        public string? OldContent { get; init; }
        public List<JsonChange>? Children { get; init; }
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
