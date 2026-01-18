using FluentAssertions;
using RoslynDiff.TestUtilities.Comparers;
using Xunit;

namespace RoslynDiff.TestUtilities.Tests.Comparers;

public class LineRangeComparerTests
{
    public class LineRangeTests
    {
        [Fact]
        public void LineRange_Constructor_SetsProperties()
        {
            // Arrange & Act
            var range = new LineRange(5, 10, "test.cs");

            // Assert
            range.Start.Should().Be(5);
            range.End.Should().Be(10);
            range.Source.Should().Be("test.cs");
        }

        [Fact]
        public void LineRange_WithoutSource_HasNullSource()
        {
            // Arrange & Act
            var range = new LineRange(1, 5);

            // Assert
            range.Source.Should().BeNull();
        }

        [Theory]
        [InlineData(1, 1, 1)]
        [InlineData(1, 5, 5)]
        [InlineData(10, 20, 11)]
        [InlineData(100, 200, 101)]
        public void LineCount_ReturnsCorrectValue(int start, int end, int expectedCount)
        {
            // Arrange
            var range = new LineRange(start, end);

            // Act
            var count = range.LineCount;

            // Assert
            count.Should().Be(expectedCount);
        }

        [Theory]
        [InlineData(1, 5, 1, true)]
        [InlineData(1, 5, 3, true)]
        [InlineData(1, 5, 5, true)]
        [InlineData(1, 5, 0, false)]
        [InlineData(1, 5, 6, false)]
        [InlineData(10, 10, 10, true)]
        [InlineData(10, 10, 9, false)]
        [InlineData(10, 10, 11, false)]
        public void ContainsLine_ReturnsCorrectResult(int start, int end, int line, bool expected)
        {
            // Arrange
            var range = new LineRange(start, end);

            // Act
            var result = range.ContainsLine(line);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(1, 5, 3, 7, true)]  // Overlap in middle
        [InlineData(1, 5, 5, 10, true)] // Touch at boundary
        [InlineData(3, 7, 1, 5, true)]  // Reverse overlap
        [InlineData(1, 10, 3, 7, true)] // One contains the other
        [InlineData(3, 7, 1, 10, true)] // Reverse containment
        [InlineData(1, 5, 6, 10, false)] // No overlap (adjacent)
        [InlineData(6, 10, 1, 5, false)] // No overlap (reverse adjacent)
        [InlineData(1, 3, 5, 7, false)]  // No overlap (gap)
        public void OverlapsWith_ReturnsCorrectResult(int start1, int end1, int start2, int end2, bool expected)
        {
            // Arrange
            var range1 = new LineRange(start1, end1);
            var range2 = new LineRange(start2, end2);

            // Act
            var result = range1.OverlapsWith(range2);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ToString_SingleLine_ReturnsCorrectFormat()
        {
            // Arrange
            var range = new LineRange(5, 5);

            // Act
            var result = range.ToString();

            // Assert
            result.Should().Be("Line 5");
        }

        [Fact]
        public void ToString_MultiLine_ReturnsCorrectFormat()
        {
            // Arrange
            var range = new LineRange(5, 10);

            // Act
            var result = range.ToString();

            // Assert
            result.Should().Be("Lines 5-10");
        }

        [Fact]
        public void ToString_WithSource_IncludesSource()
        {
            // Arrange
            var range = new LineRange(5, 10, "test.cs");

            // Act
            var result = range.ToString();

            // Assert
            result.Should().Be("Lines 5-10 (test.cs)");
        }
    }

    public class DetectOverlapsTests
    {
        [Fact]
        public void DetectOverlaps_EmptyCollection_ReturnsEmpty()
        {
            // Arrange
            var ranges = Array.Empty<LineRange>();

            // Act
            var overlaps = LineRangeComparer.DetectOverlaps(ranges);

            // Assert
            overlaps.Should().BeEmpty();
        }

        [Fact]
        public void DetectOverlaps_SingleRange_ReturnsEmpty()
        {
            // Arrange
            var ranges = new[] { new LineRange(1, 5) };

            // Act
            var overlaps = LineRangeComparer.DetectOverlaps(ranges);

            // Assert
            overlaps.Should().BeEmpty();
        }

        [Fact]
        public void DetectOverlaps_NonOverlappingRanges_ReturnsEmpty()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(6, 10),
                new LineRange(11, 15)
            };

            // Act
            var overlaps = LineRangeComparer.DetectOverlaps(ranges);

            // Assert
            overlaps.Should().BeEmpty();
        }

        [Fact]
        public void DetectOverlaps_TwoOverlappingRanges_ReturnsOnePair()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5, "A"),
                new LineRange(3, 7, "B")
            };

            // Act
            var overlaps = LineRangeComparer.DetectOverlaps(ranges);

            // Assert
            overlaps.Should().HaveCount(1);
            overlaps[0].First.Should().Be(ranges[0]);
            overlaps[0].Second.Should().Be(ranges[1]);
        }

        [Fact]
        public void DetectOverlaps_MultipleOverlaps_ReturnsAllPairs()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5, "A"),
                new LineRange(3, 7, "B"),
                new LineRange(6, 10, "C")
            };

            // Act
            var overlaps = LineRangeComparer.DetectOverlaps(ranges);

            // Assert
            overlaps.Should().HaveCount(2);
            overlaps.Should().Contain(pair => pair.First.Source == "A" && pair.Second.Source == "B");
            overlaps.Should().Contain(pair => pair.First.Source == "B" && pair.Second.Source == "C");
        }

        [Fact]
        public void DetectOverlaps_RangeOverlapsMultiple_ReturnsAllPairs()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 10, "A"),
                new LineRange(2, 4, "B"),
                new LineRange(5, 7, "C"),
                new LineRange(8, 9, "D")
            };

            // Act
            var overlaps = LineRangeComparer.DetectOverlaps(ranges);

            // Assert
            overlaps.Should().HaveCount(3); // A overlaps with B, C, and D
            overlaps.Should().Contain(pair => pair.First.Source == "A" && pair.Second.Source == "B");
            overlaps.Should().Contain(pair => pair.First.Source == "A" && pair.Second.Source == "C");
            overlaps.Should().Contain(pair => pair.First.Source == "A" && pair.Second.Source == "D");
        }

        [Fact]
        public void DetectOverlaps_TouchingRanges_DetectsOverlap()
        {
            // Arrange - ranges that touch at a single line should be considered overlapping
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(5, 10)
            };

            // Act
            var overlaps = LineRangeComparer.DetectOverlaps(ranges);

            // Assert
            overlaps.Should().HaveCount(1);
        }

        [Fact]
        public void DetectOverlaps_AdjacentRanges_NoOverlap()
        {
            // Arrange - ranges that are adjacent but don't touch should not overlap
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(6, 10)
            };

            // Act
            var overlaps = LineRangeComparer.DetectOverlaps(ranges);

            // Assert
            overlaps.Should().BeEmpty();
        }

        [Fact]
        public void DetectOverlaps_NullCollection_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<LineRange> ranges = null!;

            // Act
            Action act = () => LineRangeComparer.DetectOverlaps(ranges);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("ranges");
        }

        [Fact]
        public void OverlapPair_ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var pair = new LineRangeComparer.OverlapPair(
                new LineRange(1, 5, "A"),
                new LineRange(3, 7, "B")
            );

            // Act
            var result = pair.ToString();

            // Assert
            result.Should().Contain("Overlap");
            result.Should().Contain("Lines 1-5 (A)");
            result.Should().Contain("Lines 3-7 (B)");
        }
    }

    public class DetectDuplicatesTests
    {
        [Fact]
        public void DetectDuplicates_EmptyCollection_ReturnsEmpty()
        {
            // Arrange
            var lineNumbers = Array.Empty<int>();

            // Act
            var duplicates = LineRangeComparer.DetectDuplicates(lineNumbers);

            // Assert
            duplicates.Should().BeEmpty();
        }

        [Fact]
        public void DetectDuplicates_NoDuplicates_ReturnsEmpty()
        {
            // Arrange
            var lineNumbers = new[] { 1, 2, 3, 4, 5 };

            // Act
            var duplicates = LineRangeComparer.DetectDuplicates(lineNumbers);

            // Assert
            duplicates.Should().BeEmpty();
        }

        [Fact]
        public void DetectDuplicates_OneDuplicate_ReturnsThatNumber()
        {
            // Arrange
            var lineNumbers = new[] { 1, 2, 3, 2, 4 };

            // Act
            var duplicates = LineRangeComparer.DetectDuplicates(lineNumbers);

            // Assert
            duplicates.Should().ContainSingle()
                .Which.Should().Be(2);
        }

        [Fact]
        public void DetectDuplicates_MultipleDuplicates_ReturnsAllDuplicates()
        {
            // Arrange
            var lineNumbers = new[] { 1, 2, 3, 2, 4, 3, 5, 1 };

            // Act
            var duplicates = LineRangeComparer.DetectDuplicates(lineNumbers);

            // Assert
            duplicates.Should().HaveCount(3);
            duplicates.Should().Contain(new[] { 1, 2, 3 });
        }

        [Fact]
        public void DetectDuplicates_TripleOccurrence_ReturnsNumberOnce()
        {
            // Arrange
            var lineNumbers = new[] { 1, 2, 2, 2, 3 };

            // Act
            var duplicates = LineRangeComparer.DetectDuplicates(lineNumbers);

            // Assert
            duplicates.Should().ContainSingle()
                .Which.Should().Be(2);
        }

        [Fact]
        public void DetectDuplicates_ReturnsInAscendingOrder()
        {
            // Arrange
            var lineNumbers = new[] { 5, 3, 1, 3, 5, 1 };

            // Act
            var duplicates = LineRangeComparer.DetectDuplicates(lineNumbers);

            // Assert
            duplicates.Should().Equal(1, 3, 5);
        }

        [Fact]
        public void DetectDuplicates_NullCollection_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<int> lineNumbers = null!;

            // Act
            Action act = () => LineRangeComparer.DetectDuplicates(lineNumbers);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("lineNumbers");
        }
    }

    public class AreSequentialTests
    {
        [Fact]
        public void AreSequential_EmptyCollection_ReturnsTrue()
        {
            // Arrange
            var ranges = Array.Empty<LineRange>();

            // Act
            var result = LineRangeComparer.AreSequential(ranges);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void AreSequential_SingleRange_ReturnsTrue()
        {
            // Arrange
            var ranges = new[] { new LineRange(1, 5) };

            // Act
            var result = LineRangeComparer.AreSequential(ranges);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void AreSequential_SequentialRanges_ReturnsTrue()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(6, 10),
                new LineRange(11, 15)
            };

            // Act
            var result = LineRangeComparer.AreSequential(ranges);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void AreSequential_SequentialWithGaps_ReturnsTrue()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(10, 15),
                new LineRange(20, 25)
            };

            // Act
            var result = LineRangeComparer.AreSequential(ranges);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void AreSequential_OverlappingRanges_ReturnsFalse()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(3, 7)
            };

            // Act
            var result = LineRangeComparer.AreSequential(ranges);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void AreSequential_TouchingRanges_ReturnsFalse()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(5, 10)
            };

            // Act
            var result = LineRangeComparer.AreSequential(ranges);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void AreSequential_OutOfOrder_ReturnsFalse()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(10, 15),
                new LineRange(1, 5)
            };

            // Act
            var result = LineRangeComparer.AreSequential(ranges);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void AreSequential_NullCollection_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<LineRange> ranges = null!;

            // Act
            Action act = () => LineRangeComparer.AreSequential(ranges);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("ranges");
        }
    }
}
