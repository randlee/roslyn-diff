namespace RoslynDiff.TestUtilities.Tests.Parsers;

using FluentAssertions;
using RoslynDiff.TestUtilities.Parsers;
using Xunit;

/// <summary>
/// Unit tests for <see cref="JsonOutputParser"/>.
/// </summary>
public class JsonOutputParserTests
{
    private readonly JsonOutputParser _parser = new();

    [Fact]
    public void FormatName_ShouldReturnJson()
    {
        // Assert
        _parser.FormatName.Should().Be("JSON");
    }

    [Fact]
    public void CanParse_WithValidJson_ShouldReturnTrue()
    {
        // Arrange
        var json = @"{""test"": ""value""}";

        // Act
        var result = _parser.CanParse(json);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanParse_WithInvalidJson_ShouldReturnFalse()
    {
        // Arrange
        var invalidJson = @"{""test"": invalid}";

        // Act
        var result = _parser.CanParse(invalidJson);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanParse_WithEmptyString_ShouldReturnFalse()
    {
        // Arrange
        var empty = string.Empty;

        // Act
        var result = _parser.CanParse(empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithEmptyString_ShouldReturnResultWithError()
    {
        // Arrange
        var empty = string.Empty;

        // Act
        var result = JsonOutputParser.Parse(empty);

        // Assert
        result.Should().NotBeNull();
        result.Format.Should().Be("json");
        result.IsValid.Should().BeFalse();
        result.ParsingErrors.Should().ContainSingle();
    }

    [Fact]
    public void Parse_WithInvalidJson_ShouldReturnResultWithError()
    {
        // Arrange
        var invalidJson = @"{invalid json}";

        // Act
        var result = JsonOutputParser.Parse(invalidJson);

        // Assert
        result.Should().NotBeNull();
        result.Format.Should().Be("json");
        result.IsValid.Should().BeFalse();
        result.ParsingErrors.Should().ContainSingle();
    }

    [Fact]
    public void Parse_WithValidMinimalJson_ShouldReturnValidResult()
    {
        // Arrange
        var json = @"{
            ""metadata"": {
                ""version"": ""0.5.0"",
                ""timestamp"": ""2026-01-17T02:59:00.611965+00:00"",
                ""mode"": ""roslyn""
            },
            ""summary"": {
                ""totalChanges"": 0,
                ""additions"": 0,
                ""deletions"": 0,
                ""modifications"": 0,
                ""renames"": 0,
                ""moves"": 0
            },
            ""files"": []
        }";

        // Act
        var result = JsonOutputParser.Parse(json);

        // Assert
        result.Should().NotBeNull();
        result.Format.Should().Be("json");
        result.IsValid.Should().BeTrue();
        result.Mode.Should().Be("roslyn");
        result.Version.Should().Be("0.5.0");
        result.Timestamp.Should().NotBeNull();
        result.Summary.Should().NotBeNull();
        result.Summary!.TotalChanges.Should().Be(0);
    }

    [Fact]
    public void Parse_WithChanges_ShouldExtractChanges()
    {
        // Arrange
        var json = @"{
            ""metadata"": {},
            ""summary"": {
                ""totalChanges"": 1,
                ""additions"": 1,
                ""deletions"": 0,
                ""modifications"": 0
            },
            ""files"": [
                {
                    ""oldPath"": ""old.cs"",
                    ""newPath"": ""new.cs"",
                    ""changes"": [
                        {
                            ""type"": ""added"",
                            ""kind"": ""method"",
                            ""name"": ""TestMethod"",
                            ""location"": {
                                ""startLine"": 10,
                                ""endLine"": 20,
                                ""startColumn"": 5,
                                ""endColumn"": 6
                            },
                            ""content"": ""public void TestMethod() { }""
                        }
                    ]
                }
            ]
        }";

        // Act
        var result = JsonOutputParser.Parse(json);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.OldPath.Should().Be("old.cs");
        result.NewPath.Should().Be("new.cs");
        result.Changes.Should().HaveCount(1);

        var change = result.Changes.First();
        change.ChangeType.Should().Be("added");
        change.Kind.Should().Be("method");
        change.Name.Should().Be("TestMethod");
        change.LineRange.Should().NotBeNull();
        change.LineRange!.Start.Should().Be(10);
        change.LineRange.End.Should().Be(20);
        change.Content.Should().Be("public void TestMethod() { }");
    }

    [Fact]
    public void Parse_WithNestedChanges_ShouldExtractChildren()
    {
        // Arrange
        var json = @"{
            ""metadata"": {},
            ""summary"": { ""totalChanges"": 1 },
            ""files"": [
                {
                    ""changes"": [
                        {
                            ""type"": ""modified"",
                            ""kind"": ""class"",
                            ""name"": ""TestClass"",
                            ""location"": {
                                ""startLine"": 1,
                                ""endLine"": 50
                            },
                            ""children"": [
                                {
                                    ""type"": ""added"",
                                    ""kind"": ""method"",
                                    ""name"": ""NewMethod"",
                                    ""location"": {
                                        ""startLine"": 10,
                                        ""endLine"": 15
                                    }
                                }
                            ]
                        }
                    ]
                }
            ]
        }";

        // Act
        var result = JsonOutputParser.Parse(json);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Changes.Should().HaveCount(1);

        var parentChange = result.Changes.First();
        parentChange.ChangeType.Should().Be("modified");
        parentChange.Children.Should().HaveCount(1);

        var childChange = parentChange.Children.First();
        childChange.ChangeType.Should().Be("added");
        childChange.Kind.Should().Be("method");
        childChange.Name.Should().Be("NewMethod");
    }

    [Fact]
    public void ExtractLineNumbers_ShouldReturnAllLineNumbers()
    {
        // Arrange
        var json = @"{
            ""files"": [
                {
                    ""changes"": [
                        {
                            ""type"": ""added"",
                            ""location"": { ""startLine"": 10, ""endLine"": 12 }
                        },
                        {
                            ""type"": ""removed"",
                            ""oldLocation"": { ""startLine"": 5, ""endLine"": 7 }
                        }
                    ]
                }
            ]
        }";

        // Act
        var lineNumbers = _parser.ExtractLineNumbers(json).ToList();

        // Assert
        lineNumbers.Should().NotBeEmpty();
        lineNumbers.Should().Contain(new[] { 5, 6, 7, 10, 11, 12 });
    }

    [Fact]
    public void ExtractLineRanges_ShouldReturnAllRanges()
    {
        // Arrange
        var json = @"{
            ""files"": [
                {
                    ""changes"": [
                        {
                            ""type"": ""added"",
                            ""location"": { ""startLine"": 10, ""endLine"": 12 }
                        }
                    ]
                }
            ]
        }";

        // Act
        var ranges = _parser.ExtractLineRanges(json).ToList();

        // Assert
        ranges.Should().HaveCount(1);
        ranges[0].Start.Should().Be(10);
        ranges[0].End.Should().Be(12);
    }

    [Fact]
    public void Parse_WithOldAndNewLocations_ShouldExtractBoth()
    {
        // Arrange
        var json = @"{
            ""files"": [
                {
                    ""changes"": [
                        {
                            ""type"": ""modified"",
                            ""location"": { ""startLine"": 10, ""endLine"": 15 },
                            ""oldLocation"": { ""startLine"": 5, ""endLine"": 8 }
                        }
                    ]
                }
            ]
        }";

        // Act
        var result = JsonOutputParser.Parse(json);

        // Assert
        result.Should().NotBeNull();
        result.Changes.Should().HaveCount(1);

        var change = result.Changes.First();
        change.LineRange.Should().NotBeNull();
        change.LineRange!.Start.Should().Be(10);
        change.LineRange.End.Should().Be(15);

        change.OldLineRange.Should().NotBeNull();
        change.OldLineRange!.Start.Should().Be(5);
        change.OldLineRange.End.Should().Be(8);
    }

    [Fact]
    public void Normalize_ShouldRemoveWhitespace()
    {
        // Arrange
        var json = @"{
            ""test"": ""value"",
            ""number"": 123
        }";

        // Act
        var normalized = _parser.Normalize(json);

        // Assert
        normalized.Should().NotContain("\n");
        normalized.Should().NotContain("  ");
        normalized.Should().Contain("\"test\"");
        normalized.Should().Contain("\"value\"");
    }
}
