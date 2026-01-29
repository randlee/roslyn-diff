namespace RoslynDiff.Output;

using System.Text.Json;
using System.Text.Json.Serialization;
using RoslynDiff.Core.Models;

/// <summary>
/// Formats diff results as simple JSON output (direct serialization of DiffResult).
/// </summary>
/// <remarks>
/// This is a simple formatter that directly serializes the <see cref="DiffResult"/> model.
/// For AI-friendly output with metadata and structured format, use <see cref="JsonFormatter"/> instead.
/// </remarks>
[Obsolete("Use JsonFormatter for AI-friendly JSON output with metadata. This formatter will be removed in a future version.")]
public class JsonOutputFormatter : IOutputFormatter
{
    /// <inheritdoc/>
    public string Format => "json-raw";

    /// <inheritdoc/>
    public string ContentType => "application/json";

    /// <summary>
    /// Gets the name of the output format.
    /// </summary>
    [Obsolete("Use Format property instead.")]
    public string FormatName => "json";

    /// <inheritdoc/>
    public string FormatResult(DiffResult result, OutputOptions? options = null)
    {
        options ??= new OutputOptions();

        var serializerOptions = new JsonSerializerOptions
        {
#pragma warning disable CS0618 // Type or member is obsolete
            WriteIndented = options.IndentJson,
#pragma warning restore CS0618
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(result, serializerOptions);
    }

    /// <inheritdoc/>
    public async Task FormatResultAsync(DiffResult result, TextWriter writer, OutputOptions? options = null)
    {
        var json = FormatResult(result, options);
        await writer.WriteAsync(json);
    }

    /// <inheritdoc/>
    public string FormatMultiFileResult(MultiFileDiffResult result, OutputOptions? options = null)
    {
        options ??= new OutputOptions();

        var serializerOptions = new JsonSerializerOptions
        {
#pragma warning disable CS0618 // Type or member is obsolete
            WriteIndented = options.IndentJson,
#pragma warning restore CS0618
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(result, serializerOptions);
    }

    /// <inheritdoc/>
    public async Task FormatMultiFileResultAsync(MultiFileDiffResult result, TextWriter writer, OutputOptions? options = null)
    {
        var json = FormatMultiFileResult(result, options);
        await writer.WriteAsync(json);
    }
}
