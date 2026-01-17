namespace RoslynDiff.Integration.Tests;

using System.Text.RegularExpressions;
using FluentAssertions;
using RoslynDiff.Core.Differ;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;
using Xunit;

/// <summary>
/// Tests for the HTML duplication bug fix where method content was shown twice:
/// once in the parent class change panel and again in the nested method change panel.
///
/// The fix ensures that parent changes with children show a summary placeholder
/// instead of full content, letting children display their own detailed code.
/// </summary>
public class HtmlDuplicationBugTests
{
    private readonly OutputFormatterFactory _formatterFactory = new();

    /// <summary>
    /// Verifies that when a class contains modified methods, the parent class panel
    /// shows a summary message instead of duplicating the full content that will
    /// be shown in the child panels.
    /// </summary>
    [Fact]
    public void HtmlOutput_WhenClassHasModifiedMethod_ParentShowsSummaryNotFullContent()
    {
        // Arrange
        const string oldCode = @"
namespace TestNamespace
{
    public class Calculator
    {
        public int Add(int a, int b)
        {
            return a + b;
        }
    }
}";

        const string newCode = @"
namespace TestNamespace
{
    public class Calculator
    {
        public int Add(int a, int b)
        {
            return a + b + 0; // Modified line
        }
    }
}";

        var differ = new CSharpDiffer();
        var options = new DiffOptions
        {
            OldPath = "Calculator.cs",
            NewPath = "Calculator.cs"
        };

        // Act
        var result = differ.Compare(oldCode, newCode, options);
        var formatter = _formatterFactory.GetFormatter("html");
        var html = formatter.FormatResult(result, new OutputOptions { IncludeContent = true, IncludeStats = true });

        // Assert: Verify the structure first
        result.FileChanges.Should().NotBeEmpty();
        result.FileChanges[0].Changes.Should().NotBeEmpty();

        // The fix shows "Contains X nested changes shown below" for parent elements with children
        // This prevents visual duplication while keeping full content in data attributes for copy functionality
        html.Should().Contain("nested change",
            "parent changes with children should show a summary placeholder instead of full content");
    }

    /// <summary>
    /// Verifies that the visual HTML content does not show the same code block twice.
    /// Parent changes with children show a summary, and only leaf changes show full code.
    /// Note: Data attributes intentionally contain full content for copy-to-clipboard functionality.
    /// </summary>
    [Fact]
    public void HtmlOutput_ParentChangeWithChildren_ShowsSummaryInsteadOfFullContent()
    {
        // Arrange
        const string oldCode = @"
public class Calculator
{
    public int Add(int a, int b) => a + b;
}";

        const string newCode = @"
public class Calculator
{
    public int Add(int a, int b) => a + b + 0;
}";

        var differ = new CSharpDiffer();
        var options = new DiffOptions
        {
            OldPath = "Calculator.cs",
            NewPath = "Calculator.cs"
        };

        // Act
        var result = differ.Compare(oldCode, newCode, options);
        var formatter = _formatterFactory.GetFormatter("html");
        var html = formatter.FormatResult(result, new OutputOptions { IncludeContent = true, IncludeStats = true });

        // Assert: Count visual code displays (in diff-line elements with line-content spans)
        // The method body should appear in the child change's visual display,
        // NOT duplicated in the parent's visual display

        // Look for the nested changes summary in parent panels
        var nestedChangesSummary = Regex.Matches(html, @"Contains \d+ nested changes? shown below");
        nestedChangesSummary.Count.Should().BeGreaterThan(0,
            "parent changes with children should display a summary placeholder");

        // The fix ensures parent panels with children show summary, not full code
        // This is the key behavior that prevents visual duplication
    }

    /// <summary>
    /// Tests that the Change model correctly represents hierarchical changes.
    /// The model intentionally keeps full content in both parent and children
    /// for completeness - the HtmlFormatter handles deduplication during rendering.
    /// </summary>
    [Fact]
    public void DiffResult_WhenClassHasModifiedMethod_StructureIsCorrect()
    {
        // Arrange
        const string oldCode = @"
public class Calculator
{
    public int Add(int a, int b) => a + b;
}";

        const string newCode = @"
public class Calculator
{
    public int Add(int a, int b) => a + b + 0;
}";

        var differ = new CSharpDiffer();
        var options = new DiffOptions
        {
            OldPath = "Calculator.cs",
            NewPath = "Calculator.cs"
        };

        // Act
        var result = differ.Compare(oldCode, newCode, options);

        // Assert - find the class change that has method children
        var classChange = result.FileChanges
            .SelectMany(fc => fc.Changes)
            .FirstOrDefault(c => c.Kind == ChangeKind.Class && c.Children?.Count > 0);

        classChange.Should().NotBeNull("there should be a class change with method children");

        var methodChild = classChange!.Children!
            .FirstOrDefault(c => c.Kind == ChangeKind.Method);

        methodChild.Should().NotBeNull("the class should have a method child change");

        // The Change model keeps full content for both parent and children
        // This is correct - the HtmlFormatter handles visual deduplication
        classChange.NewContent.Should().NotBeNullOrEmpty("parent change should have content for copy operations");
        methodChild!.NewContent.Should().NotBeNullOrEmpty("child change should have its content");

        // Verify the hierarchical structure is correct
        classChange.Children.Should().HaveCountGreaterThan(0, "parent should have children");
    }
}
