namespace RoslynDiff.Core.Tests;

using Microsoft.CodeAnalysis.CSharp;
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Matching;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Tests to investigate the Calculator diff behavior and understand
/// how the semantic diff handles doc comment changes vs body changes.
/// </summary>
public class CalculatorDiffTests
{
    private const string OldCalculator = @"
namespace Samples;

/// <summary>
/// A simple calculator class for basic arithmetic operations.
/// </summary>
public class Calculator
{
    /// <summary>
    /// Adds two numbers together.
    /// </summary>
    public int Add(int a, int b)
    {
        return a + b;
    }

    /// <summary>
    /// Subtracts the second number from the first.
    /// </summary>
    public int Subtract(int a, int b)
    {
        return a - b;
    }
}";

    private const string NewCalculator = @"
namespace Samples;

/// <summary>
/// A simple calculator class for basic arithmetic operations.
/// </summary>
public class Calculator
{
    /// <summary>
    /// Adds two numbers together.
    /// </summary>
    /// <param name=""a"">The first number.</param>
    /// <param name=""b"">The second number.</param>
    /// <returns>The sum of a and b.</returns>
    public int Add(int a, int b)
    {
        return a + b;
    }

    /// <summary>
    /// Subtracts the second number from the first.
    /// </summary>
    /// <param name=""a"">The first number.</param>
    /// <param name=""b"">The second number.</param>
    /// <returns>The difference of a and b.</returns>
    public int Subtract(int a, int b)
    {
        return a - b;
    }

    /// <summary>
    /// Multiplies two numbers.
    /// </summary>
    /// <param name=""a"">The first number.</param>
    /// <param name=""b"">The second number.</param>
    /// <returns>The product of a and b.</returns>
    public int Multiply(int a, int b)
    {
        return a * b;
    }

    /// <summary>
    /// Divides the first number by the second.
    /// </summary>
    /// <param name=""a"">The dividend.</param>
    /// <param name=""b"">The divisor.</param>
    /// <returns>The quotient of a divided by b.</returns>
    /// <exception cref=""DivideByZeroException"">Thrown when b is zero.</exception>
    public int Divide(int a, int b)
    {
        if (b == 0)
        {
            throw new DivideByZeroException(""Cannot divide by zero."");
        }
        return a / b;
    }
}";

    /// <summary>
    /// Test: Verify Add method is detected as modified (only doc comments changed).
    /// Question: Should this be "Modified" when only trivia (comments) changed?
    /// </summary>
    [Fact]
    public void AddMethod_OnlyDocCommentsChanged_ShouldBeModified()
    {
        // Arrange
        var oldTree = CSharpSyntaxTree.ParseText(OldCalculator);
        var newTree = CSharpSyntaxTree.ParseText(NewCalculator);
        var oldRoot = oldTree.GetRoot();
        var newRoot = newTree.GetRoot();

        var nodeMatcher = new NodeMatcher();
        var oldClass = oldRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First();
        var newClass = newRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First();

        // Act
        var oldNodes = nodeMatcher.ExtractStructuralNodes(oldClass)
            .Where(n => n.Node != oldClass)
            .ToList();
        var newNodes = nodeMatcher.ExtractStructuralNodes(newClass)
            .Where(n => n.Node != newClass)
            .ToList();

        var matchResult = nodeMatcher.MatchNodes(oldNodes, newNodes);

        // Find the Add method match
        var addMatch = matchResult.MatchedPairs
            .FirstOrDefault(p => NodeMatcher.GetNodeName(p.Old) == "Add");

        // Assert
        Assert.NotNull(addMatch.Old);

        var oldText = addMatch.Old.ToFullString();
        var newText = addMatch.New.ToFullString();

        // Output for debugging
        System.Console.WriteLine("=== Old Add Method ===");
        System.Console.WriteLine(oldText);
        System.Console.WriteLine("=== New Add Method ===");
        System.Console.WriteLine(newText);
        System.Console.WriteLine("=== Are they equal? ===");
        System.Console.WriteLine(oldText == newText);
    }

    /// <summary>
    /// Test: Verify Multiply and Divide are detected as new (added).
    /// </summary>
    [Fact]
    public void MultiplyAndDivide_AreNewMethods_ShouldBeAdded()
    {
        // Arrange
        var oldTree = CSharpSyntaxTree.ParseText(OldCalculator);
        var newTree = CSharpSyntaxTree.ParseText(NewCalculator);
        var oldRoot = oldTree.GetRoot();
        var newRoot = newTree.GetRoot();

        var nodeMatcher = new NodeMatcher();
        var oldClass = oldRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First();
        var newClass = newRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First();

        // Act
        var oldNodes = nodeMatcher.ExtractStructuralNodes(oldClass)
            .Where(n => n.Node != oldClass)
            .ToList();
        var newNodes = nodeMatcher.ExtractStructuralNodes(newClass)
            .Where(n => n.Node != newClass)
            .ToList();

        var matchResult = nodeMatcher.MatchNodes(oldNodes, newNodes);

        // Assert - Multiply and Divide should be in UnmatchedNew
        var addedNames = matchResult.UnmatchedNew
            .Select(n => NodeMatcher.GetNodeName(n))
            .ToList();

        Assert.Contains("Multiply", addedNames);
        Assert.Contains("Divide", addedNames);
    }

    /// <summary>
    /// Test: Compare using ToFullString() vs ToString() to understand
    /// if trivia (comments) are included in the comparison.
    /// </summary>
    [Fact]
    public void CompareFullStringVsToString_UnderstandTriviaBehavior()
    {
        var oldTree = CSharpSyntaxTree.ParseText(OldCalculator);
        var newTree = CSharpSyntaxTree.ParseText(NewCalculator);
        var oldRoot = oldTree.GetRoot();
        var newRoot = newTree.GetRoot();

        var oldAddMethod = oldRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.Text == "Add");
        var newAddMethod = newRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .First(m => m.Identifier.Text == "Add");

        // ToFullString includes leading/trailing trivia (comments, whitespace)
        var oldFull = oldAddMethod.ToFullString();
        var newFull = newAddMethod.ToFullString();

        // ToString does NOT include leading/trailing trivia
        var oldStr = oldAddMethod.ToString();
        var newStr = newAddMethod.ToString();

        System.Console.WriteLine("=== ToFullString comparison ===");
        System.Console.WriteLine($"Old length: {oldFull.Length}, New length: {newFull.Length}");
        System.Console.WriteLine($"Equal: {oldFull == newFull}");

        System.Console.WriteLine("\n=== ToString comparison ===");
        System.Console.WriteLine($"Old length: {oldStr.Length}, New length: {newStr.Length}");
        System.Console.WriteLine($"Equal: {oldStr == newStr}");

        System.Console.WriteLine("\n=== Old ToString ===");
        System.Console.WriteLine(oldStr);
        System.Console.WriteLine("\n=== New ToString ===");
        System.Console.WriteLine(newStr);

        // The method body should be identical when comparing without trivia
        Assert.Equal(oldStr, newStr);

        // But ToFullString should differ due to doc comment changes
        Assert.NotEqual(oldFull, newFull);
    }

    /// <summary>
    /// Test: What does NodeMatcher actually use for comparison?
    /// </summary>
    [Fact]
    public void NodeMatcher_UsesToFullString_ForComparison()
    {
        var oldTree = CSharpSyntaxTree.ParseText(OldCalculator);
        var newTree = CSharpSyntaxTree.ParseText(NewCalculator);
        var oldRoot = oldTree.GetRoot();
        var newRoot = newTree.GetRoot();

        var nodeMatcher = new NodeMatcher();
        var oldClass = oldRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First();
        var newClass = newRoot.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First();

        var oldNodes = nodeMatcher.ExtractStructuralNodes(oldClass)
            .Where(n => n.Node != oldClass)
            .ToList();
        var newNodes = nodeMatcher.ExtractStructuralNodes(newClass)
            .Where(n => n.Node != newClass)
            .ToList();

        var matchResult = nodeMatcher.MatchNodes(oldNodes, newNodes);

        // Find matched Add methods
        var addMatch = matchResult.MatchedPairs
            .First(p => NodeMatcher.GetNodeName(p.Old) == "Add");

        // Check: ClassCommand uses ToFullString for comparison
        // See line ~340 in ClassCommand.cs:
        //   var oldText = oldNode.ToFullString();
        //   var newText = newNode.ToFullString();
        //   if (oldText != newText) { ... mark as modified ... }

        var oldText = addMatch.Old.ToFullString();
        var newText = addMatch.New.ToFullString();

        System.Console.WriteLine($"ToFullString comparison - equal: {oldText == newText}");

        // This demonstrates the issue: ToFullString includes trivia,
        // so methods with only doc comment changes are marked as "Modified"
        Assert.NotEqual(oldText, newText);
    }
}
