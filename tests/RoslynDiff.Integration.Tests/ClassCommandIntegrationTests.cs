namespace RoslynDiff.Integration.Tests;

using System.Text.Json;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynDiff.Core.Matching;
using RoslynDiff.Core.Models;
using RoslynDiff.Output;
using Xunit;

/// <summary>
/// Integration tests for the class command functionality.
/// Tests class matching strategies and file:ClassName syntax.
/// </summary>
public class ClassCommandIntegrationTests
{
    private readonly OutputFormatterFactory _formatterFactory = new();

    #region Helper Methods

    /// <summary>
    /// Gets the path to a local test fixture file.
    /// </summary>
    private static string GetLocalFixturePath(string relativePath)
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, "TestFixtures", relativePath);
    }

    /// <summary>
    /// Parses C# content and returns the syntax tree.
    /// </summary>
    private static async Task<SyntaxTree> ParseCSharpAsync(string filePath)
    {
        var content = await File.ReadAllTextAsync(filePath);
        return CSharpSyntaxTree.ParseText(content, path: filePath);
    }

    #endregion

    #region ClassMatcher - Exact Name Strategy Tests

    [Fact]
    public async Task ClassMatcher_ExactNameStrategy_FindsClassByName()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("ClassMatching/Calculator_Old.cs");
        var newPath = GetLocalFixturePath("ClassMatching/Calculator_New.cs");

        var oldTree = await ParseCSharpAsync(oldPath);
        var newTree = await ParseCSharpAsync(newPath);

        var oldRoot = await oldTree.GetRootAsync();
        var oldClass = oldRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Calculator");

        var matcher = new ClassMatcher();
        var options = new ClassMatchOptions { Strategy = ClassMatchStrategy.ExactName };

        // Act
        var result = matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.NewClass.Identifier.Text.Should().Be("Calculator");
        result.MatchedBy.Should().Be(ClassMatchStrategy.ExactName);
    }

    [Fact]
    public async Task ClassMatcher_ExactNameStrategy_ReturnsNullWhenNotFound()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("ClassMatching/RenamedCalculator_Old.cs");
        var newPath = GetLocalFixturePath("ClassMatching/RenamedCalculator_New.cs");

        var oldTree = await ParseCSharpAsync(oldPath);
        var newTree = await ParseCSharpAsync(newPath);

        var oldRoot = await oldTree.GetRootAsync();
        var oldClass = oldRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .First(c => c.Identifier.Text == "MathHelper");

        var matcher = new ClassMatcher();
        var options = new ClassMatchOptions { Strategy = ClassMatchStrategy.ExactName };

        // Act
        var result = matcher.FindMatch(oldClass, newTree, options);

        // Assert - MathHelper was renamed to Calculator, exact match fails
        result.Should().BeNull();
    }

    #endregion

    #region ClassMatcher - Interface Strategy Tests

    [Fact]
    public async Task ClassMatcher_InterfaceStrategy_FindsClassImplementingInterface()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("ClassMatching/MultipleClasses_Old.cs");
        var newPath = GetLocalFixturePath("ClassMatching/MultipleClasses_New.cs");

        var oldTree = await ParseCSharpAsync(oldPath);
        var newTree = await ParseCSharpAsync(newPath);

        var oldRoot = await oldTree.GetRootAsync();
        var oldClass = oldRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .First(c => c.Identifier.Text == "ClassA");

        var matcher = new ClassMatcher();
        var options = ClassMatchOptions.ForInterface("IService");

        // Act
        var result = matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.NewClass.Identifier.Text.Should().Be("ClassA");
        result.MatchedBy.Should().Be(ClassMatchStrategy.Interface);
    }

    #endregion

    #region ClassMatcher - Similarity Strategy Tests

    [Fact]
    public async Task ClassMatcher_SimilarityStrategy_FindsSimilarClass()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("ClassMatching/RenamedCalculator_Old.cs");
        var newPath = GetLocalFixturePath("ClassMatching/RenamedCalculator_New.cs");

        var oldTree = await ParseCSharpAsync(oldPath);
        var newTree = await ParseCSharpAsync(newPath);

        var oldRoot = await oldTree.GetRootAsync();
        var oldClass = oldRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .First(c => c.Identifier.Text == "MathHelper");

        var matcher = new ClassMatcher();
        var options = ClassMatchOptions.ForSimilarity(0.7);

        // Act
        var result = matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.NewClass.Identifier.Text.Should().Be("Calculator");
        result.MatchedBy.Should().Be(ClassMatchStrategy.Similarity);
        result.Similarity.Should().BeGreaterThan(0.7);
    }

    [Fact]
    public async Task ClassMatcher_SimilarityStrategy_ReturnsNullWhenBelowThreshold()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("ClassMatching/Calculator_Old.cs");
        var newPath = GetLocalFixturePath("ClassMatching/MultipleClasses_New.cs");

        var oldTree = await ParseCSharpAsync(oldPath);
        var newTree = await ParseCSharpAsync(newPath);

        var oldRoot = await oldTree.GetRootAsync();
        var oldClass = oldRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Calculator");

        var matcher = new ClassMatcher();
        var options = ClassMatchOptions.ForSimilarity(0.99); // Very high threshold

        // Act
        var result = matcher.FindMatch(oldClass, newTree, options);

        // Assert - No class in MultipleClasses_New is similar enough to Calculator
        result.Should().BeNull();
    }

    #endregion

    #region ClassMatcher - Auto Strategy Tests

    [Fact]
    public async Task ClassMatcher_AutoStrategy_FindsExactMatchFirst()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("ClassMatching/Calculator_Old.cs");
        var newPath = GetLocalFixturePath("ClassMatching/Calculator_New.cs");

        var oldTree = await ParseCSharpAsync(oldPath);
        var newTree = await ParseCSharpAsync(newPath);

        var oldRoot = await oldTree.GetRootAsync();
        var oldClass = oldRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Calculator");

        var matcher = new ClassMatcher();
        var options = ClassMatchOptions.ForAuto();

        // Act
        var result = matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.NewClass.Identifier.Text.Should().Be("Calculator");
        // Auto strategy should prefer exact name match
        result.MatchedBy.Should().Be(ClassMatchStrategy.ExactName);
    }

    [Fact]
    public async Task ClassMatcher_AutoStrategy_FallsBackToSimilarity()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("ClassMatching/RenamedCalculator_Old.cs");
        var newPath = GetLocalFixturePath("ClassMatching/RenamedCalculator_New.cs");

        var oldTree = await ParseCSharpAsync(oldPath);
        var newTree = await ParseCSharpAsync(newPath);

        var oldRoot = await oldTree.GetRootAsync();
        var oldClass = oldRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .First(c => c.Identifier.Text == "MathHelper");

        var matcher = new ClassMatcher();
        var options = ClassMatchOptions.ForAuto();

        // Act
        var result = matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.NewClass.Identifier.Text.Should().Be("Calculator");
        // Auto should fall back to similarity when exact match fails
        result.MatchedBy.Should().Be(ClassMatchStrategy.Similarity);
    }

    #endregion

    #region Output Format Tests for Class Comparison

    [Fact]
    public async Task ClassComparison_JsonOutput_ProducesValidJson()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("ClassMatching/Calculator_Old.cs");
        var newPath = GetLocalFixturePath("ClassMatching/Calculator_New.cs");

        var oldTree = await ParseCSharpAsync(oldPath);
        var newTree = await ParseCSharpAsync(newPath);

        var oldRoot = await oldTree.GetRootAsync();
        var newRoot = await newTree.GetRootAsync();

        var oldClass = oldRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Calculator");

        var newClass = newRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Calculator");

        // Create a simple diff result for the comparison
        var result = new DiffResult
        {
            OldPath = oldPath,
            NewPath = newPath,
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = newPath,
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "Multiply",
                            NewContent = "public int Multiply(int a, int b) => a * b;"
                        }
                    ]
                }
            ],
            Stats = new DiffStats
            {
                Additions = 1,
                Deletions = 0,
                Modifications = 0
            }
        };

        // Act
        var formatter = _formatterFactory.GetFormatter("json");
        var json = formatter.FormatResult(result, new OutputOptions { PrettyPrint = true });

        // Assert
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("metadata").Should().NotBeNull();
        doc.RootElement.GetProperty("summary").GetProperty("additions").GetInt32().Should().Be(1);
        doc.RootElement.GetProperty("files").GetArrayLength().Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task ClassComparison_HtmlOutput_ContainsExpectedElements()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("ClassMatching/Calculator_Old.cs");
        var newPath = GetLocalFixturePath("ClassMatching/Calculator_New.cs");

        var result = new DiffResult
        {
            OldPath = oldPath,
            NewPath = newPath,
            Mode = DiffMode.Roslyn,
            FileChanges =
            [
                new FileChange
                {
                    Path = newPath,
                    Changes =
                    [
                        new Change
                        {
                            Type = ChangeType.Added,
                            Kind = ChangeKind.Method,
                            Name = "Multiply",
                            NewContent = "public int Multiply(int a, int b) => a * b;",
                            NewLocation = new RoslynDiff.Core.Models.Location { StartLine = 10, EndLine = 10 }
                        }
                    ]
                }
            ],
            Stats = new DiffStats { Additions = 1 }
        };

        // Act
        var formatter = _formatterFactory.GetFormatter("html");
        var html = formatter.FormatResult(result, new OutputOptions { IncludeContent = true, IncludeStats = true });

        // Assert
        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("Diff Report");
        html.Should().Contain("added");
    }

    [Fact]
    public void AllOutputFormats_AreSupported()
    {
        // Assert that all expected formats are registered
        var formats = _formatterFactory.SupportedFormats;

        formats.Should().Contain("json");
        formats.Should().Contain("html");
        formats.Should().Contain("text");
        formats.Should().Contain("plain");
        formats.Should().Contain("terminal");
    }

    #endregion

    #region ClassMatchOptions Tests

    [Fact]
    public void ClassMatchOptions_ForAuto_CreatesDefaultOptions()
    {
        // Act
        var options = ClassMatchOptions.ForAuto();

        // Assert
        options.Strategy.Should().Be(ClassMatchStrategy.Auto);
        options.SimilarityThreshold.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ClassMatchOptions_ForInterface_SetsInterfaceName()
    {
        // Act
        var options = ClassMatchOptions.ForInterface("IMyInterface");

        // Assert
        options.Strategy.Should().Be(ClassMatchStrategy.Interface);
        options.InterfaceName.Should().Be("IMyInterface");
    }

    [Fact]
    public void ClassMatchOptions_ForSimilarity_SetsSimilarityThreshold()
    {
        // Act
        var options = ClassMatchOptions.ForSimilarity(0.85);

        // Assert
        options.Strategy.Should().Be(ClassMatchStrategy.Similarity);
        options.SimilarityThreshold.Should().Be(0.85);
    }

    [Fact]
    public void ClassMatchOptions_Default_ReturnsExactNameStrategy()
    {
        // Act
        var options = ClassMatchOptions.Default;

        // Assert - Default uses ExactName strategy as the safest default
        options.Strategy.Should().Be(ClassMatchStrategy.ExactName);
    }

    #endregion

    #region Class Comparison with Changes Tests

    [Fact]
    public async Task ClassComparison_WithMethodAdded_DetectsAddition()
    {
        // Arrange
        var oldPath = GetLocalFixturePath("ClassMatching/Calculator_Old.cs");
        var newPath = GetLocalFixturePath("ClassMatching/Calculator_New.cs");

        var oldTree = await ParseCSharpAsync(oldPath);
        var newTree = await ParseCSharpAsync(newPath);

        var oldRoot = await oldTree.GetRootAsync();
        var newRoot = await newTree.GetRootAsync();

        var oldClass = oldRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Calculator");

        var newClass = newRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .First(c => c.Identifier.Text == "Calculator");

        // Act - Compare members
        var oldMethods = oldClass.Members.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().ToList();
        var newMethods = newClass.Members.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>().ToList();

        // Assert
        newMethods.Count.Should().BeGreaterThan(oldMethods.Count);
        newMethods.Should().Contain(m => m.Identifier.Text == "Multiply");
    }

    #endregion
}
