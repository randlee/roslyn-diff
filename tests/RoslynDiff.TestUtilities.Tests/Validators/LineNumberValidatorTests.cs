using FluentAssertions;
using RoslynDiff.TestUtilities.Comparers;
using RoslynDiff.TestUtilities.Validators;
using Xunit;

namespace RoslynDiff.TestUtilities.Tests.Validators;

public class LineNumberValidatorTests
{
    public class ValidateNoOverlapsTests
    {
        [Fact]
        public void ValidateNoOverlaps_NoOverlaps_ReturnsPass()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(6, 10),
                new LineRange(11, 15)
            };

            // Act
            var result = LineNumberValidator.ValidateNoOverlaps(ranges, "Test context");

            // Assert
            result.Passed.Should().BeTrue();
            result.Context.Should().Be("Test context");
            result.Message.Should().Contain("No overlapping");
            result.Issues.Should().BeEmpty();
        }

        [Fact]
        public void ValidateNoOverlaps_WithOverlaps_ReturnsFail()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5, "A"),
                new LineRange(3, 7, "B")
            };

            // Act
            var result = LineNumberValidator.ValidateNoOverlaps(ranges, "Test context");

            // Assert
            result.Passed.Should().BeFalse();
            result.Context.Should().Be("Test context");
            result.Message.Should().Contain("1 overlapping");
            result.Issues.Should().ContainSingle();
            result.Issues[0].Should().Contain("Lines 1-5 (A)")
                .And.Contain("Lines 3-7 (B)")
                .And.Contain("overlaps");
        }

        [Fact]
        public void ValidateNoOverlaps_MultipleOverlaps_ReturnsAllIssues()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5, "A"),
                new LineRange(3, 7, "B"),
                new LineRange(6, 10, "C")
            };

            // Act
            var result = LineNumberValidator.ValidateNoOverlaps(ranges, "Test context");

            // Assert
            result.Passed.Should().BeFalse();
            result.Message.Should().Contain("2 overlapping");
            result.Issues.Should().HaveCount(2);
        }

        [Fact]
        public void ValidateNoOverlaps_EmptyRanges_ReturnsPass()
        {
            // Arrange
            var ranges = Array.Empty<LineRange>();

            // Act
            var result = LineNumberValidator.ValidateNoOverlaps(ranges, "Test context");

            // Assert
            result.Passed.Should().BeTrue();
        }

        [Fact]
        public void ValidateNoOverlaps_NullRanges_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<LineRange> ranges = null!;

            // Act
            Action act = () => LineNumberValidator.ValidateNoOverlaps(ranges, "Test context");

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("ranges");
        }

        [Fact]
        public void ValidateNoOverlaps_NullContext_ThrowsArgumentNullException()
        {
            // Arrange
            var ranges = new[] { new LineRange(1, 5) };

            // Act
            Action act = () => LineNumberValidator.ValidateNoOverlaps(ranges, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("context");
        }
    }

    public class ValidateNoDuplicatesTests
    {
        [Fact]
        public void ValidateNoDuplicates_NoDuplicates_ReturnsPass()
        {
            // Arrange
            var lineNumbers = new[] { 1, 2, 3, 4, 5 };

            // Act
            var result = LineNumberValidator.ValidateNoDuplicates(lineNumbers, "Test context");

            // Assert
            result.Passed.Should().BeTrue();
            result.Context.Should().Be("Test context");
            result.Message.Should().Contain("No duplicate");
            result.Issues.Should().BeEmpty();
        }

        [Fact]
        public void ValidateNoDuplicates_WithDuplicates_ReturnsFail()
        {
            // Arrange
            var lineNumbers = new[] { 1, 2, 3, 2, 4 };

            // Act
            var result = LineNumberValidator.ValidateNoDuplicates(lineNumbers, "Test context");

            // Assert
            result.Passed.Should().BeFalse();
            result.Context.Should().Be("Test context");
            result.Message.Should().Contain("1 duplicate");
            result.Issues.Should().ContainSingle();
            result.Issues[0].Should().Contain("Line 2")
                .And.Contain("multiple times");
        }

        [Fact]
        public void ValidateNoDuplicates_MultipleDuplicates_ReturnsAllIssues()
        {
            // Arrange
            var lineNumbers = new[] { 1, 2, 3, 2, 4, 3, 5, 1 };

            // Act
            var result = LineNumberValidator.ValidateNoDuplicates(lineNumbers, "Test context");

            // Assert
            result.Passed.Should().BeFalse();
            result.Message.Should().Contain("3 duplicate");
            result.Issues.Should().HaveCount(3);
            result.Issues.Should().Contain(i => i.Contains("Line 1"));
            result.Issues.Should().Contain(i => i.Contains("Line 2"));
            result.Issues.Should().Contain(i => i.Contains("Line 3"));
        }

        [Fact]
        public void ValidateNoDuplicates_EmptyCollection_ReturnsPass()
        {
            // Arrange
            var lineNumbers = Array.Empty<int>();

            // Act
            var result = LineNumberValidator.ValidateNoDuplicates(lineNumbers, "Test context");

            // Assert
            result.Passed.Should().BeTrue();
        }

        [Fact]
        public void ValidateNoDuplicates_NullCollection_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<int> lineNumbers = null!;

            // Act
            Action act = () => LineNumberValidator.ValidateNoDuplicates(lineNumbers, "Test context");

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("lineNumbers");
        }

        [Fact]
        public void ValidateNoDuplicates_NullContext_ThrowsArgumentNullException()
        {
            // Arrange
            var lineNumbers = new[] { 1, 2, 3 };

            // Act
            Action act = () => LineNumberValidator.ValidateNoDuplicates(lineNumbers, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("context");
        }
    }

    public class ValidateRangesSequentialTests
    {
        [Fact]
        public void ValidateRangesSequential_SequentialRanges_ReturnsPass()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(6, 10),
                new LineRange(11, 15)
            };

            // Act
            var result = LineNumberValidator.ValidateRangesSequential(ranges, "Test context");

            // Assert
            result.Passed.Should().BeTrue();
            result.Context.Should().Be("Test context");
            result.Message.Should().Contain("All 3 range(s) are sequential");
            result.Issues.Should().BeEmpty();
        }

        [Fact]
        public void ValidateRangesSequential_OverlappingRanges_ReturnsFail()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(3, 7)
            };

            // Act
            var result = LineNumberValidator.ValidateRangesSequential(ranges, "Test context");

            // Assert
            result.Passed.Should().BeFalse();
            result.Context.Should().Be("Test context");
            result.Message.Should().Contain("1 sequential ordering violation");
            result.Issues.Should().ContainSingle();
            result.Issues[0].Should().Contain("not before")
                .And.Contain("sequential");
        }

        [Fact]
        public void ValidateRangesSequential_OutOfOrder_ReturnsFail()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(10, 15),
                new LineRange(1, 5)
            };

            // Act
            var result = LineNumberValidator.ValidateRangesSequential(ranges, "Test context");

            // Assert
            result.Passed.Should().BeFalse();
            result.Issues.Should().ContainSingle();
        }

        [Fact]
        public void ValidateRangesSequential_MultipleViolations_ReturnsAllIssues()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(3, 7),
                new LineRange(6, 10)
            };

            // Act
            var result = LineNumberValidator.ValidateRangesSequential(ranges, "Test context");

            // Assert
            result.Passed.Should().BeFalse();
            result.Message.Should().Contain("2 sequential ordering violation");
            result.Issues.Should().HaveCount(2);
        }

        [Fact]
        public void ValidateRangesSequential_EmptyCollection_ReturnsPass()
        {
            // Arrange
            var ranges = Array.Empty<LineRange>();

            // Act
            var result = LineNumberValidator.ValidateRangesSequential(ranges, "Test context");

            // Assert
            result.Passed.Should().BeTrue();
            result.Message.Should().Contain("empty collection");
        }

        [Fact]
        public void ValidateRangesSequential_SingleRange_ReturnsPass()
        {
            // Arrange
            var ranges = new[] { new LineRange(1, 5) };

            // Act
            var result = LineNumberValidator.ValidateRangesSequential(ranges, "Test context");

            // Assert
            result.Passed.Should().BeTrue();
        }

        [Fact]
        public void ValidateRangesSequential_NullRanges_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<LineRange> ranges = null!;

            // Act
            Action act = () => LineNumberValidator.ValidateRangesSequential(ranges, "Test context");

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("ranges");
        }

        [Fact]
        public void ValidateRangesSequential_NullContext_ThrowsArgumentNullException()
        {
            // Arrange
            var ranges = new[] { new LineRange(1, 5) };

            // Act
            Action act = () => LineNumberValidator.ValidateRangesSequential(ranges, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("context");
        }
    }

    public class ValidateRangesTests
    {
        [Fact]
        public void ValidateRanges_ValidRanges_WithoutSequentialCheck_ReturnsPass()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(6, 10),
                new LineRange(11, 15)
            };

            // Act
            var result = LineNumberValidator.ValidateRanges(ranges, "Test context", requireSequential: false);

            // Assert
            result.Passed.Should().BeTrue();
            result.Message.Should().Contain("overlap");
            result.Message.Should().NotContain("sequential");
        }

        [Fact]
        public void ValidateRanges_ValidRanges_WithSequentialCheck_ReturnsPass()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(6, 10),
                new LineRange(11, 15)
            };

            // Act
            var result = LineNumberValidator.ValidateRanges(ranges, "Test context", requireSequential: true);

            // Assert
            result.Passed.Should().BeTrue();
            result.Message.Should().Contain("overlap and sequential");
        }

        [Fact]
        public void ValidateRanges_OverlappingRanges_WithoutSequentialCheck_ReturnsFail()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(3, 7)
            };

            // Act
            var result = LineNumberValidator.ValidateRanges(ranges, "Test context", requireSequential: false);

            // Assert
            result.Passed.Should().BeFalse();
            result.Issues.Should().ContainSingle();
            result.Issues[0].Should().Contain("overlaps");
        }

        [Fact]
        public void ValidateRanges_OverlappingRanges_WithSequentialCheck_ReturnsFailWithMultipleIssues()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(1, 5),
                new LineRange(3, 7)
            };

            // Act
            var result = LineNumberValidator.ValidateRanges(ranges, "Test context", requireSequential: true);

            // Assert
            result.Passed.Should().BeFalse();
            result.Issues.Should().HaveCount(2); // One overlap issue, one sequential issue
            result.Issues.Should().Contain(i => i.Contains("overlaps"));
            result.Issues.Should().Contain(i => i.Contains("sequential"));
        }

        [Fact]
        public void ValidateRanges_OutOfOrderButNotOverlapping_WithSequentialCheck_ReturnsFail()
        {
            // Arrange
            var ranges = new[]
            {
                new LineRange(10, 15),
                new LineRange(1, 5)
            };

            // Act
            var result = LineNumberValidator.ValidateRanges(ranges, "Test context", requireSequential: true);

            // Assert
            result.Passed.Should().BeFalse();
            result.Issues.Should().ContainSingle();
            result.Issues[0].Should().Contain("sequential");
        }

        [Fact]
        public void ValidateRanges_OutOfOrderButNotOverlapping_WithoutSequentialCheck_ReturnsPass()
        {
            // Arrange - Out of order but no overlaps
            var ranges = new[]
            {
                new LineRange(10, 15),
                new LineRange(1, 5)
            };

            // Act
            var result = LineNumberValidator.ValidateRanges(ranges, "Test context", requireSequential: false);

            // Assert
            result.Passed.Should().BeTrue();
        }

        [Fact]
        public void ValidateRanges_EmptyCollection_ReturnsPass()
        {
            // Arrange
            var ranges = Array.Empty<LineRange>();

            // Act
            var result = LineNumberValidator.ValidateRanges(ranges, "Test context", requireSequential: true);

            // Assert
            result.Passed.Should().BeTrue();
        }

        [Fact]
        public void ValidateRanges_NullRanges_ThrowsArgumentNullException()
        {
            // Arrange
            IEnumerable<LineRange> ranges = null!;

            // Act
            Action act = () => LineNumberValidator.ValidateRanges(ranges, "Test context");

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("ranges");
        }

        [Fact]
        public void ValidateRanges_NullContext_ThrowsArgumentNullException()
        {
            // Arrange
            var ranges = new[] { new LineRange(1, 5) };

            // Act
            Action act = () => LineNumberValidator.ValidateRanges(ranges, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("context");
        }
    }

    public class TestResultTests
    {
        [Fact]
        public void TestResult_Pass_CreatesPassingResult()
        {
            // Act
            var result = Models.TestResult.Pass("Test context", "Success message");

            // Assert
            result.Passed.Should().BeTrue();
            result.Context.Should().Be("Test context");
            result.Message.Should().Be("Success message");
            result.Issues.Should().BeEmpty();
        }

        [Fact]
        public void TestResult_Pass_WithoutMessage_UsesDefaultMessage()
        {
            // Act
            var result = Models.TestResult.Pass("Test context");

            // Assert
            result.Passed.Should().BeTrue();
            result.Message.Should().Be("Validation passed");
        }

        [Fact]
        public void TestResult_Fail_CreatesFailingResult()
        {
            // Arrange
            var issues = new[] { "Issue 1", "Issue 2" };

            // Act
            var result = Models.TestResult.Fail("Test context", "Failure message", issues);

            // Assert
            result.Passed.Should().BeFalse();
            result.Context.Should().Be("Test context");
            result.Message.Should().Be("Failure message");
            result.Issues.Should().BeEquivalentTo(issues);
        }

        [Fact]
        public void TestResult_Fail_WithoutIssues_HasEmptyIssuesList()
        {
            // Act
            var result = Models.TestResult.Fail("Test context", "Failure message");

            // Assert
            result.Passed.Should().BeFalse();
            result.Issues.Should().BeEmpty();
        }

        [Fact]
        public void TestResult_ToString_PassingResult_ContainsPASS()
        {
            // Arrange
            var result = Models.TestResult.Pass("Test context", "Success");

            // Act
            var str = result.ToString();

            // Assert
            str.Should().Contain("[PASS]")
                .And.Contain("Test context")
                .And.Contain("Success");
        }

        [Fact]
        public void TestResult_ToString_FailingResult_ContainsFAIL()
        {
            // Arrange
            var result = Models.TestResult.Fail("Test context", "Failure");

            // Act
            var str = result.ToString();

            // Assert
            str.Should().Contain("[FAIL]")
                .And.Contain("Test context")
                .And.Contain("Failure");
        }

        [Fact]
        public void TestResult_ToString_WithIssues_IncludesIssuesList()
        {
            // Arrange
            var issues = new[] { "Issue 1", "Issue 2" };
            var result = Models.TestResult.Fail("Test context", "Failure", issues);

            // Act
            var str = result.ToString();

            // Assert
            str.Should().Contain("Issues:")
                .And.Contain("Issue 1")
                .And.Contain("Issue 2");
        }
    }
}
