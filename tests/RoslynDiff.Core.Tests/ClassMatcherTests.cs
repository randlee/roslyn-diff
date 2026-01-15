namespace RoslynDiff.Core.Tests;

using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynDiff.Core.Matching;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ClassMatcher"/>.
/// </summary>
public class ClassMatcherTests
{
    private readonly ClassMatcher _matcher = new();

    #region Helper Methods

    private static TypeDeclarationSyntax GetFirstClass(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .First();
    }

    private static Microsoft.CodeAnalysis.SyntaxTree ParseTree(string code)
    {
        return CSharpSyntaxTree.ParseText(code);
    }

    #endregion

    #region ExactName Strategy Tests

    [Fact]
    public void FindMatch_ExactName_MatchesClassByName()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class MyService
            {
                public void DoWork() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class MyService
            {
                public void DoWork() { }
                public void DoMoreWork() { }
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = new ClassMatchOptions { Strategy = ClassMatchStrategy.ExactName };

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.MatchedBy.Should().Be(ClassMatchStrategy.ExactName);
        result.OldClassName.Should().Be("MyService");
        result.NewClassName.Should().Be("MyService");
        result.Similarity.Should().Be(1.0);
        result.IsExactMatch.Should().BeTrue();
    }

    [Fact]
    public void FindMatch_ExactName_ReturnsNullWhenNoMatch()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class OldService { }
            """;
        var newCode = """
            namespace Test;
            public class NewService { }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = new ClassMatchOptions { Strategy = ClassMatchStrategy.ExactName };

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindMatch_ExactName_IsCaseSensitive()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class MyService { }
            """;
        var newCode = """
            namespace Test;
            public class myservice { }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = new ClassMatchOptions { Strategy = ClassMatchStrategy.ExactName };

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Interface Strategy Tests

    [Fact]
    public void FindMatch_Interface_MatchesClassByInterface()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class OldRepository : IRepository
            {
                public void Save() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class NewRepository : IRepository
            {
                public void Save() { }
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = ClassMatchOptions.ForInterface("IRepository");

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.MatchedBy.Should().Be(ClassMatchStrategy.Interface);
        result.OldClassName.Should().Be("OldRepository");
        result.NewClassName.Should().Be("NewRepository");
    }

    [Fact]
    public void FindMatch_Interface_ReturnsNullWhenOldClassDoesNotImplementInterface()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class OldService { }
            """;
        var newCode = """
            namespace Test;
            public class NewService : IRepository { }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = ClassMatchOptions.ForInterface("IRepository");

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindMatch_Interface_HandlesGenericInterfaces()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class OldHandler : IHandler<Request, Response>
            {
                public Response Handle(Request request) => new();
            }
            """;
        var newCode = """
            namespace Test;
            public class NewHandler : IHandler<Request, Response>
            {
                public Response Handle(Request request) => new();
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = ClassMatchOptions.ForInterface("IHandler");

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.MatchedBy.Should().Be(ClassMatchStrategy.Interface);
    }

    [Fact]
    public void FindMatch_Interface_PrefersExactNameWhenMultipleImplementors()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class UserRepository : IRepository
            {
                public void Save() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class UserRepository : IRepository
            {
                public void Save() { }
            }
            public class ProductRepository : IRepository
            {
                public void Save() { }
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = ClassMatchOptions.ForInterface("IRepository");

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.NewClassName.Should().Be("UserRepository");
    }

    #endregion

    #region Similarity Strategy Tests

    [Fact]
    public void FindMatch_Similarity_MatchesSimilarClasses()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class OldCalculator
            {
                public int Add(int a, int b) => a + b;
                public int Subtract(int a, int b) => a - b;
            }
            """;
        var newCode = """
            namespace Test;
            public class NewCalculator
            {
                public int Add(int a, int b) => a + b;
                public int Subtract(int a, int b) => a - b;
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = ClassMatchOptions.ForSimilarity(0.8);

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.MatchedBy.Should().Be(ClassMatchStrategy.Similarity);
        result.Similarity.Should().BeGreaterOrEqualTo(0.8);
    }

    [Fact]
    public void FindMatch_Similarity_ReturnsNullBelowThreshold()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class OldService
            {
                public void MethodA() { Console.WriteLine("A"); }
                public void MethodB() { Console.WriteLine("B"); }
            }
            """;
        var newCode = """
            namespace Test;
            public class NewService
            {
                public int Calculate(int x) => x * 2;
                public string Format(string s) => s.ToUpper();
                public void Process() { }
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = ClassMatchOptions.ForSimilarity(0.95); // High threshold

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindMatch_Similarity_FindsBestMatch()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Calculator
            {
                public int Add(int a, int b) => a + b;
            }
            """;
        var newCode = """
            namespace Test;
            public class WrongClass
            {
                public string DoSomething() => "test";
            }
            public class RenamedCalculator
            {
                public int Add(int a, int b) => a + b;
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = ClassMatchOptions.ForSimilarity(0.8);

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.NewClassName.Should().Be("RenamedCalculator");
    }

    #endregion

    #region Auto Strategy Tests

    [Fact]
    public void FindMatch_Auto_PrefersExactName()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class MyService
            {
                public void DoWork() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class MyService
            {
                public void DoWork() { }
            }
            public class SimilarService
            {
                public void DoWork() { }
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = ClassMatchOptions.ForAuto();

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.MatchedBy.Should().Be(ClassMatchStrategy.ExactName);
        result.NewClassName.Should().Be("MyService");
    }

    [Fact]
    public void FindMatch_Auto_FallsBackToInterface()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class OldHandler : IHandler
            {
                public void Handle() { }
            }
            """;
        var newCode = """
            namespace Test;
            public class NewHandler : IHandler
            {
                public void Handle() { }
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = ClassMatchOptions.ForAuto("IHandler");

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.MatchedBy.Should().Be(ClassMatchStrategy.Interface);
    }

    [Fact]
    public void FindMatch_Auto_FallsBackToSimilarity()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class OldService
            {
                public void Process(string data) => Console.WriteLine(data);
            }
            """;
        var newCode = """
            namespace Test;
            public class RenamedService
            {
                public void Process(string data) => Console.WriteLine(data);
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = ClassMatchOptions.ForAuto(similarityThreshold: 0.8);

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.MatchedBy.Should().Be(ClassMatchStrategy.Similarity);
    }

    #endregion

    #region Nested Class Tests

    [Fact]
    public void FindMatch_NestedClass_MatchesByFullPath()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class OuterClass
            {
                public class InnerClass
                {
                    public void Method() { }
                }
            }
            """;
        var newCode = """
            namespace Test;
            public class OuterClass
            {
                public class InnerClass
                {
                    public void Method() { }
                    public void NewMethod() { }
                }
            }
            """;
        var oldTree = ParseTree(oldCode);
        var oldClasses = _matcher.GetClasses(oldTree, includeNested: true);
        var innerClass = oldClasses.First(c => c.Identifier.Text == "InnerClass");
        var newTree = ParseTree(newCode);
        var options = new ClassMatchOptions { Strategy = ClassMatchStrategy.ExactName };

        // Act
        var result = _matcher.FindMatch(innerClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.OldClassPath.Should().Be("OuterClass.InnerClass");
        result.NewClassPath.Should().Be("OuterClass.InnerClass");
    }

    [Fact]
    public void GetClasses_IncludeNestedFalse_ExcludesNestedClasses()
    {
        // Arrange
        var code = """
            namespace Test;
            public class OuterClass
            {
                public class InnerClass { }
            }
            public class AnotherClass { }
            """;
        var tree = ParseTree(code);

        // Act
        var classes = _matcher.GetClasses(tree, includeNested: false);

        // Assert
        classes.Should().HaveCount(2);
        classes.Should().Contain(c => c.Identifier.Text == "OuterClass");
        classes.Should().Contain(c => c.Identifier.Text == "AnotherClass");
        classes.Should().NotContain(c => c.Identifier.Text == "InnerClass");
    }

    [Fact]
    public void GetClasses_IncludeNestedTrue_IncludesAllClasses()
    {
        // Arrange
        var code = """
            namespace Test;
            public class OuterClass
            {
                public class InnerClass
                {
                    public class DeepNestedClass { }
                }
            }
            """;
        var tree = ParseTree(code);

        // Act
        var classes = _matcher.GetClasses(tree, includeNested: true);

        // Assert
        classes.Should().HaveCount(3);
        classes.Should().Contain(c => c.Identifier.Text == "OuterClass");
        classes.Should().Contain(c => c.Identifier.Text == "InnerClass");
        classes.Should().Contain(c => c.Identifier.Text == "DeepNestedClass");
    }

    #endregion

    #region Generic Class Tests

    [Fact]
    public void FindMatch_GenericClass_MatchesByNameWithoutTypeParameters()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Repository<T> where T : class
            {
                public T Get(int id) => default!;
            }
            """;
        var newCode = """
            namespace Test;
            public class Repository<T> where T : class
            {
                public T Get(int id) => default!;
                public void Save(T entity) { }
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = new ClassMatchOptions { Strategy = ClassMatchStrategy.ExactName };

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.OldClassName.Should().Be("Repository");
        result.NewClassName.Should().Be("Repository");
    }

    [Fact]
    public void FindMatch_GenericClass_MatchesWithDifferentConstraints()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class Container<T> where T : struct
            {
                public T Value { get; set; }
            }
            """;
        var newCode = """
            namespace Test;
            public class Container<T> where T : class
            {
                public T Value { get; set; }
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = new ClassMatchOptions { Strategy = ClassMatchStrategy.ExactName };

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.NewClassName.Should().Be("Container");
    }

    [Fact]
    public void GetFullTypeName_ReturnsNameWithGenerics()
    {
        // Arrange
        var code = """
            namespace Test;
            public class MyClass<T, TResult> { }
            """;
        var typeDecl = GetFirstClass(code);

        // Act
        var fullName = ClassMatcher.GetFullTypeName(typeDecl);

        // Assert
        fullName.Should().Be("MyClass<T, TResult>");
    }

    #endregion

    #region Partial Class Tests

    [Fact]
    public void FindPartialDeclarations_ReturnsAllPartials()
    {
        // Arrange
        var code = """
            namespace Test;
            public partial class MyService
            {
                public void MethodA() { }
            }
            public partial class MyService
            {
                public void MethodB() { }
            }
            public class OtherClass { }
            """;
        var tree = ParseTree(code);

        // Act
        var partials = _matcher.FindPartialDeclarations("MyService", tree);

        // Assert
        partials.Should().HaveCount(2);
        partials.Should().AllSatisfy(p => p.Identifier.Text.Should().Be("MyService"));
    }

    [Fact]
    public void IsPartialClass_ReturnsTrueForPartial()
    {
        // Arrange
        var code = """
            namespace Test;
            public partial class MyService { }
            """;
        var typeDecl = GetFirstClass(code);

        // Act & Assert
        ClassMatcher.IsPartialClass(typeDecl).Should().BeTrue();
    }

    [Fact]
    public void IsPartialClass_ReturnsFalseForNonPartial()
    {
        // Arrange
        var code = """
            namespace Test;
            public class MyService { }
            """;
        var typeDecl = GetFirstClass(code);

        // Act & Assert
        ClassMatcher.IsPartialClass(typeDecl).Should().BeFalse();
    }

    #endregion

    #region Records and Structs Tests

    [Fact]
    public void FindMatch_Record_MatchesByName()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public record Person(string Name, int Age);
            """;
        var newCode = """
            namespace Test;
            public record Person(string Name, int Age, string Email);
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = new ClassMatchOptions { Strategy = ClassMatchStrategy.ExactName };

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.OldClassName.Should().Be("Person");
    }

    [Fact]
    public void FindMatch_RecordStruct_MatchesByName()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public record struct Point(int X, int Y);
            """;
        var newCode = """
            namespace Test;
            public record struct Point(int X, int Y, int Z);
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = new ClassMatchOptions { Strategy = ClassMatchStrategy.ExactName };

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.OldClassName.Should().Be("Point");
    }

    [Fact]
    public void FindMatch_Struct_MatchesByName()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public struct Vector
            {
                public float X;
                public float Y;
            }
            """;
        var newCode = """
            namespace Test;
            public struct Vector
            {
                public float X;
                public float Y;
                public float Z;
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = new ClassMatchOptions { Strategy = ClassMatchStrategy.ExactName };

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.OldClassName.Should().Be("Vector");
    }

    [Fact]
    public void FindMatch_Interface_MatchesInterfaceByName()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public interface IRepository
            {
                void Save();
            }
            """;
        var newCode = """
            namespace Test;
            public interface IRepository
            {
                void Save();
                void Delete();
            }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = new ClassMatchOptions { Strategy = ClassMatchStrategy.ExactName };

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.OldClassName.Should().Be("IRepository");
    }

    [Fact]
    public void GetTypeKind_ReturnsCorrectKinds()
    {
        // Arrange & Act & Assert
        var classCode = "public class MyClass { }";
        ClassMatcher.GetTypeKind(GetFirstClass(classCode)).Should().Be("class");

        var recordCode = "public record MyRecord;";
        ClassMatcher.GetTypeKind(GetFirstClass(recordCode)).Should().Be("record");

        var recordStructCode = "public record struct MyRecordStruct;";
        ClassMatcher.GetTypeKind(GetFirstClass(recordStructCode)).Should().Be("record struct");

        var structCode = "public struct MyStruct { }";
        ClassMatcher.GetTypeKind(GetFirstClass(structCode)).Should().Be("struct");

        var interfaceCode = "public interface IMyInterface { }";
        ClassMatcher.GetTypeKind(GetFirstClass(interfaceCode)).Should().Be("interface");
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public void FindMatch_NullOldClass_ThrowsArgumentNullException()
    {
        // Arrange
        var newTree = ParseTree("namespace Test; public class Test { }");
        var options = ClassMatchOptions.Default;

        // Act
        var act = () => _matcher.FindMatch(null!, newTree, options);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("oldClass");
    }

    [Fact]
    public void FindMatch_NullNewTree_ThrowsArgumentNullException()
    {
        // Arrange
        var oldClass = GetFirstClass("namespace Test; public class Test { }");
        var options = ClassMatchOptions.Default;

        // Act
        var act = () => _matcher.FindMatch(oldClass, null!, options);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("newTree");
    }

    [Fact]
    public void FindMatch_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var oldClass = GetFirstClass("namespace Test; public class Test { }");
        var newTree = ParseTree("namespace Test; public class Test { }");

        // Act
        var act = () => _matcher.FindMatch(oldClass, newTree, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void GetClasses_NullTree_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _matcher.GetClasses(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("tree");
    }

    [Fact]
    public void FindMatch_EmptyNewTree_ReturnsNull()
    {
        // Arrange
        var oldClass = GetFirstClass("namespace Test; public class MyClass { }");
        var newTree = ParseTree("namespace Test;");
        var options = ClassMatchOptions.Default;

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FindMatch_EmptyClasses_MatchWithHighSimilarity()
    {
        // Arrange
        var oldCode = """
            namespace Test;
            public class EmptyClass { }
            """;
        var newCode = """
            namespace Test;
            public class EmptyClass { }
            """;
        var oldClass = GetFirstClass(oldCode);
        var newTree = ParseTree(newCode);
        var options = ClassMatchOptions.ForSimilarity(0.8);

        // Act
        var result = _matcher.FindMatch(oldClass, newTree, options);

        // Assert
        result.Should().NotBeNull();
        result!.Similarity.Should().Be(1.0);
    }

    #endregion

    #region ClassMatchOptions Factory Methods Tests

    [Fact]
    public void ClassMatchOptions_Default_HasCorrectValues()
    {
        // Act
        var options = ClassMatchOptions.Default;

        // Assert
        options.Strategy.Should().Be(ClassMatchStrategy.ExactName);
        options.InterfaceName.Should().BeNull();
        options.SimilarityThreshold.Should().Be(0.8);
        options.IncludeNestedClasses.Should().BeTrue();
    }

    [Fact]
    public void ClassMatchOptions_ForSimilarity_SetsCorrectStrategy()
    {
        // Act
        var options = ClassMatchOptions.ForSimilarity(0.9);

        // Assert
        options.Strategy.Should().Be(ClassMatchStrategy.Similarity);
        options.SimilarityThreshold.Should().Be(0.9);
    }

    [Fact]
    public void ClassMatchOptions_ForInterface_SetsCorrectStrategy()
    {
        // Act
        var options = ClassMatchOptions.ForInterface("IMyInterface");

        // Assert
        options.Strategy.Should().Be(ClassMatchStrategy.Interface);
        options.InterfaceName.Should().Be("IMyInterface");
    }

    [Fact]
    public void ClassMatchOptions_ForAuto_SetsCorrectStrategy()
    {
        // Act
        var options = ClassMatchOptions.ForAuto("IHandler", 0.75);

        // Assert
        options.Strategy.Should().Be(ClassMatchStrategy.Auto);
        options.InterfaceName.Should().Be("IHandler");
        options.SimilarityThreshold.Should().Be(0.75);
    }

    #endregion

    #region ClassMatchResult Properties Tests

    [Fact]
    public void ClassMatchResult_IsExactMatch_TrueForExactNameStrategy()
    {
        // Arrange
        var code = "namespace Test; public class Test { }";
        var typeDecl = GetFirstClass(code);

        // Act
        var result = new ClassMatchResult
        {
            OldClass = typeDecl,
            NewClass = typeDecl,
            MatchedBy = ClassMatchStrategy.ExactName,
            Similarity = 1.0
        };

        // Assert
        result.IsExactMatch.Should().BeTrue();
    }

    [Fact]
    public void ClassMatchResult_IsExactMatch_FalseForLowSimilarity()
    {
        // Arrange
        var code = "namespace Test; public class Test { }";
        var typeDecl = GetFirstClass(code);

        // Act
        var result = new ClassMatchResult
        {
            OldClass = typeDecl,
            NewClass = typeDecl,
            MatchedBy = ClassMatchStrategy.Similarity,
            Similarity = 0.85
        };

        // Assert
        result.IsExactMatch.Should().BeFalse();
    }

    #endregion
}
