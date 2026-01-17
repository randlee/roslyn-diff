namespace RoslynDiff.Output.Tests;

using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="HtmlFormatter"/>.
/// </summary>
public class HtmlFormatterTests
{
    private readonly HtmlFormatter _formatter = new();

    [Fact]
    public void Format_ShouldBeHtml()
    {
        // Assert
        _formatter.Format.Should().Be("html");
    }

    [Fact]
    public void ContentType_ShouldBeTextHtml()
    {
        // Assert
        _formatter.ContentType.Should().Be("text/html");
    }

    [Fact]
    public void Format_EmptyResult_ShouldProduceValidHtml()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().NotBeNullOrWhiteSpace();
        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<html");
        html.Should().Contain("</html>");
        html.Should().Contain("<head>");
        html.Should().Contain("</head>");
        html.Should().Contain("<body>");
        html.Should().Contain("</body>");
    }

    [Fact]
    public void Format_EmptyResult_ShouldShowNoChangesMessage()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("No changes detected");
    }

    [Fact]
    public void Format_WithFilePaths_ShouldIncludeInTitle()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "/path/to/OldFile.cs",
            NewPath = "/path/to/NewFile.cs"
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("<title>");
        html.Should().Contain("OldFile.cs");
    }

    [Fact]
    public void Format_WithStats_ShouldShowSummary()
    {
        // Arrange
        var result = new DiffResult
        {
            Stats = new DiffStats
            {
                Additions = 5,
                Deletions = 3,
                Modifications = 2
            }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("10 changes");
        html.Should().Contain("+5 added");
        html.Should().Contain("-3 deleted");
        html.Should().Contain("~2 modified");
    }

    [Fact]
    public void Format_WithoutStats_ShouldOmitSummary()
    {
        // Arrange
        var result = new DiffResult
        {
            Stats = new DiffStats { Additions = 5 }
        };
        var options = new OutputOptions { IncludeStats = false };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        // Note: CSS class definitions will still be present in <style>, but actual stat content should be absent
        html.Should().NotContain("+5 added");
        html.Should().NotContain("5 changes</span>");
    }

    [Fact]
    public void Format_ShouldContainInlineStyles()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("<style>");
        html.Should().Contain("</style>");
        html.Should().Contain(".diff-container");
        html.Should().Contain(".line-added");
        html.Should().Contain(".line-removed");
        html.Should().Contain(".line-modified");
    }

    [Fact]
    public void Format_ShouldContainInlineScript()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("<script>");
        html.Should().Contain("</script>");
        html.Should().Contain("toggleTopDiff");
        html.Should().Contain("toggleChange");
    }

    [Fact]
    public void Format_WithChange_ShouldShowSideBySideView()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "TestMethod",
                            OldContent = "public void Test() { }",
                            NewContent = "public void Test() { return; }",
                            OldLocation = new Location { StartLine = 1, EndLine = 1 },
                            NewLocation = new Location { StartLine = 1, EndLine = 1 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("diff-side diff-old");
        html.Should().Contain("diff-side diff-new");
        html.Should().Contain("Old Version");
        html.Should().Contain("New Version");
    }

    [Fact]
    public void Format_WithAddedChange_ShouldShowAddedBadge()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            NewContent = "public void NewMethod() { }",
                            NewLocation = new Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("badge-added");
        html.Should().Contain("Added");
    }

    [Fact]
    public void Format_WithRemovedChange_ShouldShowRemovedBadge()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Removed,
                            Kind = ChangeKind.Method,
                            Name = "OldMethod",
                            OldContent = "public void OldMethod() { }",
                            OldLocation = new Location { StartLine = 5, EndLine = 5 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Deletions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("badge-removed");
        html.Should().Contain("Removed");
    }

    [Fact]
    public void HtmlEncode_ShouldEscapeSpecialCharacters()
    {
        // Arrange & Act & Assert
        HtmlFormatter.HtmlEncode("<").Should().Be("&lt;");
        HtmlFormatter.HtmlEncode(">").Should().Be("&gt;");
        HtmlFormatter.HtmlEncode("&").Should().Be("&amp;");
        HtmlFormatter.HtmlEncode("\"").Should().Be("&quot;");
        HtmlFormatter.HtmlEncode("'").Should().Be("&#39;");
    }

    [Fact]
    public void HtmlEncode_WithNull_ShouldReturnEmpty()
    {
        // Act & Assert
        HtmlFormatter.HtmlEncode(null).Should().BeEmpty();
        HtmlFormatter.HtmlEncode(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void HtmlEncode_WithCodeContent_ShouldEscapeAll()
    {
        // Arrange
        var code = "if (x < 10 && y > 5) { return \"test\"; }";

        // Act
        var encoded = HtmlFormatter.HtmlEncode(code);

        // Assert
        encoded.Should().Contain("&lt;");
        encoded.Should().Contain("&gt;");
        encoded.Should().Contain("&amp;");
        encoded.Should().Contain("&quot;");
        encoded.Should().NotContain("<");
        encoded.Should().NotContain(">");
    }

    [Fact]
    public void Format_WithSpecialCharactersInCode_ShouldEscapeProperly()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "Test",
                            NewContent = "if (x < 10 && y > 5) { return \"test\"; }",
                            NewLocation = new Location { StartLine = 1, EndLine = 1 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("&lt;");
        html.Should().Contain("&gt;");
        html.Should().Contain("&amp;");
    }

    [Fact]
    public void HighlightSyntax_WithCSharpKeywords_ShouldAddKeywordClass()
    {
        // Arrange
        var code = "public class Test { }";

        // Act
        var highlighted = HtmlFormatter.HighlightSyntax(code, ".cs");

        // Assert
        highlighted.Should().Contain("<span class=\"keyword\">public</span>");
        highlighted.Should().Contain("<span class=\"keyword\">class</span>");
    }

    [Fact]
    public void HighlightSyntax_WithStrings_ShouldAddStringClass()
    {
        // Arrange
        var code = "var x = \"hello\";";

        // Act
        var highlighted = HtmlFormatter.HighlightSyntax(code, ".cs");

        // Assert
        highlighted.Should().Contain("<span class=\"string\">");
    }

    [Fact]
    public void HighlightSyntax_WithComments_ShouldAddCommentClass()
    {
        // Arrange
        var code = "// This is a comment";

        // Act
        var highlighted = HtmlFormatter.HighlightSyntax(code, ".cs");

        // Assert
        highlighted.Should().Contain("<span class=\"comment\">");
    }

    [Fact]
    public void HighlightSyntax_WithNumbers_ShouldAddNumberClass()
    {
        // Arrange
        var code = "var x = 42;";

        // Act
        var highlighted = HtmlFormatter.HighlightSyntax(code, ".cs");

        // Assert
        highlighted.Should().Contain("<span class=\"number\">42</span>");
    }

    [Fact]
    public void HighlightSyntax_WithVbCode_ShouldHighlightVbKeywords()
    {
        // Arrange
        var code = "Public Sub Test()";

        // Act
        var highlighted = HtmlFormatter.HighlightSyntax(code, ".vb");

        // Assert
        highlighted.Should().Contain("<span class=\"keyword\">Public</span>");
        highlighted.Should().Contain("<span class=\"keyword\">Sub</span>");
    }

    [Fact]
    public void HighlightSyntax_WithEmptyCode_ShouldReturnEmpty()
    {
        // Act & Assert
        HtmlFormatter.HighlightSyntax(string.Empty, ".cs").Should().BeEmpty();
        HtmlFormatter.HighlightSyntax(null!, ".cs").Should().BeEmpty();
    }

    [Fact]
    public void Format_ShouldIncludeNavigationButtons()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("nav-container");
        html.Should().Contain("nav-btn");
        html.Should().Contain("scrollToTop");
        html.Should().Contain("prevChange");
        html.Should().Contain("nextChange");
    }

    [Fact]
    public void Format_ShouldIncludeCollapsibleSections()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "TestMethod",
                            OldContent = "old",
                            NewContent = "new"
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("top-diff-header");
        html.Should().Contain("expand-icon");
        html.Should().Contain("change-header");
        html.Should().Contain("change-body");
    }

    [Fact]
    public void Format_ShouldIncludePrintStyles()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("@media print");
    }

    [Fact]
    public void Format_ShouldIncludeResponsiveStyles()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("@media (max-width: 768px)");
        html.Should().Contain("viewport");
    }

    [Fact]
    public void Format_WithLineNumbers_ShouldShowLineNumbers()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "Test",
                            OldContent = "line1",
                            NewContent = "line2",
                            OldLocation = new Location { StartLine = 10, EndLine = 10 },
                            NewLocation = new Location { StartLine = 15, EndLine = 15 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("line-number");
        html.Should().Contain(">10<");
        html.Should().Contain(">15<");
    }

    [Fact]
    public void Format_WithNestedChanges_ShouldRenderHierarchy()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Class,
                            Name = "TestClass",
                            Children =
                            [
                                new Change
                                {
                                    Type = ChangeType.Added,
                                    Kind = ChangeKind.Method,
                                    Name = "NewMethod",
                                    NewContent = "public void NewMethod() { }",
                                    NewLocation = new Location { StartLine = 5, EndLine = 5 }
                                }
                            ]
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1, Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("TestClass");
        html.Should().Contain("NewMethod");
        html.Should().Contain("Class");
        html.Should().Contain("Method");
    }

    [Fact]
    public async Task FormatAsync_ShouldWriteToWriter()
    {
        // Arrange
        var result = new DiffResult();
        using var writer = new StringWriter();

        // Act
        await _formatter.FormatResultAsync(result, writer);

        // Assert
        var html = writer.ToString();
        html.Should().NotBeNullOrWhiteSpace();
        html.Should().StartWith("<!DOCTYPE html>");
    }

    [Fact]
    public void Format_WithMoveChange_ShouldShowMovedBadge()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Moved,
                            Kind = ChangeKind.Method,
                            Name = "MovedMethod",
                            OldContent = "public void MovedMethod() { }",
                            NewContent = "public void MovedMethod() { }",
                            OldLocation = new Location { StartLine = 10, EndLine = 10 },
                            NewLocation = new Location { StartLine = 50, EndLine = 50 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Moves = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("badge-moved");
        html.Should().Contain("Moved");
    }

    [Fact]
    public void Format_WithRenamedChange_ShouldShowRenamedBadge()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Renamed,
                            Kind = ChangeKind.Method,
                            Name = "RenamedMethod",
                            OldContent = "public void OldName() { }",
                            NewContent = "public void RenamedMethod() { }",
                            OldLocation = new Location { StartLine = 5, EndLine = 5 },
                            NewLocation = new Location { StartLine = 5, EndLine = 5 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Renames = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("badge-renamed");
        html.Should().Contain("Renamed");
    }

    [Fact]
    public void Format_ShouldProduceSelfContainedHtml()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "Old.cs",
            NewPath = "New.cs",
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "Test",
                            OldContent = "public void Test() { }",
                            NewContent = "public void Test() { return; }"
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert - Verify no external dependencies
        html.Should().NotContain("href=\"http");
        html.Should().NotContain("src=\"http");
        html.Should().NotContain("<link rel=\"stylesheet\"");
        html.Should().NotContain("<script src=");

        // Verify self-contained
        html.Should().Contain("<style>");
        html.Should().Contain("<script>");
    }

    [Fact]
    public void Format_WithKeyboardNavigation_ShouldIncludeEventListeners()
    {
        // Arrange
        var result = new DiffResult();

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("addEventListener");
        html.Should().Contain("keydown");
    }

    [Fact]
    public void Format_WithDiffMode_ShouldBeValid()
    {
        // Arrange
        var result = new DiffResult
        {
            Mode = DiffMode.Roslyn
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().NotBeNullOrWhiteSpace();
        // HTML should be valid regardless of mode
        html.Should().StartWith("<!DOCTYPE html>");
    }

    [Fact]
    public void HighlightSyntax_WithUnknownExtension_ShouldReturnEncodedCode()
    {
        // Arrange
        var code = "some <code>";

        // Act
        var highlighted = HtmlFormatter.HighlightSyntax(code, ".unknown");

        // Assert
        highlighted.Should().Be("some &lt;code&gt;");
    }

    [Fact]
    public void Format_MultipleFileChanges_ShouldRenderAll()
    {
        // Arrange
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "File1.cs",
                    Changes = [new Change { Type = ChangeType.Added, Kind = ChangeKind.Class, Name = "Class1" }]
                },
                new FileChange
                {
                    Path = "File2.cs",
                    Changes = [new Change { Type = ChangeType.Removed, Kind = ChangeKind.Class, Name = "Class2" }]
                }
            ],
            Stats = new DiffStats { Additions = 1, Deletions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("File1.cs");
        html.Should().Contain("File2.cs");
        html.Should().Contain("Class1");
        html.Should().Contain("Class2");
    }

    [Fact]
    public void Format_WithMultilineContent_ShouldShowAllLines()
    {
        // Arrange
        var multilineContent = "line1\nline2\nline3";
        var result = new DiffResult
        {
            FileChanges =
            [
                new FileChange
                {
                    Path = "Test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "Test",
                            NewContent = multilineContent,
                            NewLocation = new Location { StartLine = 1, EndLine = 3 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var html = _formatter.FormatResult(result);

        // Assert
        html.Should().Contain("line1");
        html.Should().Contain("line2");
        html.Should().Contain("line3");
        html.Should().Contain(">1<");
        html.Should().Contain(">2<");
        html.Should().Contain(">3<");
    }
}
