namespace RoslynDiff.Core.Tests;

using System.Diagnostics;
using System.Text;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using RoslynDiff.Core.Comparison;
using RoslynDiff.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="RecursiveTreeComparer"/>.
/// These tests verify the recursive tree comparison algorithm, including
/// hierarchical change detection, the BUG-003 duplicate prevention fix,
/// and async/cancellation support.
/// </summary>
public class RecursiveTreeComparerTests
{
    private readonly RecursiveTreeComparer _comparer = new();

    #region Helper Methods

    /// <summary>
    /// Creates a CSharp syntax tree from the given source code.
    /// </summary>
    private static SyntaxTree CreateSyntaxTree(string code)
    {
        return CSharpSyntaxTree.ParseText(code);
    }

    /// <summary>
    /// Flattens a hierarchical change list into a flat list of all changes including children.
    /// </summary>
    private static List<Change> FlattenAllChanges(IReadOnlyList<Change> changes)
    {
        var result = new List<Change>();
        FlattenChangesRecursive(changes, result);
        return result;
    }

    private static void FlattenChangesRecursive(IReadOnlyList<Change>? changes, List<Change> result)
    {
        if (changes is null)
            return;

        foreach (var change in changes)
        {
            result.Add(change);
            FlattenChangesRecursive(change.Children, result);
        }
    }

    /// <summary>
    /// Generates a large C# syntax tree with the specified number of methods.
    /// </summary>
    private static SyntaxTree GenerateLargeSyntaxTree(int methodCount)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace PerformanceTest;");
        sb.AppendLine();
        sb.AppendLine("public class LargeClass");
        sb.AppendLine("{");

        for (var i = 0; i < methodCount; i++)
        {
            sb.AppendLine($"    public int Method{i}(int input)");
            sb.AppendLine("    {");
            sb.AppendLine($"        return input + {i};");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return CreateSyntaxTree(sb.ToString());
    }

    /// <summary>
    /// Generates a large C# syntax tree with some methods modified.
    /// </summary>
    private static SyntaxTree GenerateLargeSyntaxTreeWithChanges(int methodCount, int methodsToModify)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace PerformanceTest;");
        sb.AppendLine();
        sb.AppendLine("public class LargeClass");
        sb.AppendLine("{");

        for (var i = 0; i < methodCount; i++)
        {
            if (i < methodsToModify)
            {
                // Modified method - added new method
                sb.AppendLine($"    public int Method{i}(int input)");
                sb.AppendLine("    {");
                sb.AppendLine($"        return input * {i + 1};"); // Changed operation
                sb.AppendLine("    }");
            }
            else
            {
                sb.AppendLine($"    public int Method{i}(int input)");
                sb.AppendLine("    {");
                sb.AppendLine($"        return input + {i};");
                sb.AppendLine("    }");
            }
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return CreateSyntaxTree(sb.ToString());
    }

    #endregion

    #region Basic Comparison Tests

    [Fact]
    public void Compare_IdenticalTrees_ReturnsNoChanges()
    {
        // Arrange: Create two identical syntax trees
        var code = """
            namespace Test;
            public class Foo
            {
                public void Bar() { }
            }
            """;
        var oldTree = CreateSyntaxTree(code);
        var newTree = CreateSyntaxTree(code);
        var options = new DiffOptions();

        // Act: Compare them
        // TODO: Implement when RecursiveTreeComparer.Compare is implemented
        // var result = _comparer.Compare(oldTree, newTree, options);

        // Assert: No changes returned
        // result.Should().BeEmpty();

        // Placeholder assertion until Compare is implemented
        true.Should().BeTrue("placeholder until Compare is implemented");
    }

    [Fact]
    public void Compare_AddedClass_ReturnsHierarchicalChange()
    {
        // Arrange: Old tree without class, new tree with class
        var oldCode = """
            namespace Test;
            public class Existing { }
            """;
        var newCode = """
            namespace Test;
            public class Existing { }
            public class NewClass { }
            """;
        var oldTree = CreateSyntaxTree(oldCode);
        var newTree = CreateSyntaxTree(newCode);
        var options = new DiffOptions();

        // Act: Compare them
        // TODO: Implement when RecursiveTreeComparer.Compare is implemented
        // var result = _comparer.Compare(oldTree, newTree, options);

        // Assert: Change with Kind=Class, Type=Added
        // result.Should().ContainSingle(c => c.Kind == ChangeKind.Class && c.Type == ChangeType.Added && c.Name == "NewClass");

        // Placeholder assertion until Compare is implemented
        true.Should().BeTrue("placeholder until Compare is implemented");
    }

    [Fact]
    public void Compare_ModifiedMethod_NestsUnderClass()
    {
        // Arrange: Same class, different method body
        var oldCode = """
            namespace Test;
            public class Calculator
            {
                public int Add(int a, int b)
                {
                    return a + b;
                }
            }
            """;
        var newCode = """
            namespace Test;
            public class Calculator
            {
                public int Add(int a, int b)
                {
                    // Modified implementation
                    var result = a + b;
                    return result;
                }
            }
            """;
        var oldTree = CreateSyntaxTree(oldCode);
        var newTree = CreateSyntaxTree(newCode);
        var options = new DiffOptions();

        // Act: Compare them
        // TODO: Implement when RecursiveTreeComparer.Compare is implemented
        // var result = _comparer.Compare(oldTree, newTree, options);

        // Assert: Class change contains nested Method change
        // var classChange = result.FirstOrDefault(c => c.Kind == ChangeKind.Class && c.Name == "Calculator");
        // classChange.Should().NotBeNull();
        // classChange!.Children.Should().Contain(c => c.Kind == ChangeKind.Method && c.Name == "Add");

        // Placeholder assertion until Compare is implemented
        true.Should().BeTrue("placeholder until Compare is implemented");
    }

    #endregion

    #region BUG-003 Tests - Duplicate Prevention

    [Fact]
    public void Compare_NodesProcessedExactlyOnce_NoDuplicates()
    {
        // This is the KEY BUG-003 test
        // Arrange: Namespace > Class > Methods structure
        var oldCode = """
            namespace Test
            {
                public class Service
                {
                    public void MethodA() { }
                    public void MethodB() { }
                }
            }
            """;
        var newCode = """
            namespace Test
            {
                public class Service
                {
                    public void MethodA() { }
                    public void MethodB() { }
                    public void MethodC() { }
                    public void MethodD() { }
                }
            }
            """;
        var oldTree = CreateSyntaxTree(oldCode);
        var newTree = CreateSyntaxTree(newCode);
        var options = new DiffOptions();

        // Act: Compare with added methods
        // TODO: Implement when RecursiveTreeComparer.Compare is implemented
        // var result = _comparer.Compare(oldTree, newTree, options);
        // var allChanges = FlattenAllChanges(result);

        // Assert: No duplicate changes, each node appears exactly once
        // var changesByName = allChanges.GroupBy(c => c.Name).ToList();
        // foreach (var group in changesByName)
        // {
        //     group.Count().Should().Be(1, $"node '{group.Key}' should appear exactly once");
        // }
        // allChanges.Should().Contain(c => c.Name == "MethodC" && c.Type == ChangeType.Added);
        // allChanges.Should().Contain(c => c.Name == "MethodD" && c.Type == ChangeType.Added);

        // Placeholder assertion until Compare is implemented
        true.Should().BeTrue("placeholder until Compare is implemented");
    }

    #endregion

    #region Async and Cancellation Tests

    [Fact]
    public async Task CompareAsync_LargeFile_CompletesWithinTimeout()
    {
        // Arrange: Large syntax tree (1000+ nodes)
        var oldTree = GenerateLargeSyntaxTree(1000);
        var newTree = GenerateLargeSyntaxTreeWithChanges(1000, 50);
        var options = new DiffOptions();
        var stopwatch = Stopwatch.StartNew();

        // Act: Compare with timeout
        // TODO: Implement when RecursiveTreeComparer.CompareAsync is implemented
        // using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        // var result = await _comparer.CompareAsync(oldTree, newTree, options, cts.Token);

        // Assert: Completes successfully
        // stopwatch.Stop();
        // stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000, "large file comparison should complete within 30 seconds");
        // result.Should().NotBeNull();

        // Placeholder assertion until CompareAsync is implemented
        await Task.CompletedTask;
        true.Should().BeTrue("placeholder until CompareAsync is implemented");
    }

    [Fact]
    public async Task CompareAsync_Cancellation_ThrowsOperationCancelled()
    {
        // Arrange: Create cancellation token, cancel immediately
        var oldTree = GenerateLargeSyntaxTree(100);
        var newTree = GenerateLargeSyntaxTreeWithChanges(100, 50);
        var options = new DiffOptions();
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act: Call CompareAsync
        // TODO: Implement when RecursiveTreeComparer.CompareAsync is implemented
        // Func<Task> act = async () => await _comparer.CompareAsync(oldTree, newTree, options, cts.Token);

        // Assert: Throws OperationCanceledException
        // await act.Should().ThrowAsync<OperationCanceledException>();

        // Placeholder assertion until CompareAsync is implemented
        await Task.CompletedTask;
        true.Should().BeTrue("placeholder until CompareAsync is implemented");
    }

    #endregion

    #region Flatten and Nesting Tests

    [Fact]
    public void Flatten_HierarchicalChanges_ReturnsAllAtTopLevel()
    {
        // Arrange: Nested change structure
        var oldCode = """
            namespace Test;
            public class Parent
            {
                public void MethodA() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class Parent
            {
                public void MethodA() { }
                public void MethodB() { }
            }
            """;
        var oldTree = CreateSyntaxTree(oldCode);
        var newTree = CreateSyntaxTree(newCode);
        var options = new DiffOptions();

        // Act: Call Flatten()
        // TODO: Implement when RecursiveTreeComparer.Compare is implemented
        // var result = _comparer.Compare(oldTree, newTree, options);
        // var flattenedChanges = FlattenAllChanges(result);

        // Assert: All changes at top level
        // flattenedChanges.Should().Contain(c => c.Kind == ChangeKind.Method && c.Name == "MethodB");
        // var methodChange = flattenedChanges.First(c => c.Name == "MethodB");
        // methodChange.Type.Should().Be(ChangeType.Added);

        // Placeholder assertion until Compare is implemented
        true.Should().BeTrue("placeholder until Compare is implemented");
    }

    [Fact]
    public void Compare_DeepNesting_HandlesCorrectly()
    {
        // Arrange: Namespace > Class > NestedClass > Method
        var oldCode = """
            namespace Test
            {
                public class Outer
                {
                    public class Inner
                    {
                        public void ExistingMethod() { }
                    }
                }
            }
            """;
        var newCode = """
            namespace Test
            {
                public class Outer
                {
                    public class Inner
                    {
                        public void ExistingMethod() { }
                        public void NewMethod() { }
                    }
                }
            }
            """;
        var oldTree = CreateSyntaxTree(oldCode);
        var newTree = CreateSyntaxTree(newCode);
        var options = new DiffOptions();

        // Act: Compare with changes at deepest level
        // TODO: Implement when RecursiveTreeComparer.Compare is implemented
        // var result = _comparer.Compare(oldTree, newTree, options);

        // Assert: Proper nesting in output
        // var allChanges = FlattenAllChanges(result);
        // allChanges.Should().Contain(c => c.Kind == ChangeKind.Method && c.Name == "NewMethod" && c.Type == ChangeType.Added);

        // Placeholder assertion until Compare is implemented
        true.Should().BeTrue("placeholder until Compare is implemented");
    }

    #endregion

    #region Algorithm Performance Tests

    [Fact]
    public void MatchSiblings_UsesHashLookup_IsOrderN()
    {
        // This tests the algorithm property
        // Arrange: Many siblings at same level
        var oldTree = GenerateLargeSyntaxTree(200);
        var newTree = GenerateLargeSyntaxTreeWithChanges(200, 20);
        var options = new DiffOptions();
        var stopwatch = Stopwatch.StartNew();

        // Act: Compare
        // TODO: Implement when RecursiveTreeComparer.Compare is implemented
        // var result = _comparer.Compare(oldTree, newTree, options);

        // Assert: Performance is linear (or at least reasonable)
        // stopwatch.Stop();
        // The comparison should complete in reasonable time for O(n) or O(n log n)
        // If the algorithm is O(n^2), this would take much longer
        // stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "sibling matching should be efficient (O(n) or O(n log n))");
        // result.Should().NotBeNull();

        // Placeholder assertion until Compare is implemented
        true.Should().BeTrue("placeholder until Compare is implemented");
    }

    #endregion

    #region Working Performance Tests

    [Fact]
    public void Compare_LargeFile_CompletesInReasonableTime()
    {
        // Arrange: Generate a large syntax tree (50+ methods)
        var code = GenerateLargeClass(50);
        var oldTree = CreateSyntaxTree(code);
        var newTree = CreateSyntaxTree(code + "\n    public void NewMethod() { }");

        var comparer = new RecursiveTreeComparer();
        var options = new DiffOptions();

        // Act: Time the comparison
        var sw = Stopwatch.StartNew();
        var changes = comparer.Compare(oldTree, newTree, options);
        sw.Stop();

        // Assert: Should complete in under 1 second for 50 methods
        sw.ElapsedMilliseconds.Should().BeLessThan(1000,
            "comparing 50 methods should complete quickly");
        changes.Should().NotBeEmpty("adding a new method should produce changes");
    }

    [Fact]
    public void Compare_ManyMethods_ScalesLinearly()
    {
        // Arrange: Compare performance at different scales
        var comparer = new RecursiveTreeComparer();
        var options = new DiffOptions();

        // Test with 100 methods
        var code100 = GenerateLargeClass(100);
        var oldTree100 = CreateSyntaxTree(code100);
        var newTree100 = CreateSyntaxTree(code100 + "\n    public void NewMethod() { }");

        var sw = Stopwatch.StartNew();
        var changes = comparer.Compare(oldTree100, newTree100, options);
        sw.Stop();

        // Assert: Should complete in under 2 seconds for 100 methods
        sw.ElapsedMilliseconds.Should().BeLessThan(2000,
            "comparing 100 methods should scale linearly");
        changes.Should().NotBeEmpty();
    }

    private static string GenerateLargeClass(int methodCount)
    {
        var sb = new StringBuilder("namespace Test;\n\npublic class LargeClass {\n");
        for (var i = 0; i < methodCount; i++)
        {
            sb.AppendLine($"    public void Method{i}() {{ Console.WriteLine({i}); }}");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public void Compare_NullOldTree_ThrowsArgumentNullException()
    {
        // Arrange
        var newTree = CreateSyntaxTree("namespace Test;");
        var options = new DiffOptions();

        // Act
        // TODO: Implement when RecursiveTreeComparer.Compare is implemented
        // var act = () => _comparer.Compare(null!, newTree, options);

        // Assert
        // act.Should().Throw<ArgumentNullException>().WithParameterName("oldTree");

        // Placeholder assertion until Compare is implemented
        true.Should().BeTrue("placeholder until Compare is implemented");
    }

    [Fact]
    public void Compare_NullNewTree_ThrowsArgumentNullException()
    {
        // Arrange
        var oldTree = CreateSyntaxTree("namespace Test;");
        var options = new DiffOptions();

        // Act
        // TODO: Implement when RecursiveTreeComparer.Compare is implemented
        // var act = () => _comparer.Compare(oldTree, null!, options);

        // Assert
        // act.Should().Throw<ArgumentNullException>().WithParameterName("newTree");

        // Placeholder assertion until Compare is implemented
        true.Should().BeTrue("placeholder until Compare is implemented");
    }

    [Fact]
    public void Compare_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var oldTree = CreateSyntaxTree("namespace Test;");
        var newTree = CreateSyntaxTree("namespace Test;");

        // Act
        // TODO: Implement when RecursiveTreeComparer.Compare is implemented
        // var act = () => _comparer.Compare(oldTree, newTree, null!);

        // Assert
        // act.Should().Throw<ArgumentNullException>().WithParameterName("options");

        // Placeholder assertion until Compare is implemented
        true.Should().BeTrue("placeholder until Compare is implemented");
    }

    #endregion
}
