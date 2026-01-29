namespace RoslynDiff.Output.Tests;

using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="OutputFormatterFactory"/>.
/// </summary>
public class OutputFormatterFactoryTests
{
    private readonly OutputFormatterFactory _factory = new();

    #region SupportedFormats

    [Fact]
    public void SupportedFormats_ShouldContainJson()
    {
        _factory.SupportedFormats.Should().Contain("json");
    }

    [Fact]
    public void SupportedFormats_ShouldContainText()
    {
        _factory.SupportedFormats.Should().Contain("text");
    }

    [Fact]
    public void SupportedFormats_ShouldNotBeEmpty()
    {
        _factory.SupportedFormats.Should().NotBeEmpty();
    }

    #endregion

    #region GetFormatter

    [Fact]
    public void GetFormatter_Json_ShouldReturnJsonFormatter()
    {
        // Act
        var formatter = _factory.GetFormatter("json");

        // Assert
        formatter.Should().BeOfType<JsonFormatter>();
        formatter.Format.Should().Be("json");
        formatter.ContentType.Should().Be("application/json");
    }

    [Fact]
    public void GetFormatter_Text_ShouldReturnUnifiedFormatter()
    {
        // Act
        var formatter = _factory.GetFormatter("text");

        // Assert
        formatter.Should().BeOfType<UnifiedFormatter>();
        formatter.Format.Should().Be("text");
        formatter.ContentType.Should().Be("text/plain");
    }

    [Fact]
    public void GetFormatter_CaseInsensitive_ShouldWork()
    {
        // Act & Assert
        _factory.GetFormatter("JSON").Should().BeOfType<JsonFormatter>();
        _factory.GetFormatter("Json").Should().BeOfType<JsonFormatter>();
        _factory.GetFormatter("TEXT").Should().BeOfType<UnifiedFormatter>();
    }

    [Fact]
    public void GetFormatter_UnsupportedFormat_ShouldThrowArgumentException()
    {
        // Act
        var action = () => _factory.GetFormatter("unsupported");

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*unsupported*")
            .WithParameterName("format");
    }

    [Fact]
    public void GetFormatter_NullFormat_ShouldThrowArgumentException()
    {
        // Act
        var action = () => _factory.GetFormatter(null!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetFormatter_EmptyFormat_ShouldThrowArgumentException()
    {
        // Act
        var action = () => _factory.GetFormatter("");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetFormatter_WhitespaceFormat_ShouldThrowArgumentException()
    {
        // Act
        var action = () => _factory.GetFormatter("   ");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region IsFormatSupported

    [Fact]
    public void IsFormatSupported_Json_ShouldReturnTrue()
    {
        _factory.IsFormatSupported("json").Should().BeTrue();
    }

    [Fact]
    public void IsFormatSupported_Text_ShouldReturnTrue()
    {
        _factory.IsFormatSupported("text").Should().BeTrue();
    }

    [Fact]
    public void IsFormatSupported_UnsupportedFormat_ShouldReturnFalse()
    {
        _factory.IsFormatSupported("unsupported").Should().BeFalse();
    }

    [Fact]
    public void IsFormatSupported_Null_ShouldReturnFalse()
    {
        _factory.IsFormatSupported(null!).Should().BeFalse();
    }

    [Fact]
    public void IsFormatSupported_Empty_ShouldReturnFalse()
    {
        _factory.IsFormatSupported("").Should().BeFalse();
    }

    [Fact]
    public void IsFormatSupported_CaseInsensitive_ShouldWork()
    {
        _factory.IsFormatSupported("JSON").Should().BeTrue();
        _factory.IsFormatSupported("Json").Should().BeTrue();
    }

    #endregion

    #region RegisterFormatter

    [Fact]
    public void RegisterFormatter_CustomFormatter_ShouldBeAvailable()
    {
        // Arrange
        var customFormatter = new CustomTestFormatter();
        _factory.RegisterFormatter("custom", () => customFormatter);

        // Act
        var formatter = _factory.GetFormatter("custom");

        // Assert
        formatter.Should().BeSameAs(customFormatter);
        _factory.SupportedFormats.Should().Contain("custom");
    }

    [Fact]
    public void RegisterFormatter_OverrideExisting_ShouldReplaceFormatter()
    {
        // Arrange
        var customFormatter = new CustomTestFormatter();
        _factory.RegisterFormatter("json", () => customFormatter);

        // Act
        var formatter = _factory.GetFormatter("json");

        // Assert
        formatter.Should().BeSameAs(customFormatter);
    }

    [Fact]
    public void RegisterFormatter_NullFormat_ShouldThrowArgumentException()
    {
        // Act
        var action = () => _factory.RegisterFormatter(null!, () => new JsonFormatter());

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegisterFormatter_NullFactory_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => _factory.RegisterFormatter("custom", null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Test Helpers

    private sealed class CustomTestFormatter : IOutputFormatter
    {
        public string Format => "custom";
        public string ContentType => "text/custom";

        public string FormatResult(RoslynDiff.Core.Models.DiffResult result, OutputOptions? options = null)
            => "custom output";

        public Task FormatResultAsync(RoslynDiff.Core.Models.DiffResult result, TextWriter writer, OutputOptions? options = null)
        {
            writer.Write("custom output");
            return Task.CompletedTask;
        }

        public string FormatMultiFileResult(RoslynDiff.Core.Models.MultiFileDiffResult result, OutputOptions? options = null)
            => "custom multi-file output";

        public Task FormatMultiFileResultAsync(RoslynDiff.Core.Models.MultiFileDiffResult result, TextWriter writer, OutputOptions? options = null)
        {
            writer.Write("custom multi-file output");
            return Task.CompletedTask;
        }
    }

    #endregion
}
