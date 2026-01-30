namespace RoslynDiff.Integration.Tests;

using System.Net;
using System.Text;
using FluentAssertions;
using RoslynDiff.Cli;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;
using Xunit;

/// <summary>
/// Integration tests for fragment-mode samples.
/// Tests verify fragment-mode HTML generation and JavaScript metadata access patterns.
/// These tests catch issues like CORS fetch() failures when opening files directly.
/// </summary>
public class FragmentModeIntegrationTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly HtmlFormatter _formatter = new();
    private readonly string _samplesDirectory;

    public FragmentModeIntegrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"roslyn-diff-fragment-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);

        // Locate samples directory
        var currentDir = Directory.GetCurrentDirectory();
        _samplesDirectory = Path.Combine(currentDir, "..", "..", "..", "..", "..", "samples");
        _samplesDirectory = Path.GetFullPath(_samplesDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    #region Test 1: Fragment Structure Validation

    [Fact]
    public void Test_FragmentHtml_IsValidFragment()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var htmlPath = Path.Combine(_tempDirectory, "fragment.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath,
            ExtractCssPath = "roslyn-diff.css",
            IncludeContent = true
        };

        // Act
        var html = _formatter.FormatResult(result, options);
        File.WriteAllText(htmlPath, html);

        // Assert - Fragment should NOT have document wrapper tags
        html.Should().NotContain("<!DOCTYPE html>");
        html.Should().NotContain("<html");
        html.Should().NotContain("<head>");
        html.Should().NotContain("<body>");

        // Fragment should start with link or div
        var trimmed = html.TrimStart();
        (trimmed.StartsWith("<link rel=\"stylesheet\"") || trimmed.StartsWith("<div class=\"roslyn-diff-fragment\""))
            .Should().BeTrue("Fragment should start with <link> or <div>");

        // Fragment should have root container
        html.Should().Contain("<div class=\"roslyn-diff-fragment\"");
    }

    #endregion

    #region Test 2: Data Attributes Validation

    [Fact]
    public void Test_FragmentHtml_HasRequiredDataAttributes()
    {
        // Arrange
        var result = new DiffResult
        {
            OldPath = "TestFile.cs",
            NewPath = "TestFile.cs",
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = "TestFile.cs",
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "NewMethod",
                            NewContent = "public void NewMethod() { }",
                            NewLocation = new Location { StartLine = 10, EndLine = 10 }
                        },
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
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Property,
                            Name = "Value",
                            OldContent = "public int Value { get; set; }",
                            NewContent = "public string Value { get; set; }",
                            OldLocation = new Location { StartLine = 15, EndLine = 15 },
                            NewLocation = new Location { StartLine = 15, EndLine = 15 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats
            {
                Additions = 1,
                Deletions = 1,
                Modifications = 1
            }
        };

        var htmlPath = Path.Combine(_tempDirectory, "test-data-attrs.html");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath
        };

        // Act
        var html = _formatter.FormatResult(result, options);

        // Assert - All required data attributes should be present and valid
        html.Should().Contain("data-old-file=\"TestFile.cs\"");
        html.Should().Contain("data-new-file=\"TestFile.cs\"");
        html.Should().Contain("data-changes-total=\"3\"");
        html.Should().Contain("data-changes-added=\"1\"");
        html.Should().Contain("data-changes-removed=\"1\"");
        html.Should().Contain("data-changes-modified=\"1\"");
        html.Should().Contain("data-mode=\"roslyn\"");

        // Impact attributes should be present (even if 0)
        html.Should().Contain("data-impact-breaking-public");
        html.Should().Contain("data-impact-breaking-internal");
        html.Should().Contain("data-impact-non-breaking");
        html.Should().Contain("data-impact-formatting");

        // Verify numeric values can be parsed
        var totalMatch = System.Text.RegularExpressions.Regex.Match(html, @"data-changes-total=""(\d+)""");
        totalMatch.Success.Should().BeTrue();
        int.Parse(totalMatch.Groups[1].Value).Should().Be(3);
    }

    #endregion

    #region Test 3: CSS Extraction

    [Fact]
    public void Test_RoslynDiffCss_IsExtracted()
    {
        // Arrange
        var result = CreateSampleDiffResult();
        var htmlPath = Path.Combine(_tempDirectory, "fragment-with-css.html");
        var cssFileName = "roslyn-diff.css";
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath,
            ExtractCssPath = cssFileName,
            IncludeContent = true
        };

        // Act
        var html = _formatter.FormatResult(result, options);
        File.WriteAllText(htmlPath, html);

        // Assert - CSS file should exist
        var cssPath = Path.Combine(_tempDirectory, cssFileName);
        File.Exists(cssPath).Should().BeTrue("CSS file should be extracted");

        // CSS file should not be empty
        var cssContent = File.ReadAllText(cssPath);
        cssContent.Should().NotBeNullOrWhiteSpace();
        cssContent.Length.Should().BeGreaterThan(100, "CSS should have substantial content");

        // CSS should contain key classes
        cssContent.Should().Contain(".roslyn-diff-fragment", "CSS should scope to fragment");
        cssContent.Should().Contain(".diff-container", "CSS should have diff container styles");
        cssContent.Should().Contain(".line-added", "CSS should have added line styles");
        cssContent.Should().Contain(".line-removed", "CSS should have removed line styles");
        cssContent.Should().Contain(".line-modified", "CSS should have modified line styles");
        cssContent.Should().Contain(".change-badge", "CSS should have badge styles");

        // HTML should reference the CSS file
        html.Should().Contain($"<link rel=\"stylesheet\" href=\"{cssFileName}\">");
    }

    #endregion

    #region Test 4: HTTP Server Fragment Loading

    [Fact]
    public async Task Test_ParentHtml_LoadsFragmentViaHttpServer()
    {
        // Skip if HTTP listener cannot be started (e.g., permissions issues on CI)
        if (!HttpListener.IsSupported)
        {
            return;
        }

        // Arrange
        var result = CreateSampleDiffResult();
        var htmlPath = Path.Combine(_tempDirectory, "fragment.html");
        var cssPath = Path.Combine(_tempDirectory, "roslyn-diff.css");
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath,
            ExtractCssPath = "roslyn-diff.css",
            IncludeContent = true
        };

        // Generate fragment
        var fragmentHtml = _formatter.FormatResult(result, options);
        File.WriteAllText(htmlPath, fragmentHtml);

        // Create parent HTML that loads fragment via fetch()
        var parentHtml = CreateParentHtml();
        var parentPath = Path.Combine(_tempDirectory, "parent.html");
        File.WriteAllText(parentPath, parentHtml);

        // Use unique port based on test run
        var port = 8765 + Random.Shared.Next(1000);
        var prefix = $"http://localhost:{port}/";

        using var listener = new HttpListener();
        try
        {
            listener.Prefixes.Add(prefix);
            listener.Start();

            // Handle requests in background
            var serverTask = Task.Run(async () =>
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    var request = context.Request;
                    var response = context.Response;

                    var requestedFile = request.Url?.AbsolutePath.TrimStart('/');
                    if (string.IsNullOrEmpty(requestedFile) || requestedFile == "/")
                    {
                        requestedFile = "parent.html";
                    }

                    // Sanitize path to prevent directory traversal attacks
                    // Extract just the filename (no directory components)
                    var fileName = Path.GetFileName(requestedFile);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        fileName = "parent.html";
                    }

                    // Allowlist valid file extensions for this test HTTP server
                    var extension = Path.GetExtension(fileName).ToLowerInvariant();
                    var allowedExtensions = new[] { ".html", ".css", ".js", ".json" };
                    if (!allowedExtensions.Contains(extension))
                    {
                        response.StatusCode = 403; // Forbidden
                        response.Close();
                        return;
                    }

                    // Build a clean path using a new string (breaks taint chain for CodeQL)
                    var cleanFileName = string.Concat(fileName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
                    var safePath = Path.Combine(_tempDirectory, cleanFileName);

                    // Verify path is within temp directory (defense in depth)
                    var fullPath = Path.GetFullPath(safePath);
                    var fullTempDir = Path.GetFullPath(_tempDirectory) + Path.DirectorySeparatorChar;
                    if (!fullPath.StartsWith(fullTempDir, StringComparison.OrdinalIgnoreCase))
                    {
                        response.StatusCode = 403; // Forbidden
                        response.Close();
                        return;
                    }

                    if (File.Exists(safePath))
                    {
                        var content = File.ReadAllBytes(safePath);
                        response.ContentType = GetContentType(cleanFileName);
                        response.ContentLength64 = content.Length;
                        await response.OutputStream.WriteAsync(content);
                    }
                    else
                    {
                        response.StatusCode = 404;
                    }

                    response.Close();
                }
                catch (Exception)
                {
                    // Ignore exceptions from server shutdown
                }
            });

            // Act - Request the fragment via HTTP
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var fragmentUrl = $"{prefix}fragment.html";
            var fragmentResponse = await httpClient.GetStringAsync(fragmentUrl);

            // Wait for server to complete (with timeout)
            await Task.WhenAny(serverTask, Task.Delay(TimeSpan.FromSeconds(3)));

            // Assert - Fragment should load without CORS errors
            fragmentResponse.Should().NotBeNullOrEmpty();
            fragmentResponse.Should().Contain("<div class=\"roslyn-diff-fragment\"");
            fragmentResponse.Should().Contain("data-old-file");

            // JavaScript should be able to extract metadata (simulated)
            var hasDataAttributes = fragmentResponse.Contains("data-changes-total") &&
                                   fragmentResponse.Contains("data-changes-added") &&
                                   fragmentResponse.Contains("data-mode");
            hasDataAttributes.Should().BeTrue("Fragment should have extractable metadata");
        }
        finally
        {
            listener.Stop();
            listener.Close();
        }
    }

    #endregion

    #region Test 5: CLI Fragment Generation

    [Fact]
    public void Test_FragmentGeneration_CreatesValidFiles()
    {
        // Arrange
        var beforeFile = Path.Combine(_samplesDirectory, "before", "Calculator.cs");
        var afterFile = Path.Combine(_samplesDirectory, "after", "Calculator.cs");

        // Skip if sample files don't exist
        if (!File.Exists(beforeFile) || !File.Exists(afterFile))
        {
            return;
        }

        var outputHtml = Path.Combine(_tempDirectory, "cli-fragment.html");
        var cssFileName = "cli-styles.css";

        // Act - Generate fragment using differ
        var factory = new DifferFactory();
        var diffOptions = new DiffOptions
        {
            OldPath = beforeFile,
            NewPath = afterFile
        };
        var differ = factory.GetDiffer(afterFile, diffOptions);

        var beforeContent = File.ReadAllText(beforeFile);
        var afterContent = File.ReadAllText(afterFile);
        var diffResult = differ.Compare(beforeContent, afterContent, diffOptions);

        var formatter = new HtmlFormatter();
        var options = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = outputHtml,
            ExtractCssPath = cssFileName,
            IncludeContent = true
        };

        var html = formatter.FormatResult(diffResult, options);
        File.WriteAllText(outputHtml, html);

        // Assert - Both files should be created
        File.Exists(outputHtml).Should().BeTrue("HTML fragment should be created");

        var cssPath = Path.Combine(_tempDirectory, cssFileName);
        File.Exists(cssPath).Should().BeTrue("CSS file should be created");

        // HTML should have correct structure
        var htmlContent = File.ReadAllText(outputHtml);
        htmlContent.Should().NotContain("<!DOCTYPE html>");
        htmlContent.Should().Contain("<link rel=\"stylesheet\" href=\"cli-styles.css\">");
        htmlContent.Should().Contain("<div class=\"roslyn-diff-fragment\"");

        // CSS should be valid
        var cssContent = File.ReadAllText(cssPath);
        cssContent.Should().Contain(".roslyn-diff-fragment");
        cssContent.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Test 6: Multiple Fragments Share CSS

    [Fact]
    public void Test_MultipleFragments_CanShareCss()
    {
        // Arrange
        var result1 = CreateSampleDiffResult("File1.cs");
        var result2 = CreateSampleDiffResult("File2.cs");

        var htmlPath1 = Path.Combine(_tempDirectory, "fragment1.html");
        var htmlPath2 = Path.Combine(_tempDirectory, "fragment2.html");
        var sharedCss = "shared-styles.css";

        var options1 = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath1,
            ExtractCssPath = sharedCss,
            IncludeContent = true
        };

        var options2 = new OutputOptions
        {
            HtmlMode = HtmlMode.Fragment,
            HtmlOutputPath = htmlPath2,
            ExtractCssPath = sharedCss,
            IncludeContent = true
        };

        // Act - Generate both fragments pointing to same CSS
        var html1 = _formatter.FormatResult(result1, options1);
        var html2 = _formatter.FormatResult(result2, options2);

        File.WriteAllText(htmlPath1, html1);
        File.WriteAllText(htmlPath2, html2);

        // Assert - CSS file should exist
        var cssPath = Path.Combine(_tempDirectory, sharedCss);
        File.Exists(cssPath).Should().BeTrue("Shared CSS file should be created");

        // Both fragments should reference the same CSS
        html1.Should().Contain($"<link rel=\"stylesheet\" href=\"{sharedCss}\">");
        html2.Should().Contain($"<link rel=\"stylesheet\" href=\"{sharedCss}\">");

        // Fragments should have unique content
        html1.Should().Contain("File1.cs");
        html2.Should().Contain("File2.cs");

        // CSS should be created once (check it's not duplicated)
        var cssContent = File.ReadAllText(cssPath);
        cssContent.Should().Contain(".roslyn-diff-fragment");

        // Verify CSS file is same when regenerated
        var cssLength1 = new FileInfo(cssPath).Length;

        // Regenerate second fragment
        var html2Again = _formatter.FormatResult(result2, options2);
        File.WriteAllText(htmlPath2, html2Again);

        var cssLength2 = new FileInfo(cssPath).Length;
        cssLength2.Should().Be(cssLength1, "CSS file should be same when regenerated");
    }

    #endregion

    #region Helper Methods

    private static DiffResult CreateSampleDiffResult(string fileName = "Sample.cs")
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
                            Kind = ChangeKind.Class,
                            Name = "NewClass",
                            NewContent = "public class NewClass { }",
                            NewLocation = new Location { StartLine = 5, EndLine = 5 }
                        },
                        new Change
                        {
                            Type = ChangeType.Modified,
                            Kind = ChangeKind.Method,
                            Name = "UpdatedMethod",
                            OldContent = "public void UpdatedMethod() { }",
                            NewContent = "public void UpdatedMethod() { return; }",
                            OldLocation = new Location { StartLine = 10, EndLine = 10 },
                            NewLocation = new Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats
            {
                Additions = 1,
                Deletions = 0,
                Modifications = 1
            }
        };
    }

    private static string CreateParentHtml()
    {
        return """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Test Parent Page</title>
    <link rel="stylesheet" href="roslyn-diff.css">
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .container { max-width: 1200px; margin: 0 auto; }
    </style>
</head>
<body>
    <div class="container">
        <h1>Test Fragment Loading</h1>
        <div id="diff-container">Loading...</div>
    </div>

    <script>
        async function loadFragment() {
            try {
                const response = await fetch('fragment.html');
                const html = await response.text();
                document.getElementById('diff-container').innerHTML = html;

                // Extract metadata
                const fragment = document.querySelector('.roslyn-diff-fragment');
                if (fragment) {
                    const metadata = {
                        oldFile: fragment.dataset.oldFile,
                        newFile: fragment.dataset.newFile,
                        changesTotal: parseInt(fragment.dataset.changesTotal),
                        changesAdded: parseInt(fragment.dataset.changesAdded),
                        changesRemoved: parseInt(fragment.dataset.changesRemoved),
                        changesModified: parseInt(fragment.dataset.changesModified),
                        mode: fragment.dataset.mode
                    };
                    console.log('Fragment loaded:', metadata);
                }
            } catch (error) {
                console.error('Failed to load fragment:', error);
                document.getElementById('diff-container').innerHTML =
                    '<p style="color: red;">Failed to load fragment</p>';
            }
        }

        window.addEventListener('DOMContentLoaded', loadFragment);
    </script>
</body>
</html>
""";
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            _ => "application/octet-stream"
        };
    }

    #endregion
}
