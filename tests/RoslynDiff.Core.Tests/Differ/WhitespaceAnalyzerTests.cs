namespace RoslynDiff.Core.Tests.Differ;

using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using Xunit;

public class WhitespaceAnalyzerTests
{
    #region Analyze Method Tests

    [Fact]
    public void Analyze_BothLinesNull_ReturnsNone()
    {
        WhitespaceAnalyzer.Analyze(null, null).Should().Be(WhitespaceIssue.None);
    }

    [Fact]
    public void Analyze_LinesIdentical_ReturnsNone()
    {
        WhitespaceAnalyzer.Analyze("    code", "    code").Should().Be(WhitespaceIssue.None);
    }

    [Fact]
    public void Analyze_IndentationDiffers_ReturnsIndentationChanged()
    {
        var result = WhitespaceAnalyzer.Analyze("    code", "        code");
        result.Should().HaveFlag(WhitespaceIssue.IndentationChanged);
    }

    [Fact]
    public void Analyze_NewLineHasMixedIndentation_ReturnsMixedTabsSpaces()
    {
        var result = WhitespaceAnalyzer.Analyze("    code", " \tcode");
        result.Should().HaveFlag(WhitespaceIssue.MixedTabsSpaces);
    }

    [Fact]
    public void Analyze_TrailingWhitespaceChanges_ReturnsTrailingWhitespace()
    {
        var result = WhitespaceAnalyzer.Analyze("code", "code   ");
        result.Should().HaveFlag(WhitespaceIssue.TrailingWhitespace);
    }

    [Fact]
    public void Analyze_MultipleIssues_ReturnsCombinedFlags()
    {
        // Change indentation, add mixed tabs/spaces, and add trailing whitespace
        var result = WhitespaceAnalyzer.Analyze("    code", " \tcode   ");

        result.Should().HaveFlag(WhitespaceIssue.IndentationChanged);
        result.Should().HaveFlag(WhitespaceIssue.MixedTabsSpaces);
        result.Should().HaveFlag(WhitespaceIssue.TrailingWhitespace);
    }

    [Fact]
    public void Analyze_EmptyStrings_ReturnsNone()
    {
        WhitespaceAnalyzer.Analyze("", "").Should().Be(WhitespaceIssue.None);
    }

    [Fact]
    public void Analyze_OldLineNull_NewLineHasMixedIndentation_ReturnsMixedTabsSpaces()
    {
        var result = WhitespaceAnalyzer.Analyze(null, " \tcode");
        result.Should().Be(WhitespaceIssue.MixedTabsSpaces);
    }

    [Fact]
    public void Analyze_OldLineNull_NewLineHasNormalIndentation_ReturnsNone()
    {
        var result = WhitespaceAnalyzer.Analyze(null, "    code");
        result.Should().Be(WhitespaceIssue.None);
    }

    [Fact]
    public void Analyze_NewLineNull_ReturnsNone()
    {
        var result = WhitespaceAnalyzer.Analyze("    code", null);
        result.Should().Be(WhitespaceIssue.None);
    }

    [Fact]
    public void Analyze_TabsToSpaces_ReturnsIndentationChanged()
    {
        var result = WhitespaceAnalyzer.Analyze("\tcode", "    code");
        result.Should().HaveFlag(WhitespaceIssue.IndentationChanged);
    }

    [Fact]
    public void Analyze_SpacesToTabs_ReturnsIndentationChanged()
    {
        var result = WhitespaceAnalyzer.Analyze("    code", "\tcode");
        result.Should().HaveFlag(WhitespaceIssue.IndentationChanged);
    }

    #endregion

    #region GetIndentation Tests

    [Fact]
    public void GetIndentation_NoIndentation_ReturnsEmptyString()
    {
        WhitespaceAnalyzer.GetIndentation("code").Should().BeEmpty();
    }

    [Fact]
    public void GetIndentation_SpaceIndentation_ReturnsSpaces()
    {
        WhitespaceAnalyzer.GetIndentation("    code").Should().Be("    ");
    }

    [Fact]
    public void GetIndentation_TabIndentation_ReturnsTabs()
    {
        WhitespaceAnalyzer.GetIndentation("\t\tcode").Should().Be("\t\t");
    }

    [Fact]
    public void GetIndentation_MixedIndentation_ReturnsMixed()
    {
        WhitespaceAnalyzer.GetIndentation(" \t code").Should().Be(" \t ");
    }

    [Fact]
    public void GetIndentation_StopsAtFirstNonWhitespace()
    {
        WhitespaceAnalyzer.GetIndentation("  x  y").Should().Be("  ");
    }

    [Fact]
    public void GetIndentation_EmptyString_ReturnsEmptyString()
    {
        WhitespaceAnalyzer.GetIndentation("").Should().BeEmpty();
    }

    [Fact]
    public void GetIndentation_AllWhitespace_ReturnsEntireString()
    {
        WhitespaceAnalyzer.GetIndentation("    ").Should().Be("    ");
    }

    [Fact]
    public void GetIndentation_NullThrowsArgumentNullException()
    {
        var act = () => WhitespaceAnalyzer.GetIndentation(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region GetTrailingWhitespace Tests

    [Fact]
    public void GetTrailingWhitespace_NoTrailing_ReturnsEmptyString()
    {
        WhitespaceAnalyzer.GetTrailingWhitespace("code").Should().BeEmpty();
    }

    [Fact]
    public void GetTrailingWhitespace_TrailingSpaces_ReturnsSpaces()
    {
        WhitespaceAnalyzer.GetTrailingWhitespace("code   ").Should().Be("   ");
    }

    [Fact]
    public void GetTrailingWhitespace_TrailingTabs_ReturnsTabs()
    {
        WhitespaceAnalyzer.GetTrailingWhitespace("code\t\t").Should().Be("\t\t");
    }

    [Fact]
    public void GetTrailingWhitespace_MixedTrailing_ReturnsMixed()
    {
        WhitespaceAnalyzer.GetTrailingWhitespace("code \t ").Should().Be(" \t ");
    }

    [Fact]
    public void GetTrailingWhitespace_AllWhitespace_ReturnsEntireString()
    {
        WhitespaceAnalyzer.GetTrailingWhitespace("    ").Should().Be("    ");
    }

    [Fact]
    public void GetTrailingWhitespace_EmptyString_ReturnsEmptyString()
    {
        WhitespaceAnalyzer.GetTrailingWhitespace("").Should().BeEmpty();
    }

    [Fact]
    public void GetTrailingWhitespace_NullThrowsArgumentNullException()
    {
        var act = () => WhitespaceAnalyzer.GetTrailingWhitespace(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetTrailingWhitespace_NewlineCharacters_ReturnsNewlines()
    {
        WhitespaceAnalyzer.GetTrailingWhitespace("code\r\n").Should().Be("\r\n");
    }

    #endregion

    #region HasMixedTabsSpaces Tests

    [Fact]
    public void HasMixedTabsSpaces_SpacesOnly_ReturnsFalse()
    {
        WhitespaceAnalyzer.HasMixedTabsSpaces("    ").Should().BeFalse();
    }

    [Fact]
    public void HasMixedTabsSpaces_TabsOnly_ReturnsFalse()
    {
        WhitespaceAnalyzer.HasMixedTabsSpaces("\t\t").Should().BeFalse();
    }

    [Fact]
    public void HasMixedTabsSpaces_SpaceThenTab_ReturnsTrue()
    {
        WhitespaceAnalyzer.HasMixedTabsSpaces(" \t").Should().BeTrue();
    }

    [Fact]
    public void HasMixedTabsSpaces_TabThenSpace_ReturnsTrue()
    {
        WhitespaceAnalyzer.HasMixedTabsSpaces("\t ").Should().BeTrue();
    }

    [Fact]
    public void HasMixedTabsSpaces_EmptyString_ReturnsFalse()
    {
        WhitespaceAnalyzer.HasMixedTabsSpaces("").Should().BeFalse();
    }

    [Fact]
    public void HasMixedTabsSpaces_SingleSpace_ReturnsFalse()
    {
        WhitespaceAnalyzer.HasMixedTabsSpaces(" ").Should().BeFalse();
    }

    [Fact]
    public void HasMixedTabsSpaces_SingleTab_ReturnsFalse()
    {
        WhitespaceAnalyzer.HasMixedTabsSpaces("\t").Should().BeFalse();
    }

    [Fact]
    public void HasMixedTabsSpaces_NullThrowsArgumentNullException()
    {
        var act = () => WhitespaceAnalyzer.HasMixedTabsSpaces(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region CalculateIndentWidth Tests

    [Fact]
    public void CalculateIndentWidth_EmptyString_ReturnsZero()
    {
        WhitespaceAnalyzer.CalculateIndentWidth("").Should().Be(0);
    }

    [Fact]
    public void CalculateIndentWidth_SpacesOnly_ReturnsSpaceCount()
    {
        WhitespaceAnalyzer.CalculateIndentWidth("    ").Should().Be(4);
    }

    [Fact]
    public void CalculateIndentWidth_TabsOnly_ReturnsTabWidthTimesCount()
    {
        WhitespaceAnalyzer.CalculateIndentWidth("\t\t", tabWidth: 4).Should().Be(8);
    }

    [Fact]
    public void CalculateIndentWidth_MixedTabsSpaces_CalculatesCorrectly()
    {
        // Tab at position 0 goes to position 4, then 2 spaces = 6
        WhitespaceAnalyzer.CalculateIndentWidth("\t  ", tabWidth: 4).Should().Be(6);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void CalculateIndentWidth_RespectsCustomTabWidth(int tabWidth)
    {
        WhitespaceAnalyzer.CalculateIndentWidth("\t", tabWidth: tabWidth).Should().Be(tabWidth);
    }

    [Fact]
    public void CalculateIndentWidth_TabsAlignToTabStops()
    {
        // 2 spaces + tab should align to next tab stop
        // Position 2 + tab with width 4 -> next tab stop at 4
        WhitespaceAnalyzer.CalculateIndentWidth("  \t", tabWidth: 4).Should().Be(4);
    }

    [Fact]
    public void CalculateIndentWidth_ThreeSpacesThenTab_AlignsToNextTabStop()
    {
        // 3 spaces (width 3) + tab with width 4 -> next tab stop at 4
        WhitespaceAnalyzer.CalculateIndentWidth("   \t", tabWidth: 4).Should().Be(4);
    }

    [Fact]
    public void CalculateIndentWidth_FourSpacesThenTab_AlignsToNextTabStop()
    {
        // 4 spaces (width 4) + tab with width 4 -> next tab stop at 8
        WhitespaceAnalyzer.CalculateIndentWidth("    \t", tabWidth: 4).Should().Be(8);
    }

    [Fact]
    public void CalculateIndentWidth_NullThrowsArgumentNullException()
    {
        var act = () => WhitespaceAnalyzer.CalculateIndentWidth(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CalculateIndentWidth_ZeroTabWidthThrowsArgumentOutOfRangeException()
    {
        var act = () => WhitespaceAnalyzer.CalculateIndentWidth("  ", tabWidth: 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateIndentWidth_NegativeTabWidthThrowsArgumentOutOfRangeException()
    {
        var act = () => WhitespaceAnalyzer.CalculateIndentWidth("  ", tabWidth: -1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region DetectLineEndingChange Tests

    [Fact]
    public void DetectLineEndingChange_BothLF_ReturnsFalse()
    {
        WhitespaceAnalyzer.DetectLineEndingChange("line1\nline2", "line3\nline4").Should().BeFalse();
    }

    [Fact]
    public void DetectLineEndingChange_BothCRLF_ReturnsFalse()
    {
        WhitespaceAnalyzer.DetectLineEndingChange("line1\r\nline2", "line3\r\nline4").Should().BeFalse();
    }

    [Fact]
    public void DetectLineEndingChange_CRLFToLF_ReturnsTrue()
    {
        WhitespaceAnalyzer.DetectLineEndingChange("line1\r\nline2", "line3\nline4").Should().BeTrue();
    }

    [Fact]
    public void DetectLineEndingChange_LFToCRLF_ReturnsTrue()
    {
        WhitespaceAnalyzer.DetectLineEndingChange("line1\nline2", "line3\r\nline4").Should().BeTrue();
    }

    [Fact]
    public void DetectLineEndingChange_MixedLineEndings_DetectsDominant()
    {
        // Old has more CRLF, new has more LF
        var oldContent = "line1\r\nline2\r\nline3\nline4";
        var newContent = "line1\nline2\nline3\r\nline4";

        WhitespaceAnalyzer.DetectLineEndingChange(oldContent, newContent).Should().BeTrue();
    }

    [Fact]
    public void DetectLineEndingChange_EmptyContent_ReturnsFalse()
    {
        WhitespaceAnalyzer.DetectLineEndingChange("", "").Should().BeFalse();
    }

    [Fact]
    public void DetectLineEndingChange_OneEmptyOneWithLineEndings_ReturnsTrue()
    {
        WhitespaceAnalyzer.DetectLineEndingChange("", "line1\nline2").Should().BeTrue();
    }

    [Fact]
    public void DetectLineEndingChange_NoLineEndingsInEither_ReturnsFalse()
    {
        WhitespaceAnalyzer.DetectLineEndingChange("single line", "another line").Should().BeFalse();
    }

    [Fact]
    public void DetectLineEndingChange_NullOldContentThrowsArgumentNullException()
    {
        var act = () => WhitespaceAnalyzer.DetectLineEndingChange(null!, "content");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DetectLineEndingChange_NullNewContentThrowsArgumentNullException()
    {
        var act = () => WhitespaceAnalyzer.DetectLineEndingChange("content", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DetectLineEndingChange_CROnlyOld_LFNew_ReturnsTrue()
    {
        // Old Mac-style CR only vs Unix LF
        WhitespaceAnalyzer.DetectLineEndingChange("line1\rline2", "line3\nline4").Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Analyze_RealWorldPythonIndentChange_DetectsIssue()
    {
        // Simulating a Python function with changed indentation
        var oldLine = "    print('hello')";
        var newLine = "        print('hello')";

        var result = WhitespaceAnalyzer.Analyze(oldLine, newLine);
        result.Should().HaveFlag(WhitespaceIssue.IndentationChanged);
    }

    [Fact]
    public void Analyze_RealWorldYamlSpaceToTab_DetectsMixedIssue()
    {
        // Simulating YAML indentation that became mixed
        var oldLine = "  key: value";
        var newLine = " \tkey: value";

        var result = WhitespaceAnalyzer.Analyze(oldLine, newLine);
        result.Should().HaveFlag(WhitespaceIssue.IndentationChanged);
        result.Should().HaveFlag(WhitespaceIssue.MixedTabsSpaces);
    }

    [Fact]
    public void Analyze_ContentChangeWithSameWhitespace_ReturnsNone()
    {
        // Only the content changed, not the whitespace
        var oldLine = "    int x = 1;";
        var newLine = "    int x = 2;";

        var result = WhitespaceAnalyzer.Analyze(oldLine, newLine);
        result.Should().Be(WhitespaceIssue.None);
    }

    #endregion
}
