namespace RoslynDiff.TestUtilities.Parsers;

using System.Text.RegularExpressions;

/// <summary>
/// Provides utilities for normalizing and stripping timestamps from diff outputs.
/// Useful for comparing outputs where only timestamps differ.
/// </summary>
public static class TimestampNormalizer
{
    // ISO 8601 timestamp pattern (e.g., 2026-01-17T02:59:00.611965+00:00)
    private static readonly Regex Iso8601Pattern = new(
        @"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})",
        RegexOptions.Compiled);

    // Common date-time patterns
    private static readonly Regex CommonDateTimePattern = new(
        @"\d{4}[-/]\d{2}[-/]\d{2}[T\s]\d{2}:\d{2}:\d{2}(?:\.\d+)?",
        RegexOptions.Compiled);

    // Unix timestamp pattern (10 digits for seconds, 13 for milliseconds)
    private static readonly Regex UnixTimestampPattern = new(
        @"\b\d{10,13}\b",
        RegexOptions.Compiled);

    // Relative timestamp patterns (e.g., "2 hours ago", "yesterday")
    private static readonly Regex RelativeTimestampPattern = new(
        @"\b(?:\d+\s+(?:second|minute|hour|day|week|month|year)s?\s+ago|yesterday|today|just now)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // JSON timestamp field pattern
    private static readonly Regex JsonTimestampFieldPattern = new(
        @"""timestamp""\s*:\s*""[^""]+""",
        RegexOptions.Compiled);

    /// <summary>
    /// Strips all recognized timestamp formats from the given content.
    /// </summary>
    /// <param name="content">The content to normalize.</param>
    /// <param name="replacement">The replacement text for timestamps. Defaults to "[TIMESTAMP]".</param>
    /// <returns>Content with timestamps replaced.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    public static string StripTimestamps(string content, string replacement = "[TIMESTAMP]")
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return content;
        }

        // Replace in order of specificity (most specific first)
        var result = content;

        // ISO 8601 timestamps
        result = Iso8601Pattern.Replace(result, replacement);

        // Common date-time patterns
        result = CommonDateTimePattern.Replace(result, replacement);

        // Relative timestamps
        result = RelativeTimestampPattern.Replace(result, replacement);

        return result;
    }

    /// <summary>
    /// Strips timestamps from JSON content, preserving JSON structure.
    /// </summary>
    /// <param name="jsonContent">The JSON content to normalize.</param>
    /// <param name="replacement">The replacement text for timestamp values. Defaults to "[TIMESTAMP]".</param>
    /// <returns>JSON content with timestamp values replaced.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jsonContent"/> is null.</exception>
    public static string StripJsonTimestamps(string jsonContent, string replacement = "[TIMESTAMP]")
    {
        if (jsonContent == null)
        {
            throw new ArgumentNullException(nameof(jsonContent));
        }

        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return jsonContent;
        }

        // Replace JSON timestamp fields
        var result = JsonTimestampFieldPattern.Replace(jsonContent, $@"""timestamp"": ""{replacement}""");

        return result;
    }

    /// <summary>
    /// Normalizes a DateTime value to a consistent format for comparison.
    /// </summary>
    /// <param name="timestamp">The timestamp to normalize.</param>
    /// <param name="useUtc">Whether to convert to UTC. Defaults to true.</param>
    /// <returns>A normalized timestamp string.</returns>
    public static string NormalizeDateTime(DateTime timestamp, bool useUtc = true)
    {
        var normalizedTime = useUtc ? timestamp.ToUniversalTime() : timestamp;
        return normalizedTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    /// <summary>
    /// Normalizes a timestamp string to a consistent format.
    /// </summary>
    /// <param name="timestampString">The timestamp string to normalize.</param>
    /// <param name="useUtc">Whether to convert to UTC. Defaults to true.</param>
    /// <returns>A normalized timestamp string, or the original string if parsing fails.</returns>
    public static string NormalizeTimestampString(string timestampString, bool useUtc = true)
    {
        if (string.IsNullOrWhiteSpace(timestampString))
        {
            return timestampString;
        }

        if (DateTime.TryParse(timestampString, out var timestamp))
        {
            return NormalizeDateTime(timestamp, useUtc);
        }

        return timestampString;
    }

    /// <summary>
    /// Checks if the given string contains a timestamp pattern.
    /// </summary>
    /// <param name="content">The content to check.</param>
    /// <returns>True if a timestamp pattern is found; otherwise, false.</returns>
    public static bool ContainsTimestamp(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        return Iso8601Pattern.IsMatch(content) ||
               CommonDateTimePattern.IsMatch(content) ||
               RelativeTimestampPattern.IsMatch(content) ||
               JsonTimestampFieldPattern.IsMatch(content);
    }

    /// <summary>
    /// Extracts all timestamps found in the content.
    /// </summary>
    /// <param name="content">The content to search.</param>
    /// <returns>A collection of timestamp strings found in the content.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="content"/> is null.</exception>
    public static IReadOnlyList<string> ExtractTimestamps(string content)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return Array.Empty<string>();
        }

        var timestamps = new List<string>();

        // Extract ISO 8601 timestamps
        var iso8601Matches = Iso8601Pattern.Matches(content);
        foreach (Match match in iso8601Matches)
        {
            timestamps.Add(match.Value);
        }

        // Extract common date-time patterns (if not already captured)
        var dateTimeMatches = CommonDateTimePattern.Matches(content);
        foreach (Match match in dateTimeMatches)
        {
            if (!timestamps.Contains(match.Value))
            {
                timestamps.Add(match.Value);
            }
        }

        // Extract relative timestamps
        var relativeMatches = RelativeTimestampPattern.Matches(content);
        foreach (Match match in relativeMatches)
        {
            if (!timestamps.Contains(match.Value))
            {
                timestamps.Add(match.Value);
            }
        }

        return timestamps;
    }

    /// <summary>
    /// Compares two timestamp strings, returning true if they represent the same time within a tolerance.
    /// </summary>
    /// <param name="timestamp1">The first timestamp string.</param>
    /// <param name="timestamp2">The second timestamp string.</param>
    /// <param name="toleranceSeconds">The tolerance in seconds for comparison. Defaults to 0.</param>
    /// <returns>True if the timestamps are equal within tolerance; otherwise, false.</returns>
    public static bool AreTimestampsEqual(string timestamp1, string timestamp2, int toleranceSeconds = 0)
    {
        if (timestamp1 == timestamp2)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(timestamp1) || string.IsNullOrWhiteSpace(timestamp2))
        {
            return false;
        }

        if (!DateTime.TryParse(timestamp1, out var dt1) || !DateTime.TryParse(timestamp2, out var dt2))
        {
            return false;
        }

        var difference = Math.Abs((dt1 - dt2).TotalSeconds);
        return difference <= toleranceSeconds;
    }
}
