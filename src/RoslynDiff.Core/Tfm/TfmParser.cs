namespace RoslynDiff.Core.Tfm;

using System.Text.RegularExpressions;

/// <summary>
/// Provides parsing and validation functionality for Target Framework Monikers (TFMs).
/// </summary>
/// <remarks>
/// <para>
/// This class handles the validation and normalization of TFM strings to ensure they conform
/// to the expected format and naming conventions used by .NET. It supports parsing both single
/// TFMs and semicolon-separated lists of TFMs.
/// </para>
/// <para>
/// Supported TFM formats:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>.NET 5+ (modern): netX.0 where X >= 5 (e.g., net5.0, net6.0, net8.0, net10.0)</description>
/// </item>
/// <item>
/// <description>.NET Framework: netXY or netX.Y[.Z] where X &lt;= 4 (e.g., net462, net48, net4.8)</description>
/// </item>
/// <item>
/// <description>.NET Core: netcoreappX.Y (e.g., netcoreapp3.1, netcoreapp2.1)</description>
/// </item>
/// <item>
/// <description>.NET Standard: netstandardX.Y (e.g., netstandard2.0, netstandard2.1)</description>
/// </item>
/// </list>
/// <para>
/// All TFMs are normalized to lowercase during parsing to ensure consistent handling.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Parse a single TFM
/// var normalized = TfmParser.ParseSingle("NET8.0");
/// // Returns: "net8.0"
///
/// // Parse multiple TFMs
/// var tfms = TfmParser.ParseMultiple("net8.0;net10.0;netstandard2.1");
/// // Returns: ["net8.0", "net10.0", "netstandard2.1"]
///
/// // Validate a TFM format
/// if (TfmParser.Validate("net8.0"))
/// {
///     Console.WriteLine("Valid TFM");
/// }
/// </code>
/// </example>
public static partial class TfmParser
{
    private static readonly HashSet<string> ValidFrameworks = new(StringComparer.OrdinalIgnoreCase)
    {
        "net",
        "netcoreapp",
        "netstandard",
    };

    // Regex for validating TFM format
    // Matches: net8.0, net462, net4.8, net4.6.2, netcoreapp3.1, netstandard2.0, etc.
    // Group 1: framework (net, netcoreapp, netstandard)
    // Group 2: version (8.0, 462, 4.8, 4.6.2, 3.1, 2.0)
    [GeneratedRegex(@"^(net|netcoreapp|netstandard)(\d+(?:\.\d+(?:\.\d+)?)?)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TfmRegex();

    /// <summary>
    /// Parses a single TFM string and normalizes it to canonical form.
    /// </summary>
    /// <param name="tfm">The TFM string to parse.</param>
    /// <returns>The normalized TFM string in lowercase with version numbers.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tfm"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tfm"/> is empty, whitespace, or has an invalid format.</exception>
    /// <remarks>
    /// <para>
    /// This method validates and normalizes a TFM string to ensure it conforms to .NET's TFM
    /// naming conventions. The normalization process includes:
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description>Trimming leading and trailing whitespace</description>
    /// </item>
    /// <item>
    /// <description>Converting to lowercase for case-insensitive matching</description>
    /// </item>
    /// <item>
    /// <description>Validating format against TFM patterns</description>
    /// </item>
    /// </list>
    /// <para>
    /// The method is strict about TFM format requirements. For example, .NET 5+ TFMs must
    /// include the ".0" suffix (e.g., "net8.0" is valid, "net8" is invalid).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Parse and normalize a TFM
    /// var normalized = TfmParser.ParseSingle("NET8.0");
    /// Console.WriteLine(normalized); // Outputs: "net8.0"
    ///
    /// // Whitespace is trimmed
    /// var normalized2 = TfmParser.ParseSingle("  net10.0  ");
    /// Console.WriteLine(normalized2); // Outputs: "net10.0"
    ///
    /// // Invalid TFMs throw ArgumentException
    /// try
    /// {
    ///     var invalid = TfmParser.ParseSingle("net8"); // Missing ".0"
    /// }
    /// catch (ArgumentException ex)
    /// {
    ///     Console.WriteLine(ex.Message);
    ///     // "Invalid TFM format: 'net8'. Expected format is 'net8.0'..."
    /// }
    /// </code>
    /// </example>
    public static string ParseSingle(string tfm)
    {
        if (tfm == null)
            throw new ArgumentNullException(nameof(tfm), "TFM cannot be null.");

        var trimmed = tfm.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("TFM cannot be empty or whitespace.", nameof(tfm));

        var normalized = trimmed.ToLowerInvariant();

        if (!Validate(normalized))
        {
            throw new ArgumentException(
                $"Invalid TFM format: '{tfm}'. Expected format is 'net8.0', 'net462', 'netcoreapp3.1', or 'netstandard2.0'.",
                nameof(tfm));
        }

        return normalized;
    }

    /// <summary>
    /// Parses a semicolon-separated list of TFM strings.
    /// </summary>
    /// <param name="tfms">The semicolon-separated TFM string to parse.</param>
    /// <returns>An array of normalized TFM strings, with duplicates removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tfms"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tfms"/> is empty, whitespace, or contains invalid TFMs.</exception>
    /// <remarks>
    /// <para>
    /// This method parses a semicolon-delimited list of TFMs, commonly used in MSBuild project
    /// files (e.g., &lt;TargetFrameworks&gt;net8.0;net10.0&lt;/TargetFrameworks&gt;). Each TFM in the
    /// list is validated and normalized individually.
    /// </para>
    /// <para>
    /// Processing steps:
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description>Split on semicolon (;) separator</description>
    /// </item>
    /// <item>
    /// <description>Trim whitespace from each TFM</description>
    /// </item>
    /// <item>
    /// <description>Remove empty entries</description>
    /// </item>
    /// <item>
    /// <description>Validate and normalize each TFM using <see cref="ParseSingle"/></description>
    /// </item>
    /// <item>
    /// <description>Remove duplicates (case-insensitive)</description>
    /// </item>
    /// <item>
    /// <description>Preserve original order</description>
    /// </item>
    /// </list>
    /// <para>
    /// If any TFM in the list is invalid, an <see cref="ArgumentException"/> is thrown with
    /// details about all invalid TFMs found.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Parse multiple TFMs from a semicolon-separated string
    /// var tfms = TfmParser.ParseMultiple("net8.0;net10.0;netstandard2.1");
    /// // Returns: ["net8.0", "net10.0", "netstandard2.1"]
    ///
    /// // Duplicates are removed (case-insensitive)
    /// var tfms2 = TfmParser.ParseMultiple("net8.0;NET8.0;net10.0");
    /// // Returns: ["net8.0", "net10.0"]
    ///
    /// // Whitespace is handled gracefully
    /// var tfms3 = TfmParser.ParseMultiple("  net8.0  ;  net10.0  ");
    /// // Returns: ["net8.0", "net10.0"]
    ///
    /// // Empty entries are ignored
    /// var tfms4 = TfmParser.ParseMultiple("net8.0;;net10.0");
    /// // Returns: ["net8.0", "net10.0"]
    ///
    /// // Invalid TFMs in the list throw ArgumentException
    /// try
    /// {
    ///     var invalid = TfmParser.ParseMultiple("net8.0;invalid;net10.0");
    /// }
    /// catch (ArgumentException ex)
    /// {
    ///     Console.WriteLine(ex.Message);
    ///     // "Invalid TFM(s) in list: 'invalid': Invalid TFM format..."
    /// }
    /// </code>
    /// </example>
    public static string[] ParseMultiple(string tfms)
    {
        if (tfms == null)
            throw new ArgumentNullException(nameof(tfms), "TFM list cannot be null.");

        var trimmed = tfms.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("TFM list cannot be empty or whitespace.", nameof(tfms));

        var parts = trimmed.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
            throw new ArgumentException("TFM list cannot be empty after parsing.", nameof(tfms));

        var parsed = new List<string>();
        var errors = new List<string>();

        foreach (var part in parts)
        {
            try
            {
                var normalized = ParseSingle(part);
                parsed.Add(normalized);
            }
            catch (ArgumentException ex)
            {
                errors.Add($"'{part}': {ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"Invalid TFM(s) in list: {string.Join(", ", errors)}",
                nameof(tfms));
        }

        // Remove duplicates while preserving order
        var result = parsed.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        return result;
    }

    /// <summary>
    /// Validates whether a TFM string has a valid format.
    /// </summary>
    /// <param name="tfm">The TFM string to validate.</param>
    /// <returns><c>true</c> if the TFM format is valid; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Valid formats include:
    /// <list type="bullet">
    /// <item><description>net8.0, net6.0, net462 (Modern .NET and .NET Framework)</description></item>
    /// <item><description>netcoreapp3.1, netcoreapp2.1 (.NET Core)</description></item>
    /// <item><description>netstandard2.1, netstandard2.0 (.NET Standard)</description></item>
    /// </list>
    /// The TFM is expected to be in lowercase for validation.
    /// </remarks>
    public static bool Validate(string tfm)
    {
        if (string.IsNullOrWhiteSpace(tfm))
            return false;

        var match = TfmRegex().Match(tfm);
        if (!match.Success)
            return false;

        var framework = match.Groups[1].Value;
        var version = match.Groups[2].Value;  // This now includes the full version (e.g., "8.0", "462", "4.8")

        // Validate framework identifier
        if (!ValidFrameworks.Contains(framework))
            return false;

        // Validate version format
        if (string.IsNullOrEmpty(version))
            return false;

        // For "net" framework:
        // - Version 5+ must have exactly one dot (e.g., net5.0, net6.0, net8.0) - no more, no less
        // - Version <= 4 can be 3-digit (e.g., net462, net48) or dotted (e.g., net4.8, net4.6.2)
        if (framework.Equals("net", StringComparison.OrdinalIgnoreCase))
        {
            var dotCount = version.Count(c => c == '.');

            // Try to extract major version
            var firstDotIndex = version.IndexOf('.');
            int major;
            if (firstDotIndex > 0)
            {
                // Has a dot, extract everything before it
                var majorPart = version.Substring(0, firstDotIndex);
                if (!int.TryParse(majorPart, out major))
                    return false;
            }
            else
            {
                // No dot - need to determine if it's .NET Framework or modern .NET
                // - .NET Framework: net35, net40, net46, net48, net462, net472 (starts with 3 or 4)
                // - Invalid modern .NET: net5, net8, net10, net11, net12 (starts with 5+ or 1x for 10+)

                // Parse the first digit
                var firstDigit = int.Parse(version.Substring(0, 1));

                if (firstDigit >= 5 && firstDigit <= 9)
                {
                    // Single digit 5-9: must be modern .NET (net5, net6, net7, net8, net9)
                    major = firstDigit;
                }
                else if (firstDigit == 1 && version.Length >= 2 && char.IsDigit(version[1]))
                {
                    // Starts with 1 and has another digit: parse as 2-digit number
                    // Could be net10, net11, net12 (modern .NET, invalid without dot)
                    major = int.Parse(version.Substring(0, 2));
                }
                else if (firstDigit <= 4)
                {
                    // Starts with 3 or 4: .NET Framework
                    major = firstDigit;
                }
                else
                {
                    return false;
                }
            }

            if (major >= 5)
            {
                // Modern .NET (5+) requires exactly one dot (e.g., net5.0, net6.0, net8.0)
                // Reject: net5, net8, net8.0.0, net10, net11, etc.
                if (dotCount != 1)
                    return false;
            }
            else if (major <= 4)
            {
                // .NET Framework - valid formats:
                // - 2-3 digit no dots: net35, net40, net46, net48, net462, net472
                // - 1-2 dots: net4.8, net4.6.2, net4.7.2
                // All are valid
                return true;
            }
        }
        // For "netcoreapp" and "netstandard", version must contain exactly one dot
        else if (framework.Equals("netcoreapp", StringComparison.OrdinalIgnoreCase) ||
                 framework.Equals("netstandard", StringComparison.OrdinalIgnoreCase))
        {
            var dotCount = version.Count(c => c == '.');
            if (dotCount != 1)
                return false;
        }

        return true;
    }
}
