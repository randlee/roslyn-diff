namespace RoslynDiff.Output;

using System.Text.Json;
using System.Text.Json.Serialization;
using RoslynDiff.Core.Models;

/// <summary>
/// Formats diff results as JSON output.
/// </summary>
public class JsonOutputFormatter : IOutputFormatter
{
    /// <inheritdoc/>
    public string FormatName => "json";

    /// <inheritdoc/>
    public string Format(DiffResult result, OutputOptions? options = null)
    {
        options ??= new OutputOptions();

        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = options.IndentJson,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(result, serializerOptions);
    }

    /// <inheritdoc/>
    public async Task FormatAsync(DiffResult result, TextWriter writer, OutputOptions? options = null)
    {
        var json = Format(result, options);
        await writer.WriteAsync(json);
    }
}
