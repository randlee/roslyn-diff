namespace RoslynDiff.Output.Tests;

using System.Text.Json;
using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="JsonOutputFormatter"/> (deprecated formatter).
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
public class JsonOutputFormatterTests
{
    private readonly JsonOutputFormatter _formatter = new();

    [Fact]
    public void Format_ShouldBeJsonRaw()
    {
        // Assert - The Format property for the deprecated formatter is "json-raw"
        _formatter.Format.Should().Be("json-raw");
    }

    [Fact]
    public void ContentType_ShouldBeApplicationJson()
    {
        // Assert
        _formatter.ContentType.Should().Be("application/json");
    }

    [Fact]
    public void FormatName_ShouldBeJson()
    {
        // Assert - Legacy property still works
        _formatter.FormatName.Should().Be("json");
    }

    [Fact]
    public void FormatResult_EmptyResult_ShouldProduceValidJson()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var json = _formatter.FormatResult(result);

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        var action = () => JsonDocument.Parse(json);
        action.Should().NotThrow();
    }

    [Fact]
    public void FormatResult_WithOptions_ShouldRespectIndentation()
    {
        // Arrange
        var result = new DiffResult();
        var options = new OutputOptions { PrettyPrint = false };

        // Act
        var json = _formatter.FormatResult(result, options);

        // Assert
        json.Should().NotContain("\n  "); // No indentation
    }

    [Fact]
    public async Task FormatResultAsync_ShouldWriteToWriter()
    {
        // Arrange
        var result = new DiffResult();
        using var writer = new StringWriter();

        // Act
        await _formatter.FormatResultAsync(result, writer);

        // Assert
        writer.ToString().Should().NotBeNullOrWhiteSpace();
    }
}
#pragma warning restore CS0618
