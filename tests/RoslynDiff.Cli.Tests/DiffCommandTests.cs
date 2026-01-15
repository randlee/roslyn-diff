namespace RoslynDiff.Cli.Tests;

using FluentAssertions;
using RoslynDiff.Cli.Commands;
using Xunit;

/// <summary>
/// Unit tests for <see cref="DiffCommand"/>.
/// </summary>
public class DiffCommandTests
{
    #region Default Values

    [Fact]
    public void Settings_DefaultMode_ShouldBeAuto()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.Mode.Should().Be("auto");
    }

    [Fact]
    public void Settings_DefaultOutputFormat_ShouldBeText()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.OutputFormat.Should().Be("text");
    }

    [Fact]
    public void Settings_DefaultContextLines_ShouldBeThree()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.ContextLines.Should().Be(3);
    }

    [Fact]
    public void Settings_DefaultIgnoreWhitespace_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.IgnoreWhitespace.Should().BeFalse();
    }

    [Fact]
    public void Settings_DefaultIgnoreComments_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.IgnoreComments.Should().BeFalse();
    }

    [Fact]
    public void Settings_DefaultRichOutput_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.RichOutput.Should().BeFalse();
    }

    [Fact]
    public void Settings_DefaultOutputFile_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.OutputFile.Should().BeNull();
    }

    [Fact]
    public void Settings_DefaultPaths_ShouldBeEmpty()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.OldPath.Should().BeEmpty();
        settings.NewPath.Should().BeEmpty();
    }

    #endregion

    #region IgnoreWhitespace Option

    [Fact]
    public void Settings_IgnoreWhitespace_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { IgnoreWhitespace = true };

        // Assert
        settings.IgnoreWhitespace.Should().BeTrue();
    }

    [Fact]
    public void Settings_IgnoreWhitespace_WhenSetToFalse_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { IgnoreWhitespace = false };

        // Assert
        settings.IgnoreWhitespace.Should().BeFalse();
    }

    #endregion

    #region IgnoreComments Option

    [Fact]
    public void Settings_IgnoreComments_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { IgnoreComments = true };

        // Assert
        settings.IgnoreComments.Should().BeTrue();
    }

    [Fact]
    public void Settings_IgnoreComments_WhenSetToFalse_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { IgnoreComments = false };

        // Assert
        settings.IgnoreComments.Should().BeFalse();
    }

    #endregion

    #region Context Option

    [Fact]
    public void Settings_ContextLines_WhenSetToZero_ShouldBeZero()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { ContextLines = 0 };

        // Assert
        settings.ContextLines.Should().Be(0);
    }

    [Fact]
    public void Settings_ContextLines_WhenSetToTen_ShouldBeTen()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { ContextLines = 10 };

        // Assert
        settings.ContextLines.Should().Be(10);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(100)]
    public void Settings_ContextLines_WhenSetToPositiveValue_ShouldReturnThatValue(int lines)
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { ContextLines = lines };

        // Assert
        settings.ContextLines.Should().Be(lines);
    }

    #endregion

    #region Mode Option

    [Theory]
    [InlineData("auto")]
    [InlineData("roslyn")]
    [InlineData("line")]
    public void Settings_Mode_WhenSetToValidValue_ShouldReturnThatValue(string mode)
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { Mode = mode };

        // Assert
        settings.Mode.Should().Be(mode);
    }

    [Fact]
    public void Settings_Mode_WhenSetToAuto_ShouldBeAuto()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { Mode = "auto" };

        // Assert
        settings.Mode.Should().Be("auto");
    }

    [Fact]
    public void Settings_Mode_WhenSetToRoslyn_ShouldBeRoslyn()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { Mode = "roslyn" };

        // Assert
        settings.Mode.Should().Be("roslyn");
    }

    [Fact]
    public void Settings_Mode_WhenSetToLine_ShouldBeLine()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { Mode = "line" };

        // Assert
        settings.Mode.Should().Be("line");
    }

    #endregion

    #region OutputFormat Option

    [Theory]
    [InlineData("json")]
    [InlineData("html")]
    [InlineData("text")]
    [InlineData("terminal")]
    public void Settings_OutputFormat_WhenSetToValidValue_ShouldReturnThatValue(string format)
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { OutputFormat = format };

        // Assert
        settings.OutputFormat.Should().Be(format);
    }

    [Fact]
    public void Settings_OutputFormat_WhenSetToJson_ShouldBeJson()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { OutputFormat = "json" };

        // Assert
        settings.OutputFormat.Should().Be("json");
    }

    [Fact]
    public void Settings_OutputFormat_WhenSetToHtml_ShouldBeHtml()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { OutputFormat = "html" };

        // Assert
        settings.OutputFormat.Should().Be("html");
    }

    [Fact]
    public void Settings_OutputFormat_WhenSetToText_ShouldBeText()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { OutputFormat = "text" };

        // Assert
        settings.OutputFormat.Should().Be("text");
    }

    [Fact]
    public void Settings_OutputFormat_WhenSetToTerminal_ShouldBeTerminal()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { OutputFormat = "terminal" };

        // Assert
        settings.OutputFormat.Should().Be("terminal");
    }

    #endregion

    #region OutputFile Option

    [Fact]
    public void Settings_OutputFile_WhenSetToPath_ShouldReturnThatPath()
    {
        // Arrange
        const string expectedPath = "/path/to/output.txt";

        // Act
        var settings = new DiffCommand.Settings { OutputFile = expectedPath };

        // Assert
        settings.OutputFile.Should().Be(expectedPath);
    }

    [Fact]
    public void Settings_OutputFile_WhenSetToNull_ShouldBeNull()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { OutputFile = null };

        // Assert
        settings.OutputFile.Should().BeNull();
    }

    [Theory]
    [InlineData("output.json")]
    [InlineData("./results/diff.html")]
    [InlineData("/absolute/path/output.txt")]
    public void Settings_OutputFile_WhenSetToVariousPaths_ShouldReturnThatPath(string path)
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { OutputFile = path };

        // Assert
        settings.OutputFile.Should().Be(path);
    }

    #endregion

    #region RichOutput Option

    [Fact]
    public void Settings_RichOutput_WhenSetToTrue_ShouldBeTrue()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { RichOutput = true };

        // Assert
        settings.RichOutput.Should().BeTrue();
    }

    [Fact]
    public void Settings_RichOutput_WhenSetToFalse_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { RichOutput = false };

        // Assert
        settings.RichOutput.Should().BeFalse();
    }

    #endregion

    #region Option Combinations

    [Fact]
    public void Settings_WithIgnoreWhitespaceAndIgnoreComments_ShouldHaveBothTrue()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        // Assert
        settings.IgnoreWhitespace.Should().BeTrue();
        settings.IgnoreComments.Should().BeTrue();
    }

    [Fact]
    public void Settings_WithRoslynModeAndIgnoreComments_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            Mode = "roslyn",
            IgnoreComments = true
        };

        // Assert
        settings.Mode.Should().Be("roslyn");
        settings.IgnoreComments.Should().BeTrue();
    }

    [Fact]
    public void Settings_WithJsonOutputAndOutputFile_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            OutputFormat = "json",
            OutputFile = "results.json"
        };

        // Assert
        settings.OutputFormat.Should().Be("json");
        settings.OutputFile.Should().Be("results.json");
    }

    [Fact]
    public void Settings_WithRichOutputAndTerminalFormat_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            RichOutput = true,
            OutputFormat = "terminal"
        };

        // Assert
        settings.RichOutput.Should().BeTrue();
        settings.OutputFormat.Should().Be("terminal");
    }

    [Fact]
    public void Settings_WithAllOptionsSet_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            OldPath = "old.cs",
            NewPath = "new.cs",
            Mode = "roslyn",
            IgnoreWhitespace = true,
            IgnoreComments = true,
            ContextLines = 5,
            OutputFormat = "json",
            OutputFile = "diff-result.json",
            RichOutput = false
        };

        // Assert
        settings.OldPath.Should().Be("old.cs");
        settings.NewPath.Should().Be("new.cs");
        settings.Mode.Should().Be("roslyn");
        settings.IgnoreWhitespace.Should().BeTrue();
        settings.IgnoreComments.Should().BeTrue();
        settings.ContextLines.Should().Be(5);
        settings.OutputFormat.Should().Be("json");
        settings.OutputFile.Should().Be("diff-result.json");
        settings.RichOutput.Should().BeFalse();
    }

    [Fact]
    public void Settings_WithLineModeAndIgnoreWhitespace_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings
        {
            Mode = "line",
            IgnoreWhitespace = true,
            ContextLines = 0
        };

        // Assert
        settings.Mode.Should().Be("line");
        settings.IgnoreWhitespace.Should().BeTrue();
        settings.ContextLines.Should().Be(0);
    }

    #endregion

    #region File Paths

    [Theory]
    [InlineData("file.cs")]
    [InlineData("./relative/path/file.cs")]
    [InlineData("/absolute/path/file.cs")]
    [InlineData("file with spaces.cs")]
    public void Settings_OldPath_WhenSetToVariousPaths_ShouldReturnThatPath(string path)
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { OldPath = path };

        // Assert
        settings.OldPath.Should().Be(path);
    }

    [Theory]
    [InlineData("file.cs")]
    [InlineData("./relative/path/file.cs")]
    [InlineData("/absolute/path/file.cs")]
    [InlineData("file with spaces.cs")]
    public void Settings_NewPath_WhenSetToVariousPaths_ShouldReturnThatPath(string path)
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings { NewPath = path };

        // Assert
        settings.NewPath.Should().Be(path);
    }

    #endregion

    #region Invalid Option Values (Settings Level)

    [Theory]
    [InlineData("invalid")]
    [InlineData("ROSLYN")]
    [InlineData("LINE")]
    [InlineData("AUTO")]
    public void Settings_Mode_WhenSetToArbitraryString_ShouldStoreIt(string mode)
    {
        // Arrange & Act
        // Note: Settings allows any string value; validation happens at execution time
        var settings = new DiffCommand.Settings { Mode = mode };

        // Assert
        settings.Mode.Should().Be(mode);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("xml")]
    [InlineData("markdown")]
    public void Settings_OutputFormat_WhenSetToArbitraryString_ShouldStoreIt(string format)
    {
        // Arrange & Act
        // Note: Settings allows any string value; validation happens at execution time
        var settings = new DiffCommand.Settings { OutputFormat = format };

        // Assert
        settings.OutputFormat.Should().Be(format);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Settings_ContextLines_WhenSetToNegativeValue_ShouldStoreIt(int lines)
    {
        // Arrange & Act
        // Note: Settings allows any int value; validation could happen at execution time
        var settings = new DiffCommand.Settings { ContextLines = lines };

        // Assert
        settings.ContextLines.Should().Be(lines);
    }

    #endregion
}
