namespace RoslynDiff.Integration.Tests;

using System.Text;
using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;
using Xunit;

/// <summary>
/// Integration tests for HTML Fragment Mode.
/// Tests multiple fragments sharing CSS, page coexistence, and no style conflicts.
/// </summary>
public class HtmlFragmentIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly HtmlFormatter _formatter = new();

    public HtmlFragmentIntegrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"roslyn-diff-integration-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    #region Multiple Fragments Tests

    [Fact]
    public void MultipleFragments_ShareSameCssFile()
    {
        // Arrange
        var fragment1 = CreateDiffResult("Class1", ChangeType.Added);
        var fragment2 = CreateDiffResult("Class2", ChangeType.Modified);
        var fragment3 = CreateDiffResult("Class3", ChangeType.Removed);

        var htmlPath1 = Path.Combine(_tempDirectory, "fragment1.html");
        var htmlPath2 = Path.Combine(_tempDirectory, "fragment2.html");
        var htmlPath3 = Path.Combine(_tempDirectory, "fragment3.html");

        var options1 = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath1,
            ExtractCssPath = "shared-styles.css",
            IncludeContent = true
        };

        var options2 = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath2,
            ExtractCssPath = "shared-styles.css",
            IncludeContent = true
        };

        var options3 = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath3,
            ExtractCssPath = "shared-styles.css",
            IncludeContent = true
        };

        // Act
        var html1 = _formatter.FormatResult(fragment1, options1);
        var html2 = _formatter.FormatResult(fragment2, options2);
        var html3 = _formatter.FormatResult(fragment3, options3);

        // Assert - All fragments reference the same CSS file
        html1.Should().Contain("<link rel=\"stylesheet\" href=\"shared-styles.css\">");
        html2.Should().Contain("<link rel=\"stylesheet\" href=\"shared-styles.css\">");
        html3.Should().Contain("<link rel=\"stylesheet\" href=\"shared-styles.css\">");

        // CSS file should exist and be shared
        var cssPath = Path.Combine(_tempDirectory, "shared-styles.css");
        File.Exists(cssPath).Should().BeTrue();

        // Each fragment should have unique content
        html1.Should().Contain("Class1");
        html2.Should().Contain("Class2");
        html3.Should().Contain("Class3");
    }

    [Fact]
    public void MultipleFragments_CanCoexistOnSamePage()
    {
        // Arrange
        var fragment1 = CreateDiffResult("AddedClass", ChangeType.Added);
        var fragment2 = CreateDiffResult("ModifiedClass", ChangeType.Modified);

        var htmlPath1 = Path.Combine(_tempDirectory, "frag1.html");
        var htmlPath2 = Path.Combine(_tempDirectory, "frag2.html");

        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath1,
            ExtractCssPath = "roslyn-diff.css",
            IncludeContent = true
        };

        // Generate fragments
        var html1 = _formatter.FormatResult(fragment1, options);

        options = options with { HtmlOutputPath = htmlPath2 };
        var html2 = _formatter.FormatResult(fragment2, options);

        // Act - Combine fragments into a single page
        var combinedPage = CreateCombinedHtmlPage(html1, html2);

        // Assert - Combined page should be valid HTML
        combinedPage.Should().Contain("<!DOCTYPE html>");
        combinedPage.Should().Contain("<link rel=\"stylesheet\" href=\"roslyn-diff.css\">");

        // Both fragments should be present
        combinedPage.Should().Contain("AddedClass");
        combinedPage.Should().Contain("ModifiedClass");

        // Each fragment should have its own container
        var fragmentCount = CountOccurrences(combinedPage, "class=\"roslyn-diff-fragment\"");
        fragmentCount.Should().Be(2, "There should be exactly 2 fragment containers");
    }

    [Fact]
    public void MultipleFragments_HaveIsolatedDataAttributes()
    {
        // Arrange
        var fragment1 = new DiffResult
        {
            OldPath = "File1.cs",
            NewPath = "File1.cs",
            Mode = DiffMode.Roslyn,
            FileChanges = [],
            Stats = new DiffStats { Additions = 5, Deletions = 0, Modifications = 0 }
        };

        var fragment2 = new DiffResult
        {
            OldPath = "File2.cs",
            NewPath = "File2.cs",
            Mode = DiffMode.Roslyn,
            FileChanges = [],
            Stats = new DiffStats { Additions = 0, Deletions = 3, Modifications = 2 }
        };

        var htmlPath1 = Path.Combine(_tempDirectory, "frag1.html");
        var htmlPath2 = Path.Combine(_tempDirectory, "frag2.html");

        var options1 = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath1
        };

        var options2 = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath2
        };

        // Act
        var html1 = _formatter.FormatResult(fragment1, options1);
        var html2 = _formatter.FormatResult(fragment2, options2);

        // Assert - Each fragment has its own unique data attributes
        html1.Should().Contain("data-old-file=\"File1.cs\"");
        html1.Should().Contain("data-changes-added=\"5\"");
        html1.Should().Contain("data-changes-total=\"5\"");

        html2.Should().Contain("data-old-file=\"File2.cs\"");
        html2.Should().Contain("data-changes-removed=\"3\"");
        html2.Should().Contain("data-changes-modified=\"2\"");
        html2.Should().Contain("data-changes-total=\"5\"");
    }

    [Fact]
    public void MultipleFragments_NoCssClassConflicts()
    {
        // Arrange
        var fragment1 = CreateDiffResultWithMultipleChangeTypes();
        var fragment2 = CreateDiffResultWithMultipleChangeTypes();

        var htmlPath1 = Path.Combine(_tempDirectory, "test1.html");
        var htmlPath2 = Path.Combine(_tempDirectory, "test2.html");

        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath1,
            ExtractCssPath = "styles.css",
            IncludeContent = true
        };

        // Act
        var html1 = _formatter.FormatResult(fragment1, options);

        options = options with { HtmlOutputPath = htmlPath2 };
        var html2 = _formatter.FormatResult(fragment2, options);

        // Create combined page
        var combinedPage = CreateCombinedHtmlPage(html1, html2);

        // Assert - Verify CSS classes are properly scoped
        var cssPath = Path.Combine(_tempDirectory, "styles.css");
        var cssContent = File.ReadAllText(cssPath);

        // CSS should be scoped to .roslyn-diff-fragment
        cssContent.Should().Contain(".roslyn-diff-fragment");

        // Verify critical CSS classes exist and are scoped
        cssContent.Should().Contain(".diff-container");
        cssContent.Should().Contain(".line-added");
        cssContent.Should().Contain(".line-removed");
        cssContent.Should().Contain(".line-modified");

        // Combined page should have both fragments
        CountOccurrences(combinedPage, "class=\"roslyn-diff-fragment\"").Should().Be(2);
    }

    [Fact]
    public void Fragment_CssFileOverwrittenOnMultipleGenerations_LastWriteWins()
    {
        // Arrange
        var fragment1 = CreateDiffResult("Class1", ChangeType.Added);
        var fragment2 = CreateDiffResult("Class2", ChangeType.Modified);

        var htmlPath1 = Path.Combine(_tempDirectory, "test1.html");
        var htmlPath2 = Path.Combine(_tempDirectory, "test2.html");

        var options1 = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath1,
            ExtractCssPath = "shared.css"
        };

        var options2 = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath2,
            ExtractCssPath = "shared.css"
        };

        // Act
        _formatter.FormatResult(fragment1, options1);
        var cssPath = Path.Combine(_tempDirectory, "shared.css");
        var initialSize = new FileInfo(cssPath).Length;

        _formatter.FormatResult(fragment2, options2);
        var finalSize = new FileInfo(cssPath).Length;

        // Assert - CSS file should exist and contain valid content
        File.Exists(cssPath).Should().BeTrue();

        // File sizes should be the same (same CSS content)
        finalSize.Should().Be(initialSize, "CSS file should have consistent size on regeneration");

        var cssContent = File.ReadAllText(cssPath);
        cssContent.Should().Contain(".roslyn-diff-fragment");
        cssContent.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void RealWorldScenario_DashboardWithMultipleDiffs()
    {
        // Arrange - Simulate a dashboard showing multiple file diffs
        var file1Diff = CreateRealDiffScenario("Calculator.cs", "Add", "public int Add(int a, int b) => a + b;");
        var file2Diff = CreateRealDiffScenario("Database.cs", "GetUser", "public User GetUser(int id) { return _users[id]; }");
        var file3Diff = CreateRealDiffScenario("Logger.cs", "LogError", "public void LogError(string message) { /* ... */ }");

        var sharedCssName = "dashboard-diff-styles.css";

        // Generate all fragments
        var fragments = new List<string>();
        for (int i = 1; i <= 3; i++)
        {
            var htmlPath = Path.Combine(_tempDirectory, $"fragment{i}.html");
            var options = new OutputOptions
            {
                HtmlMode = HtmlMode.Fragment,
                HtmlOutputPath = htmlPath,
                ExtractCssPath = sharedCssName,
                IncludeContent = true,
                IncludeStats = true
            };

            var result = i switch
            {
                1 => file1Diff,
                2 => file2Diff,
                3 => file3Diff,
                _ => throw new InvalidOperationException()
            };

            fragments.Add(_formatter.FormatResult(result, options));
        }

        // Act - Create dashboard page
        var dashboardHtml = CreateDashboardPage(fragments, sharedCssName);

        // Assert
        dashboardHtml.Should().Contain("<!DOCTYPE html>");
        dashboardHtml.Should().Contain($"<link rel=\"stylesheet\" href=\"{sharedCssName}\">");

        // All fragments should be present
        dashboardHtml.Should().Contain("Calculator.cs");
        dashboardHtml.Should().Contain("Database.cs");
        dashboardHtml.Should().Contain("Logger.cs");

        // All methods should be visible
        dashboardHtml.Should().Contain("Add");
        dashboardHtml.Should().Contain("GetUser");
        dashboardHtml.Should().Contain("LogError");

        // Should have exactly 3 fragments
        CountOccurrences(dashboardHtml, "class=\"roslyn-diff-fragment\"").Should().Be(3);

        // CSS file should exist
        var cssPath = Path.Combine(_tempDirectory, sharedCssName);
        File.Exists(cssPath).Should().BeTrue();
    }

    [Fact]
    public void Fragment_WithComplexChanges_RendersCorrectly()
    {
        // Arrange - Create a complex diff with nested changes
        var result = new DiffResult
        {
            OldPath = "Service.cs",
            NewPath = "Service.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "Service.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Class,
                            Name = "UserService",
                            Children =
                            [
                                new Change
                                {
                                    Type = ChangeType.Added,
                                    Kind = ChangeKind.Method,
                                    Name = "CreateUser",
                                    NewContent = "public void CreateUser(User user) { }",
                                    NewLocation = new Location { StartLine = 10, EndLine = 10 }
                                },
                                new Change
                                {
                                    Type = ChangeType.Modified,
                                    Kind = ChangeKind.Method,
                                    Name = "UpdateUser",
                                    OldContent = "public void UpdateUser(int id) { }",
                                    NewContent = "public void UpdateUser(int id, User user) { }",
                                    OldLocation = new Location { StartLine = 15, EndLine = 15 },
                                    NewLocation = new Location { StartLine = 15, EndLine = 15 }
                                },
                                new Change
                                {
                                    Type = ChangeType.Removed,
                                    Kind = ChangeKind.Method,
                                    Name = "DeleteUser",
                                    OldContent = "public void DeleteUser(int id) { }",
                                    OldLocation = new Location { StartLine = 20, EndLine = 20 }
                                }
                            ]
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1, Deletions = 1, Modifications = 1 }
        };

        var htmlPath = Path.Combine(_tempDirectory, "complex.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath,
            IncludeContent = true
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert
        html.Should().Contain("UserService");
        html.Should().Contain("CreateUser");
        html.Should().Contain("UpdateUser");
        html.Should().Contain("DeleteUser");

        // Should have proper data attributes
        html.Should().Contain("data-changes-added=\"1\"");
        html.Should().Contain("data-changes-removed=\"1\"");
        html.Should().Contain("data-changes-modified=\"1\"");
    }

    #endregion

    #region Style Isolation Tests

    [Fact]
    public void Fragment_CssScoping_DoesNotAffectPageStyles()
    {
        // Arrange
        var result = CreateDiffResult("TestClass", ChangeType.Added);
        var htmlPath = Path.Combine(_tempDirectory, "fragment.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath,
            IncludeContent = true
        };

        // Act
        var fragmentHtml = _formatter.FormatResult(result, options);

        // Create page with existing styles
        var pageWithStyles = CreatePageWithExistingStyles(fragmentHtml);

        // Assert - Fragment CSS should be scoped
        var cssPath = Path.Combine(_tempDirectory, "roslyn-diff.css");
        var cssContent = File.ReadAllText(cssPath);

        // All selectors should be scoped (except :root and global resets)
        cssContent.Should().Contain(".roslyn-diff-fragment");

        // Fragment should not have unscoped generic selectors that could conflict
        // (div, span, etc. should be scoped)
        var lines = cssContent.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip empty, comments, :root, and media queries
            if (string.IsNullOrWhiteSpace(trimmed) ||
                trimmed.StartsWith("//") ||
                trimmed.StartsWith("/*") ||
                trimmed.StartsWith(":root") ||
                trimmed.StartsWith("@media") ||
                trimmed.StartsWith("*"))
            {
                continue;
            }

            // If it's a CSS rule (has { ), it should be scoped
            if (trimmed.Contains("{") && !trimmed.Contains("@"))
            {
                // Should contain .roslyn-diff-fragment or be a global rule like :root
                var isScoped = trimmed.Contains(".roslyn-diff-fragment") ||
                              trimmed.StartsWith(":root") ||
                              trimmed.StartsWith("*");

                isScoped.Should().BeTrue($"CSS rule '{trimmed}' should be scoped to .roslyn-diff-fragment");
            }
        }
    }

    #endregion

    #region Helper Methods

    private static DiffResult CreateDiffResult(string className, ChangeType changeType)
    {
        Change change;
        DiffStats stats;

        if (changeType == ChangeType.Added)
        {
            change = new Change
            {
                Type = changeType,
                Kind = ChangeKind.Class,
                Name = className,
                NewContent = $"public class {className} {{ }}",
                NewLocation = new Location { StartLine = 10, EndLine = 10 }
            };
            stats = new DiffStats { Additions = 1 };
        }
        else if (changeType == ChangeType.Removed)
        {
            change = new Change
            {
                Type = changeType,
                Kind = ChangeKind.Class,
                Name = className,
                OldContent = $"public class {className} {{ }}",
                OldLocation = new Location { StartLine = 10, EndLine = 10 }
            };
            stats = new DiffStats { Deletions = 1 };
        }
        else // Modified
        {
            change = new Change
            {
                Type = changeType,
                Kind = ChangeKind.Class,
                Name = className,
                OldContent = $"public class {className} {{ }}",
                NewContent = $"public class {className} {{ }}",
                OldLocation = new Location { StartLine = 10, EndLine = 10 },
                NewLocation = new Location { StartLine = 10, EndLine = 10 }
            };
            stats = new DiffStats { Modifications = 1 };
        }

        return new DiffResult
        {
            OldPath = $"{className}.cs",
            NewPath = $"{className}.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = $"{className}.cs",
                    Changes = [change]
                }
            ],
            Stats = stats
        };
    }

    private static DiffResult CreateDiffResultWithMultipleChangeTypes()
    {
        return new DiffResult
        {
            OldPath = "Mixed.cs",
            NewPath = "Mixed.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "Mixed.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            NewContent = "public void NewMethod() { }",
                            NewLocation = new Location { StartLine = 5, EndLine = 5 }
                        },
                        new Change
                        {
                            Type = ChangeType.Removed,
                            Kind = ChangeKind.Method,
                            Name = "OldMethod",
                            OldContent = "public void OldMethod() { }",
                            OldLocation = new Location { StartLine = 10, EndLine = 10 }
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "ChangedMethod",
                            OldContent = "public void ChangedMethod() { }",
                            NewContent = "public void ChangedMethod() { return; }",
                            OldLocation = new Location { StartLine = 15, EndLine = 15 },
                            NewLocation = new Location { StartLine = 15, EndLine = 15 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1, Deletions = 1, Modifications = 1 }
        };
    }

    private static DiffResult CreateRealDiffScenario(string fileName, string methodName, string content)
    {
        return new DiffResult
        {
            OldPath = fileName,
            NewPath = fileName,
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = fileName,
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = methodName,
                            NewContent = content,
                            NewLocation = new Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };
    }

    private static string CreateCombinedHtmlPage(string fragment1, string fragment2)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <title>Multiple Fragments Test</title>");
        sb.AppendLine("    <link rel=\"stylesheet\" href=\"roslyn-diff.css\">");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <h1>Multiple Diff Fragments</h1>");
        sb.AppendLine();

        // Strip the CSS link from fragments (already in head)
        var cleanFragment1 = fragment1.Replace("<link rel=\"stylesheet\" href=\"roslyn-diff.css\">", "");
        var cleanFragment2 = fragment2.Replace("<link rel=\"stylesheet\" href=\"roslyn-diff.css\">", "");

        sb.AppendLine("    <section>");
        sb.AppendLine("        <h2>Fragment 1</h2>");
        sb.AppendLine(cleanFragment1);
        sb.AppendLine("    </section>");
        sb.AppendLine();
        sb.AppendLine("    <section>");
        sb.AppendLine("        <h2>Fragment 2</h2>");
        sb.AppendLine(cleanFragment2);
        sb.AppendLine("    </section>");
        sb.AppendLine();
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string CreateDashboardPage(List<string> fragments, string cssFileName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <title>Diff Dashboard</title>");
        sb.AppendLine($"    <link rel=\"stylesheet\" href=\"{cssFileName}\">");
        sb.AppendLine("    <style>");
        sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine("        .dashboard-section { margin-bottom: 40px; border: 1px solid #ddd; padding: 20px; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <h1>Code Review Dashboard</h1>");
        sb.AppendLine();

        for (int i = 0; i < fragments.Count; i++)
        {
            var cleanFragment = fragments[i].Replace($"<link rel=\"stylesheet\" href=\"{cssFileName}\">", "");
            sb.AppendLine($"    <div class=\"dashboard-section\">");
            sb.AppendLine($"        <h2>File {i + 1}</h2>");
            sb.AppendLine(cleanFragment);
            sb.AppendLine("    </div>");
            sb.AppendLine();
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string CreatePageWithExistingStyles(string fragmentHtml)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <title>Page with Existing Styles</title>");
        sb.AppendLine("    <link rel=\"stylesheet\" href=\"roslyn-diff.css\">");
        sb.AppendLine("    <style>");
        sb.AppendLine("        /* Existing page styles that should not be affected by fragment */");
        sb.AppendLine("        body { background: #f0f0f0; font-family: Arial; }");
        sb.AppendLine("        .container { max-width: 1200px; margin: 0 auto; }");
        sb.AppendLine("        h1 { color: #333; }");
        sb.AppendLine("        .badge { padding: 5px 10px; border-radius: 3px; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"container\">");
        sb.AppendLine("        <h1>My Application</h1>");
        sb.AppendLine("        <p>This page has its own styles that should not conflict with the diff fragment.</p>");
        sb.AppendLine();

        var cleanFragment = fragmentHtml.Replace("<link rel=\"stylesheet\" href=\"roslyn-diff.css\">", "");
        sb.AppendLine(cleanFragment);
        sb.AppendLine();
        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;

        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    #endregion
}
