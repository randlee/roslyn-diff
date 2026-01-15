namespace RoslynDiff.Output.Tests;

using System.Text.Json;
using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="JsonOutputFormatter"/>.
/// </summary>
public class JsonOutputFormatterTests
{
    private readonly JsonOutputFormatter _formatter = new();

    [Fact]
    public void FormatName_ShouldBeJson()
    {
        // Assert
        _formatter.FormatName.Should().Be("json");
    }

    [Fact]
    public void Format_EmptyResult_ShouldProduceValidJson()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var json = _formatter.Format(result);

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        var action = () => JsonDocument.Parse(json);
        action.Should().NotThrow();
    }

    [Fact]
    public void Format_WithOptions_ShouldRespectIndentation()
    {
        // Arrange
        var result = new DiffResult();
        var options = new OutputOptions { IndentJson = false };

        // Act
        var json = _formatter.Format(result, options);

        // Assert
        json.Should().NotContain("\n  "); // No indentation
    }

    [Fact]
    public async Task FormatAsync_ShouldWriteToWriter()
    {
        // Arrange
        var result = new DiffResult();
        using var writer = new StringWriter();

        // Act
        await _formatter.FormatAsync(result, writer);

        // Assert
        writer.ToString().Should().NotBeNullOrWhiteSpace();
    }
}
