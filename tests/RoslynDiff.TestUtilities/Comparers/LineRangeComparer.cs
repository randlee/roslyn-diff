namespace RoslynDiff.TestUtilities.Comparers;

/// <summary>
/// Represents a range of line numbers within a source file.
/// </summary>
/// <param name="Start">The 1-based starting line number (inclusive).</param>
/// <param name="End">The 1-based ending line number (inclusive).</param>
/// <param name="Source">Optional identifier for the source of this range (e.g., file name, change ID).</param>
public record LineRange(int Start, int End, string? Source = null)
{
    /// <summary>
    /// Gets the number of lines in this range.
    /// </summary>
    public int LineCount => End - Start + 1;

    /// <summary>
    /// Determines whether this range contains the specified line number.
    /// </summary>
    /// <param name="line">The 1-based line number to check.</param>
    /// <returns><c>true</c> if the range contains the line; otherwise, <c>false</c>.</returns>
    public bool ContainsLine(int line) => line >= Start && line <= End;

    /// <summary>
    /// Determines whether this range overlaps with another range.
    /// </summary>
    /// <param name="other">The other range to check for overlap.</param>
    /// <returns><c>true</c> if the ranges overlap; otherwise, <c>false</c>.</returns>
    public bool OverlapsWith(LineRange other)
    {
        // Ranges overlap if one's start is within the other, or vice versa
        return Start <= other.End && End >= other.Start;
    }

    /// <summary>
    /// Determines whether this range fully contains another range.
    /// </summary>
    /// <param name="other">The other range to check for containment.</param>
    /// <returns><c>true</c> if this range fully contains the other; otherwise, <c>false</c>.</returns>
    public bool Contains(LineRange other)
    {
        return Start <= other.Start && End >= other.End;
    }

    /// <summary>
    /// Determines whether this range partially overlaps with another (overlaps but neither fully contains the other).
    /// </summary>
    /// <param name="other">The other range to check.</param>
    /// <returns><c>true</c> if the ranges have a partial overlap; otherwise, <c>false</c>.</returns>
    public bool PartiallyOverlapsWith(LineRange other)
    {
        // Must overlap but neither can fully contain the other
        return OverlapsWith(other) && !Contains(other) && !other.Contains(this);
    }

    /// <summary>
    /// Returns a string representation of this line range.
    /// </summary>
    public override string ToString()
    {
        var rangeStr = Start == End ? $"Line {Start}" : $"Lines {Start}-{End}";
        return Source != null ? $"{rangeStr} ({Source})" : rangeStr;
    }
}

/// <summary>
/// Provides functionality to compare and validate line ranges for overlaps and duplicates.
/// </summary>
public static class LineRangeComparer
{
    /// <summary>
    /// Represents a pair of overlapping line ranges.
    /// </summary>
    /// <param name="First">The first overlapping range.</param>
    /// <param name="Second">The second overlapping range.</param>
    public record OverlapPair(LineRange First, LineRange Second)
    {
        /// <summary>
        /// Returns a string representation of this overlap pair.
        /// </summary>
        public override string ToString()
        {
            return $"Overlap: {First} and {Second}";
        }
    }

    /// <summary>
    /// Detects overlapping line ranges within the provided collection.
    /// </summary>
    /// <param name="ranges">The collection of line ranges to check for overlaps.</param>
    /// <returns>
    /// A collection of <see cref="OverlapPair"/> instances representing all overlapping range pairs.
    /// Returns an empty collection if no overlaps are found.
    /// </returns>
    /// <remarks>
    /// Two ranges are considered overlapping if they share any line numbers and neither fully contains the other.
    /// Full containment (one range completely inside another) is allowed as it represents hierarchical parent-child
    /// relationships in diff output (e.g., a namespace containing a class).
    /// Adjacent ranges (e.g., 1-5 and 6-10) are not considered overlapping.
    /// This method performs an O(n²) comparison of all range pairs.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ranges"/> is <c>null</c>.</exception>
    public static IReadOnlyList<OverlapPair> DetectOverlaps(IEnumerable<LineRange> ranges)
    {
        if (ranges == null)
        {
            throw new ArgumentNullException(nameof(ranges));
        }

        var rangeList = ranges.ToList();
        var overlaps = new List<OverlapPair>();

        for (int i = 0; i < rangeList.Count; i++)
        {
            for (int j = i + 1; j < rangeList.Count; j++)
            {
                // Only flag partial overlaps - full containment is allowed for hierarchical output
                if (rangeList[i].PartiallyOverlapsWith(rangeList[j]))
                {
                    overlaps.Add(new OverlapPair(rangeList[i], rangeList[j]));
                }
            }
        }

        return overlaps;
    }

    /// <summary>
    /// Detects duplicate line numbers within the provided collection.
    /// </summary>
    /// <param name="lineNumbers">The collection of line numbers to check for duplicates.</param>
    /// <returns>
    /// A collection of line numbers that appear more than once in the input.
    /// Each duplicate number appears only once in the result, regardless of how many times it occurs.
    /// Returns an empty collection if no duplicates are found.
    /// </returns>
    /// <remarks>
    /// This method groups line numbers and identifies those that appear multiple times.
    /// The returned collection is sorted in ascending order.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="lineNumbers"/> is <c>null</c>.</exception>
    public static IReadOnlyList<int> DetectDuplicates(IEnumerable<int> lineNumbers)
    {
        if (lineNumbers == null)
        {
            throw new ArgumentNullException(nameof(lineNumbers));
        }

        return lineNumbers
            .GroupBy(line => line)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(line => line)
            .ToList();
    }

    /// <summary>
    /// Checks if the provided ranges are sequential (non-overlapping and in ascending order).
    /// </summary>
    /// <param name="ranges">The collection of line ranges to check.</param>
    /// <returns>
    /// <c>true</c> if the ranges are sequential (each range starts after the previous one ends);
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Sequential ranges must satisfy: range[i].End &lt; range[i+1].Start for all consecutive pairs.
    /// Adjacent ranges (e.g., 1-5 and 6-10) are considered sequential.
    /// An empty collection or a single range is considered sequential.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ranges"/> is <c>null</c>.</exception>
    public static bool AreSequential(IEnumerable<LineRange> ranges)
    {
        if (ranges == null)
        {
            throw new ArgumentNullException(nameof(ranges));
        }

        var rangeList = ranges.ToList();

        for (int i = 0; i < rangeList.Count - 1; i++)
        {
            // Next range must start after current range ends
            if (rangeList[i + 1].Start <= rangeList[i].End)
            {
                return false;
            }
        }

        return true;
    }
}
