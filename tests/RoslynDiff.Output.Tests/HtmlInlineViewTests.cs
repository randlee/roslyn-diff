namespace RoslynDiff.Output.Tests;

using FluentAssertions;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for HTML Inline View functionality.
/// Tests inline diff view mode with full file and context modes.
/// </summary>
public class HtmlInlineViewTests : IDisposable
{
    private readonly HtmlFormatter _formatter = new();
    private readonly string _tempDirectory;

    public HtmlInlineViewTests()
    {
        // Create temporary directory for CSS file output
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"roslyn-diff-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        // Clean up temporary directory
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    #region Inline View Structure Tests

    [Fact]
    public void Format_InlineView_ContainsDiffInlineContainer()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("<div class=\"diff-inline\">");
        // File-level overview should use inline view, not side-by-side
        // But individual changes section may still use side-by-side for detailed diff
        var fileOverviewSection = ExtractFileOverviewSection(html);
        fileOverviewSection.Should().Contain("diff-inline");
        fileOverviewSection.Should().NotContain("diff-side diff-old");
    }

    [Fact]
    public void Format_InlineView_ContainsLineNumbers()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("class=\"line-number\"");
        html.Should().Contain(">10<");
    }

    [Fact]
    public void Format_InlineView_ContainsLinePrefixes()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("class=\"line-prefix\"");
        html.Should().Contain("<span class=\"line-prefix\">-</span>");
        html.Should().Contain("<span class=\"line-prefix\">+</span>");
    }

    [Fact]
    public void Format_InlineView_ContainsLineAddedClass()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
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
            ],
            Stats = new DiffStats { Additions = 1 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("class=\"diff-line line-added\"");
    }

    [Fact]
    public void Format_InlineView_ContainsLineRemovedClass()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Removed,
                            Kind = ChangeKind.Method,
                            Name = "OldMethod",
                            OldContent = "public void OldMethod() { }",
                            OldLocation = new Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Deletions = 1 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("class=\"diff-line line-removed\"");
    }

    #endregion

    #region Full File Mode Tests

    [Fact]
    public void Format_InlineView_FullFileMode_ShowsAllChanges()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Removed,
                            Kind = ChangeKind.Method,
                            Name = "OldMethod",
                            OldContent = "public void OldMethod() { }",
                            OldLocation = new Location { StartLine = 5, EndLine = 5 }
                        },
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
            Stats = new DiffStats { Additions = 1, Deletions = 1 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline,
            InlineContext = null // Full file mode
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("OldMethod");
        html.Should().Contain("NewMethod");
        html.Should().Contain("line-added");
        html.Should().Contain("line-removed");
        // In full file mode, no separators between changes at different line numbers
        var inlineContentSection = ExtractInlineContent(html);
        inlineContentSection.Should().NotContain("diff-line-separator");
    }

    [Fact]
    public void Format_InlineView_FullFileMode_ShowsMultilineChanges()
    {
        // Arrange
        var multilineContent = "public void TestMethod()\n{\n    return 42;\n}";
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "TestMethod",
                            NewContent = multilineContent,
                            NewLocation = new Location { StartLine = 10, EndLine = 13 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline,
            InlineContext = null
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain(">10<");
        html.Should().Contain(">11<");
        html.Should().Contain(">12<");
        html.Should().Contain(">13<");
        html.Should().Contain("public void TestMethod()");
        html.Should().Contain("return 42;");
    }

    #endregion

    #region Context Mode Tests

    [Fact]
    public void Format_InlineView_ContextMode_ShowsContextSeparators()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "Method1",
                            OldContent = "public void Method1() { }",
                            NewContent = "public void Method1() { return; }",
                            OldLocation = new Location { StartLine = 5, EndLine = 5 },
                            NewLocation = new Location { StartLine = 5, EndLine = 5 }
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "Method2",
                            OldContent = "public void Method2() { }",
                            NewContent = "public void Method2() { return; }",
                            OldLocation = new Location { StartLine = 50, EndLine = 50 },
                            NewLocation = new Location { StartLine = 50, EndLine = 50 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 2 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline,
            InlineContext = 3
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("diff-line-separator");
        html.Should().Contain("<span class=\"line-number\">...</span>");
        html.Should().Contain("<span class=\"line-content\">...</span>");
    }

    [Fact]
    public void Format_InlineView_ContextMode_NoSeparatorForAdjacentChanges()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "Method1",
                            OldContent = "public void Method1() { }",
                            NewContent = "public void Method1() { return; }",
                            OldLocation = new Location { StartLine = 5, EndLine = 5 },
                            NewLocation = new Location { StartLine = 5, EndLine = 5 }
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "Method2",
                            OldContent = "public void Method2() { }",
                            NewContent = "public void Method2() { return; }",
                            // Line 6 is adjacent to line 5, no gap
                            OldLocation = new Location { StartLine = 6, EndLine = 6 },
                            NewLocation = new Location { StartLine = 6, EndLine = 6 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 2 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline,
            InlineContext = 3
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        // Adjacent changes (line 5 and line 6) should not have separator
        // But check only in the inline content section to avoid false positives
        var inlineContentSection = ExtractInlineContent(html);

        // Count separators - there should be none for adjacent lines
        var separatorCount = System.Text.RegularExpressions.Regex.Matches(inlineContentSection, "diff-line-separator").Count;
        separatorCount.Should().Be(0, "adjacent changes should not have separators");
    }

    [Fact]
    public void Format_InlineView_ContextModeWithZeroLines_ShowsSeparators()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "Method1",
                            OldContent = "public void Method1() { }",
                            NewContent = "public void Method1() { return; }",
                            OldLocation = new Location { StartLine = 5, EndLine = 5 },
                            NewLocation = new Location { StartLine = 5, EndLine = 5 }
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "Method2",
                            OldContent = "public void Method2() { }",
                            NewContent = "public void Method2() { return; }",
                            OldLocation = new Location { StartLine = 20, EndLine = 20 },
                            NewLocation = new Location { StartLine = 20, EndLine = 20 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 2 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline,
            InlineContext = 0
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("diff-line-separator");
    }

    #endregion

    #region Semantic Diff Tests

    [Fact]
    public void Format_InlineView_SemanticDiff_ShowsMethodHeaders()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "TestMethod",
                            OldContent = "public void TestMethod() { }",
                            NewContent = "public void TestMethod() { return; }",
                            OldLocation = new Location { StartLine = 10, EndLine = 10 },
                            NewLocation = new Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("// Modified: Method TestMethod");
        html.Should().Contain("line-semantic-header");
    }

    [Fact]
    public void Format_InlineView_SemanticDiff_ShowsClassHeaders()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Class,
                            Name = "NewClass",
                            NewContent = "public class NewClass { }",
                            NewLocation = new Location { StartLine = 15, EndLine = 15 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("// Added: Class NewClass");
        html.Should().Contain("line-semantic-header");
    }

    [Fact]
    public void Format_InlineView_SemanticDiff_ShowsPropertyHeaders()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Removed,
                            Kind = ChangeKind.Property,
                            Name = "OldProperty",
                            OldContent = "public string OldProperty { get; set; }",
                            OldLocation = new Location { StartLine = 8, EndLine = 8 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Deletions = 1 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("// Removed: Property OldProperty");
        html.Should().Contain("line-semantic-header");
    }

    [Fact]
    public void Format_InlineView_SemanticDiff_DoesNotShowFileHeaders()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.File,
                            Name = "test.cs",
                            OldContent = "// old",
                            NewContent = "// new",
                            OldLocation = new Location { StartLine = 1, EndLine = 1 },
                            NewLocation = new Location { StartLine = 1, EndLine = 1 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().NotContain("// Modified: File");
        html.Should().Contain("// old");
        html.Should().Contain("// new");
    }

    #endregion

    #region Line Diff Tests

    [Fact]
    public void Format_InlineView_LineDiff_DoesNotShowSemanticHeaders()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Line,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Line,
                            Name = null,
                            OldContent = "var x = 1;",
                            NewContent = "var x = 2;",
                            OldLocation = new Location { StartLine = 5, EndLine = 5 },
                            NewLocation = new Location { StartLine = 5, EndLine = 5 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        // For line diffs, semantic headers should not be shown in inline content
        // but may appear in individual change details section
        var inlineContentSection = ExtractInlineContent(html);
        inlineContentSection.Should().NotContain("// Modified:");
        html.Should().Contain("var x = 1;");
        html.Should().Contain("var x = 2;");
    }

    [Fact]
    public void Format_InlineView_LineDiff_ShowsLineChanges()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Line,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Line,
                            Name = null,
                            NewContent = "// new line",
                            NewLocation = new Location { StartLine = 10, EndLine = 10 }
                        },
                        new Change
                        {
                            Type = ChangeType.Removed,
                            Kind = ChangeKind.Line,
                            Name = null,
                            OldContent = "// old line",
                            OldLocation = new Location { StartLine = 15, EndLine = 15 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1, Deletions = 1 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("// new line");
        html.Should().Contain("// old line");
        html.Should().Contain("line-added");
        html.Should().Contain("line-removed");
    }

    #endregion

    #region Fragment Mode Integration Tests

    [Fact]
    public void Format_InlineView_WithFragmentMode_GeneratesFragment()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            ViewMode = ViewMode.Inline,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("roslyn-diff-fragment");
        html.Should().NotContain("<!DOCTYPE html>");
        html.Should().Contain("diff-inline");
    }

    [Fact]
    public void Format_InlineView_WithFragmentMode_ReferencesExternalCss()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            ViewMode = ViewMode.Inline,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("<link rel=\"stylesheet\" href=\"roslyn-diff.css\">");
    }

    [Fact]
    public void Format_InlineView_WithFragmentMode_GeneratesCssWithInlineStyles()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var htmlOutputPath = Path.Combine(_tempDirectory, "test.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            ViewMode = ViewMode.Inline,
            HtmlOutputPath = htmlOutputPath
        };

        // Act
        _formatter.FormatResult(result, options);

        // Assert
        var cssPath = Path.Combine(_tempDirectory, "roslyn-diff.css");
        File.Exists(cssPath).Should().BeTrue("CSS file should be generated");

        var cssContent = File.ReadAllText(cssPath);
        cssContent.Should().Contain(".diff-inline");
        cssContent.Should().Contain(".line-added");
        cssContent.Should().Contain(".line-removed");
        cssContent.Should().Contain(".line-prefix");
        cssContent.Should().Contain(".diff-line-separator");
    }

    #endregion

    #region Document Mode Integration Tests

    [Fact]
    public void Format_InlineView_WithDocumentMode_GeneratesFullDocument()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Document,
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().StartWith("<!DOCTYPE html>");
        html.Should().Contain("<html");
        html.Should().Contain("<head>");
        html.Should().Contain("<body>");
        html.Should().Contain("<style>");
        html.Should().Contain("diff-inline");
    }

    [Fact]
    public void Format_InlineView_WithDocumentMode_ContainsInlineStyles()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Document,
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("<style>");
        html.Should().Contain(".diff-inline");
        html.Should().Contain(".line-added");
        html.Should().Contain(".line-removed");
        html.Should().Contain(".line-prefix");
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void Format_InlineView_EmptyChanges_ShowsNoChangesMessage()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes = []
                }
            ],
            Stats = new DiffStats()
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("(no changes)");
    }

    [Fact]
    public void Format_InlineView_WithSpecialCharacters_EscapesProperly()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
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
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("&lt;");
        html.Should().Contain("&gt;");
        html.Should().Contain("&amp;");
        html.Should().Contain("&quot;");
    }

    [Fact]
    public void Format_InlineView_WithNullContent_HandlesGracefully()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "TestMethod",
                            OldContent = null,
                            NewContent = null,
                            OldLocation = new Location { StartLine = 10, EndLine = 10 },
                            NewLocation = new Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var act = () => _formatter.FormatResult(result, options);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Format_InlineView_WithNestedChanges_ShowsRootLevelOnly()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
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
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        // Should show the parent class change in inline view, not nested children
        html.Should().Contain("TestClass");
    }

    [Fact]
    public void Format_InlineView_WithMultipleFileChanges_ShowsAllFiles()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "File1.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Class,
                            Name = "Class1",
                            NewContent = "public class Class1 { }",
                            NewLocation = new Location { StartLine = 1, EndLine = 1 }
                        }
                    ]
                },
                new FileChange
                {
                    Path = "File2.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Removed,
                            Kind = ChangeKind.Class,
                            Name = "Class2",
                            OldContent = "public class Class2 { }",
                            OldLocation = new Location { StartLine = 1, EndLine = 1 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1, Deletions = 1 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("File1.cs");
        html.Should().Contain("File2.cs");
        html.Should().Contain("Class1");
        html.Should().Contain("Class2");
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void Format_DefaultOptions_UsesTreeView()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions();

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        options.ViewMode.Should().Be(ViewMode.Tree);
        // Tree view uses side-by-side for file-level overview
        html.Should().Contain("diff-side diff-old");
        html.Should().Contain("diff-side diff-new");
        // Tree view also includes individual change details which may use side-by-side
    }

    [Fact]
    public void Format_TreeView_DoesNotUseInlineStructureForFileOverview()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Tree
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        // Tree view should not use inline structure for file-level overview
        html.Should().NotContain("<div class=\"diff-inline\">");
        html.Should().Contain("diff-side");
    }

    #endregion

    #region CSS Class Verification Tests

    [Fact]
    public void Format_InlineView_ContainsRequiredCssClasses()
    {
        // Arrange
        var result = CreateSimpleDiffResult();
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("diff-inline");
        html.Should().Contain("diff-content");
        html.Should().Contain("diff-line");
        html.Should().Contain("line-number");
        html.Should().Contain("line-prefix");
        html.Should().Contain("line-content");
    }

    [Fact]
    public void Format_InlineView_ContextMode_ContainsSeparatorCssClass()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "Method1",
                            OldContent = "public void Method1() { }",
                            NewContent = "public void Method1() { return; }",
                            OldLocation = new Location { StartLine = 5, EndLine = 5 },
                            NewLocation = new Location { StartLine = 5, EndLine = 5 }
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "Method2",
                            OldContent = "public void Method2() { }",
                            NewContent = "public void Method2() { return; }",
                            OldLocation = new Location { StartLine = 50, EndLine = 50 },
                            NewLocation = new Location { StartLine = 50, EndLine = 50 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 2 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline,
            InlineContext = 3
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("diff-line-separator");
    }

    [Fact]
    public void Format_InlineView_SemanticDiff_ContainsSemanticHeaderCssClass()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "TestMethod",
                            OldContent = "public void TestMethod() { }",
                            NewContent = "public void TestMethod() { return; }",
                            OldLocation = new Location { StartLine = 10, EndLine = 10 },
                            NewLocation = new Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1 }
        };
        var options = new OutputOptions
        {
            ViewMode = ViewMode.Inline
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("line-semantic-header");
        html.Should().Contain("diff-line-comment");
    }

    #endregion

    #region Helper Methods

    private static DiffResult CreateSimpleDiffResult()
    {
        return new DiffResult
        {
            OldPath = "test.cs",
            NewPath = "test.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "test.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "TestMethod",
                            OldContent = "public void TestMethod() { }",
                            NewContent = "public void TestMethod() { return; }",
                            OldLocation = new Location { StartLine = 10, EndLine = 10 },
                            NewLocation = new Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Modifications = 1 }
        };
    }

    /// <summary>
    /// Extracts the inline content section from HTML (the div.diff-inline section).
    /// </summary>
    private static string ExtractInlineContent(string html)
    {
        var startIndex = html.IndexOf("<div class=\"diff-inline\">");
        if (startIndex == -1) return string.Empty;

        var endIndex = html.IndexOf("</div>", startIndex + 100);
        if (endIndex == -1) return string.Empty;

        // Get a reasonable chunk that includes the inline content
        var length = Math.Min(endIndex - startIndex + 500, html.Length - startIndex);
        return html.Substring(startIndex, length);
    }

    /// <summary>
    /// Extracts the file overview section (top diff content before individual changes).
    /// </summary>
    private static string ExtractFileOverviewSection(string html)
    {
        var startIndex = html.IndexOf("<div class=\"top-diff-content\"");
        if (startIndex == -1) return string.Empty;

        // Get content until the changes-list section
        var endIndex = html.IndexOf("<div class=\"changes-list\">", startIndex);
        if (endIndex == -1)
        {
            // If no changes-list, get until end of top-diff-content
            endIndex = html.IndexOf("</div>", startIndex + 1000);
        }

        if (endIndex == -1) return string.Empty;

        return html.Substring(startIndex, endIndex - startIndex);
    }

    #endregion
}
