namespace RoslynDiff.Cli.Tests;

using FluentAssertions;
using RoslynDiff.Cli.Commands;
using Xunit;

/// <summary>
/// Unit tests for <see cref="DiffCommand"/>.
/// </summary>
public class DiffCommandTests
{
    [Fact]
    public void Settings_DefaultFormat_ShouldBeUnified()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.Format.Should().Be("unified");
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
    public void Settings_DefaultIgnoreFlags_ShouldBeFalse()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.IgnoreWhitespace.Should().BeFalse();
        settings.IgnoreComments.Should().BeFalse();
        settings.LineMode.Should().BeFalse();
    }

    [Fact]
    public void Settings_OutputFile_ShouldBeNullByDefault()
    {
        // Arrange & Act
        var settings = new DiffCommand.Settings();

        // Assert
        settings.OutputFile.Should().BeNull();
    }
}
